using System.Text.Json;
using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Api.Modules.Notifications;
using ATA.Api.Modules.Payments.Gateways;
using ATA.Domain.Common;
using ATA.Domain.Notifications;
using ATA.Domain.Payments;
using ATA.Domain.Trips;
using ATA.Domain.Wallet;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Payments;

/// <summary>
/// Refunds (doc 08 §F11.4): to the original card (<c>refund_card</c> journal: <c>refunds → gateway_clearing</c>) or to the passenger wallet
/// (<c>refund</c> movement: <c>refunds → passenger_wallet</c>). Amounts ≥ <c>Payments:RefundAutoApproveLimit</c> need a second admin (four eyes);
/// smaller refunds are approved and executed on creation.
/// </summary>
public sealed class RefundService(
    AtaDbContext db,
    IPaymentGatewayResolver gateways,
    PaymentService payments,
    LedgerService ledger,
    INotificationDispatcher notifications,
    AuditService audit,
    ICurrentUser currentUser,
    IClock clock,
    IOptions<PaymentsOptions> options)
{
    public const string EntityType = "refund";
    private readonly PaymentsOptions _options = options.Value;

    public async Task<RefundDto> CreateForPaymentAsync(Guid paymentId, CreateRefundRequest request, CancellationToken ct)
    {
        Validate(request);
        var payment = Guard.NotFound(await db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId, ct));
        return await CreateForPaymentAsync(payment, request, ct);
    }

    public async Task<RefundDto> CreateForTripAsync(Guid tripId, CreateRefundRequest request, CancellationToken ct)
    {
        Validate(request);
        var trip = Guard.NotFound(await db.Trips.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tripId, ct));
        if (trip.Status != TripStatus.Completed || trip.FinalFare is not { } fare)
        {
            throw new DomainException(ErrorCodes.Conflict, new { status = trip.Status });
        }

        var cardPayment = trip.PaymentMethod == PaymentMethodKind.Card
            ? await db.Payments.Where(p => p.TripId == tripId && p.Purpose == PaymentPurpose.Trip && (p.Status == PaymentStatus.Captured || p.Status == PaymentStatus.PartiallyRefunded || p.Status == PaymentStatus.Refunded))
                .OrderByDescending(p => p.CreatedAt).FirstOrDefaultAsync(ct)
            : null;
        if (cardPayment is not null)
        {
            return await CreateForPaymentAsync(cardPayment, request, ct);
        }

        // Cash and wallet trips are refunded to the passenger wallet.
        var open = await OpenAmountAsync(r => r.TripId == tripId, ct);
        var refundable = Math.Max(0m, fare - open);
        var passengerUserId = await db.Passengers.AsNoTracking().Where(p => p.Id == trip.PassengerId).Select(p => p.UserId).FirstAsync(ct);
        return await CreateAsync(null, tripId, passengerUserId, fare, refundable, RefundDestination.Wallet, request, ct);
    }

    private async Task<RefundDto> CreateForPaymentAsync(Payment payment, CreateRefundRequest request, CancellationToken ct)
    {
        if (payment.Purpose == PaymentPurpose.Topup || payment.Status is not (PaymentStatus.Captured or PaymentStatus.PartiallyRefunded or PaymentStatus.Refunded))
        {
            throw new DomainException(ErrorCodes.Conflict, new { status = payment.Status, purpose = payment.Purpose });
        }

        var pending = await OpenAmountAsync(r => r.PaymentId == payment.Id && r.Status != RefundStatus.Succeeded, ct);
        var refundable = Math.Max(0m, payment.Refundable - pending);
        var destination = request.Destination ?? RefundDestination.OriginalMethod;
        return await CreateAsync(payment.Id, payment.TripId, payment.UserId, payment.CapturedAmount ?? payment.Amount, refundable, destination, request, ct);
    }

    private async Task<RefundDto> CreateAsync(Guid? paymentId, Guid? tripId, Guid userId, decimal paid, decimal refundable, RefundDestination destination, CreateRefundRequest request, CancellationToken ct)
    {
        var amount = request.Amount!.Value;
        if (amount > refundable)
        {
            throw new DomainException(ErrorCodes.RefundExceedsAmount, new { refundable });
        }

        var now = clock.UtcNow;
        var refund = new Refund
        {
            RefundNumber = string.Empty,
            PaymentId = paymentId,
            TripId = tripId,
            UserId = userId,
            Amount = amount,
            Type = amount >= paid ? RefundType.Full : RefundType.Partial,
            Destination = destination,
            ReasonCode = request.ReasonCode!.Value,
            Reason = request.Reason!.Trim(),
            RequestedBy = currentUser.UserId,
        };
        if (amount < _options.RefundAutoApproveLimit)
        {
            refund.Status = RefundStatus.Approved;
            refund.ApprovedAt = now;
        }

        db.Refunds.Add(refund);
        audit.Log("refund.create", EntityType, refund.Id, null, Snapshot(refund));
        for (var attempt = 0; ; attempt++)
        {
            refund.RefundNumber = await SequenceNumbers.NextAsync(db.Refunds.Select(r => r.RefundNumber), $"R-{now:yyyyMMdd}-", 5, attempt, ct);
            try
            {
                await db.SaveChangesAsync(ct);
                break;
            }
            catch (DbUpdateException) when (attempt < 3)
            {
            }
        }

        if (refund.Status == RefundStatus.Approved)
        {
            await ProcessAsync(refund, ct);
        }

        return await ToDtoAsync(refund, ct);
    }

    public async Task<RefundDto> ApproveAsync(Guid id, CancellationToken ct)
    {
        var refund = Guard.NotFound(await db.Refunds.FirstOrDefaultAsync(r => r.Id == id, ct));
        var before = Snapshot(refund);
        refund.Approve(currentUser.UserId, clock.UtcNow);
        audit.Log("refund.approve", EntityType, refund.Id, before, Snapshot(refund));
        await db.SaveChangesAsync(ct);
        await ProcessAsync(refund, ct);
        return await ToDtoAsync(refund, ct);
    }

    public async Task<RefundDto> RejectAsync(Guid id, ReasonBody request, CancellationToken ct)
    {
        new Validator().Require(nameof(request.Reason), request.Reason, 500).ThrowIfInvalid();
        var refund = Guard.NotFound(await db.Refunds.FirstOrDefaultAsync(r => r.Id == id, ct));
        var before = Snapshot(refund);
        refund.Reject(currentUser.UserId, request.Reason!.Trim());
        audit.Log("refund.reject", EntityType, refund.Id, before, Snapshot(refund));
        await db.SaveChangesAsync(ct);
        return await ToDtoAsync(refund, ct);
    }

    public async Task<RefundDto> RetryAsync(Guid id, CancellationToken ct)
    {
        var refund = Guard.NotFound(await db.Refunds.FirstOrDefaultAsync(r => r.Id == id, ct));
        if (refund.Status != RefundStatus.Failed)
        {
            throw new DomainException(ErrorCodes.Conflict, new { status = refund.Status });
        }

        refund.Status = RefundStatus.Approved;
        refund.FailureMessage = null;
        audit.Log("refund.retry", EntityType, refund.Id, null, Snapshot(refund));
        await db.SaveChangesAsync(ct);
        await ProcessAsync(refund, ct);
        return await ToDtoAsync(refund, ct);
    }

    /// <summary><c>RefundProcessorJob</c>: executes approved refunds left behind (e.g. after a restart).</summary>
    public async Task<int> ProcessApprovedAsync(CancellationToken ct)
    {
        var approved = await db.Refunds.Where(r => r.Status == RefundStatus.Approved).OrderBy(r => r.CreatedAt).Take(50).ToListAsync(ct);
        foreach (var refund in approved)
        {
            await ProcessAsync(refund, ct);
        }

        return approved.Count;
    }

    /// <summary><c>approved → processing</c> → gateway refund (outside the transaction) → <c>succeeded</c> with its posting, or <c>failed</c>.</summary>
    public async Task ProcessAsync(Refund refund, CancellationToken ct)
    {
        if (refund.Status != RefundStatus.Approved)
        {
            return;
        }

        refund.Status = RefundStatus.Processing;
        await db.SaveChangesAsync(ct);
        var payment = refund.PaymentId is { } paymentId ? await db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId, ct) : null;
        GatewayResult? result = null;
        if (refund.Destination == RefundDestination.OriginalMethod)
        {
            if (payment?.GatewayPaymentId is not { } gatewayId)
            {
                refund.Status = RefundStatus.Failed;
                refund.FailureMessage = "The payment has no gateway reference";
                await db.SaveChangesAsync(ct);
                return;
            }

            var gateway = gateways.Get(payment.Provider);
            result = await payments.CallAsync(t => gateway.RefundAsync(gatewayId, refund.Amount, refund.Id.ToString(), t), _options.GatewayTimeoutSeconds, ct);
            if (!result.Success)
            {
                refund.Status = RefundStatus.Failed;
                refund.FailureMessage = Truncate(result.FailureMessage ?? result.FailureCode ?? "refund_failed");
                await db.SaveChangesAsync(ct);
                return;
            }
        }

        var tripNumber = refund.TripId is { } tripId ? await db.Trips.AsNoTracking().Where(t => t.Id == tripId).Select(t => t.TripNumber).FirstOrDefaultAsync(ct) : null;
        await db.InTransactionAsync(async () =>
        {
            if (refund.Destination == RefundDestination.OriginalMethod)
            {
                await ledger.JournalAsync(JournalType.RefundCard, LedgerAccounts.Refunds, LedgerAccounts.GatewayClearing, refund.Amount, EntityType, refund.Id,
                    $"refund:{refund.Id}", $"Refund {refund.RefundNumber} to card", ct, refund.ApprovedBy ?? refund.RequestedBy);
                refund.GatewayRefundId = result?.GatewayPaymentId;
            }
            else
            {
                var wallet = await ledger.GetOrCreateWalletAsync(refund.UserId, WalletKind.Passenger, ct);
                await ledger.PostAsync(wallet, TransactionType.Refund, TransactionDirection.Credit, refund.Amount, LedgerAccounts.Refunds,
                    $"Refund {refund.RefundNumber}", $"refund:{refund.Id}", EntityType, refund.Id, ct);
            }

            payment?.ApplyRefund(refund.Amount);
            refund.Status = RefundStatus.Succeeded;
            refund.ProcessedAt = clock.UtcNow;
            await notifications.DispatchAsync(new NotificationRequest(NotificationTypes.PaymentRefunded, refund.UserId,
                NotificationPlaceholders.Of(("tripNumber", tripNumber ?? string.Empty)).Money("amount", refund.Amount), EntityType, refund.Id,
                new Dictionary<string, object?> { ["refundId"] = refund.Id, ["tripId"] = refund.TripId }), ct);
            await db.SaveChangesAsync(ct);
        }, ct);
    }

    public async Task<PagedResult<RefundDto>> ListAsync(RefundStatus? status, DateOnly? from, DateOnly? to, Paging paging, CancellationToken ct)
    {
        var query = db.Refunds.AsNoTracking().AsQueryable();
        if (status is not null) query = query.Where(r => r.Status == status);
        if (from is { } f)
        {
            var fromAt = f.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(r => r.CreatedAt >= fromAt);
        }

        if (to is { } t)
        {
            var toAt = t.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(r => r.CreatedAt < toAt);
        }

        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(r => r.CreatedAt).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        return paging.Result(await ToDtosAsync(rows, ct), total);
    }

    public async Task<IReadOnlyList<RefundDto>> ToDtosAsync(IReadOnlyList<Refund> rows, CancellationToken ct)
    {
        var userIds = rows.SelectMany(r => new[] { r.UserId, r.RequestedBy, r.ApprovedBy ?? Guid.Empty, r.RejectedBy ?? Guid.Empty }).Distinct().ToList();
        var tripIds = rows.Where(r => r.TripId != null).Select(r => r.TripId!.Value).Distinct().ToList();
        var users = await db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => new { u.FullName, u.PhoneNumber }, ct);
        var numbers = await db.Trips.AsNoTracking().Where(t => tripIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.TripNumber, ct);
        string? Name(Guid? id) => id is { } value ? users.GetValueOrDefault(value)?.FullName : null;
        return rows.Select(r => new RefundDto(r.Id, r.RefundNumber, r.PaymentId, r.TripId, r.TripId is { } id ? numbers.GetValueOrDefault(id) : null, r.UserId,
            Name(r.UserId), users.GetValueOrDefault(r.UserId)?.PhoneNumber, r.Amount, r.Type, r.Destination, r.ReasonCode, r.Reason, r.Status, r.RequestedBy,
            Name(r.RequestedBy), r.ApprovedBy, Name(r.ApprovedBy), r.ApprovedAt, r.RejectedBy, Name(r.RejectedBy), r.RejectedReason, r.GatewayRefundId,
            r.FailureMessage, r.ProcessedAt, r.CreatedAt)).ToList();
    }

    private async Task<RefundDto> ToDtoAsync(Refund refund, CancellationToken ct) => (await ToDtosAsync([refund], ct))[0];

    private async Task<decimal> OpenAmountAsync(System.Linq.Expressions.Expression<Func<Refund, bool>> filter, CancellationToken ct) =>
        await db.Refunds.Where(filter).Where(r => Refund.OpenStatuses.Contains(r.Status)).SumAsync(r => (decimal?)r.Amount, ct) ?? 0m;

    private static void Validate(CreateRefundRequest request) => new Validator()
        .Require(nameof(request.Amount), request.Amount)
        .Rule(nameof(request.Amount), request.Amount is null or > 0, "must be positive")
        .Rule(nameof(request.Amount), request.Amount is null || decimal.Round(request.Amount.Value, 2) == request.Amount.Value, "at most 2 decimal places")
        .Require(nameof(request.ReasonCode), request.ReasonCode)
        .Require(nameof(request.Reason), request.Reason, 500)
        .ThrowIfInvalid();

    private static object Snapshot(Refund r) => new { r.Status, r.Amount, r.Destination, r.ReasonCode, r.ApprovedBy, r.RejectedReason };

    private static string Truncate(string value) => value.Length > 500 ? value[..500] : value;
}
