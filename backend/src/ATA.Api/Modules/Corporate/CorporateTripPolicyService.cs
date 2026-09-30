using ATA.Api.Modules.Pricing;
using ATA.Api.Modules.Trips;
using ATA.Domain.Common;
using ATA.Domain.Corporate;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Corporate;

/// <summary>Input of a policy check (the trip has not been created yet).</summary>
public sealed record CorporateCheckInput(
    Guid RideCategoryId, GeoPoint Pickup, GeoPoint Dropoff, DateTime PickupAtUtc, decimal EstimatedFare, BookingType BookingType, string? Purpose, Guid? CostCenterId, bool IsGuest);

/// <summary>Outcome of the policy, budget and credit-limit evaluation of one prospective corporate trip.</summary>
public sealed record CorporateCheck(
    IReadOnlyList<PolicyViolationDto> Violations, decimal? RemainingBudget, bool BudgetExceeded, bool CreditExceeded, decimal CreditLimit, CorporateExposure Exposure, CorporatePolicy? Policy)
{
    public bool Allowed => Violations.Count == 0 && !BudgetExceeded && !CreditExceeded;

    /// <summary>The quote block: budget and credit-limit breaches are listed as pseudo-rules since the quote has no separate error code for them.</summary>
    public CorporateQuoteDto ToQuote()
    {
        var all = Violations.ToList();
        if (BudgetExceeded)
        {
            all.Add(new PolicyViolationDto(CorporatePolicyEvaluator.RuleBudgetExceeded, RemainingBudget));
        }

        if (CreditExceeded)
        {
            all.Add(new PolicyViolationDto(CorporatePolicyEvaluator.RuleCreditLimitExceeded, CreditLimit));
        }

        return new CorporateQuoteDto(Allowed, all, RemainingBudget);
    }

    /// <summary>Throws <c>422 corporate_policy_violation { violations }</c>, then <c>corporate_budget_exceeded { remaining }</c>, then <c>corporate_credit_limit_exceeded</c>.</summary>
    public void EnforceOrThrow()
    {
        if (Violations.Count > 0)
        {
            throw new DomainException(ErrorCodes.CorporatePolicyViolation, new { violations = Violations });
        }

        if (BudgetExceeded)
        {
            throw new DomainException(ErrorCodes.CorporateBudgetExceeded, new { remaining = RemainingBudget ?? 0m });
        }

        if (CreditExceeded)
        {
            throw new DomainException(ErrorCodes.CorporateCreditLimitExceeded, new { creditLimit = CreditLimit, used = Exposure.Used });
        }
    }
}

/// <summary>
/// <c>CorporatePolicyEvaluator</c> orchestration (doc 12 §F19.2): picks the member's policy (own → account default, inactive policies are ignored), resolves the pickup/dropoff
/// zones, runs the pure rules, then the monthly budget (<c>member.monthly_budget ?? policy.monthly_budget_per_employee</c>) and the credit limit.
/// </summary>
public sealed class CorporateTripPolicyService(AtaDbContext db, ZoneResolver zones, CorporateExposureService exposure)
{
    /// <summary>The policy of the member; guests (<paramref name="member"/> null) and members without one use the company default. <c>null</c> = no rules.</summary>
    public async Task<CorporatePolicy?> EffectivePolicyAsync(CorporateAccount account, CorporateUser? member, CancellationToken ct) =>
        Resolve(await db.CorporatePolicies.AsNoTracking().Where(p => p.CorporateAccountId == account.Id).ToListAsync(ct), account, member);

    /// <summary>Own policy → <c>default_policy_id</c> → the policy flagged <c>is_default</c>; inactive policies are ignored.</summary>
    public static CorporatePolicy? Resolve(IReadOnlyCollection<CorporatePolicy> policies, CorporateAccount account, CorporateUser? member)
    {
        if (member?.PolicyId is { } ownId && policies.FirstOrDefault(p => p.Id == ownId && p.IsActive) is { } own)
        {
            return own;
        }

        if (account.DefaultPolicyId is { } defaultId && policies.FirstOrDefault(p => p.Id == defaultId && p.IsActive) is { } configured)
        {
            return configured;
        }

        return policies.FirstOrDefault(p => p.IsDefault && p.IsActive);
    }

    public static decimal? BudgetOf(CorporateUser? member, CorporatePolicy? policy) => member?.MonthlyBudget ?? policy?.MonthlyBudgetPerEmployee;

    public async Task<CorporateCheck> CheckAsync(CorporateAccount account, CorporateUser? member, CorporateCheckInput input, CancellationToken ct)
    {
        var policy = await EffectivePolicyAsync(account, member, ct);
        var pickupZone = await zones.ResolveAsync(input.Pickup.Lat, input.Pickup.Lng, input.PickupAtUtc, ct);
        var dropoffZone = await zones.ResolveAsync(input.Dropoff.Lat, input.Dropoff.Lng, input.PickupAtUtc, ct);
        var costCenterActive = input.CostCenterId is { } costCenterId
            && await db.CorporateCostCenters.AsNoTracking().AnyAsync(c => c.Id == costCenterId && c.CorporateAccountId == account.Id && c.IsActive, ct);
        var categoryCodes = await db.RideCategories.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.Code, ct);
        var facts = new PolicyFacts(input.RideCategoryId, pickupZone?.Id, dropoffZone?.Id, input.PickupAtUtc, input.EstimatedFare, input.BookingType, input.Purpose,
            input.CostCenterId is not null, costCenterActive, input.IsGuest);
        var violations = CorporatePolicyEvaluator.Evaluate(policy, facts, categoryCodes);

        decimal? remaining = null;
        var budgetExceeded = false;
        if (member is not null && BudgetOf(member, policy) is { } budget)
        {
            var spent = await exposure.SpentAsync(member.Id, input.PickupAtUtc, null, ct);
            remaining = Math.Max(0m, budget - spent);
            budgetExceeded = spent + input.EstimatedFare > budget;
        }

        var exposureNow = await exposure.ForAccountAsync(account.Id, ct);
        var creditExceeded = exposureNow.Used + input.EstimatedFare > account.CreditLimit;
        return new CorporateCheck(violations, remaining, budgetExceeded, creditExceeded, account.CreditLimit, exposureNow, policy);
    }

    /// <summary>
    /// A completed trip may exceed the policy (doc 12 §F19.2: allowed, recorded as the <c>corporate_policy_exceeded</c> trip event): the final fare above
    /// <c>max_fare_per_trip</c> or the employee's monthly budget. Returns the reasons, empty when within policy.
    /// </summary>
    public async Task<IReadOnlyList<PolicyViolationDto>> OverageAsync(Trip trip, decimal finalFare, CancellationToken ct)
    {
        var result = new List<PolicyViolationDto>();
        if (trip.CorporateAccountId is not { } accountId)
        {
            return result;
        }

        var account = await db.CorporateAccounts.AsNoTracking().FirstAsync(a => a.Id == accountId, ct);
        var member = trip.CorporateUserId is { } memberId ? await db.CorporateUsers.AsNoTracking().FirstOrDefaultAsync(m => m.Id == memberId, ct) : null;
        var policy = await EffectivePolicyAsync(account, member, ct);
        if (policy?.MaxFarePerTrip is { } max && finalFare > max)
        {
            result.Add(new PolicyViolationDto(CorporatePolicyEvaluator.RuleMaxFare, max));
        }

        if (member is not null && BudgetOf(member, policy) is { } budget)
        {
            var spent = await exposure.SpentAsync(member.Id, trip.CompletedAt ?? trip.RequestedAt, trip.Id, ct);
            if (spent + finalFare > budget)
            {
                result.Add(new PolicyViolationDto(CorporatePolicyEvaluator.RuleBudgetExceeded, budget));
            }
        }

        return result;
    }
}

/// <summary>The company side of a booking: who pays, who rides and who booked (doc 12 §F19.2 "الحجز").</summary>
public sealed record CorporateBooking(CorporateAccount Account, CorporateUser? Member, ATA.Domain.Passengers.PassengerProfile Passenger, Guid BookedByUserId, bool IsGuest, string? GuestName, string? GuestPhone);

/// <summary>Resolves the membership of a rider who pays with <c>paymentMethod: "corporate"</c> in the app and builds the <c>corporate</c> block of quotes.</summary>
public sealed class CorporateRiderService(AtaDbContext db, CorporateTripPolicyService policies)
{
    /// <summary>The caller's own active membership (<c>403 corporate_not_member</c>) of an active company (<c>403 corporate_account_inactive</c>).</summary>
    public async Task<CorporateBooking> ResolveForAppAsync(ATA.Domain.Passengers.PassengerProfile passenger, CancellationToken ct)
    {
        var member = await db.CorporateUsers.FirstOrDefaultAsync(m => m.UserId == passenger.UserId && m.Status == CorporateUserStatus.Active, ct)
            ?? throw new DomainException(ErrorCodes.CorporateNotMember);
        var account = await db.CorporateAccounts.FirstAsync(a => a.Id == member.CorporateAccountId, ct);
        CorporateContext.EnsureBookable(account);
        return new CorporateBooking(account, member, passenger, passenger.UserId, false, null, null);
    }

    /// <summary>Checks a company cost centre id (<c>422 validation_failed</c> when it is not one of the company's).</summary>
    public async Task EnsureCostCenterAsync(Guid accountId, Guid? costCenterId, CancellationToken ct)
    {
        if (costCenterId is { } id && !await db.CorporateCostCenters.AnyAsync(c => c.Id == id && c.CorporateAccountId == accountId, ct))
        {
            throw new DomainException(ErrorCodes.ValidationFailed, new Dictionary<string, string> { ["costCenterId"] = "unknown cost center" });
        }
    }

    /// <summary>
    /// <c>QuoteResponse.corporate</c> for the primary category of the quote: whether the company accepts the trip (policy, monthly budget, credit limit) with the listed reasons
    /// and the employee's remaining budget.
    /// </summary>
    public async Task<CorporateQuoteDto> QuoteBlockAsync(CorporateBooking booking, ATA.Api.Modules.Trips.EstimateRequest request, string? purpose, Guid? costCenterId, QuoteResponse response, DateTime now, CancellationToken ct)
    {
        await EnsureCostCenterAsync(booking.Account.Id, costCenterId, ct);
        var primary = response.Categories.FirstOrDefault(c => c.RideCategoryId == request.RideCategoryId) ?? response.Categories.FirstOrDefault();
        if (primary is null)
        {
            return new CorporateQuoteDto(true, [], null);
        }

        var scheduled = request.BookingType == BookingType.Scheduled;
        var pickupAt = scheduled && request.ScheduledAt is { } at ? at.ToUniversalTime() : now;
        var input = new CorporateCheckInput(primary.RideCategoryId, new GeoPoint(request.Pickup!.Lat!.Value, request.Pickup.Lng!.Value), new GeoPoint(request.Dropoff!.Lat!.Value, request.Dropoff.Lng!.Value),
            pickupAt, primary.Total, request.BookingType ?? BookingType.Now, purpose, costCenterId, booking.IsGuest);
        return (await policies.CheckAsync(booking.Account, booking.Member, input, ct)).ToQuote();
    }
}
