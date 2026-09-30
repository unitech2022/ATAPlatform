using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Api.Modules.Notifications;
using ATA.Api.Modules.Payments;
using ATA.Domain.Cancellation;
using ATA.Domain.Common;
using ATA.Domain.Notifications;
using ATA.Domain.Payments;
using ATA.Domain.Support;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Support;

/// <summary>
/// Fare disputes (doc 11 §F18.2): opened with a <c>payment_issue</c> ticket on a completed trip (or a trip cancelled with a fee) within
/// <c>Support:DisputeWindowDays</c>, one per trip, and resolved by an agent with <c>support.disputes</c>; a refund goes through the F11 refund service
/// (<c>reason_code = fare_dispute</c>, four-eyes above the limit).
/// </summary>
public sealed class FareDisputeService(
    AtaDbContext db,
    ICurrentUser currentUser,
    IClock clock,
    SupportTicketWriter writer,
    RefundService refunds,
    INotificationDispatcher notifications,
    ISupportNotifier realtime,
    SupportReadModel read,
    AuditService audit,
    IOptions<SupportOptions> options)
{
    public const string EntityType = "support_dispute";

    /// <summary>Validates the dispute of a new ticket and adds it to the unit of work (the ticket itself is added by the caller).</summary>
    public async Task<FareDispute> AddAsync(SupportTicket ticket, Trip trip, DisputeRequest request, CancellationToken ct)
    {
        new Validator()
            .Require("dispute.reason", request.Reason)
            .Rule("dispute.requestedRefundAmount", request.RequestedRefundAmount is null or > 0, "must be positive")
            .Rule("dispute.requestedRefundAmount", request.RequestedRefundAmount is null || decimal.Round(request.RequestedRefundAmount.Value, 2) == request.RequestedRefundAmount.Value,
                "at most 2 decimal places")
            .ThrowIfInvalid();

        var (charged, reference) = await ChargedAsync(trip, ct);
        if (await db.FareDisputes.AnyAsync(d => d.TripId == trip.Id, ct))
        {
            throw new DomainException(ErrorCodes.DisputeExists);
        }

        var windowDays = options.Value.DisputeWindowDays;
        if (clock.UtcNow > reference.AddDays(windowDays))
        {
            throw new DomainException(ErrorCodes.DisputeWindowClosed, new { windowDays });
        }

        new Validator().Rule("dispute.requestedRefundAmount", request.RequestedRefundAmount is null || request.RequestedRefundAmount <= charged, "exceeds_charged").ThrowIfInvalid();
        var dispute = new FareDispute
        {
            TicketId = ticket.Id, TripId = trip.Id, RequesterUserId = ticket.RequesterUserId, Reason = request.Reason!.Value, ChargedAmount = charged,
            RequestedRefundAmount = request.RequestedRefundAmount,
        };
        db.FareDisputes.Add(dispute);
        return dispute;
    }

    /// <summary>What the passenger was charged: the final fare of a completed trip or the cancellation fee charged on a cancelled one; otherwise <c>422 validation_failed { tripId: "not_disputable" }</c>.</summary>
    private async Task<(decimal Charged, DateTime Reference)> ChargedAsync(Trip trip, CancellationToken ct)
    {
        if (trip is { Status: TripStatus.Completed, FinalFare: > 0, CompletedAt: { } completedAt })
        {
            return (trip.FinalFare.Value, completedAt);
        }

        if (trip is { Status: TripStatus.Cancelled, CancelledAt: { } cancelledAt })
        {
            var fee = await db.CancellationEvents.AsNoTracking().Where(e => e.TripId == trip.Id && e.FeeStatus == CancellationFeeStatus.Charged && e.FeeCharged > 0)
                .Select(e => (decimal?)e.FeeCharged).FirstOrDefaultAsync(ct);
            if (fee is { } charged)
            {
                return (charged, cancelledAt);
            }
        }

        throw new DomainException(ErrorCodes.ValidationFailed, new Dictionary<string, string> { ["tripId"] = "not_disputable" });
    }

    public async Task<PagedResult<AdminDisputeDto>> ListAsync(DisputeStatus? status, Guid? tripId, Paging paging, CancellationToken ct)
    {
        var query = db.FareDisputes.AsNoTracking().AsQueryable();
        if (status is { } s) query = query.Where(d => d.Status == s);
        if (tripId is { } trip) query = query.Where(d => d.TripId == trip);
        var total = await query.CountAsync(ct);
        // The queue first (oldest open dispute first), then the resolved ones, newest first.
        var rows = await query
            .OrderBy(d => d.Status == DisputeStatus.Open || d.Status == DisputeStatus.UnderReview ? 0 : 1)
            .ThenByDescending(d => d.ResolvedAt)
            .ThenBy(d => d.CreatedAt)
            .Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        return paging.Result(await ToAdminDtosAsync(rows, ct), total);
    }

    public async Task<AdminDisputeDto?> ForTicketAsync(Guid ticketId, CancellationToken ct)
    {
        var dispute = await db.FareDisputes.AsNoTracking().FirstOrDefaultAsync(d => d.TicketId == ticketId, ct);
        return dispute is null ? null : (await ToAdminDtosAsync([dispute], ct))[0];
    }

    /// <summary>
    /// <c>POST /admin/support/disputes/{id}/resolve</c>: <c>refund_full</c> refunds the charged amount, <c>refund_partial</c> the given <c>amount</c> (≤ charged),
    /// <c>no_refund</c> nothing. The refund is created first (so a failure such as <c>refund_exceeds_amount</c> leaves the dispute untouched), then the dispute,
    /// a system line in the ticket and the <c>support.status</c> notification are written.
    /// </summary>
    public async Task<AdminDisputeDto> ResolveAsync(Guid id, ResolveDisputeRequest request, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.Resolution), request.Resolution)
            .Require(nameof(request.Note), request.Note, 1000)
            .Rule(nameof(request.Amount), request.Resolution != DisputeResolution.RefundPartial || request.Amount is > 0, "required for refund_partial")
            .Rule(nameof(request.Amount), request.Amount is null || decimal.Round(request.Amount.Value, 2) == request.Amount.Value, "at most 2 decimal places")
            .ThrowIfInvalid();
        var dispute = Guard.NotFound(await db.FareDisputes.FirstOrDefaultAsync(d => d.Id == id, ct));
        if (dispute.IsResolved)
        {
            throw new DomainException(ErrorCodes.Conflict, new { status = dispute.Status });
        }

        var resolution = request.Resolution!.Value;
        new Validator().Rule(nameof(request.Amount), resolution != DisputeResolution.RefundPartial || request.Amount <= dispute.ChargedAmount, "exceeds_charged").ThrowIfInvalid();
        var ticket = Guard.NotFound(await db.SupportTickets.FirstOrDefaultAsync(t => t.Id == dispute.TicketId, ct));
        var tripNumber = await db.Trips.AsNoTracking().Where(t => t.Id == dispute.TripId).Select(t => t.TripNumber).FirstAsync(ct);
        var userId = currentUser.UserId;

        decimal? approved = null;
        Guid? refundId = null;
        var refundPending = false;
        if (resolution != DisputeResolution.NoRefund)
        {
            approved = resolution == DisputeResolution.RefundFull ? dispute.ChargedAmount : request.Amount!.Value;
            var refund = await refunds.CreateForDisputeAsync(dispute.TripId, approved.Value, dispute.Id, $"Fare dispute {ticket.TicketNumber}: {request.Note!.Trim()}", ct);
            refundId = refund.Id;
            refundPending = refund.Status == RefundStatus.PendingApproval;
        }

        var before = new { dispute.Status, dispute.Resolution, dispute.ApprovedRefundAmount };
        var now = clock.UtcNow;
        dispute.Status = resolution switch
        {
            DisputeResolution.RefundFull => DisputeStatus.Approved,
            DisputeResolution.RefundPartial => DisputeStatus.PartiallyApproved,
            _ => DisputeStatus.Rejected,
        };
        dispute.Resolution = resolution;
        dispute.ApprovedRefundAmount = approved;
        dispute.RefundId = refundId;
        dispute.ResolvedBy = userId;
        dispute.ResolvedAt = now;
        dispute.ResolutionNote = request.Note!.Trim();

        var language = await db.Users.AsNoTracking().Where(u => u.Id == ticket.RequesterUserId).Select(u => u.Language).FirstAsync(ct);
        writer.AddSystemMessage(ticket, DisputeMessage(dispute, tripNumber, refundPending, language));
        var (statusAr, statusEn) = SupportLabels.Dispute(dispute.Status);
        await notifications.DispatchAsync(new NotificationRequest(NotificationTypes.SupportStatus, ticket.RequesterUserId,
            NotificationPlaceholders.Of(("ticketNumber", ticket.TicketNumber)).Localized("status", statusAr, statusEn), "ticket", ticket.Id,
            new Dictionary<string, object?> { ["ticketId"] = ticket.Id, ["disputeId"] = dispute.Id }), ct);
        audit.Log("support_dispute.resolve", EntityType, dispute.Id, before, new { dispute.Status, dispute.Resolution, dispute.ApprovedRefundAmount, dispute.RefundId, note = dispute.ResolutionNote });
        await db.SaveChangesAsync(ct);

        await realtime.TicketUpdatedForUserAsync(ticket.RequesterUserId, new SupportTicketUserEvent(ticket.Id, ticket.Status, ticket.LastMessageAt, ticket.UnreadByUser), ct);
        await realtime.TicketUpdatedForAdminsAsync(new SupportTicketAdminEvent(ticket.Id, ticket.Status, ticket.Priority, ticket.LastMessageBy), ct);
        return (await ToAdminDtosAsync([dispute], ct))[0];
    }

    private static string DisputeMessage(FareDispute dispute, string tripNumber, bool refundPending, Language language)
    {
        var amount = dispute.ApprovedRefundAmount ?? 0m;
        if (language == Language.En)
        {
            return dispute.Status switch
            {
                DisputeStatus.Approved => $"Your dispute about trip {tripNumber} was approved: {Formats.MoneyEn(amount)} will be refunded{(refundPending ? " after a final review" : string.Empty)}.",
                DisputeStatus.PartiallyApproved => $"Your dispute about trip {tripNumber} was partially approved: {Formats.MoneyEn(amount)} will be refunded{(refundPending ? " after a final review" : string.Empty)}.",
                _ => $"After reviewing trip {tripNumber} your fare dispute was not approved.",
            };
        }

        return dispute.Status switch
        {
            DisputeStatus.Approved => $"تمت الموافقة على اعتراضك على الرحلة {tripNumber}: سيُسترد مبلغ {Formats.MoneyAr(amount)}{(refundPending ? " بعد المراجعة النهائية" : string.Empty)}.",
            DisputeStatus.PartiallyApproved => $"تمت الموافقة جزئياً على اعتراضك على الرحلة {tripNumber}: سيُسترد مبلغ {Formats.MoneyAr(amount)}{(refundPending ? " بعد المراجعة النهائية" : string.Empty)}.",
            _ => $"بعد مراجعة الرحلة {tripNumber} لم تتم الموافقة على اعتراضك على الأجرة.",
        };
    }

    public async Task<IReadOnlyList<AdminDisputeDto>> ToAdminDtosAsync(IReadOnlyList<FareDispute> rows, CancellationToken ct)
    {
        var ticketIds = rows.Select(d => d.TicketId).ToList();
        var tripIds = rows.Select(d => d.TripId).ToList();
        var tickets = await db.SupportTickets.AsNoTracking().Where(t => ticketIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.TicketNumber, ct);
        var trips = await db.Trips.AsNoTracking().Where(t => tripIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.TripNumber, ct);
        var names = await read.NamesAsync(rows.SelectMany(d => new[] { d.RequesterUserId, d.ResolvedBy ?? Guid.Empty }), ct);
        var refundIds = rows.Where(d => d.RefundId != null).Select(d => d.RefundId!.Value).ToList();
        var refundRows = await db.Refunds.AsNoTracking().Where(r => refundIds.Contains(r.Id)).ToListAsync(ct);
        var refundDtos = (await refunds.ToDtosAsync(refundRows, ct)).ToDictionary(r => r.Id);
        return rows.Select(d => new AdminDisputeDto(d.Id, d.TicketId, tickets.GetValueOrDefault(d.TicketId), d.TripId, trips.GetValueOrDefault(d.TripId), d.RequesterUserId,
            names.GetValueOrDefault(d.RequesterUserId), d.Reason, d.ChargedAmount, d.RequestedRefundAmount, d.Status, d.Resolution, d.ApprovedRefundAmount, d.RefundId, d.ResolvedBy,
            d.ResolvedBy is { } by ? names.GetValueOrDefault(by) : null, d.ResolvedAt, d.ResolutionNote, d.CreatedAt, d.RefundId is { } r ? refundDtos.GetValueOrDefault(r) : null)).ToList();
    }
}
