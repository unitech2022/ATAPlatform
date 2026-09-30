using System.Text.Json;
using System.Text.RegularExpressions;
using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Api.Modules.Notifications;
using ATA.Api.Modules.Payments;
using ATA.Domain.Common;
using ATA.Domain.Notifications;
using ATA.Domain.Support;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Support;

/// <summary>
/// The support desk of the admin console (doc 11 §F18.3 "الإدارة"): queue with SLA colouring, ticket detail with internal notes, agent messages, assignment,
/// status / priority / type changes, KPIs, canned responses and SLA policies. Every state change is audited; the messages themselves are not.
/// </summary>
public sealed partial class SupportAdminService(
    AtaDbContext db,
    ICurrentUser currentUser,
    IClock clock,
    SupportTicketWriter writer,
    SupportReadModel read,
    SupportTicketEvents events,
    SupportTicketService tickets,
    FareDisputeService disputes,
    INotificationDispatcher notifications,
    ReceiptService receipts,
    AuditService audit)
{
    public const string EntityType = "support_ticket";
    private static readonly SupportTicketStatus[] AgentStatuses =
        [SupportTicketStatus.PendingUser, SupportTicketStatus.InProgress, SupportTicketStatus.Resolved, SupportTicketStatus.Closed];

    [GeneratedRegex("^[a-z0-9_]{1,40}$")]
    private static partial Regex CodePattern();

    // ----- summary and KPIs -----

    public async Task<SupportSummaryDto> SummaryAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;
        var active = await db.SupportTickets.AsNoTracking().Where(t => t.Status != SupportTicketStatus.Resolved && t.Status != SupportTicketStatus.Closed).ToListAsync(ct);
        var responses = await db.SupportTickets.AsNoTracking().Where(t => t.FirstResponseAt != null).Select(t => new { t.CreatedAt, t.FirstResponseAt }).ToListAsync(ct);
        var resolved = await db.SupportTickets.AsNoTracking().Where(t => t.ResolvedAt != null).Select(t => new { t.CreatedAt, t.ResolvedAt, t.SlaPausedSeconds }).ToListAsync(ct);
        var csat = await db.SupportTickets.AsNoTracking().Where(t => t.CsatScore != null).Select(t => (int)t.CsatScore!.Value).ToListAsync(ct);
        return new SupportSummaryDto(
            active.Count,
            active.Count(t => t.AssignedToUserId == null),
            active.Count(t => t.Status == SupportTicketStatus.PendingUser),
            active.Count(t => t.IsFirstResponseBreached(now)),
            active.Count(t => t.IsResolutionBreached(now)),
            responses.Count == 0 ? null : Math.Round(responses.Average(r => (r.FirstResponseAt!.Value - r.CreatedAt).TotalMinutes), 1),
            resolved.Count == 0 ? null : Math.Round(resolved.Average(r => ResolutionMinutes(r.CreatedAt, r.ResolvedAt!.Value, r.SlaPausedSeconds)) / 60d, 2),
            csat.Count == 0 ? null : Math.Round(csat.Average(), 2));
    }

    /// <summary>
    /// <c>GET /admin/support/stats</c> (doc 11 §F18.5): tickets <em>created</em> in the range drive the counts and the first-response time, tickets <em>resolved</em> in the range the
    /// resolution time (<c>resolved_at − created_at − sla_paused_seconds</c>, mean and median) and the SLA compliance (resolved on or before <c>resolution_due_at</c>);
    /// CSAT covers the rated tickets resolved (or closed) in the range. No range = all time.
    /// </summary>
    public async Task<SupportStatsDto> StatsAsync(DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        new Validator().Rule("from", from is null || to is null || from <= to, "must be on or before 'to'").ThrowIfInvalid();
        var fromAt = from is { } f ? Formats.RiyadhMidnightUtc(f) : DateTime.MinValue;
        var toAt = to is { } t ? Formats.RiyadhMidnightUtc(t.AddDays(1)) : DateTime.MaxValue;
        bool In(DateTime? value) => value is { } v && v >= fromAt && v < toAt;

        var all = await db.SupportTickets.AsNoTracking().ToListAsync(ct);
        var created = all.Where(x => In(x.CreatedAt)).ToList();
        var resolved = all.Where(x => In(x.ResolvedAt)).ToList();
        var resolutionMinutes = resolved.Select(x => ResolutionMinutes(x.CreatedAt, x.ResolvedAt!.Value, x.SlaPausedSeconds)).ToList();
        var firstResponseMinutes = created.Where(x => x.FirstResponseAt != null).Select(x => Math.Max(0d, (x.FirstResponseAt!.Value - x.CreatedAt).TotalMinutes)).ToList();
        var rated = all.Where(x => x.CsatScore != null && In(x.ResolvedAt ?? x.ClosedAt)).Select(x => (int)x.CsatScore!.Value).ToList();
        var byType = Enum.GetValues<SupportTicketType>()
            .Select(type => new SupportTypeStatsDto(type, created.Count(x => x.Type == type), resolved.Count(x => x.Type == type)))
            .Where(x => x.Created > 0 || x.Resolved > 0).ToList();
        return new SupportStatsDto(
            created.Count, resolved.Count, all.Count(x => In(x.ClosedAt)), all.Count(x => x.IsActive),
            resolutionMinutes.Count == 0 ? null : Math.Round(resolutionMinutes.Average(), 1),
            SupportMath.Median(resolutionMinutes) is { } median ? Math.Round(median, 1) : null,
            firstResponseMinutes.Count == 0 ? null : Math.Round(firstResponseMinutes.Average(), 1),
            SupportMath.Median(firstResponseMinutes) is { } firstMedian ? Math.Round(firstMedian, 1) : null,
            resolved.Count == 0 ? null : Math.Round(resolved.Count(x => x.ResolvedAt <= x.ResolutionDueAt) / (double)resolved.Count, 4),
            rated.Count == 0 ? null : Math.Round(rated.Average(), 2), rated.Count, byType);
    }

    private static double ResolutionMinutes(DateTime createdAt, DateTime resolvedAt, int pausedSeconds) => Math.Max(0d, (resolvedAt - createdAt).TotalMinutes - pausedSeconds / 60d);

    // ----- queue and detail -----

    public async Task<PagedResult<AdminTicketListItemDto>> ListAsync(
        SupportTicketStatus? status, SupportTicketType? type, SupportPriority? priority, SupportChannel? channel, Guid? requesterUserId, string? assignedTo, string? sla, string? search,
        DateOnly? from, DateOnly? to, Paging paging, CancellationToken ct)
    {
        new Validator().Rule("sla", string.IsNullOrEmpty(sla) || sla is "breached" or "due_soon", "must be breached|due_soon").ThrowIfInvalid();
        var query = db.SupportTickets.AsNoTracking().AsQueryable();
        if (status is { } s) query = query.Where(t => t.Status == s);
        if (type is { } ty) query = query.Where(t => t.Type == ty);
        if (priority is { } p) query = query.Where(t => t.Priority == p);
        if (channel is { } ch) query = query.Where(t => t.Channel == ch);
        if (requesterUserId is { } requester) query = query.Where(t => t.RequesterUserId == requester);
        if (!string.IsNullOrWhiteSpace(assignedTo))
        {
            if (assignedTo == "me")
            {
                var me = currentUser.UserId;
                query = query.Where(t => t.AssignedToUserId == me);
            }
            else if (assignedTo == "unassigned")
            {
                query = query.Where(t => t.AssignedToUserId == null);
                if (status is null)
                {
                    query = query.Where(t => t.Status != SupportTicketStatus.Resolved && t.Status != SupportTicketStatus.Closed);
                }
            }
            else if (Guid.TryParse(assignedTo, out var assignee))
            {
                query = query.Where(t => t.AssignedToUserId == assignee);
            }
            else
            {
                throw new DomainException(ErrorCodes.ValidationFailed, new Dictionary<string, string> { ["assignedTo"] = "must be me|unassigned|{userId}" });
            }
        }

        if (from is { } f)
        {
            var fromAt = Formats.RiyadhMidnightUtc(f);
            query = query.Where(t => t.CreatedAt >= fromAt);
        }

        if (to is { } tt)
        {
            var toAt = Formats.RiyadhMidnightUtc(tt.AddDays(1));
            query = query.Where(t => t.CreatedAt < toAt);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var phone = PhoneNumber.TryNormalize(term, out var normalized) ? normalized : term;
            var tripIds = db.Trips.Where(tr => tr.TripNumber.Contains(term)).Select(tr => tr.Id);
            var userIds = db.Users.Where(u => u.PhoneNumber.Contains(phone) || (u.FullName != null && u.FullName.Contains(term))).Select(u => u.Id);
            // Ticket numbers match by prefix: "ST-…-00002" also contains the trip number "T-…-00002", which must not hit the ticket.
            query = query.Where(t => t.TicketNumber.StartsWith(term) || t.Subject.Contains(term) || (t.TripId != null && tripIds.Contains(t.TripId.Value)) || userIds.Contains(t.RequesterUserId));
        }

        // Active tickets first, urgent first, then the earliest resolution deadline (the queue order of the support desk).
        var ordered = query
            .OrderBy(t => t.Status == SupportTicketStatus.Resolved || t.Status == SupportTicketStatus.Closed ? 1 : 0)
            .ThenBy(t => t.Priority == SupportPriority.Urgent ? 0 : t.Priority == SupportPriority.High ? 1 : t.Priority == SupportPriority.Normal ? 2 : 3)
            .ThenBy(t => t.ResolutionDueAt)
            .ThenBy(t => t.CreatedAt);
        if (!string.IsNullOrEmpty(sla))
        {
            // The SLA state depends on the clock and on a possible pause, so it is evaluated in memory over the active tickets.
            var now = clock.UtcNow;
            var wanted = sla == "breached" ? SlaState.Breached : SlaState.DueSoon;
            var active = (await ordered.Where(t => t.Status != SupportTicketStatus.Resolved && t.Status != SupportTicketStatus.Closed).ToListAsync(ct)).Where(t => t.SlaStateAt(now) == wanted).ToList();
            return paging.Result(await read.AdminItemsAsync(active.Skip(paging.Skip).Take(paging.PageSize).ToList(), ct), active.Count);
        }

        var total = await query.CountAsync(ct);
        var rows = await ordered.Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        return paging.Result(await read.AdminItemsAsync(rows, ct), total);
    }

    public async Task<AdminTicketDetailDto> GetAsync(Guid id, Language language, CancellationToken ct)
    {
        var ticket = Guard.NotFound(await db.SupportTickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct));
        return await BuildDetailAsync(ticket, language, ct);
    }

    private async Task<AdminTicketDetailDto> BuildDetailAsync(SupportTicket ticket, Language language, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var requester = await db.Users.AsNoTracking().FirstAsync(u => u.Id == ticket.RequesterUserId, ct);
        var requesterDriverId = ticket.RequesterRole == SupportRequesterRole.Driver
            ? await db.Drivers.AsNoTracking().Where(d => d.UserId == ticket.RequesterUserId).Select(d => (Guid?)d.Id).FirstOrDefaultAsync(ct)
            : null;
        var assignedName = ticket.AssignedToUserId is { } assignee ? (await read.NamesAsync([assignee], ct)).GetValueOrDefault(assignee) : null;

        AdminTicketTripDto? trip = null;
        if (ticket.TripId is { } tripId && await db.Trips.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tripId, ct) is { } row)
        {
            var driverName = row.DriverId is { } driverId
                ? await (from d in db.Drivers.AsNoTracking() join u in db.Users.AsNoTracking() on d.UserId equals u.Id where d.Id == driverId select u.FullName ?? u.PhoneNumber).FirstOrDefaultAsync(ct)
                : null;
            var receipt = row.FinalFare is not null && row.Status == Domain.Trips.TripStatus.Completed ? await receipts.ForAdminAsync(row.Id, language, ct) : null;
            trip = new AdminTicketTripDto(row.Id, row.TripNumber, JsonSnake(row.Status), row.PickupName, row.DropoffName, row.FinalFare, row.EstimatedFare, JsonSnake(row.PaymentMethod),
                row.RequestedAt, row.CompletedAt, row.CancelledAt, driverName, receipt);
        }

        var safetyCase = ticket.SafetyCaseId is { } caseId
            ? await db.SafetyCases.AsNoTracking().Where(c => c.Id == caseId).Select(c => new { c.Id, c.CaseNumber, c.Status }).FirstOrDefaultAsync(ct)
            : null;
        var lostItem = ticket.LostItemReportId is { } reportId
            ? await db.LostItemReports.AsNoTracking().Where(r => r.Id == reportId).Select(r => new { r.Id, r.ReportNumber, r.Status }).FirstOrDefaultAsync(ct)
            : null;
        var links = new AdminTicketLinksDto(
            safetyCase is null ? null : new AdminLinkedCaseDto(safetyCase.Id, safetyCase.CaseNumber, JsonSnake(safetyCase.Status)),
            lostItem is null ? null : new AdminLinkedCaseDto(lostItem.Id, lostItem.ReportNumber, JsonSnake(lostItem.Status)));

        return new AdminTicketDetailDto(ticket.Id, ticket.TicketNumber, ticket.Type, ticket.Subject, ticket.Status, ticket.Priority, ticket.Channel, ticket.AssignedToUserId, assignedName,
            ticket.AssignedAt, ticket.FirstResponseDueAt, ticket.ResolutionDueAt, ticket.FirstResponseAt, ticket.SlaPausedAt, ticket.SlaPausedSeconds, ticket.SlaStateAt(now), ticket.ResolvedAt,
            ticket.ClosedAt, ticket.LastMessageAt, ticket.LastMessageBy, ticket.UnreadByUser, ticket.CsatScore, ticket.CsatComment, ticket.CreatedAt,
            new AdminTicketRequesterDto(requester.Id, requester.FullName, requester.PhoneNumber, ticket.RequesterRole, JsonSnake(requester.Language), requester.CreatedAt, requesterDriverId), trip,
            await read.AdminMessagesAsync(ticket.Id, ct), await disputes.ForTicketAsync(ticket.Id, ct), links, ticket.SafetyCaseId, ticket.LostItemReportId, ticket.UpdatedAt);
    }

    private static string JsonSnake<TEnum>(TEnum value) where TEnum : struct, Enum => JsonNamingPolicy.SnakeCaseLower.ConvertName(value.ToString());

    // ----- creation and messages -----

    public async Task<AdminTicketDetailDto> CreateAsync(AdminCreateTicketRequest request, Language language, CancellationToken ct)
    {
        SupportTicketService.Validate(request.Type, request.Subject, request.Message, request.TripId);
        new Validator()
            .Require(nameof(request.RequesterUserId), request.RequesterUserId)
            .Rule(nameof(request.Channel), request.Channel is null or SupportChannel.Phone or SupportChannel.Dashboard, "must be phone|dashboard")
            .ThrowIfInvalid();
        if (!await db.Users.AsNoTracking().AnyAsync(u => u.Id == request.RequesterUserId, ct))
        {
            throw new DomainException(ErrorCodes.ValidationFailed, new Dictionary<string, string> { ["requesterUserId"] = "unknown user" });
        }

        var ticket = await tickets.CreateCoreAsync(new TicketCreation(currentUser.UserId, true, request.RequesterUserId!.Value, request.Type!.Value, request.TripId, request.Subject!.Trim(),
            request.Message!.Trim(), [], null, null, request.Priority, request.Channel ?? SupportChannel.Phone), ct);
        return await BuildDetailAsync(ticket, language, ct);
    }

    /// <summary>
    /// An agent message. Internal notes are hidden from the requester and change nothing on the ticket. A public message is the first response when there is none
    /// (<c>first_response_at</c>), moves an <c>open</c> ticket to <c>in_progress</c>, raises the requester's unread counter and sends <c>support.reply</c>;
    /// on a <c>closed</c> ticket it answers <c>409 ticket_closed</c>. <c>cannedResponseCode</c> fills an empty body from the canned response in the requester's language.
    /// </summary>
    public async Task<AdminTicketMessageDto> AddMessageAsync(Guid id, AdminMessageRequest request, CancellationToken ct)
    {
        var isInternal = request.IsInternal ?? false;
        var ticket = Guard.NotFound(await db.SupportTickets.FirstOrDefaultAsync(t => t.Id == id, ct));
        var body = request.Body;
        if (string.IsNullOrWhiteSpace(body) && !string.IsNullOrWhiteSpace(request.CannedResponseCode))
        {
            var canned = await db.CannedResponses.AsNoTracking().FirstOrDefaultAsync(c => c.Code == request.CannedResponseCode && c.IsActive, ct)
                         ?? throw new DomainException(ErrorCodes.ValidationFailed, new Dictionary<string, string> { [nameof(request.CannedResponseCode)] = "unknown" });
            var requester = await db.Users.AsNoTracking().Where(u => u.Id == ticket.RequesterUserId).Select(u => u.Language).FirstAsync(ct);
            body = requester == Language.En ? canned.BodyEn : canned.BodyAr;
        }

        new Validator().Require(nameof(request.Body), body, SupportTicketService.MaxBodyLength).ThrowIfInvalid();
        if (!isInternal)
        {
            ticket.EnsureNotClosed();
        }

        var files = await writer.ValidateFilesAsync(currentUser.UserId, request.FileIds, ct);
        var rendered = await RenderPlaceholdersAsync(body!.Trim(), ticket, ct);
        var message = writer.AddMessage(ticket.Id, currentUser.UserId, SupportAuthorRole.Agent, rendered, isInternal, files);
        if (!isInternal)
        {
            await AfterPublicAgentMessageAsync(ticket, message, notifyReply: true, ct);
        }

        await db.SaveChangesAsync(ct);
        if (!isInternal)
        {
            await events.PublishUpdatedAsync(ticket, toUser: true, ct);
        }

        var attachments = await writer.AttachmentsAsync([message.Id], ct);
        var name = (await read.NamesAsync([currentUser.UserId], ct)).GetValueOrDefault(currentUser.UserId);
        return new AdminTicketMessageDto(message.Id, message.AuthorRole, message.AuthorUserId, name, message.Body, message.IsInternal, attachments.GetValueOrDefault(message.Id) ?? [], message.CreatedAt);
    }

    /// <summary>The bookkeeping of a public agent message (first response, <c>in_progress</c>, unread counter, dispute review, optional <c>support.reply</c>).</summary>
    private async Task AfterPublicAgentMessageAsync(SupportTicket ticket, SupportMessage message, bool notifyReply, CancellationToken ct)
    {
        var now = clock.UtcNow;
        ticket.LastMessageAt = message.CreatedAt;
        ticket.LastMessageBy = SupportAuthorRole.Agent;
        ticket.FirstResponseAt ??= now;
        ticket.UnreadByUser++;
        if (ticket.Status == SupportTicketStatus.Open)
        {
            ticket.Status = SupportTicketStatus.InProgress;
            await MarkDisputeUnderReviewAsync(ticket.Id, ct);
        }

        if (notifyReply)
        {
            await notifications.DispatchAsync(new NotificationRequest(NotificationTypes.SupportReply, ticket.RequesterUserId,
                NotificationPlaceholders.Of(("ticketNumber", ticket.TicketNumber), ("preview", SupportLabels.Preview(message.Body))), "ticket", ticket.Id,
                new Dictionary<string, object?> { ["ticketId"] = ticket.Id }), ct);
        }
    }

    private async Task MarkDisputeUnderReviewAsync(Guid ticketId, CancellationToken ct)
    {
        var dispute = await db.FareDisputes.FirstOrDefaultAsync(d => d.TicketId == ticketId && d.Status == DisputeStatus.Open, ct);
        if (dispute is not null)
        {
            dispute.Status = DisputeStatus.UnderReview;
        }
    }

    /// <summary>Replaces <c>{userName}</c>, <c>{ticketNumber}</c> and <c>{tripNumber}</c> (canned response placeholders); other braces are left alone.</summary>
    private async Task<string> RenderPlaceholdersAsync(string body, SupportTicket ticket, CancellationToken ct)
    {
        if (!body.Contains('{'))
        {
            return body;
        }

        var userName = (await read.NamesAsync([ticket.RequesterUserId], ct)).GetValueOrDefault(ticket.RequesterUserId) ?? string.Empty;
        var tripNumber = ticket.TripId is { } tripId ? await db.Trips.AsNoTracking().Where(t => t.Id == tripId).Select(t => t.TripNumber).FirstOrDefaultAsync(ct) ?? string.Empty : string.Empty;
        return body.Replace("{userName}", userName).Replace("{ticketNumber}", ticket.TicketNumber).Replace("{tripNumber}", tripNumber);
    }

    // ----- assignment, status, priority, type -----

    public async Task<AdminTicketDetailDto> AssignAsync(Guid id, AssignTicketRequest request, Language language, CancellationToken ct)
    {
        var ticket = Guard.NotFound(await db.SupportTickets.FirstOrDefaultAsync(t => t.Id == id, ct));
        ticket.EnsureNotClosed();
        Guid? assignee = null;
        if (request.UserId is { } userId)
        {
            var isAgent = await db.AdminAccounts.AsNoTracking().AnyAsync(a => a.UserId == userId && a.IsActive, ct);
            new Validator().Rule(nameof(request.UserId), isAgent, "not an active admin").ThrowIfInvalid();
            assignee = userId;
        }

        var before = Snapshot(ticket);
        ticket.AssignedToUserId = assignee;
        ticket.AssignedAt = assignee is null ? null : clock.UtcNow;
        if (assignee is not null && ticket.Status == SupportTicketStatus.Open)
        {
            ticket.Status = SupportTicketStatus.InProgress;
            await MarkDisputeUnderReviewAsync(ticket.Id, ct);
        }

        audit.Log("support_ticket.assign", EntityType, ticket.Id, before, Snapshot(ticket));
        await db.SaveChangesAsync(ct);
        await events.PublishUpdatedAsync(ticket, toUser: false, ct);
        return await BuildDetailAsync(ticket, language, ct);
    }

    /// <summary>
    /// <c>pending_user</c> pauses the SLA clock, leaving it resumes it; <c>resolved</c> / <c>closed</c> stamp <c>resolved_at</c> / <c>closed_at</c> (a resolved ticket that goes back to work
    /// clears <c>resolved_at</c>); <c>closed</c> is final (<c>409 ticket_closed</c>). The optional <c>note</c> is a public message for <c>pending_user</c> / <c>resolved</c> (the question /
    /// the solution) and an internal note otherwise. The requester gets <c>support.status</c> for <c>pending_user</c>, <c>resolved</c> and <c>closed</c>.
    /// </summary>
    public async Task<AdminTicketDetailDto> SetStatusAsync(Guid id, TicketStatusRequest request, Language language, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.Status), request.Status)
            .Rule(nameof(request.Status), request.Status is null || AgentStatuses.Contains(request.Status.Value), "must be pending_user|in_progress|resolved|closed")
            .Rule(nameof(request.Note), request.Note is null || request.Note.Length <= SupportTicketService.MaxBodyLength, $"max_length:{SupportTicketService.MaxBodyLength}")
            .ThrowIfInvalid();
        var ticket = Guard.NotFound(await db.SupportTickets.FirstOrDefaultAsync(t => t.Id == id, ct));
        ticket.EnsureNotClosed();
        var target = request.Status!.Value;
        if (ticket.Status == target)
        {
            throw new DomainException(ErrorCodes.Conflict, new { status = ticket.Status });
        }

        var before = Snapshot(ticket);
        var now = clock.UtcNow;
        if (ticket.Status == SupportTicketStatus.PendingUser)
        {
            ticket.ResumeSla(now);
        }

        if (ticket.Status == SupportTicketStatus.Resolved && target != SupportTicketStatus.Closed)
        {
            ticket.ResolvedAt = null;
        }

        switch (target)
        {
            case SupportTicketStatus.PendingUser:
                ticket.PauseSla(now);
                break;
            case SupportTicketStatus.InProgress:
                await MarkDisputeUnderReviewAsync(ticket.Id, ct);
                break;
            case SupportTicketStatus.Resolved:
                ticket.ResolvedAt = now;
                break;
            case SupportTicketStatus.Closed:
                ticket.ClosedAt = now;
                break;
        }

        ticket.Status = target;
        if (!string.IsNullOrWhiteSpace(request.Note))
        {
            var isPublic = target is SupportTicketStatus.PendingUser or SupportTicketStatus.Resolved;
            var message = writer.AddMessage(ticket.Id, currentUser.UserId, SupportAuthorRole.Agent, await RenderPlaceholdersAsync(request.Note.Trim(), ticket, ct), !isPublic);
            if (isPublic)
            {
                await AfterPublicAgentMessageAsync(ticket, message, notifyReply: false, ct);
            }
        }

        if (target is SupportTicketStatus.PendingUser or SupportTicketStatus.Resolved or SupportTicketStatus.Closed)
        {
            await NotifyStatusAsync(ticket, ct);
        }

        audit.Log("support_ticket.status", EntityType, ticket.Id, before, new { ticket.Status, ticket.ResolvedAt, ticket.ClosedAt, ticket.SlaPausedAt, ticket.SlaPausedSeconds, note = request.Note?.Trim() });
        await db.SaveChangesAsync(ct);
        await events.PublishUpdatedAsync(ticket, toUser: true, ct);
        return await BuildDetailAsync(ticket, language, ct);
    }

    /// <summary>A new priority recomputes both due dates from its SLA policy (<c>created_at + minutes (+ paused seconds)</c>).</summary>
    public async Task<AdminTicketDetailDto> SetPriorityAsync(Guid id, TicketPriorityRequest request, Language language, CancellationToken ct)
    {
        new Validator().Require(nameof(request.Priority), request.Priority).ThrowIfInvalid();
        var ticket = Guard.NotFound(await db.SupportTickets.FirstOrDefaultAsync(t => t.Id == id, ct));
        ticket.EnsureNotClosed();
        var before = Snapshot(ticket);
        ticket.Priority = request.Priority!.Value;
        ticket.ApplySla(await writer.PolicyAsync(ticket.Priority, ct));
        audit.Log("support_ticket.priority", EntityType, ticket.Id, before, new { ticket.Priority, ticket.FirstResponseDueAt, ticket.ResolutionDueAt });
        await db.SaveChangesAsync(ct);
        await events.PublishUpdatedAsync(ticket, toUser: false, ct);
        return await BuildDetailAsync(ticket, language, ct);
    }

    public async Task<AdminTicketDetailDto> SetTypeAsync(Guid id, TicketTypeRequest request, Language language, CancellationToken ct)
    {
        new Validator().Require(nameof(request.Type), request.Type).ThrowIfInvalid();
        var ticket = Guard.NotFound(await db.SupportTickets.FirstOrDefaultAsync(t => t.Id == id, ct));
        ticket.EnsureNotClosed();
        var type = request.Type!.Value;
        new Validator().Rule(nameof(request.Type), !SupportTicket.RequiresTrip(type) || ticket.TripId is not null, "trip_required").ThrowIfInvalid();
        if (type != SupportTicketType.PaymentIssue && await db.FareDisputes.AnyAsync(d => d.TicketId == ticket.Id, ct))
        {
            throw new DomainException(ErrorCodes.Conflict, new { reason = "has_dispute" });
        }

        var before = Snapshot(ticket);
        ticket.Type = type;
        audit.Log("support_ticket.type", EntityType, ticket.Id, before, Snapshot(ticket));
        await db.SaveChangesAsync(ct);
        await events.PublishUpdatedAsync(ticket, toUser: false, ct);
        return await BuildDetailAsync(ticket, language, ct);
    }

    private async Task NotifyStatusAsync(SupportTicket ticket, CancellationToken ct)
    {
        var (ar, en) = SupportLabels.Status(ticket.Status);
        await notifications.DispatchAsync(new NotificationRequest(NotificationTypes.SupportStatus, ticket.RequesterUserId,
            NotificationPlaceholders.Of(("ticketNumber", ticket.TicketNumber)).Localized("status", ar, en), "ticket", ticket.Id,
            new Dictionary<string, object?> { ["ticketId"] = ticket.Id, ["status"] = JsonSnake(ticket.Status) }), ct);
    }

    private static object Snapshot(SupportTicket t) => new { t.Status, t.Priority, t.Type, t.AssignedToUserId, t.ResolutionDueAt };

    // ----- canned responses -----

    public async Task<IReadOnlyList<CannedResponseDto>> ListCannedAsync(SupportTicketType? ticketType, bool? active, CancellationToken ct)
    {
        var query = db.CannedResponses.AsNoTracking().AsQueryable();
        // A response tied to a type is offered for that type only; untyped ones are offered everywhere.
        if (ticketType is { } type) query = query.Where(c => c.TicketType == null || c.TicketType == type);
        if (active is { } a) query = query.Where(c => c.IsActive == a);
        return (await query.OrderBy(c => c.Code).ToListAsync(ct)).Select(ToDto).ToList();
    }

    public async Task<CannedResponseDto> CreateCannedAsync(CannedResponseUpsertRequest request, CancellationToken ct)
    {
        Validate(request);
        var code = request.Code!.Trim();
        if (await db.CannedResponses.AnyAsync(c => c.Code == code, ct))
        {
            throw new DomainException(ErrorCodes.Conflict, new Dictionary<string, string> { ["code"] = "exists" });
        }

        var response = new CannedResponse { Code = code, Title = request.Title!.Trim(), BodyAr = request.BodyAr!.Trim(), BodyEn = request.BodyEn!.Trim(), CreatedBy = currentUser.UserId };
        Apply(response, request);
        db.CannedResponses.Add(response);
        audit.Log("canned_response.create", "canned_response", response.Id, null, CannedSnapshot(response));
        await db.SaveChangesAsync(ct);
        return ToDto(response);
    }

    public async Task<CannedResponseDto> UpdateCannedAsync(Guid id, CannedResponseUpsertRequest request, CancellationToken ct)
    {
        Validate(request);
        var response = Guard.NotFound(await db.CannedResponses.FirstOrDefaultAsync(c => c.Id == id, ct));
        var code = request.Code!.Trim();
        if (code != response.Code && await db.CannedResponses.AnyAsync(c => c.Code == code, ct))
        {
            throw new DomainException(ErrorCodes.Conflict, new Dictionary<string, string> { ["code"] = "exists" });
        }

        var before = CannedSnapshot(response);
        response.Code = code;
        response.Title = request.Title!.Trim();
        response.BodyAr = request.BodyAr!.Trim();
        response.BodyEn = request.BodyEn!.Trim();
        Apply(response, request);
        audit.Log("canned_response.update", "canned_response", response.Id, before, CannedSnapshot(response));
        await db.SaveChangesAsync(ct);
        return ToDto(response);
    }

    public async Task DeleteCannedAsync(Guid id, CancellationToken ct)
    {
        var response = Guard.NotFound(await db.CannedResponses.FirstOrDefaultAsync(c => c.Id == id, ct));
        audit.Log("canned_response.delete", "canned_response", response.Id, CannedSnapshot(response), null);
        db.CannedResponses.Remove(response);
        await db.SaveChangesAsync(ct);
    }

    private static void Apply(CannedResponse response, CannedResponseUpsertRequest request)
    {
        response.TicketType = request.TicketType;
        response.IsActive = request.IsActive ?? response.IsActive;
    }

    private static void Validate(CannedResponseUpsertRequest request) => new Validator()
        .Require(nameof(request.Code), request.Code, 40)
        .Rule(nameof(request.Code), string.IsNullOrWhiteSpace(request.Code) || CodePattern().IsMatch(request.Code.Trim()), "must be lowercase letters, digits or underscores")
        .Require(nameof(request.Title), request.Title, 120)
        .Require(nameof(request.BodyAr), request.BodyAr, 4000)
        .Require(nameof(request.BodyEn), request.BodyEn, 4000)
        .ThrowIfInvalid();

    private static object CannedSnapshot(CannedResponse c) => new { c.Code, c.Title, c.TicketType, c.IsActive };

    private static CannedResponseDto ToDto(CannedResponse c) => new(c.Id, c.Code, c.Title, c.BodyAr, c.BodyEn, c.TicketType, c.IsActive, c.CreatedAt, c.UpdatedAt);

    // ----- SLA policies -----

    public async Task<IReadOnlyList<SlaPolicyDto>> ListSlaPoliciesAsync(CancellationToken ct)
    {
        var rows = await db.SupportSlaPolicies.AsNoTracking().ToListAsync(ct);
        return Enum.GetValues<SupportPriority>().Select(p => rows.FirstOrDefault(r => r.Priority == p) ?? SupportTicketWriter.DefaultPolicy(p))
            .Select(p => new SlaPolicyDto(p.Priority, p.FirstResponseMinutes, p.ResolutionMinutes, p.UpdatedAt)).ToList();
    }

    /// <summary>Upserts the given policies (a priority left out keeps its policy). New values apply to tickets created (or re-prioritised) afterwards.</summary>
    public async Task<IReadOnlyList<SlaPolicyDto>> UpdateSlaPoliciesAsync(IReadOnlyList<SlaPolicyUpsertRequest> requests, CancellationToken ct)
    {
        var validator = new Validator().Rule("policies", requests.Count > 0, "required");
        for (var i = 0; i < requests.Count; i++)
        {
            var r = requests[i];
            validator
                .Rule($"policies[{i}].priority", r.Priority is not null, "required")
                .Rule($"policies[{i}].firstResponseMinutes", r.FirstResponseMinutes is >= 1 and <= 525600, "must be between 1 and 525600")
                .Rule($"policies[{i}].resolutionMinutes", r.ResolutionMinutes is >= 1 and <= 525600, "must be between 1 and 525600")
                .Rule($"policies[{i}].resolutionMinutes", r.ResolutionMinutes is null || r.FirstResponseMinutes is null || r.ResolutionMinutes >= r.FirstResponseMinutes, "must not be below firstResponseMinutes");
        }

        validator.Rule("policies", requests.Select(r => r.Priority).Distinct().Count() == requests.Count, "duplicate priority").ThrowIfInvalid();
        var before = await ListSlaPoliciesAsync(ct);
        var rows = await db.SupportSlaPolicies.ToListAsync(ct);
        foreach (var r in requests)
        {
            var row = rows.FirstOrDefault(p => p.Priority == r.Priority);
            if (row is null)
            {
                row = new SupportSlaPolicy { Priority = r.Priority!.Value };
                db.SupportSlaPolicies.Add(row);
            }

            row.FirstResponseMinutes = r.FirstResponseMinutes!.Value;
            row.ResolutionMinutes = r.ResolutionMinutes!.Value;
        }

        audit.Log("support_sla.update", "support_sla_policy", null, before, requests.Select(r => new { r.Priority, r.FirstResponseMinutes, r.ResolutionMinutes }));
        await db.SaveChangesAsync(ct);
        return await ListSlaPoliciesAsync(ct);
    }
}
