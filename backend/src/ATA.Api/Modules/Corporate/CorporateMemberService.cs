using ATA.Api.Common;
using ATA.Domain.Common;
using ATA.Domain.Corporate;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Corporate;

/// <summary>
/// The rider's side of a company account (<c>/passenger/corporate*</c>, doc 12 §F19.4): membership with the effective policy and the month's budget, and the invitations
/// matching the verified phone number (accept → <c>active</c>, decline).
/// </summary>
public sealed class CorporateMemberService(
    AtaDbContext db, ICurrentUser currentUser, IClock clock, CorporateMembershipService membership, CorporateExposureService exposure, CorporateTripPolicyService policies)
{
    /// <summary>
    /// <c>null</c> for a user who is not a member. An <c>active</c> membership (or a <c>disabled</c> one, so the app can explain) comes with the policy, the budget of the month
    /// (<c>member.monthly_budget ?? policy.monthly_budget_per_employee</c>) and the active cost centres; a disabled member gets neither.
    /// </summary>
    public async Task<CorporateMembershipResponse?> GetAsync(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var member = await db.CorporateUsers.AsNoTracking().Where(m => m.UserId == userId && (m.Status == CorporateUserStatus.Active || m.Status == CorporateUserStatus.Disabled))
            .OrderBy(m => m.Status == CorporateUserStatus.Active ? 0 : 1).ThenByDescending(m => m.UpdatedAt).FirstOrDefaultAsync(ct);
        if (member is null)
        {
            return null;
        }

        var account = await db.CorporateAccounts.AsNoTracking().FirstAsync(a => a.Id == member.CorporateAccountId, ct);
        if (account.Status == CorporateAccountStatus.Closed)
        {
            return null;
        }

        var dto = new CorporateMembershipDto(member.Id, account.Id, account.DisplayName, CorporateDtos.RoleName(member.Role), member.EmployeeNumber, member.Department,
            member.Status == CorporateUserStatus.Active ? "active" : "disabled");
        if (member.Status != CorporateUserStatus.Active)
        {
            return new CorporateMembershipResponse(dto, null, null, []);
        }

        var policy = await policies.EffectivePolicyAsync(account, member, ct);
        CorporatePolicySummaryDto? summary = null;
        if (policy is not null)
        {
            var categoryIds = ATA.Api.Modules.Promotions.JsonLists.Parse<Guid>(policy.AllowedRideCategoryIds);
            var codes = categoryIds is { Count: > 0 }
                ? await db.RideCategories.AsNoTracking().Where(c => categoryIds.Contains(c.Id)).OrderBy(c => c.SortOrder).Select(c => c.Code).ToListAsync(ct)
                : null;
            summary = new CorporatePolicySummaryDto(policy.Name, codes, ATA.Api.Modules.Promotions.JsonLists.Parse<TimeWindowDto>(policy.TimeWindows),
                ATA.Api.Modules.Promotions.JsonLists.Parse<int>(policy.AllowedDays), policy.MaxFarePerTrip, policy.RequirePurpose, policy.RequireCostCenter, policy.AllowScheduled);
        }

        CorporateBudgetDto? budget = null;
        if (CorporateTripPolicyService.BudgetOf(member, policy) is { } monthly)
        {
            var spent = await exposure.SpentAsync(member.Id, clock.UtcNow, null, ct);
            budget = new CorporateBudgetDto(monthly, spent, Math.Max(0m, monthly - spent));
        }

        var costCenters = await db.CorporateCostCenters.AsNoTracking().Where(c => c.CorporateAccountId == account.Id && c.IsActive).OrderBy(c => c.Code)
            .Select(c => new CostCenterRefDto(c.Id, c.Code, c.Name)).ToListAsync(ct);
        return new CorporateMembershipResponse(dto, summary, budget, costCenters);
    }

    public async Task<IReadOnlyList<CorporateInvitationDto>> InvitationsAsync(CancellationToken ct) =>
        await membership.InvitationsForAsync(await PhoneAsync(ct), ct);

    public async Task AcceptAsync(Guid invitationId, CancellationToken ct)
    {
        var user = await db.Users.FirstAsync(u => u.Id == currentUser.UserId, ct);
        await membership.AcceptAsync(invitationId, user, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeclineAsync(Guid invitationId, CancellationToken ct)
    {
        var user = await db.Users.FirstAsync(u => u.Id == currentUser.UserId, ct);
        await membership.DeclineAsync(invitationId, user, ct);
        await db.SaveChangesAsync(ct);
    }

    private async Task<string> PhoneAsync(CancellationToken ct) =>
        await db.Users.AsNoTracking().Where(u => u.Id == currentUser.UserId).Select(u => u.PhoneNumber).FirstAsync(ct);
}
