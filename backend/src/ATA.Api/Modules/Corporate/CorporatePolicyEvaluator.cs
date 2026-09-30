using ATA.Api.Common;
using ATA.Api.Modules.Promotions;
using ATA.Domain.Corporate;
using ATA.Domain.Trips;

namespace ATA.Api.Modules.Corporate;

/// <summary>What a booking is checked with (doc 12 §F19.2 <c>CorporatePolicyEvaluator</c> inputs); zones are already resolved by <c>ZoneResolver</c>.</summary>
public sealed record PolicyFacts(
    Guid RideCategoryId, Guid? PickupZoneId, Guid? DropoffZoneId, DateTime PickupAtUtc, decimal EstimatedFare, BookingType BookingType, string? Purpose, bool HasCostCenter,
    bool CostCenterActive, bool IsGuest);

/// <summary>The pure rule part of the policy evaluation (budget and credit limit need the ledger, see <c>CorporateTripPolicyService</c>).</summary>
public static class CorporatePolicyEvaluator
{
    public const string RuleCategory = "category";
    public const string RuleDay = "day";
    public const string RuleTimeWindow = "time_window";
    public const string RuleZone = "zone";
    public const string RuleMaxFare = "max_fare";
    public const string RuleScheduled = "scheduled";
    public const string RulePurposeRequired = "purpose_required";
    public const string RuleCostCenterRequired = "cost_center_required";
    public const string RuleGuestBooking = "guest_booking";
    public const string RuleBudgetExceeded = "budget_exceeded";
    public const string RuleCreditLimitExceeded = "credit_limit_exceeded";

    /// <summary>A <c>null</c> policy (the company has none) allows everything. Time rules use the Riyadh wall clock of the pickup time.</summary>
    public static IReadOnlyList<PolicyViolationDto> Evaluate(CorporatePolicy? policy, PolicyFacts facts, IReadOnlyDictionary<Guid, string> categoryCodes)
    {
        var violations = new List<PolicyViolationDto>();
        if (policy is null)
        {
            return violations;
        }

        if (facts.IsGuest && !policy.AllowGuestBooking)
        {
            violations.Add(new PolicyViolationDto(RuleGuestBooking));
        }

        if (JsonLists.Parse<Guid>(policy.AllowedRideCategoryIds) is { Count: > 0 } categories && !categories.Contains(facts.RideCategoryId))
        {
            violations.Add(new PolicyViolationDto(RuleCategory, null, categories.Select(id => categoryCodes.GetValueOrDefault(id, id.ToString())).ToList()));
        }

        var local = Formats.ToRiyadh(facts.PickupAtUtc);
        if (JsonLists.Parse<int>(policy.AllowedDays) is { Count: > 0 } days && !days.Contains((int)local.DayOfWeek))
        {
            violations.Add(new PolicyViolationDto(RuleDay, null, days));
        }

        if (JsonLists.Parse<TimeWindowDto>(policy.TimeWindows) is { Count: > 0 } windows && !windows.Any(w => InWindow(TimeOnly.FromDateTime(local), w)))
        {
            violations.Add(new PolicyViolationDto(RuleTimeWindow, null, windows));
        }

        if (JsonLists.Parse<Guid>(policy.AllowedZoneIds) is { Count: > 0 } zones)
        {
            var pickupOk = facts.PickupZoneId is { } p && zones.Contains(p);
            var dropoffOk = facts.DropoffZoneId is { } d && zones.Contains(d);
            if (policy.ZoneMatch == ZoneMatch.PickupAndDropoff ? !(pickupOk && dropoffOk) : !(pickupOk || dropoffOk))
            {
                violations.Add(new PolicyViolationDto(RuleZone, null, zones));
            }
        }

        if (policy.MaxFarePerTrip is { } max && facts.EstimatedFare > max)
        {
            violations.Add(new PolicyViolationDto(RuleMaxFare, max));
        }

        if (facts.BookingType == BookingType.Scheduled && !policy.AllowScheduled)
        {
            violations.Add(new PolicyViolationDto(RuleScheduled));
        }

        if (policy.RequirePurpose && string.IsNullOrWhiteSpace(facts.Purpose))
        {
            violations.Add(new PolicyViolationDto(RulePurposeRequired));
        }

        if (policy.RequireCostCenter && (!facts.HasCostCenter || !facts.CostCenterActive))
        {
            violations.Add(new PolicyViolationDto(RuleCostCenterRequired));
        }

        return violations;
    }

    /// <summary><c>from ≤ time ≤ to</c>; a window whose end is before its start wraps past midnight.</summary>
    public static bool InWindow(TimeOnly time, TimeWindowDto window)
    {
        if (!TimeOnly.TryParse(window.From, out var from) || !TimeOnly.TryParse(window.To, out var to))
        {
            return false;
        }

        return from <= to ? time >= from && time <= to : time >= from || time <= to;
    }
}
