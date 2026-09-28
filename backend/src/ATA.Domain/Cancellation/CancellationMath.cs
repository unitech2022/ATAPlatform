using ATA.Domain.Trips;

namespace ATA.Domain.Cancellation;

/// <summary>What a rule gives for one cancellation (§F14.3 "الحساب").</summary>
public sealed record FeeOutcome(CancellationRule? Rule, decimal Fee, int PenaltyPoints, bool WithinFreeWindow, DateTime? FreeUntil)
{
    public bool IsFree => Fee == 0m && PenaltyPoints == 0;
}

/// <summary>Pure cancellation rules of F14: stage resolution, rule selection and fee calculation.</summary>
public static class CancellationMath
{
    /// <summary>The stage of a cancellation now (<c>no_show</c> only through the no-show endpoint; <c>in_trip</c> and terminal trips cannot be cancelled).</summary>
    public static CancellationStage StageOf(Trip trip, int freeWaitingSeconds, DateTime now) => trip.Status switch
    {
        TripStatus.Requested or TripStatus.Searching => CancellationStage.BeforeAccept,
        TripStatus.DriverAssigned => CancellationStage.AfterAccept,
        TripStatus.DriverEnRoute => CancellationStage.EnRoute,
        TripStatus.DriverArrived or TripStatus.Waiting or TripStatus.PinVerified =>
            trip.ArrivedAt is { } arrived && (now - arrived).TotalSeconds > freeWaitingSeconds ? CancellationStage.Waiting : CancellationStage.Arrived,
        _ => throw new Common.DomainException(Common.ErrorCodes.Conflict, new { status = trip.Status }),
    };

    /// <summary>The instant the stage's timers start from.</summary>
    public static DateTime AnchorOf(Trip trip, CancellationStage stage) => stage switch
    {
        CancellationStage.AfterAccept or CancellationStage.EnRoute => trip.AssignedAt ?? trip.RequestedAt,
        CancellationStage.Arrived or CancellationStage.Waiting or CancellationStage.NoShow => trip.ArrivedAt ?? trip.AssignedAt ?? trip.RequestedAt,
        CancellationStage.Scheduled => trip.ScheduledAt ?? trip.RequestedAt,
        _ => trip.RequestedAt,
    };

    /// <summary>
    /// Candidates: active, same actor and stage, booking type / category / zone equal or null. Most specific first (number of non-null
    /// matching fields), then priority descending, then the newest.
    /// </summary>
    public static CancellationRule? SelectRule(IEnumerable<CancellationRule> rules, CancellationActor actor, CancellationStage stage, BookingType bookingType, Guid rideCategoryId, Guid? zoneId) =>
        rules
            .Where(r => r.IsActive && r.Actor == actor && r.Stage == stage
                        && (r.BookingType == null || r.BookingType == bookingType)
                        && (r.RideCategoryId == null || r.RideCategoryId == rideCategoryId)
                        && (r.ZoneId == null || (zoneId != null && r.ZoneId == zoneId)))
            .OrderByDescending(r => (r.BookingType != null ? 1 : 0) + (r.RideCategoryId != null ? 1 : 0) + (r.ZoneId != null ? 1 : 0))
            .ThenByDescending(r => r.Priority)
            .ThenByDescending(r => r.CreatedAt)
            .FirstOrDefault();

    /// <summary>
    /// <c>elapsed &lt; free_window → 0</c>; otherwise the fee by type, clamped to <c>[min_fee, max_fee]</c>, never above the estimated fare,
    /// rounded to 2 decimals, plus the rule's penalty points. No rule → no fee and no points.
    /// </summary>
    public static FeeOutcome Calculate(CancellationRule? rule, DateTime anchor, DateTime now, decimal estimatedFare, decimal pricingRuleFee)
    {
        if (rule is null)
        {
            return new FeeOutcome(null, 0m, 0, false, null);
        }

        var elapsed = (now - anchor).TotalSeconds;
        if (elapsed < rule.FreeWindowSeconds)
        {
            return new FeeOutcome(rule, 0m, 0, true, anchor.AddSeconds(rule.FreeWindowSeconds));
        }

        var fee = rule.FeeType switch
        {
            CancellationFeeType.Fixed => rule.FeeAmount ?? 0m,
            CancellationFeeType.Percent => estimatedFare * (rule.FeePercent ?? 0m) / 100m,
            CancellationFeeType.PricingRule => pricingRuleFee,
            _ => 0m,
        };
        if (rule.FeeType != CancellationFeeType.None)
        {
            if (rule.MinFee is { } min) fee = Math.Max(fee, min);
            if (rule.MaxFee is { } max) fee = Math.Min(fee, max);
        }

        fee = Math.Max(0m, Math.Min(fee, estimatedFare));
        return new FeeOutcome(rule, Round2(fee), rule.PenaltyPoints, false, null);
    }

    public static decimal Compensation(decimal feeCharged, CancellationRule? rule) =>
        rule is null ? 0m : Round2(feeCharged * rule.DriverCompensationPercent / 100m);

    public static decimal Round2(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);
}
