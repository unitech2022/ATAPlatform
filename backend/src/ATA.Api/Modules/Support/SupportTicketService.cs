using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Api.Modules.Safety;
using ATA.Domain.Common;
using ATA.Domain.Notifications;
using ATA.Domain.Safety;
using ATA.Domain.Support;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using ATA.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Support;

/// <summary>Publishes the SignalR events of a ticket after its unit of work was saved (doc 11 §F18.3 "SignalR").</summary>
public sealed class SupportTicketEvents(AtaDbContext db, ISupportNotifier realtime, SupportReadModel read)
{
    /// <summary><c>SupportTicketCreated(ticketSummary)</c> to the admins.</summary>
    public async Task PublishCreatedAsync(SupportTicket ticket, CancellationToken ct) =>
        await realtime.TicketCreatedForAdminsAsync((await read.AdminItemsAsync([ticket], ct))[0], ct);

    /// <summary><c>SupportTicketUpdated</c> to the requester (status, last message, unread) and to the admins (status, priority, last message by).</summary>
    public async Task PublishUpdatedAsync(SupportTicket ticket, bool toUser, CancellationToken ct)
    {
        if (toUser)
        {
            await realtime.TicketUpdatedForUserAsync(ticket.RequesterUserId, new SupportTicketUserEvent(ticket.Id, ticket.Status, ticket.LastMessageAt, ticket.UnreadByUser), ct);
        }

        await realtime.TicketUpdatedForAdminsAsync(new SupportTicketAdminEvent(ticket.Id, ticket.Status, ticket.Priority, ticket.LastMessageBy), ct);
    }

    public async Task PublishUpdatedAsync(Guid ticketId, bool toUser, CancellationToken ct)
    {
        var ticket = await db.SupportTickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == ticketId, ct);
        if (ticket is not null)
        {
            await PublishUpdatedAsync(ticket, toUser, ct);
        }
    }
}

/// <summary>How a ticket is being created: by the requester from an app / the website, or by an agent on the phone.</summary>
public sealed record TicketCreation(
    Guid ActorUserId, bool ByAgent, Guid RequesterUserId, SupportTicketType Type, Guid? TripId, string Subject, string Message, IReadOnlyList<Guid> FileIds, DisputeRequest? Dispute,
    LostItemTicketRequest? LostItem, SupportPriority? Priority, SupportChannel Channel);

/// <summary>
/// Rider / driver support (doc 11 §F18.2, §F18.3): create tickets (with a fare dispute, a linked safety case or a linked lost item report), the conversation with
/// agents, unread counters and the one-time CSAT rating.
/// </summary>
public sealed class SupportTicketService(
    AtaDbContext db,
    ICurrentUser currentUser,
    IClock clock,
    SupportTicketWriter writer,
    SupportReadModel read,
    SupportTicketEvents events,
    FareDisputeService disputes,
    SafetyService safety,
    SafetyCaseFactory cases,
    LostItemService lostItems,
    AuditService audit)
{
    public const int MaxSubjectLength = 160;
    public const int MaxBodyLength = 4000;

    public async Task<TicketDetailDto> CreateAsync(CreateTicketRequest request, Language language, CancellationToken ct)
    {
        Validate(request.Type, request.Subject, request.Message, request.TripId);
        var userId = currentUser.UserId;
        var files = await writer.ValidateFilesAsync(userId, request.FileIds, ct);
        var ticket = await CreateCoreAsync(new TicketCreation(userId, false, userId, request.Type!.Value, request.TripId, request.Subject!.Trim(), request.Message!.Trim(), files,
            request.Dispute, request.LostItem, null, SupportChannel.App), ct);
        return await read.UserDetailAsync(ticket, language, ct);
    }

    /// <summary>
    /// Creates the ticket with its first message and, by type: a fare dispute (<c>payment_issue</c> with <c>dispute</c>), a safety case (<c>safety</c>: <c>safety_report</c> from
    /// source <c>support</c>) or a lost item report (<c>lost_item</c> of a rider), linked both ways, in one unit of work. Used by riders / drivers and by agents.
    /// </summary>
    public async Task<SupportTicket> CreateCoreAsync(TicketCreation c, CancellationToken ct)
    {
        TripParties? parties = null;
        SupportRequesterRole role;
        if (c.TripId is { } tripId)
        {
            parties = await safety.PartiesAsync(tripId, ct) ?? throw new DomainException(ErrorCodes.NotFound);
            var partyRole = parties.RoleOf(c.RequesterUserId);
            if (partyRole is null)
            {
                throw c.ByAgent
                    ? new DomainException(ErrorCodes.ValidationFailed, new Dictionary<string, string> { ["tripId"] = "not_a_party" })
                    : new DomainException(ErrorCodes.Forbidden);
            }

            role = partyRole == SafetyReporterRole.Driver ? SupportRequesterRole.Driver : SupportRequesterRole.Passenger;
        }
        else
        {
            role = await RoleOfAsync(c.RequesterUserId, c.ByAgent, ct);
        }

        new Validator()
            .Rule("dispute", c.Dispute is null || c.Type == SupportTicketType.PaymentIssue, "payment_issue_only")
            .Rule("dispute", c.Dispute is null || role == SupportRequesterRole.Passenger, "passenger_only")
            .ThrowIfInvalid();

        var ticket = await writer.AddAsync(new SupportTicketWriter.Draft(c.RequesterUserId, role, c.Type, c.TripId, c.Subject, c.Message, c.Priority, c.Channel, c.RequesterUserId, c.FileIds), ct);
        if (c.Dispute is not null)
        {
            await disputes.AddAsync(ticket, parties!.Trip, c.Dispute, ct);
        }

        SafetyCase? safetyCase = null;
        LostItemReport? report = null;
        if (c.Type == SupportTicketType.Safety)
        {
            safetyCase = await AddSafetyCaseAsync(ticket, parties, role, c, ct);
        }
        else if (c.Type == SupportTicketType.LostItem && role == SupportRequesterRole.Passenger)
        {
            var phone = c.LostItem?.ContactPhone;
            var description = c.Message.Length > 1000 ? c.Message[..1000] : c.Message;
            report = await lostItems.AddLinkedReportAsync(parties!.Trip, c.RequesterUserId, c.LostItem?.ItemCategory ?? LostItemCategory.Other, description, phone, ticket.Id, ct);
            ticket.LostItemReportId = report.Id;
        }

        if (c.ByAgent)
        {
            audit.Log("support_ticket.create", "support_ticket", ticket.Id, null, new { ticket.Type, ticket.Priority, ticket.Channel, ticket.RequesterUserId, ticket.TripId });
        }

        try
        {
            await writer.SaveNewAsync(ticket, report is null ? null : async (attempt, token) =>
                report.ReportNumber = await lostItems.NextNumberAsync(attempt, token), ct);
        }
        catch (DbUpdateException ex)
        {
            // Two disputes for one trip raced: the unique trip index rejected the second one.
            if (c.Dispute is not null)
            {
                db.ChangeTracker.Clear();
                if (await db.FareDisputes.AsNoTracking().AnyAsync(d => d.TripId == c.TripId, ct))
                {
                    throw new DomainException(ErrorCodes.DisputeExists);
                }
            }

            throw new InvalidOperationException("The ticket could not be saved.", ex);
        }

        await cases.PublishAsync(ct);
        await events.PublishCreatedAsync(ticket, ct);
        return ticket;
    }

    private async Task<SafetyCase> AddSafetyCaseAsync(SupportTicket ticket, TripParties? parties, SupportRequesterRole role, TicketCreation c, CancellationToken ct)
    {
        var safetyCase = new SafetyCase
        {
            CaseNumber = string.Empty,
            Type = SafetyCaseType.SafetyReport,
            Source = SafetyCaseSource.Support,
            Priority = SafetyPriority.High,
            TripId = c.TripId,
            ReporterUserId = c.RequesterUserId,
            ReporterRole = role == SupportRequesterRole.Driver ? SafetyReporterRole.Driver : SafetyReporterRole.Passenger,
            SubjectUserId = parties?.OtherParty(c.RequesterUserId),
            ReportCategory = SafetyReportCategory.Other,
            Description = c.Message.Length > 2000 ? c.Message[..2000] : c.Message,
            SupportTicketId = ticket.Id,
        };
        await cases.AddAsync(safetyCase, $"Safety report from support ticket {ticket.TicketNumber}", notifyOps: false, null, ct);
        ticket.SafetyCaseId = safetyCase.Id;
        return safetyCase;
    }

    public async Task<PagedResult<TicketSummaryDto>> ListMineAsync(string? status, Paging paging, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var query = db.SupportTickets.AsNoTracking().Where(t => t.RequesterUserId == userId);
        query = status switch
        {
            null or "" or "all" => query,
            "open" => query.Where(t => t.Status != SupportTicketStatus.Closed),
            "closed" => query.Where(t => t.Status == SupportTicketStatus.Closed),
            _ => throw new DomainException(ErrorCodes.ValidationFailed, new Dictionary<string, string> { ["status"] = "must be open|closed" }),
        };
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(t => t.LastMessageAt).ThenByDescending(t => t.CreatedAt).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        return paging.Result(await read.SummariesAsync(rows, ct), total);
    }

    /// <summary>The requester's view; opening the ticket resets <c>unread_by_user</c>.</summary>
    public async Task<TicketDetailDto> GetMineAsync(Guid id, Language language, CancellationToken ct)
    {
        var ticket = await LoadOwnAsync(id, ct);
        if (ticket.UnreadByUser != 0)
        {
            ticket.UnreadByUser = 0;
            await db.SaveChangesAsync(ct);
            await events.PublishUpdatedAsync(ticket, toUser: true, ct);
        }

        return await read.UserDetailAsync(ticket, language, ct);
    }

    /// <summary>
    /// The requester answers: a <c>pending_user</c> / <c>resolved</c> ticket goes back to <c>in_progress</c> (assigned) or <c>open</c>, the SLA clock resumes
    /// (the time spent paused is added to <c>resolution_due_at</c>) and the admins are told over SignalR; a <c>closed</c> ticket answers <c>409 ticket_closed</c>.
    /// </summary>
    public async Task<TicketMessageDto> ReplyAsync(Guid id, ReplyRequest request, CancellationToken ct)
    {
        new Validator().Require(nameof(request.Body), request.Body, MaxBodyLength).ThrowIfInvalid();
        var ticket = await LoadOwnAsync(id, ct);
        ticket.EnsureNotClosed();
        var userId = currentUser.UserId;
        var files = await writer.ValidateFilesAsync(userId, request.FileIds, ct);
        var now = clock.UtcNow;
        var message = writer.AddMessage(ticket.Id, userId, SupportAuthorRole.User, request.Body!.Trim(), isInternal: false, files);
        ticket.LastMessageAt = message.CreatedAt;
        ticket.LastMessageBy = SupportAuthorRole.User;
        if (ticket.Status is SupportTicketStatus.PendingUser or SupportTicketStatus.Resolved)
        {
            ticket.ResumeSla(now);
            if (ticket.Status == SupportTicketStatus.Resolved)
            {
                ticket.ResolvedAt = null;
            }

            ticket.Status = ticket.AssignedToUserId is null ? SupportTicketStatus.Open : SupportTicketStatus.InProgress;
        }

        await db.SaveChangesAsync(ct);
        await events.PublishUpdatedAsync(ticket, toUser: true, ct);
        var attachments = await writer.AttachmentsAsync([message.Id], ct);
        var name = (await read.NamesAsync([userId], ct)).GetValueOrDefault(userId);
        return new TicketMessageDto(message.Id, message.AuthorRole, name, message.Body, attachments.GetValueOrDefault(message.Id) ?? [], message.CreatedAt);
    }

    /// <summary>One rating (1–5 and an optional comment) once the ticket is <c>resolved</c> or <c>closed</c>; otherwise or when rated already <c>409 conflict</c>.</summary>
    public async Task RateAsync(Guid id, CsatRequest request, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.Score), request.Score)
            .Rule(nameof(request.Score), request.Score is null or (>= 1 and <= 5), "must be between 1 and 5")
            .Rule(nameof(request.Comment), request.Comment is null || request.Comment.Length <= 500, "max_length:500")
            .ThrowIfInvalid();
        var ticket = await LoadOwnAsync(id, ct);
        if (ticket.Status is not (SupportTicketStatus.Resolved or SupportTicketStatus.Closed) || ticket.CsatScore is not null)
        {
            throw new DomainException(ErrorCodes.Conflict, new { status = ticket.Status, rated = ticket.CsatScore is not null });
        }

        ticket.CsatScore = (byte)request.Score!.Value;
        ticket.CsatComment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();
        await db.SaveChangesAsync(ct);
    }

    private async Task<SupportTicket> LoadOwnAsync(Guid id, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        return await db.SupportTickets.FirstOrDefaultAsync(t => t.Id == id && t.RequesterUserId == userId, ct) ?? throw new DomainException(ErrorCodes.NotFound);
    }

    /// <summary>The role a ticket without a trip is opened in: the caller's login role, else the user's only role (a user holding both defaults to passenger).</summary>
    private async Task<SupportRequesterRole> RoleOfAsync(Guid userId, bool byAgent, CancellationToken ct)
    {
        if (!byAgent)
        {
            var isDriver = currentUser.HasRole(RoleNames.Driver);
            var isPassenger = currentUser.HasRole(RoleNames.Passenger);
            if (isDriver != isPassenger)
            {
                return isDriver ? SupportRequesterRole.Driver : SupportRequesterRole.Passenger;
            }
        }

        var roles = await db.UserRoles.AsNoTracking().Where(r => r.UserId == userId).Select(r => r.Role).ToListAsync(ct);
        if (roles.Contains(Role.Passenger))
        {
            return SupportRequesterRole.Passenger;
        }

        if (roles.Contains(Role.Driver))
        {
            return SupportRequesterRole.Driver;
        }

        if (roles.Contains(Role.CorporateAdmin))
        {
            return SupportRequesterRole.CorporateAdmin;
        }

        throw new DomainException(ErrorCodes.ValidationFailed, new Dictionary<string, string> { ["requesterUserId"] = "no_support_role" });
    }

    /// <summary>The field rules of a new ticket: type, subject (≤ 160), message (≤ 4000) and the trip that <c>trip_issue</c> / <c>payment_issue</c> / <c>lost_item</c> require.</summary>
    public static void Validate(SupportTicketType? type, string? subject, string? message, Guid? tripId) => new Validator()
        .Require(nameof(CreateTicketRequest.Type), type)
        .Require(nameof(CreateTicketRequest.Subject), subject, MaxSubjectLength)
        .Require(nameof(CreateTicketRequest.Message), message, MaxBodyLength)
        .Rule(nameof(CreateTicketRequest.TripId), type is null || !SupportTicket.RequiresTrip(type.Value) || tripId is not null, "required")
        .ThrowIfInvalid();
}
