using ATA.Api.Common;
using ATA.Domain.Common;
using ATA.Domain.Corporate;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Corporate;

/// <summary>
/// Employees of one company (doc 12 §F19.4): list / detail with the month-to-date spend, invite, import, edit, disable / enable, resend and cancel an invitation. Used by the
/// portal (<c>actorRole = corporate_admin</c>) and by the platform admin console; the caller passes the company, so a company's rows are never reachable through another's id.
/// </summary>
public sealed class CorporateEmployeeService(
    AtaDbContext db, IClock clock, CorporateMembershipService membership, CorporateExposureService exposure)
{
    public async Task<PagedResult<CorporateEmployeeDto>> ListAsync(
        CorporateAccount account, CorporateUserStatus? status, string? department, Guid? costCenterId, string? search, Paging paging, CancellationToken ct)
    {
        var query = db.CorporateUsers.AsNoTracking().Where(m => m.CorporateAccountId == account.Id);
        if (status is not null) query = query.Where(m => m.Status == status);
        if (!string.IsNullOrWhiteSpace(department)) query = query.Where(m => m.Department == department.Trim());
        if (costCenterId is { } cc) query = query.Where(m => m.CostCenterId == cc);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var phoneTerm = PhoneNumber.TryNormalize(term, out var normalized) ? normalized : term;
            query = query.Where(m => (m.FullName != null && m.FullName.Contains(term)) || m.PhoneNumber.Contains(phoneTerm) || (m.EmployeeNumber != null && m.EmployeeNumber.Contains(term)));
        }

        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(m => m.FullName ?? m.PhoneNumber).ThenBy(m => m.CreatedAt).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        return paging.Result(await ToDtosAsync(account, rows, ct), total);
    }

    public async Task<CorporateEmployeeDto> GetAsync(CorporateAccount account, Guid id, CancellationToken ct)
    {
        var member = Guard.NotFound(await db.CorporateUsers.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id && m.CorporateAccountId == account.Id, ct));
        return (await ToDtosAsync(account, [member], ct))[0];
    }

    public async Task<CorporateEmployeeDto> InviteAsync(CorporateAccount account, InviteEmployeeRequest request, Guid invitedBy, string? actorRole, CancellationToken ct)
    {
        var spec = ParseSpec(request.PhoneNumber, request.FullName, request.Role, request.EmployeeNumber, request.Department, request.CostCenterId, request.PolicyId, request.MonthlyBudget, fullNameRequired: true);
        var member = await membership.InviteAsync(account, spec, invitedBy, actorRole, ct);
        await db.SaveChangesAsync(ct);
        return (await ToDtosAsync(account, [member], ct))[0];
    }

    public async Task<CorporateEmployeeDto> UpdateAsync(CorporateAccount account, Guid id, UpdateEmployeeRequest request, string? actorRole, CancellationToken ct)
    {
        var member = Guard.NotFound(await db.CorporateUsers.FirstOrDefaultAsync(m => m.Id == id && m.CorporateAccountId == account.Id, ct));
        var spec = ParseSpec(member.PhoneNumber, request.FullName ?? member.FullName, request.Role ?? CorporateDtos.RoleName(member.Role), request.EmployeeNumber, request.Department, request.CostCenterId, request.PolicyId,
            request.MonthlyBudget, fullNameRequired: false);
        await membership.UpdateAsync(member, spec, actorRole, ct);
        await db.SaveChangesAsync(ct);
        return (await ToDtosAsync(account, [member], ct))[0];
    }

    public async Task<ImportResultDto> ImportAsync(CorporateAccount account, Stream csv, Guid invitedBy, CancellationToken ct)
    {
        var result = await membership.ImportAsync(account, csv, invitedBy, ct);
        await db.SaveChangesAsync(ct);
        return result;
    }

    public async Task<CorporateEmployeeDto> DisableAsync(CorporateAccount account, Guid id, string? actorRole, CancellationToken ct)
    {
        var member = Guard.NotFound(await db.CorporateUsers.FirstOrDefaultAsync(m => m.Id == id && m.CorporateAccountId == account.Id, ct));
        await membership.DisableAsync(member, actorRole, ct);
        await db.SaveChangesAsync(ct);
        return (await ToDtosAsync(account, [member], ct))[0];
    }

    public async Task<CorporateEmployeeDto> EnableAsync(CorporateAccount account, Guid id, string? actorRole, CancellationToken ct)
    {
        var member = Guard.NotFound(await db.CorporateUsers.FirstOrDefaultAsync(m => m.Id == id && m.CorporateAccountId == account.Id, ct));
        await membership.EnableAsync(member, actorRole, ct);
        await db.SaveChangesAsync(ct);
        return (await ToDtosAsync(account, [member], ct))[0];
    }

    public async Task<CorporateEmployeeDto> ResendAsync(CorporateAccount account, Guid id, string? actorRole, CancellationToken ct)
    {
        var member = Guard.NotFound(await db.CorporateUsers.FirstOrDefaultAsync(m => m.Id == id && m.CorporateAccountId == account.Id, ct));
        await membership.ResendAsync(account, member, actorRole, ct);
        await db.SaveChangesAsync(ct);
        return (await ToDtosAsync(account, [member], ct))[0];
    }

    public async Task RevokeInvitationAsync(CorporateAccount account, Guid id, string? actorRole, CancellationToken ct)
    {
        var member = Guard.NotFound(await db.CorporateUsers.FirstOrDefaultAsync(m => m.Id == id && m.CorporateAccountId == account.Id, ct));
        await membership.RevokeInvitationAsync(member, actorRole, ct);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>The platform admin invites the first (or another) company admin: <c>POST /admin/corporate/accounts/{id}/admins</c>.</summary>
    public async Task<CorporateEmployeeDto> InviteAdminAsync(CorporateAccount account, InviteAdminRequest request, Guid invitedBy, CancellationToken ct)
    {
        var spec = ParseSpec(request.PhoneNumber, request.FullName, "corporate_admin", null, null, null, null, null, fullNameRequired: true);
        var member = await membership.InviteAsync(account, spec, invitedBy, null, ct);
        await db.SaveChangesAsync(ct);
        return (await ToDtosAsync(account, [member], ct))[0];
    }

    // ----- mapping -----

    public async Task<IReadOnlyList<CorporateEmployeeDto>> ToDtosAsync(CorporateAccount account, IReadOnlyList<CorporateUser> members, CancellationToken ct)
    {
        if (members.Count == 0)
        {
            return [];
        }

        var costCenters = await db.CorporateCostCenters.AsNoTracking().Where(c => c.CorporateAccountId == account.Id).ToDictionaryAsync(c => c.Id, c => c.Code, ct);
        var policies = await db.CorporatePolicies.AsNoTracking().Where(p => p.CorporateAccountId == account.Id).ToDictionaryAsync(p => p.Id, p => p.Name, ct);
        var spent = await exposure.SpentByEmployeeAsync(members.Select(m => m.Id).ToList(), clock.UtcNow, ct);
        var ids = members.Select(m => m.Id).ToList();
        var now = clock.UtcNow;
        var invitations = (await db.CorporateInvitations.AsNoTracking()
                .Where(i => ids.Contains(i.CorporateUserId) && i.AcceptedAt == null && i.DeclinedAt == null && i.RevokedAt == null)
                .Select(i => new { i.CorporateUserId, i.ExpiresAt }).ToListAsync(ct))
            .GroupBy(i => i.CorporateUserId).ToDictionary(g => g.Key, g => g.Max(i => i.ExpiresAt));
        var defaultPolicyName = account.DefaultPolicyId is { } d ? policies.GetValueOrDefault(d) : null;
        return members.Select(m => new CorporateEmployeeDto(
            m.Id, m.FullName, m.PhoneNumber, m.Role, m.EmployeeNumber, m.Department, m.CostCenterId is { } c ? costCenters.GetValueOrDefault(c) : null, m.CostCenterId, m.PolicyId,
            m.PolicyId is { } p ? policies.GetValueOrDefault(p) ?? defaultPolicyName : defaultPolicyName, m.MonthlyBudget, spent.GetValueOrDefault(m.Id), m.Status, m.ActivatedAt,
            m.Status == CorporateUserStatus.Invited ? invitations.GetValueOrDefault(m.Id) : null)).ToList();
    }

    private static MemberSpec ParseSpec(
        string? phoneNumber, string? fullName, string? role, string? employeeNumber, string? department, Guid? costCenterId, Guid? policyId, decimal? monthlyBudget, bool fullNameRequired)
    {
        var v = new Validator();
        var parsedRole = role is null ? CorporateRole.Employee : CorporateDtos.ParseRole(role);
        var phoneOk = PhoneNumber.TryNormalize(phoneNumber, out var phone);
        v.Rule("phoneNumber", phoneOk, "invalid phone number")
            .Rule("role", parsedRole is not null, "must be employee|corporate_admin")
            .Rule("monthlyBudget", monthlyBudget is null || (monthlyBudget >= 0 && decimal.Round(monthlyBudget.Value, 2) == monthlyBudget), "must be a positive amount with at most 2 decimals");
        if (fullNameRequired)
        {
            v.Require("fullName", fullName, 120);
        }

        v.ThrowIfInvalid();
        return new MemberSpec(phone, fullName, parsedRole!.Value, employeeNumber, department, costCenterId, policyId, monthlyBudget);
    }
}
