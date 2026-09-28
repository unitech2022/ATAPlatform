using System.Text.Json;
using ATA.Api.Common;
using ATA.Api.Modules.Notifications;
using ATA.Api.Modules.Payments;
using ATA.Api.Modules.Payments.Gateways;
using ATA.Api.Modules.Pricing;
using ATA.Api.Modules.Safety;
using ATA.Api.Modules.Trips;
using ATA.Domain.Cancellation;
using ATA.Domain.Common;
using ATA.Domain.Notifications;
using ATA.Domain.Payments;
using ATA.Domain.Safety;
using ATA.Domain.Trips;
using ATA.Domain.Wallet;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Cancellation;

/// <summary>A cancellation request handled by <see cref="CancellationEngine"/>.</summary>
public sealed record CancelCommand(
    TripActor Actor,
    Guid? UserId,
    string ReasonCode,
    string? Note,
    decimal? ExpectedFee = null,
    int? ExpectedPenaltyPoints = null,
    AtFault? AdminAtFault = null,
    bool AdminChargeFee = false,
    bool NoShow = false);

/// <summary>What a cancellation costs right now (shared by the preview and the cancellation itself).</summary>
public sealed record CancellationQuote(
    CancellationStage Stage,
    CancellationReason? Reason,
    FeeOutcome Outcome,
    AtFault AtFault,
    bool CountsTowardRate,
    bool RequiresReview,
    decimal FeeToCharge,
    int PointsToApply,
    int? SecondsSinceAccept,
    int? SecondsSinceArrival);

/// <summary>
/// The F14 cancellation engine (doc 09 §F14.3): stage and rule resolution, fault attribution, fee collection through the F11 ledger
/// (card capture of the authorization, else a <c>cancellation_fee</c> wallet debit with overdraft), driver compensation, excuses pending review,
/// the <c>cancellation_events</c> row, notifications and the reliability refresh. Every F8 cancellation endpoint goes through <see cref="CancelAsync"/>.
/// </summary>
public sealed class CancellationEngine(
    AtaDbContext db,
    IClock clock,
    IPricingService pricing,
    ZoneResolver zones,
    TripReadService reads,
    TripEventRecorder events,
    LedgerService ledger,
    PaymentService payments,
    IPaymentGatewayResolver gateways,
    CardTripPaymentService cardPayments,
    INotificationDispatcher notifications,
    SafetyCaseFactory safetyCases,
    TripShareService shares,
    ReliabilityService reliability,
    IOptions<PaymentsOptions> paymentOptions,
    ILogger<CancellationEngine> logger)
{
    public const string ReferenceType = "cancellation";

    public async Task<CancellationQuote> QuoteAsync(Trip trip, TripActor actor, string? reasonCode, bool noShow, AtFault? adminAtFault, bool adminChargeFee, CancellationToken ct)
    {
        if (!trip.CanBeCancelled)
        {
            throw new DomainException(ErrorCodes.Conflict, new { status = trip.Status });
        }

        var now = clock.UtcNow;
        var freeWaitingSeconds = await FreeWaitingSecondsAsync(trip, ct);
        var stage = noShow ? CancellationStage.NoShow : CancellationMath.StageOf(trip, freeWaitingSeconds, now);
        var reason = reasonCode is null ? null : await ResolveReasonAsync(actor, reasonCode, stage, noShow, ct);

        var ruleActor = actor switch
        {
            TripActor.Passenger => (CancellationActor?)CancellationActor.Passenger,
            TripActor.Driver when noShow => CancellationActor.Passenger,
            TripActor.Driver => CancellationActor.Driver,
            TripActor.Admin => adminAtFault switch
            {
                AtFault.Passenger => CancellationActor.Passenger,
                AtFault.Driver => CancellationActor.Driver,
                _ => null,
            },
            _ => null,
        };
        var anchor = CancellationMath.AnchorOf(trip, stage);
        FeeOutcome outcome;
        if (ruleActor is { } forActor)
        {
            var zone = await zones.ResolveAsync(trip.PickupLat, trip.PickupLng, trip.RequestedAt, ct);
            var rules = await db.CancellationRules.AsNoTracking().Where(r => r.IsActive && r.Actor == forActor && r.Stage == stage).ToListAsync(ct);
            var rule = CancellationMath.SelectRule(rules, forActor, stage, trip.BookingType, trip.RideCategoryId, zone?.Id);
            var pricingFee = rule?.FeeType == CancellationFeeType.PricingRule ? await PricingRuleFeeAsync(trip, zone?.Id, ct) : 0m;
            outcome = CancellationMath.Calculate(rule, anchor, now, trip.OfferedPrice ?? trip.EstimatedFare, pricingFee);
        }
        else
        {
            outcome = new FeeOutcome(null, 0m, 0, false, null);
        }

        AtFault atFault;
        bool counts;
        switch (actor)
        {
            case TripActor.Passenger:
                atFault = stage == CancellationStage.BeforeAccept || outcome.WithinFreeWindow ? AtFault.None : AtFault.Passenger;
                counts = atFault == AtFault.Passenger;
                break;
            case TripActor.Driver when noShow:
                atFault = AtFault.Passenger;
                counts = true;
                break;
            case TripActor.Driver:
                // Driver cancellations after acceptance always count, even inside the free window (which only cancels points and fees).
                atFault = AtFault.Driver;
                counts = true;
                break;
            case TripActor.Admin:
                atFault = adminAtFault ?? AtFault.None;
                counts = atFault != AtFault.None;
                break;
            default:
                atFault = AtFault.None;
                counts = false;
                break;
        }

        // Excusable / emergency reasons wait for an operations review before any fee, points or rate impact (nothing to review when nobody is at fault).
        var requiresReview = reason?.NeedsReview == true && actor is TripActor.Passenger or TripActor.Driver && atFault != AtFault.None;
        var fee = atFault == AtFault.None ? 0m : actor == TripActor.Admin && !adminChargeFee ? 0m : outcome.Fee;
        var points = atFault == AtFault.None ? 0 : outcome.PenaltyPoints;
        if (requiresReview)
        {
            fee = 0m;
            points = 0;
            counts = false;
        }

        int? sinceAccept = trip.AssignedAt is { } assigned ? (int)Math.Max(0, (now - assigned).TotalSeconds) : null;
        int? sinceArrival = trip.ArrivedAt is { } arrived ? (int)Math.Max(0, (now - arrived).TotalSeconds) : null;
        return new CancellationQuote(stage, reason, outcome, atFault, counts, requiresReview, fee, points, sinceAccept, sinceArrival);
    }

    /// <summary>Validates the reason, applies the quote and cancels the trip. Returns the event; the trip is published by the caller.</summary>
    public async Task<CancellationEvent> CancelAsync(Trip trip, CancelCommand command, CancellationToken ct)
    {
        var quote = await QuoteAsync(trip, command.Actor, command.ReasonCode, command.NoShow, command.AdminAtFault, command.AdminChargeFee, ct);
        if (quote.Reason is { RequiresNote: true } && string.IsNullOrWhiteSpace(command.Note))
        {
            throw new DomainException(ErrorCodes.ValidationFailed, new Dictionary<string, string> { ["note"] = "required" });
        }

        if ((command.ExpectedFee is { } expectedFee && quote.FeeToCharge > expectedFee)
            || (command.ExpectedPenaltyPoints is { } expectedPoints && quote.PointsToApply > expectedPoints))
        {
            throw new DomainException(ErrorCodes.CancellationFeeChanged, new { fee = quote.FeeToCharge, penaltyPoints = quote.PointsToApply });
        }

        var now = clock.UtcNow;
        var participants = await reads.ParticipantsAsync(trip, ct);
        var hadDriver = trip.HasDriver;
        var passengerFee = quote.AtFault == AtFault.Passenger ? quote.FeeToCharge : 0m;
        var driverFee = quote.AtFault == AtFault.Driver ? quote.FeeToCharge : 0m;
        var cancellation = new CancellationEvent
        {
            TripId = trip.Id,
            Actor = command.Actor,
            UserId = command.UserId,
            AtFault = quote.AtFault,
            Stage = quote.Stage,
            BookingType = trip.BookingType,
            ReasonId = quote.Reason?.Id,
            ReasonCode = command.ReasonCode,
            Note = string.IsNullOrWhiteSpace(command.Note) ? null : command.Note.Trim(),
            RuleId = quote.Outcome.Rule?.Id,
            SecondsSinceAccept = quote.SecondsSinceAccept,
            SecondsSinceArrival = quote.SecondsSinceArrival,
            EstimatedFare = trip.OfferedPrice ?? trip.EstimatedFare,
            FeeAmount = quote.RequiresReview ? quote.Outcome.Fee : quote.FeeToCharge,
            PenaltyPoints = quote.PointsToApply,
            CountsTowardRate = quote.CountsTowardRate,
            FeeStatus = quote.RequiresReview ? CancellationFeeStatus.PendingReview : CancellationFeeStatus.None,
            ExcuseStatus = quote.RequiresReview ? ExcuseStatus.Pending : ExcuseStatus.NotApplicable,
        };

        // Card trips: the fee is captured from the authorization before the transaction (gateway calls never run inside one).
        var cardCaptured = passengerFee > 0 && trip.PaymentMethod == PaymentMethodKind.Card && await CaptureFeeAsync(trip, passengerFee, ct);

        await db.InTransactionAsync(async () =>
        {
            trip.Cancel(command.Actor switch
            {
                TripActor.Passenger => CancelledBy.Passenger,
                TripActor.Driver => CancelledBy.Driver,
                TripActor.Admin => CancelledBy.Admin,
                _ => CancelledBy.System,
            }, command.ReasonCode, now);
            db.CancellationEvents.Add(cancellation);
            await reads.ReleaseDriverAsync(trip, now, ct);
            if (command.NoShow)
            {
                events.Add(trip.Id, TripEventTypes.PassengerNoShow, TripActor.Driver, command.UserId, data: new { waitedSeconds = quote.SecondsSinceArrival });
            }

            events.Add(trip.Id, TripEventTypes.Cancelled, command.Actor, command.UserId, data: new
            {
                reasonCode = command.ReasonCode, note = cancellation.Note, hadDriver, stage = quote.Stage, atFault = quote.AtFault, fee = quote.FeeToCharge,
                penaltyPoints = quote.PointsToApply, requiresReview = quote.RequiresReview,
            });

            if (passengerFee > 0)
            {
                await ChargePassengerAsync(cancellation, trip, participants, passengerFee, cardCaptured, quote.Outcome.Rule, ct);
            }
            else if (driverFee > 0 && participants.DriverUserId is { } driverUserId)
            {
                await ChargeDriverAsync(cancellation, trip, driverUserId, driverFee, ct);
            }

            await NotifyPartiesAsync(trip, command.Actor, participants, hadDriver, cancellation, ct);
            if (quote.Reason is { IsEmergency: true } && command.UserId is { } reporter)
            {
                var role = command.Actor == TripActor.Driver ? SafetyReporterRole.Driver : SafetyReporterRole.Passenger;
                await safetyCases.AddAsync(new SafetyCase
                {
                    CaseNumber = string.Empty, Type = SafetyCaseType.SafetyReport, Source = SafetyCaseSource.Report, Priority = SafetyPriority.Medium, TripId = trip.Id,
                    ReporterUserId = reporter, ReporterRole = role, SubjectUserId = role == SafetyReporterRole.Driver ? participants.PassengerUserId : participants.DriverUserId,
                    Description = cancellation.Note ?? "Trip cancelled for a safety concern",
                }, $"Trip {trip.TripNumber} cancelled with reason {command.ReasonCode}", notifyOps: false, null, ct);
            }

            await shares.ExpireForTripAsync(trip.Id, now, ct);
            await db.SaveChangesAsync(ct);
        }, ct);

        await cardPayments.ReleaseAsync(trip.Id, ct);
        await payments.PublishPendingAsync(ct);
        await safetyCases.PublishAsync(ct);
        await RefreshReliabilityAsync(participants, ct);
        return cancellation;
    }

    /// <summary>Approve / reject a pending excuse (doc 09 §F14.3): approval waives (and refunds anything charged); rejection charges now.</summary>
    public async Task ApplyRejectionAsync(CancellationEvent cancellation, Trip trip, CancellationToken ct)
    {
        var participants = await reads.ParticipantsAsync(trip, ct);
        var rule = cancellation.RuleId is { } ruleId ? await db.CancellationRules.AsNoTracking().FirstOrDefaultAsync(r => r.Id == ruleId, ct) : null;
        cancellation.PenaltyPoints = PendingPoints(cancellation, rule);
        cancellation.CountsTowardRate = cancellation.AtFault != AtFault.None;
        cancellation.FeeStatus = CancellationFeeStatus.None;
        var fee = cancellation.FeeAmount;
        if (fee > 0 && cancellation.AtFault == AtFault.Passenger)
        {
            // The authorization was released at cancellation time: the fee now goes to the passenger wallet.
            await ChargePassengerAsync(cancellation, trip, participants, fee, false, rule, ct);
        }
        else if (fee > 0 && cancellation.AtFault == AtFault.Driver && participants.DriverUserId is { } driverUserId)
        {
            await ChargeDriverAsync(cancellation, trip, driverUserId, fee, ct);
        }
    }

    public async Task RefreshReliabilityAsync(TripParticipants participants, CancellationToken ct)
    {
        try
        {
            await reliability.RefreshAsync(participants.PassengerUserId, Role.Passenger, ct);
            if (participants.DriverUserId is { } driverUserId)
            {
                await reliability.RefreshAsync(driverUserId, Role.Driver, ct);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Reliability refresh failed");
        }
    }

    /// <summary>Points the matched rule gives once a pending excuse is rejected (the free window is re-evaluated from the stored elapsed seconds).</summary>
    public static int PendingPoints(CancellationEvent e, CancellationRule? rule)
    {
        if (rule is null || e.AtFault == AtFault.None)
        {
            return 0;
        }

        var elapsed = e.Stage switch
        {
            CancellationStage.AfterAccept or CancellationStage.EnRoute => e.SecondsSinceAccept,
            CancellationStage.Arrived or CancellationStage.Waiting or CancellationStage.NoShow => e.SecondsSinceArrival,
            _ => null,
        } ?? int.MaxValue;
        return elapsed < rule.FreeWindowSeconds ? 0 : rule.PenaltyPoints;
    }

    private async Task ChargePassengerAsync(CancellationEvent cancellation, Trip trip, TripParticipants participants, decimal fee, bool cardCaptured, CancellationRule? rule, CancellationToken ct)
    {
        try
        {
            if (cardCaptured)
            {
                await ledger.JournalAsync(JournalType.CancellationFeeCard, LedgerAccounts.GatewayClearing, LedgerAccounts.CancellationFees, fee, ReferenceType, cancellation.Id,
                    $"cancellation:{cancellation.Id}:fee", $"Trip {trip.TripNumber} cancellation fee (card)", ct);
                cancellation.FeeMethod = CancellationFeeMethod.Card;
            }
            else
            {
                var wallet = await ledger.GetOrCreateWalletAsync(participants.PassengerUserId, WalletKind.Passenger, ct);
                await ledger.PostAsync(wallet, TransactionType.CancellationFee, TransactionDirection.Debit, fee, LedgerAccounts.CancellationFees,
                    $"Trip {trip.TripNumber} cancellation fee", $"cancellation:{cancellation.Id}:fee", ReferenceType, cancellation.Id, ct, allowOverdraft: true);
                cancellation.FeeMethod = CancellationFeeMethod.Wallet;
            }
        }
        catch (DomainException ex)
        {
            logger.LogWarning("Cancellation fee of trip {TripNumber} could not be collected: {Code}", trip.TripNumber, ex.Code);
            cancellation.FeeStatus = CancellationFeeStatus.Failed;
            return;
        }

        cancellation.FeeCharged = fee;
        cancellation.FeeStatus = CancellationFeeStatus.Charged;
        events.Add(trip.Id, TripEventTypes.CancellationFeeCharged, TripActor.System, data: new { fee, method = cancellation.FeeMethod, cancellationId = cancellation.Id });
        await notifications.DispatchAsync(new NotificationRequest(NotificationTypes.CancellationFeeCharged, participants.PassengerUserId,
            NotificationPlaceholders.Of(("tripNumber", trip.TripNumber)).Money("fee", fee), "trip", trip.Id,
            new Dictionary<string, object?> { ["tripNumber"] = trip.TripNumber, ["cancellationId"] = cancellation.Id }), ct);

        var compensation = participants.DriverUserId is not null && trip.DriverId is not null ? CancellationMath.Compensation(fee, rule) : 0m;
        if (compensation > 0 && participants.DriverUserId is { } driverUserId)
        {
            var driverWallet = await ledger.GetOrCreateWalletAsync(driverUserId, WalletKind.Driver, ct);
            await ledger.PostAsync(driverWallet, TransactionType.CancellationCompensation, TransactionDirection.Credit, compensation, LedgerAccounts.CancellationFees,
                $"Trip {trip.TripNumber} cancellation compensation", $"cancellation:{cancellation.Id}:compensation", ReferenceType, cancellation.Id, ct);
            cancellation.CompensationAmount = compensation;
            await notifications.DispatchAsync(new NotificationRequest(NotificationTypes.CancellationCompensation, driverUserId,
                NotificationPlaceholders.Of(("tripNumber", trip.TripNumber)).Money("amount", compensation), "trip", trip.Id,
                new Dictionary<string, object?> { ["tripNumber"] = trip.TripNumber, ["cancellationId"] = cancellation.Id }), ct);
        }
    }

    private async Task ChargeDriverAsync(CancellationEvent cancellation, Trip trip, Guid driverUserId, decimal fee, CancellationToken ct)
    {
        var wallet = await ledger.GetOrCreateWalletAsync(driverUserId, WalletKind.Driver, ct);
        await ledger.PostAsync(wallet, TransactionType.CancellationFee, TransactionDirection.Debit, fee, LedgerAccounts.CancellationFees,
            $"Trip {trip.TripNumber} driver cancellation fee", $"cancellation:{cancellation.Id}:driver_fee", ReferenceType, cancellation.Id, ct, allowOverdraft: true);
        cancellation.FeeCharged = fee;
        cancellation.FeeMethod = CancellationFeeMethod.Wallet;
        cancellation.FeeStatus = CancellationFeeStatus.Charged;
    }

    /// <summary><c>trip.cancelled</c> to the other party (both for admins; the passenger for a no-show, with the fee).</summary>
    private async Task NotifyPartiesAsync(Trip trip, TripActor actor, TripParticipants participants, bool hadDriver, CancellationEvent cancellation, CancellationToken ct)
    {
        var passengerFee = cancellation.AtFault == AtFault.Passenger ? cancellation.FeeCharged : 0m;
        if (actor is TripActor.Driver or TripActor.Admin)
        {
            await notifications.DispatchAsync(TripNotifications.Cancelled(trip, participants.PassengerUserId, passengerFee), ct);
        }

        if (actor is TripActor.Passenger or TripActor.Admin && hadDriver && participants.DriverUserId is { } driverUserId)
        {
            await notifications.DispatchAsync(TripNotifications.Cancelled(trip, driverUserId), ct);
        }
    }

    /// <summary>Captures the fee from the trip's card authorization; <c>false</c> when there is none or the gateway declines (the fee then goes to the wallet).</summary>
    private async Task<bool> CaptureFeeAsync(Trip trip, decimal fee, CancellationToken ct)
    {
        var authorized = await db.Payments
            .Where(p => p.TripId == trip.Id && p.Purpose == PaymentPurpose.Trip && p.Status == PaymentStatus.Authorized && p.GatewayPaymentId != null)
            .OrderByDescending(p => p.CreatedAt).FirstOrDefaultAsync(ct);
        if (authorized is null || fee > (authorized.AuthorizedAmount ?? 0m))
        {
            return false;
        }

        var result = await payments.CallAsync(t => gateways.Get(authorized.Provider).CaptureAsync(authorized.GatewayPaymentId!, fee, t), paymentOptions.Value.CaptureTimeoutSeconds, ct);
        if (!result.Success || result.Status != GatewayStatus.Captured)
        {
            logger.LogWarning("Cancellation fee capture failed for trip {TripNumber}: {Code}", trip.TripNumber, result.FailureCode);
            return false;
        }

        PaymentMetadata.Update(authorized, m => m["cancellationFee"] = fee);
        await payments.ApplyResultAsync(authorized, result with { CapturedAmount = fee }, ct);
        return true;
    }

    private async Task<CancellationReason> ResolveReasonAsync(TripActor actor, string reasonCode, CancellationStage stage, bool noShow, CancellationToken ct)
    {
        var reasonActor = actor switch
        {
            TripActor.Passenger => CancellationActor.Passenger,
            TripActor.Driver => CancellationActor.Driver,
            _ => CancellationActor.System,
        };
        var code = reasonCode.Trim();
        var reason = await db.CancellationReasons.AsNoTracking().FirstOrDefaultAsync(r => r.Actor == reasonActor && r.Code == code && r.IsActive, ct);
        var selectable = reason is not null && (reason.IsSelectable || noShow || reasonActor == CancellationActor.System);
        if (reason is null || !selectable || !AllowsStage(reason, stage))
        {
            throw new DomainException(ErrorCodes.CancellationReasonInvalid, new { reasonCode = code, stage });
        }

        return reason;
    }

    public static IReadOnlyList<CancellationStage>? StagesOf(CancellationReason reason)
    {
        if (string.IsNullOrWhiteSpace(reason.Stages))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<List<CancellationStage>>(reason.Stages, JsonDefaults.Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static bool AllowsStage(CancellationReason reason, CancellationStage stage) => StagesOf(reason) is not { } stages || stages.Contains(stage);

    private async Task<int> FreeWaitingSecondsAsync(Trip trip, CancellationToken ct)
    {
        var category = await db.RideCategories.AsNoTracking().FirstAsync(c => c.Id == trip.RideCategoryId, ct);
        return await pricing.FreeWaitingMinutesAsync(category, new GeoPoint(trip.PickupLat, trip.PickupLng), trip.RequestedAt, ct) * 60;
    }

    /// <summary><c>pricing_rules.cancellation_fee</c> of the rule that priced the trip (its quote), else of the rule that applies now.</summary>
    private async Task<decimal> PricingRuleFeeAsync(Trip trip, Guid? zoneId, CancellationToken ct)
    {
        var ruleId = await db.FareQuotes.AsNoTracking().Where(q => q.UsedTripId == trip.Id).Select(q => q.PricingRuleId).FirstOrDefaultAsync(ct);
        if (ruleId is { } id && await db.PricingRules.AsNoTracking().Where(r => r.Id == id).Select(r => (decimal?)r.CancellationFee).FirstOrDefaultAsync(ct) is { } fee)
        {
            return fee;
        }

        var at = trip.RequestedAt;
        var rules = await db.PricingRules.AsNoTracking()
            .Where(r => r.RideCategoryId == trip.RideCategoryId && r.IsActive && (r.ZoneId == null || r.ZoneId == zoneId) && r.EffectiveFrom <= at && (r.EffectiveTo == null || r.EffectiveTo > at))
            .ToListAsync(ct);
        return rules.OrderByDescending(r => r.ZoneId != null).ThenByDescending(r => r.Priority).ThenByDescending(r => r.EffectiveFrom).Select(r => r.CancellationFee).FirstOrDefault();
    }
}

/// <summary>Records the <c>cancellation_events</c> row of a system cancellation (<c>no_drivers</c>, <c>payment_failed</c>); no DI so any module can use it.</summary>
public static class SystemCancellation
{
    public static async Task RecordAsync(AtaDbContext db, Trip trip, string reasonCode, CancellationToken ct)
    {
        if (db.CancellationEvents.Local.Any(e => e.TripId == trip.Id) || await db.CancellationEvents.AnyAsync(e => e.TripId == trip.Id, ct))
        {
            return;
        }

        var reasonId = await db.CancellationReasons.AsNoTracking().Where(r => r.Actor == CancellationActor.System && r.Code == reasonCode).Select(r => (Guid?)r.Id).FirstOrDefaultAsync(ct);
        db.CancellationEvents.Add(CancellationEvent.System(trip, reasonCode, reasonId));
    }
}
