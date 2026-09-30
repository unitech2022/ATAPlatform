using ATA.Api.Common;
using ATA.Domain.Common;
using ATA.Domain.Support;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Support;

/// <summary>Read side shared by the rider / driver and the admin ticket APIs: names, conversations, list rows and the user's ticket detail.</summary>
public sealed class SupportReadModel(AtaDbContext db, SupportTicketWriter writer, IClock clock)
{
    /// <summary>Display names (full name, else the phone number) of the given users.</summary>
    public async Task<Dictionary<Guid, string?>> NamesAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        var distinct = ids.Where(id => id != Guid.Empty).Distinct().ToList();
        return await db.Users.AsNoTracking().Where(u => distinct.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => (string?)(u.FullName ?? u.PhoneNumber), ct);
    }

    /// <summary>The conversation as the requester sees it: no internal notes; agents and the system appear as the support team.</summary>
    public async Task<IReadOnlyList<TicketMessageDto>> UserMessagesAsync(SupportTicket ticket, Language language, CancellationToken ct)
    {
        var rows = await db.SupportMessages.AsNoTracking().Where(m => m.TicketId == ticket.Id && !m.IsInternal).OrderBy(m => m.CreatedAt).ThenBy(m => m.Id).ToListAsync(ct);
        var attachments = await writer.AttachmentsAsync(rows.Select(m => m.Id).ToList(), ct);
        var requesterName = (await NamesAsync([ticket.RequesterUserId], ct)).GetValueOrDefault(ticket.RequesterUserId);
        return rows.Select(m => new TicketMessageDto(m.Id, m.AuthorRole, m.AuthorRole == SupportAuthorRole.User ? requesterName : SupportLabels.Team(language), m.Body,
            attachments.GetValueOrDefault(m.Id) ?? [], m.CreatedAt)).ToList();
    }

    /// <summary>The full conversation for agents, internal notes included, with the real agent names.</summary>
    public async Task<IReadOnlyList<AdminTicketMessageDto>> AdminMessagesAsync(Guid ticketId, CancellationToken ct)
    {
        var rows = await db.SupportMessages.AsNoTracking().Where(m => m.TicketId == ticketId).OrderBy(m => m.CreatedAt).ThenBy(m => m.Id).ToListAsync(ct);
        var attachments = await writer.AttachmentsAsync(rows.Select(m => m.Id).ToList(), ct);
        var names = await NamesAsync(rows.Where(m => m.AuthorUserId != null).Select(m => m.AuthorUserId!.Value), ct);
        return rows.Select(m => new AdminTicketMessageDto(m.Id, m.AuthorRole, m.AuthorUserId, m.AuthorUserId is { } a ? names.GetValueOrDefault(a) : null, m.Body, m.IsInternal,
            attachments.GetValueOrDefault(m.Id) ?? [], m.CreatedAt)).ToList();
    }

    public async Task<IReadOnlyList<TicketSummaryDto>> SummariesAsync(IReadOnlyList<SupportTicket> tickets, CancellationToken ct)
    {
        var numbers = await TripNumbersAsync(tickets, ct);
        return tickets.Select(t => new TicketSummaryDto(t.Id, t.TicketNumber, t.Type, t.Subject, t.Status, t.TripId is { } trip ? numbers.GetValueOrDefault(trip) : null, t.LastMessageAt,
            t.UnreadByUser, t.CreatedAt)).ToList();
    }

    public async Task<TicketDetailDto> UserDetailAsync(SupportTicket ticket, Language language, CancellationToken ct)
    {
        TicketTripDto? trip = null;
        if (ticket.TripId is { } tripId)
        {
            trip = await db.Trips.AsNoTracking().Where(t => t.Id == tripId).Select(t => new TicketTripDto(t.Id, t.TripNumber, t.CompletedAt)).FirstOrDefaultAsync(ct);
        }

        var dispute = await db.FareDisputes.AsNoTracking().Where(d => d.TicketId == ticket.Id)
            .Select(d => new TicketDisputeDto(d.Reason, d.ChargedAmount, d.RequestedRefundAmount, d.Status, d.Resolution, d.ApprovedRefundAmount)).FirstOrDefaultAsync(ct);
        var closed = ticket.IsClosed;
        var canRate = ticket.Status is SupportTicketStatus.Resolved or SupportTicketStatus.Closed && ticket.CsatScore is null;
        return new TicketDetailDto(ticket.Id, ticket.TicketNumber, ticket.Type, ticket.Subject, ticket.Status, ticket.Priority, trip, await UserMessagesAsync(ticket, language, ct), dispute,
            !closed, canRate, ticket.CsatScore, ticket.CreatedAt, ticket.ResolvedAt);
    }

    public async Task<IReadOnlyList<AdminTicketListItemDto>> AdminItemsAsync(IReadOnlyList<SupportTicket> tickets, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var numbers = await TripNumbersAsync(tickets, ct);
        var names = await NamesAsync(tickets.SelectMany(t => new[] { t.RequesterUserId, t.AssignedToUserId ?? Guid.Empty }), ct);
        return tickets.Select(t => new AdminTicketListItemDto(t.Id, t.TicketNumber, t.Type, t.Subject, t.Status, t.Priority, names.GetValueOrDefault(t.RequesterUserId), t.RequesterRole,
            t.TripId is { } trip ? numbers.GetValueOrDefault(trip) : null, t.AssignedToUserId is { } a ? names.GetValueOrDefault(a) : null, t.FirstResponseDueAt, t.ResolutionDueAt,
            t.SlaStateAt(now), t.LastMessageAt, t.LastMessageBy, t.CreatedAt, t.Channel, t.FirstResponseAt, t.RequesterUserId)).ToList();
    }

    private async Task<Dictionary<Guid, string>> TripNumbersAsync(IReadOnlyList<SupportTicket> tickets, CancellationToken ct)
    {
        var tripIds = tickets.Where(t => t.TripId != null).Select(t => t.TripId!.Value).Distinct().ToList();
        return await db.Trips.AsNoTracking().Where(t => tripIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.TripNumber, ct);
    }
}
