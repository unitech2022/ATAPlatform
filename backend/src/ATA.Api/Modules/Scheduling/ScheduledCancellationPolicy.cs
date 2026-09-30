using ATA.Domain.Scheduling;
using ATA.Domain.Trips;

namespace ATA.Api.Modules.Scheduling;

/// <summary>What cancelling a <c>scheduled</c> trip costs now (doc 11 §F17.3 "إلغاء الراكب لرحلة scheduled").</summary>
public sealed record ScheduledCancellationOutcome(decimal Fee, bool WithinFreeWindow, DateTime FreeUntil, decimal DriverCompensationPercent);

/// <summary>The F14 cancellation engine delegates the <c>scheduled</c> stage here: free until <c>T − free_cancel_minutes_before</c>, then the rule's late-cancellation fee.</summary>
public interface IScheduledCancellationPolicy
{
    Task<ScheduledRideRule> RuleAsync(Trip trip, CancellationToken ct);

    /// <summary><paramref name="pricingRuleFee"/> is only read when the rule's fee type is <c>pricing_rule</c>.</summary>
    ScheduledCancellationOutcome Evaluate(ScheduledRideRule rule, Trip trip, DateTime now, decimal pricingRuleFee);
}

public sealed class ScheduledCancellationPolicy(ScheduleRuleProvider rules) : IScheduledCancellationPolicy
{
    public Task<ScheduledRideRule> RuleAsync(Trip trip, CancellationToken ct) => rules.ResolveForTripAsync(trip, ct);

    public ScheduledCancellationOutcome Evaluate(ScheduledRideRule rule, Trip trip, DateTime now, decimal pricingRuleFee)
    {
        var scheduledAt = trip.ScheduledAt ?? trip.RequestedAt;
        var minutesBefore = (scheduledAt - now).TotalMinutes;
        var free = minutesBefore >= rule.FreeCancelMinutesBefore;
        var fee = free ? 0m : rule.LateCancelFee(trip.OfferedPrice ?? trip.EstimatedFare, pricingRuleFee);
        return new ScheduledCancellationOutcome(fee, free, rule.FreeCancelUntil(scheduledAt), rule.LateCancelDriverCompensationPercent);
    }
}
