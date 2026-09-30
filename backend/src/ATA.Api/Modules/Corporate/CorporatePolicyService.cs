using System.Text.Json;
using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Api.Modules.Promotions;
using ATA.Domain.Common;
using ATA.Domain.Corporate;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Corporate;

/// <summary>Booking policies and cost centres of one company (doc 12 §F19.1 / §F19.4). Audit rows use <c>entity_type = corporate_account</c>, <c>entity_id</c> = the company.</summary>
public sealed class CorporatePolicyService(AtaDbContext db, AuditService audit)
{
    // ----- policies -----

    public async Task<IReadOnlyList<CorporatePolicyDto>> ListPoliciesAsync(Guid accountId, CancellationToken ct) =>
        (await db.CorporatePolicies.AsNoTracking().Where(p => p.CorporateAccountId == accountId).OrderByDescending(p => p.IsDefault).ThenBy(p => p.Name).ToListAsync(ct)).Select(CorporateDtos.ToDto).ToList();

    public async Task<CorporatePolicyDto> CreatePolicyAsync(CorporateAccount account, CorporatePolicyRequest request, string? actorRole, CancellationToken ct)
    {
        await ValidatePolicyAsync(request, ct);
        var first = !await db.CorporatePolicies.AnyAsync(p => p.CorporateAccountId == account.Id, ct);
        var policy = new CorporatePolicy { CorporateAccountId = account.Id, Name = request.Name!.Trim() };
        Apply(policy, request);
        if (first)
        {
            // The first policy of a company is its default (members without an own policy fall back to it).
            policy.IsDefault = true;
            account.DefaultPolicyId = policy.Id;
        }

        db.CorporatePolicies.Add(policy);
        audit.Log("corporate_policy.create", "corporate_account", account.Id, null, Snapshot(policy), actorRole);
        await db.SaveChangesAsync(ct);
        return CorporateDtos.ToDto(policy);
    }

    public async Task<CorporatePolicyDto> UpdatePolicyAsync(Guid accountId, Guid id, CorporatePolicyRequest request, string? actorRole, CancellationToken ct)
    {
        await ValidatePolicyAsync(request, ct);
        var policy = Guard.NotFound(await db.CorporatePolicies.FirstOrDefaultAsync(p => p.Id == id && p.CorporateAccountId == accountId, ct));
        var before = Snapshot(policy);
        policy.Name = request.Name!.Trim();
        Apply(policy, request);
        if (policy.IsDefault && !policy.IsActive)
        {
            throw new DomainException(ErrorCodes.Conflict, new { reason = "default_policy" });
        }

        audit.Log("corporate_policy.update", "corporate_account", accountId, before, Snapshot(policy), actorRole);
        await db.SaveChangesAsync(ct);
        return CorporateDtos.ToDto(policy);
    }

    public async Task DeletePolicyAsync(Guid accountId, Guid id, string? actorRole, CancellationToken ct)
    {
        var policy = Guard.NotFound(await db.CorporatePolicies.FirstOrDefaultAsync(p => p.Id == id && p.CorporateAccountId == accountId, ct));
        if (policy.IsDefault)
        {
            throw new DomainException(ErrorCodes.Conflict, new { reason = "default_policy" });
        }

        // Members that used it fall back to the default policy.
        await db.CorporateUsers.Where(m => m.PolicyId == id).ExecuteUpdateAsync(s => s.SetProperty(m => m.PolicyId, (Guid?)null), ct);
        audit.Log("corporate_policy.delete", "corporate_account", accountId, Snapshot(policy), null, actorRole);
        db.CorporatePolicies.Remove(policy);
        await db.SaveChangesAsync(ct);
    }

    public async Task<CorporatePolicyDto> SetDefaultAsync(CorporateAccount account, Guid id, string? actorRole, CancellationToken ct)
    {
        var policy = Guard.NotFound(await db.CorporatePolicies.FirstOrDefaultAsync(p => p.Id == id && p.CorporateAccountId == account.Id, ct));
        if (!policy.IsActive)
        {
            throw new DomainException(ErrorCodes.Conflict, new { reason = "inactive_policy" });
        }

        foreach (var other in await db.CorporatePolicies.Where(p => p.CorporateAccountId == account.Id && p.IsDefault && p.Id != id).ToListAsync(ct))
        {
            other.IsDefault = false;
        }

        policy.IsDefault = true;
        account.DefaultPolicyId = policy.Id;
        audit.Log("corporate_policy.default", "corporate_account", account.Id, null, new { policyId = policy.Id, policy.Name }, actorRole);
        await db.SaveChangesAsync(ct);
        return CorporateDtos.ToDto(policy);
    }

    private async Task ValidatePolicyAsync(CorporatePolicyRequest request, CancellationToken ct)
    {
        var v = new Validator()
            .Require(nameof(request.Name), request.Name, 120)
            .Rule(nameof(request.AllowedDays), request.AllowedDays is null || request.AllowedDays.All(d => d is >= 0 and <= 6), "days must be 0 (Sunday) to 6 (Saturday)")
            .Rule(nameof(request.TimeWindows), request.TimeWindows is null || request.TimeWindows.All(w => CorporateRules.IsTime(w.From) && CorporateRules.IsTime(w.To)), "windows must be HH:mm")
            .Rule(nameof(request.MaxFarePerTrip), request.MaxFarePerTrip is null or > 0, "must be positive")
            .Rule(nameof(request.MonthlyBudgetPerEmployee), request.MonthlyBudgetPerEmployee is null or >= 0, "must be positive");
        if (request.AllowedRideCategoryIds is { Count: > 0 } categories)
        {
            var known = await db.RideCategories.AsNoTracking().Where(c => categories.Contains(c.Id)).Select(c => c.Id).ToListAsync(ct);
            v.Rule(nameof(request.AllowedRideCategoryIds), known.Count == categories.Distinct().Count(), "unknown ride category");
        }

        if (request.AllowedZoneIds is { Count: > 0 } zones)
        {
            var known = await db.Zones.AsNoTracking().Where(z => zones.Contains(z.Id)).Select(z => z.Id).ToListAsync(ct);
            v.Rule(nameof(request.AllowedZoneIds), known.Count == zones.Distinct().Count(), "unknown zone");
        }

        v.ThrowIfInvalid();
    }

    private static void Apply(CorporatePolicy policy, CorporatePolicyRequest r)
    {
        policy.AllowedRideCategoryIds = JsonLists.Serialize(r.AllowedRideCategoryIds);
        policy.AllowedDays = JsonLists.Serialize(r.AllowedDays?.Distinct().Order());
        policy.TimeWindows = r.TimeWindows is { Count: > 0 } ? JsonSerializer.Serialize(r.TimeWindows.Select(w => new TimeWindowDto(w.From, w.To)).ToList(), JsonDefaults.Options) : null;
        policy.AllowedZoneIds = JsonLists.Serialize(r.AllowedZoneIds);
        policy.ZoneMatch = r.ZoneMatch ?? ZoneMatch.PickupAndDropoff;
        policy.MaxFarePerTrip = r.MaxFarePerTrip;
        policy.MonthlyBudgetPerEmployee = r.MonthlyBudgetPerEmployee;
        policy.RequirePurpose = r.RequirePurpose ?? false;
        policy.RequireCostCenter = r.RequireCostCenter ?? false;
        policy.AllowScheduled = r.AllowScheduled ?? true;
        policy.AllowGuestBooking = r.AllowGuestBooking ?? true;
        policy.IsActive = r.IsActive ?? true;
    }

    private static object Snapshot(CorporatePolicy p) => new
    {
        policyId = p.Id, p.Name, p.IsDefault, p.AllowedRideCategoryIds, p.AllowedDays, p.TimeWindows, p.AllowedZoneIds, p.ZoneMatch, p.MaxFarePerTrip, p.MonthlyBudgetPerEmployee,
        p.RequirePurpose, p.RequireCostCenter, p.AllowScheduled, p.AllowGuestBooking, p.IsActive,
    };

    // ----- cost centres -----

    public async Task<IReadOnlyList<CostCenterDto>> ListCostCentersAsync(Guid accountId, CancellationToken ct) =>
        (await db.CorporateCostCenters.AsNoTracking().Where(c => c.CorporateAccountId == accountId).OrderBy(c => c.Code).ToListAsync(ct)).Select(CorporateDtos.ToDto).ToList();

    public async Task<CostCenterDto> CreateCostCenterAsync(Guid accountId, CostCenterRequest request, string? actorRole, CancellationToken ct)
    {
        Validate(request);
        var code = request.Code!.Trim();
        await EnsureCodeFreeAsync(accountId, code, null, ct);
        var center = new CorporateCostCenter { CorporateAccountId = accountId, Code = code, Name = request.Name!.Trim(), IsActive = request.IsActive ?? true };
        db.CorporateCostCenters.Add(center);
        audit.Log("corporate_cost_center.create", "corporate_account", accountId, null, new { costCenterId = center.Id, center.Code, center.Name, center.IsActive }, actorRole);
        await db.SaveChangesAsync(ct);
        return CorporateDtos.ToDto(center);
    }

    public async Task<CostCenterDto> UpdateCostCenterAsync(Guid accountId, Guid id, CostCenterRequest request, string? actorRole, CancellationToken ct)
    {
        Validate(request);
        var center = Guard.NotFound(await db.CorporateCostCenters.FirstOrDefaultAsync(c => c.Id == id && c.CorporateAccountId == accountId, ct));
        var code = request.Code!.Trim();
        await EnsureCodeFreeAsync(accountId, code, id, ct);
        var before = new { costCenterId = center.Id, center.Code, center.Name, center.IsActive };
        center.Code = code;
        center.Name = request.Name!.Trim();
        center.IsActive = request.IsActive ?? center.IsActive;
        audit.Log("corporate_cost_center.update", "corporate_account", accountId, before, new { costCenterId = center.Id, center.Code, center.Name, center.IsActive }, actorRole);
        await db.SaveChangesAsync(ct);
        return CorporateDtos.ToDto(center);
    }

    /// <summary>A cost centre that trips or invoices already reference is only deactivated (history keeps its code); an unused one is deleted.</summary>
    public async Task DeleteCostCenterAsync(Guid accountId, Guid id, string? actorRole, CancellationToken ct)
    {
        var center = Guard.NotFound(await db.CorporateCostCenters.FirstOrDefaultAsync(c => c.Id == id && c.CorporateAccountId == accountId, ct));
        var used = await db.Trips.AnyAsync(t => t.CostCenterId == id, ct);
        if (used)
        {
            center.IsActive = false;
        }
        else
        {
            await db.CorporateUsers.Where(m => m.CostCenterId == id).ExecuteUpdateAsync(s => s.SetProperty(m => m.CostCenterId, (Guid?)null), ct);
            db.CorporateCostCenters.Remove(center);
        }

        audit.Log("corporate_cost_center.delete", "corporate_account", accountId, new { costCenterId = center.Id, center.Code, center.Name }, new { deactivated = used }, actorRole);
        await db.SaveChangesAsync(ct);
    }

    private static void Validate(CostCenterRequest request) => new Validator()
        .Require(nameof(request.Code), request.Code, 30)
        .Require(nameof(request.Name), request.Name, 120)
        .ThrowIfInvalid();

    private async Task EnsureCodeFreeAsync(Guid accountId, string code, Guid? exceptId, CancellationToken ct)
    {
        var lower = code.ToLowerInvariant();
        if (await db.CorporateCostCenters.AnyAsync(c => c.CorporateAccountId == accountId && c.Id != exceptId && c.Code.ToLower() == lower, ct))
        {
            throw new DomainException(ErrorCodes.Conflict, new { reason = "code_exists", code });
        }
    }
}
