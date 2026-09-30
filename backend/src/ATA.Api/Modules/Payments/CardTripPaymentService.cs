using ATA.Api.Modules.Payments.Gateways;
using ATA.Domain.Common;
using ATA.Domain.Payments;
using ATA.Domain.Trips;
using ATA.Domain.Wallet;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Payments;

public enum CardCaptureKind { Captured, Declined, Pending, Unavailable }

/// <summary>What happened when a card trip was charged at completion.</summary>
public sealed record CardCaptureOutcome(CardCaptureKind Kind, Payment? Payment, string? FailureCode, string? FailureMessage);

/// <summary>
/// Card trips (doc 08 §F11.4): authorization of <c>ceil(estimatedFare × (1 + AuthBufferPercent/100))</c> before the trip is inserted,
/// capture of the final fare on completion (void + purchase when the fare exceeds the authorization), release of the authorization on
/// cancellation/no drivers, and the capture retry that ends in a passenger wallet debt.
/// </summary>
public sealed class CardTripPaymentService(
    AtaDbContext db,
    PaymentService payments,
    IPaymentGatewayResolver gateways,
    LedgerService ledger,
    Trips.TripEventRecorder events,
    IClock clock,
    IOptions<PaymentsOptions> options,
    ILogger<CardTripPaymentService> logger)
{
    private readonly PaymentsOptions _options = options.Value;

    /// <summary>
    /// Resolves and checks the card, then authorizes it (unless authorization is disabled). Throws <c>422 payment_failed</c> on an
    /// immediate decline. Returns the payment (initiated with an action for 3-D Secure) or <c>null</c> when nothing was authorized.
    /// Saves the payment row before calling the gateway; the trip is inserted afterwards by the caller.
    /// </summary>
    public async Task<Payment?> AuthorizeForTripAsync(Trip trip, Guid passengerUserId, Guid? requestedMethodId, Guid? defaultMethodId, CancellationToken ct)
    {
        var card = await ResolveCardAsync(trip, passengerUserId, requestedMethodId, defaultMethodId, ct);
        var now = clock.UtcNow;
        if (!_options.AuthorizeCardTrips)
        {
            return null;
        }

        var amount = Math.Ceiling((trip.OfferedPrice ?? trip.EstimatedFare) * (1m + _options.AuthBufferPercent / 100m));
        var payment = new Payment
        {
            UserId = passengerUserId,
            Purpose = PaymentPurpose.Trip,
            TripId = trip.Id,
            PaymentMethodId = card.Id,
            Method = PaymentInstrument.Card,
            Provider = card.Provider,
            Amount = amount,
            CaptureMode = CaptureMode.Manual,
            ReturnUrl = _options.DefaultReturnUrl,
            IdempotencyKey = $"trip:{trip.Id}:authorize",
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);

        var gateway = gateways.Get(card.Provider);
        var result = await payments.CallAsync(t => gateway.AuthorizeAsync(Charge(payment, card, $"Trip authorization"), t), _options.GatewayTimeoutSeconds, ct);
        if (result.IsTransient)
        {
            payment.MarkFailed(result.FailureCode, result.FailureMessage, now);
            await db.SaveChangesAsync(ct);
            throw new DomainException(ErrorCodes.PaymentProviderUnavailable, new { failureCode = result.FailureCode });
        }

        await payments.ApplyResultAsync(payment, result, ct);
        if (payment.Status == PaymentStatus.Failed || (!result.Success && result.Status != GatewayStatus.RequiresAction))
        {
            if (payment.Status != PaymentStatus.Failed) payment.MarkFailed(result.FailureCode, result.FailureMessage, now);
            await db.SaveChangesAsync(ct);
            throw new DomainException(ErrorCodes.PaymentFailed, new { failureCode = payment.FailureCode, failureMessage = payment.FailureMessage });
        }

        return payment;
    }

    /// <summary>
    /// Resolves and checks the card of a trip (<c>404</c> unknown, <c>422 payment_method_expired</c>) and sets <c>trip.PaymentMethodId</c>. Scheduled trips (F17) only do this at
    /// booking; the authorization itself is made when the search starts or the final confirmation assigns the driver.
    /// </summary>
    public async Task<PaymentMethod> ResolveCardAsync(Trip trip, Guid passengerUserId, Guid? requestedMethodId, Guid? defaultMethodId, CancellationToken ct)
    {
        var methodId = requestedMethodId ?? defaultMethodId;
        if (methodId is null)
        {
            throw new DomainException(ErrorCodes.ValidationFailed, new Dictionary<string, string> { ["paymentMethodId"] = "required" });
        }

        var card = await db.PaymentMethods.AsNoTracking().FirstOrDefaultAsync(m => m.Id == methodId && m.UserId == passengerUserId && m.Status != SavedCardStatus.Removed, ct)
                   ?? throw new DomainException(ErrorCodes.NotFound, new { paymentMethodId = methodId });
        card.EnsureUsable(clock.UtcNow);
        trip.PaymentMethodId = card.Id;
        return card;
    }

    /// <summary>
    /// Charges the final fare before the completion transaction (gateway calls only; the caller saves the payment changes with the trip):
    /// capture ≤ authorization, otherwise void + purchase with the same card; purchase when nothing was authorized.
    /// </summary>
    public async Task<CardCaptureOutcome> CaptureForCompletionAsync(Trip trip, decimal fare, CancellationToken ct)
    {
        var card = trip.PaymentMethodId is { } id ? await db.PaymentMethods.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, ct) : null;
        var authorized = await db.Payments
            .Where(p => p.TripId == trip.Id && p.Purpose == PaymentPurpose.Trip && p.Status == PaymentStatus.Authorized)
            .OrderByDescending(p => p.CreatedAt).FirstOrDefaultAsync(ct);
        if (authorized is not null && fare <= (authorized.AuthorizedAmount ?? 0m) && authorized.GatewayPaymentId is { } gatewayId)
        {
            var gateway = gateways.Get(authorized.Provider);
            var result = await payments.CallAsync(t => gateway.CaptureAsync(gatewayId, fare, t), _options.CaptureTimeoutSeconds, ct);
            return await OutcomeAsync(authorized, result, fare, ct);
        }

        if (card is null)
        {
            return new CardCaptureOutcome(CardCaptureKind.Unavailable, authorized, "card_unavailable", null);
        }

        if (authorized?.GatewayPaymentId is { } oldGatewayId)
        {
            var voided = await payments.CallAsync(t => gateways.Get(authorized.Provider).VoidAsync(oldGatewayId, t), _options.CaptureTimeoutSeconds, ct);
            await payments.ApplyResultAsync(authorized, voided.Success ? voided with { Status = GatewayStatus.Voided } : voided, ct);
        }

        var purchase = new Payment
        {
            UserId = authorized?.UserId ?? card.UserId,
            Purpose = PaymentPurpose.Trip,
            TripId = trip.Id,
            PaymentMethodId = card.Id,
            Method = PaymentInstrument.Card,
            Provider = card.Provider,
            Amount = fare,
            CaptureMode = CaptureMode.Auto,
            ReturnUrl = _options.DefaultReturnUrl,
            IdempotencyKey = $"trip:{trip.Id}:purchase",
        };
        db.Payments.Add(purchase);
        await db.SaveChangesAsync(ct);
        var purchaseResult = await payments.CallAsync(t => gateways.Get(card.Provider).PurchaseAsync(Charge(purchase, card, "Trip fare"), t), _options.CaptureTimeoutSeconds, ct);
        return await OutcomeAsync(purchase, purchaseResult, fare, ct);
    }

    private async Task<CardCaptureOutcome> OutcomeAsync(Payment payment, GatewayResult result, decimal fare, CancellationToken ct)
    {
        if (result.IsTransient)
        {
            PaymentMetadata.Update(payment, m =>
            {
                m["capturePending"] = true;
                m["captureAttempts"] = 1;
                m["amountDue"] = fare;
            });
            payment.FailureCode = result.FailureCode;
            payment.FailureMessage = result.FailureMessage;
            return new CardCaptureOutcome(CardCaptureKind.Pending, payment, result.FailureCode, result.FailureMessage);
        }

        if (result.Success && result.Status == GatewayStatus.Captured)
        {
            await payments.ApplyResultAsync(payment, result with { CapturedAmount = result.CapturedAmount ?? fare }, ct);
            return new CardCaptureOutcome(CardCaptureKind.Captured, payment, null, null);
        }

        // Final decline: release the hold (best effort) and fall back to cash.
        if (payment.Status == PaymentStatus.Authorized && payment.GatewayPaymentId is { } gatewayId)
        {
            await payments.CallAsync(t => gateways.Get(payment.Provider).VoidAsync(gatewayId, t), _options.CaptureTimeoutSeconds, ct);
        }

        payment.MarkFailed(result.FailureCode ?? "card_declined", result.FailureMessage, clock.UtcNow);
        return new CardCaptureOutcome(CardCaptureKind.Declined, payment, payment.FailureCode, payment.FailureMessage);
    }

    /// <summary>Releases the authorization of a cancelled / <c>no_drivers</c> trip (void; a pending 3-D Secure payment fails). Saves.</summary>
    public async Task ReleaseAsync(Guid tripId, CancellationToken ct)
    {
        var open = await db.Payments
            .Where(p => p.TripId == tripId && p.Purpose == PaymentPurpose.Trip && (p.Status == PaymentStatus.Authorized || p.Status == PaymentStatus.Initiated))
            .ToListAsync(ct);
        if (open.Count == 0)
        {
            return;
        }

        foreach (var payment in open)
        {
            if (payment.Status == PaymentStatus.Initiated)
            {
                payment.MarkFailed("trip_cancelled", "The trip ended before the payment completed", clock.UtcNow);
                continue;
            }

            if (payment.GatewayPaymentId is { } gatewayId)
            {
                var result = await payments.CallAsync(t => gateways.Get(payment.Provider).VoidAsync(gatewayId, t), _options.GatewayTimeoutSeconds, ct);
                if (result.Success)
                {
                    await payments.ApplyResultAsync(payment, result with { Status = GatewayStatus.Voided }, ct);
                }
                else
                {
                    logger.LogWarning("Void failed for payment {PaymentId}: {Code}", payment.Id, result.FailureCode);
                }
            }
        }

        await db.SaveChangesAsync(ct);
        await payments.PublishPendingAsync(ct);
    }

    /// <summary>
    /// <c>PaymentCaptureRetryJob</c>: retries captures left pending by a timeout. After <c>Payments:CaptureMaxAttempts</c> (or a final decline)
    /// the fare becomes a <c>trip_payment</c> debt on the passenger wallet (overdraft) and <c>payment.failed</c> is sent.
    /// </summary>
    public async Task<int> RetryPendingCapturesAsync(CancellationToken ct)
    {
        var pending = await db.Payments
            .Where(p => p.Purpose == PaymentPurpose.Trip && (p.Status == PaymentStatus.Authorized || p.Status == PaymentStatus.Initiated)
                        && p.Metadata != null && p.Metadata.Contains("capturePending"))
            .ToListAsync(ct);
        var processed = 0;
        foreach (var payment in pending)
        {
            var meta = PaymentMetadata.Get(payment);
            if (!meta.CapturePending || payment.TripId is not { } tripId)
            {
                continue;
            }

            var due = meta.AmountDue ?? payment.Amount;
            var gateway = gateways.Get(payment.Provider);
            GatewayResult result;
            if (payment.Status == PaymentStatus.Authorized && payment.GatewayPaymentId is { } gatewayId && due <= (payment.AuthorizedAmount ?? 0m))
            {
                result = await payments.CallAsync(t => gateway.CaptureAsync(gatewayId, due, t), _options.CaptureTimeoutSeconds, ct);
            }
            else
            {
                var card = payment.PaymentMethodId is { } methodId ? await db.PaymentMethods.AsNoTracking().FirstOrDefaultAsync(m => m.Id == methodId, ct) : null;
                result = card is null
                    ? GatewayResult.Declined("card_unavailable", "The card is no longer available")
                    : await payments.CallAsync(t => gateway.PurchaseAsync(Charge(payment, card, "Trip fare") with { Amount = due }, t), _options.CaptureTimeoutSeconds, ct);
            }

            var attempts = meta.CaptureAttempts + 1;
            var trip = await db.Trips.FirstAsync(t => t.Id == tripId, ct);
            await ATA.Api.Common.DbTransactions.InTransactionAsync(db, async () =>
            {
                if (result.Success && result.Status == GatewayStatus.Captured)
                {
                    await payments.ApplyResultAsync(payment, result with { CapturedAmount = result.CapturedAmount ?? due }, ct);
                    events.Add(tripId, TripEventTypes.PaymentRecorded, TripActor.System, data: new { method = PaymentMethodKind.Card, amount = due, paymentId = payment.Id, retried = true });
                }
                else if (!result.IsTransient || attempts >= _options.CaptureMaxAttempts)
                {
                    PaymentMetadata.Update(payment, m => m.Remove("capturePending"));
                    payment.MarkFailed(result.FailureCode ?? "capture_failed", result.FailureMessage, clock.UtcNow);
                    var passengerUserId = payment.UserId;
                    var wallet = await ledger.GetOrCreateWalletAsync(passengerUserId, WalletKind.Passenger, ct);
                    await ledger.PostAsync(wallet, TransactionType.TripPayment, TransactionDirection.Debit, due, LedgerAccounts.TripRevenue,
                        $"Trip {trip.TripNumber} (card collection failed)", $"trip:{tripId}:payment", "trip", tripId, ct, allowOverdraft: true);
                    events.Add(tripId, TripEventTypes.PaymentFailed, TripActor.System, data: new { paymentId = payment.Id, payment.FailureCode, debtAmount = due });
                    await payments.NotifyFailedAsync(payment, due, ct);
                }
                else
                {
                    PaymentMetadata.Update(payment, m => m["captureAttempts"] = attempts);
                }

                await db.SaveChangesAsync(ct);
            }, ct);
            await payments.PublishPendingAsync(ct);
            processed++;
        }

        return processed;
    }

    /// <summary><c>AuthorizationReconcileJob</c>: voids authorizations left on cancelled / no-drivers trips.</summary>
    public async Task<int> ReconcileAuthorizationsAsync(CancellationToken ct)
    {
        var tripIds = await (from p in db.Payments
                             join t in db.Trips on p.TripId equals t.Id
                             where p.Purpose == PaymentPurpose.Trip && p.Status == PaymentStatus.Authorized
                                   && (t.Status == TripStatus.Cancelled || t.Status == TripStatus.NoDrivers)
                             select t.Id).Distinct().ToListAsync(ct);
        foreach (var tripId in tripIds)
        {
            await ReleaseAsync(tripId, ct);
        }

        return tripIds.Count;
    }

    private GatewayChargeRequest Charge(Payment payment, PaymentMethod card, string description) =>
        new(payment.Id, payment.Amount, payment.Currency, new GatewaySource("token", card.GatewayToken), description, payments.CallbackUrl(payment.Provider),
            payment.IdempotencyKey, new Dictionary<string, string> { ["purpose"] = "trip", ["tripId"] = payment.TripId?.ToString() ?? string.Empty });
}
