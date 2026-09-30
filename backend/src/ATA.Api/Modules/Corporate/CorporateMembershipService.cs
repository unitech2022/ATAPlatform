using System.Globalization;
using System.Text;
using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Api.Modules.Notifications;
using ATA.Domain.Common;
using ATA.Domain.Corporate;
using ATA.Domain.Identity;
using ATA.Domain.Notifications;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Corporate;

/// <summary>Validated input of an invitation (employee or company admin).</summary>
public sealed record MemberSpec(
    string PhoneNumber, string? FullName, CorporateRole Role, string? EmployeeNumber, string? Department, Guid? CostCenterId, Guid? PolicyId, decimal? MonthlyBudget);

/// <summary>
/// Membership of companies (doc 12 §F19.2): invitations by phone number (SMS + in-app/push for existing users), acceptance in the rider app (or implicitly at the first
/// portal sign-in of an admin), the one-company-per-user rule, disabling and the protection of the last admin. Every method adds to the caller's unit of work.
/// </summary>
public sealed class CorporateMembershipService(
    AtaDbContext db, IClock clock, INotificationDispatcher notifications, AuditService audit, IOptions<CorporateOptions> options)
{
    private readonly CorporateOptions _options = options.Value;

    public const string AdminActor = "corporate_admin";

    // ----- invitations -----

    /// <summary>Checks the optional references of a member spec (cost centre / policy of the same company, non-negative budget).</summary>
    public async Task ValidateSpecAsync(Guid accountId, MemberSpec spec, CancellationToken ct)
    {
        var v = new Validator()
            .Rule("monthlyBudget", spec.MonthlyBudget is null or >= 0, "must be positive")
            .Rule("employeeNumber", spec.EmployeeNumber is null || spec.EmployeeNumber.Length <= 40, "max_length:40")
            .Rule("department", spec.Department is null || spec.Department.Length <= 80, "max_length:80")
            .Rule("fullName", spec.FullName is null || spec.FullName.Length <= 120, "max_length:120");
        if (spec.CostCenterId is { } costCenterId)
        {
            v.Rule("costCenterId", await db.CorporateCostCenters.AnyAsync(c => c.Id == costCenterId && c.CorporateAccountId == accountId, ct), "unknown cost center");
        }

        if (spec.PolicyId is { } policyId)
        {
            v.Rule("policyId", await db.CorporatePolicies.AnyAsync(p => p.Id == policyId && p.CorporateAccountId == accountId, ct), "unknown policy");
        }

        v.ThrowIfInvalid();
    }

    public async Task<CorporateUser> InviteAsync(CorporateAccount account, MemberSpec spec, Guid? invitedBy, string? actorRole, CancellationToken ct)
    {
        await ValidateSpecAsync(account.Id, spec, ct);
        if (await db.CorporateUsers.AnyAsync(m => m.CorporateAccountId == account.Id && m.PhoneNumber == spec.PhoneNumber, ct)
            || db.CorporateUsers.Local.Any(m => m.CorporateAccountId == account.Id && m.PhoneNumber == spec.PhoneNumber))
        {
            throw new DomainException(ErrorCodes.Conflict, new { reason = "already_member" });
        }

        var member = new CorporateUser
        {
            CorporateAccountId = account.Id, PhoneNumber = spec.PhoneNumber, FullName = CorporateRules.Clean(spec.FullName), Role = spec.Role,
            EmployeeNumber = CorporateRules.Clean(spec.EmployeeNumber), Department = CorporateRules.Clean(spec.Department), CostCenterId = spec.CostCenterId,
            PolicyId = spec.PolicyId, MonthlyBudget = spec.MonthlyBudget, Status = CorporateUserStatus.Invited, InvitedBy = invitedBy,
        };
        db.CorporateUsers.Add(member);
        await SendInvitationAsync(account, member, ct);
        audit.Log("corporate_user.invite", "corporate_account", member.CorporateAccountId, null, Snapshot(member), actorRole);
        return member;
    }

    /// <summary><c>POST …/resend-invitation</c>: revokes the open invitations of the member and sends a fresh one (only while the member is <c>invited</c>).</summary>
    public async Task ResendAsync(CorporateAccount account, CorporateUser member, string? actorRole, CancellationToken ct)
    {
        if (member.Status != CorporateUserStatus.Invited)
        {
            throw new DomainException(ErrorCodes.Conflict, new { reason = "not_invited", status = member.Status });
        }

        var now = clock.UtcNow;
        foreach (var open in await db.CorporateInvitations.Where(i => i.CorporateUserId == member.Id && i.AcceptedAt == null && i.DeclinedAt == null && i.RevokedAt == null).ToListAsync(ct))
        {
            open.RevokedAt = now;
        }

        await SendInvitationAsync(account, member, ct);
        audit.Log("corporate_user.invite", "corporate_account", member.CorporateAccountId, null, new { memberId = member.Id, resend = true, member.PhoneNumber }, actorRole);
    }

    /// <summary><c>DELETE …/employees/{id}</c>: cancels the invitation of a member who never accepted (the row and its invitations are removed).</summary>
    public Task RevokeInvitationAsync(CorporateUser member, string? actorRole, CancellationToken ct)
    {
        if (member.Status != CorporateUserStatus.Invited)
        {
            throw new DomainException(ErrorCodes.Conflict, new { reason = "not_invited", status = member.Status });
        }

        audit.Log("corporate_user.update", "corporate_account", member.CorporateAccountId, Snapshot(member), new { memberId = member.Id, invitationRevoked = true }, actorRole);
        db.CorporateUsers.Remove(member);
        return Task.CompletedTask;
    }

    private async Task SendInvitationAsync(CorporateAccount account, CorporateUser member, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var token = CorporateRules.NewToken();
        var invitation = new CorporateInvitation
        {
            CorporateAccountId = account.Id, CorporateUserId = member.Id, PhoneNumber = member.PhoneNumber, TokenHash = CorporateRules.HashToken(token),
            ExpiresAt = now.AddDays(_options.InvitationDays), SentAt = now,
        };
        db.CorporateInvitations.Add(invitation);
        var existingUser = await db.Users.AsNoTracking().Where(u => u.PhoneNumber == member.PhoneNumber).Select(u => new { u.Id }).FirstOrDefaultAsync(ct);
        var joinUrl = $"{_options.PortalBaseUrl.TrimEnd('/')}/business/join/{token}";
        await notifications.DispatchAsync(new NotificationRequest(
            "corporate.invitation", existingUser?.Id ?? Guid.Empty,
            NotificationPlaceholders.Of(("companyName", account.DisplayName), ("joinUrl", joinUrl)), "corporate_invitation", invitation.Id,
            new Dictionary<string, object?> { ["invitationId"] = invitation.Id, ["accountId"] = account.Id, ["role"] = member.Role },
            RecipientPhoneOverride: member.PhoneNumber), ct);
    }

    // ----- acceptance -----

    /// <summary>The open, unexpired invitations addressed to the verified phone number of <paramref name="phoneNumber"/>.</summary>
    public async Task<IReadOnlyList<CorporateInvitationDto>> InvitationsForAsync(string phoneNumber, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var rows = await (from i in db.CorporateInvitations.AsNoTracking()
                          join m in db.CorporateUsers.AsNoTracking() on i.CorporateUserId equals m.Id
                          join a in db.CorporateAccounts.AsNoTracking() on i.CorporateAccountId equals a.Id
                          where i.PhoneNumber == phoneNumber && i.AcceptedAt == null && i.DeclinedAt == null && i.RevokedAt == null && i.ExpiredAt == null && i.ExpiresAt > now
                                && m.Status == CorporateUserStatus.Invited && a.Status != CorporateAccountStatus.Closed
                          orderby i.SentAt descending
                          select new { i.Id, a.DisplayName, m.Role, i.ExpiresAt }).ToListAsync(ct);
        return rows.Select(r => new CorporateInvitationDto(r.Id, r.DisplayName, CorporateDtos.RoleName(r.Role), r.ExpiresAt)).ToList();
    }

    public async Task AcceptAsync(Guid invitationId, User user, CancellationToken ct)
    {
        var (invitation, member, account) = await LoadInvitationAsync(invitationId, user.PhoneNumber, ct);
        var now = clock.UtcNow;
        if (invitation.IsExpiredAt(now))
        {
            throw new DomainException(ErrorCodes.InvitationExpired, new { invitation.ExpiresAt });
        }

        if (!invitation.IsOpen || member.Status != CorporateUserStatus.Invited)
        {
            throw new DomainException(ErrorCodes.Conflict, new { reason = "invitation_closed" });
        }

        if (account.Status == CorporateAccountStatus.Closed)
        {
            throw new DomainException(ErrorCodes.CorporateAccountInactive, new { status = account.Status });
        }

        await ActivateAsync(member, user, now, "corporate_user.accept", null, ct);
    }

    public async Task DeclineAsync(Guid invitationId, User user, CancellationToken ct)
    {
        var (invitation, member, _) = await LoadInvitationAsync(invitationId, user.PhoneNumber, ct);
        var now = clock.UtcNow;
        if (invitation.IsExpiredAt(now))
        {
            throw new DomainException(ErrorCodes.InvitationExpired, new { invitation.ExpiresAt });
        }

        if (!invitation.IsOpen || member.Status != CorporateUserStatus.Invited)
        {
            throw new DomainException(ErrorCodes.Conflict, new { reason = "invitation_closed" });
        }

        invitation.DeclinedAt = now;
        audit.Log("corporate_user.decline", "corporate_account", member.CorporateAccountId, null, new { memberId = member.Id, member.PhoneNumber }, null);
    }

    private async Task<(CorporateInvitation Invitation, CorporateUser Member, CorporateAccount Account)> LoadInvitationAsync(Guid invitationId, string phoneNumber, CancellationToken ct)
    {
        var invitation = Guard.NotFound(await db.CorporateInvitations.FirstOrDefaultAsync(i => i.Id == invitationId && i.PhoneNumber == phoneNumber, ct));
        var member = await db.CorporateUsers.FirstAsync(m => m.Id == invitation.CorporateUserId, ct);
        var account = await db.CorporateAccounts.FirstAsync(a => a.Id == invitation.CorporateAccountId, ct);
        return (invitation, member, account);
    }

    /// <summary>
    /// <c>invited → active</c>: binds the <c>users</c> row, stamps <c>activated_at</c>, closes the member's open invitations and grants the <c>corporate_admin</c> role to
    /// admins. A user is an active member of one company only (<c>409 corporate_member_elsewhere</c>).
    /// </summary>
    public async Task ActivateAsync(CorporateUser member, User user, DateTime now, string auditAction, string? actorRole, CancellationToken ct)
    {
        await EnsureNotMemberElsewhereAsync(user.Id, member.Id, ct);
        member.UserId = user.Id;
        member.Status = CorporateUserStatus.Active;
        member.ActivatedAt ??= now;
        member.DisabledAt = null;
        member.FullName ??= user.FullName;
        foreach (var open in await db.CorporateInvitations.Where(i => i.CorporateUserId == member.Id && i.AcceptedAt == null && i.DeclinedAt == null && i.RevokedAt == null).ToListAsync(ct))
        {
            open.AcceptedAt = now;
        }

        if (member.Role == CorporateRole.CorporateAdmin)
        {
            GrantAdminRole(user, now);
        }

        audit.Log(auditAction, "corporate_account", member.CorporateAccountId, null, new { memberId = member.Id, member.Role, userId = user.Id }, actorRole ?? (member.Role == CorporateRole.CorporateAdmin ? AdminActor : null));
    }

    public async Task EnsureNotMemberElsewhereAsync(Guid userId, Guid exceptMemberId, CancellationToken ct)
    {
        if (await db.CorporateUsers.AnyAsync(m => m.UserId == userId && m.Status == CorporateUserStatus.Active && m.Id != exceptMemberId, ct))
        {
            throw new DomainException(ErrorCodes.CorporateMemberElsewhere);
        }
    }

    private void GrantAdminRole(User user, DateTime now)
    {
        if (!user.HasRole(Role.CorporateAdmin) && !db.UserRoles.Local.Any(r => r.UserId == user.Id && r.Role == Role.CorporateAdmin))
        {
            db.UserRoles.Add(new UserRole { UserId = user.Id, Role = Role.CorporateAdmin, GrantedAt = now });
        }
    }

    private async Task RevokeAdminRoleAsync(Guid userId, CancellationToken ct)
    {
        // The role stays while the user administers another company; only one active membership exists, so it can go.
        foreach (var role in await db.UserRoles.Where(r => r.UserId == userId && r.Role == Role.CorporateAdmin).ToListAsync(ct))
        {
            db.UserRoles.Remove(role);
        }
    }

    // ----- lifecycle -----

    public async Task<bool> IsLastActiveAdminAsync(CorporateUser member, CancellationToken ct) =>
        member.Role == CorporateRole.CorporateAdmin && member.Status == CorporateUserStatus.Active
        && !await db.CorporateUsers.AnyAsync(m => m.CorporateAccountId == member.CorporateAccountId && m.Id != member.Id && m.Role == CorporateRole.CorporateAdmin && m.Status == CorporateUserStatus.Active, ct);

    public async Task DisableAsync(CorporateUser member, string? actorRole, CancellationToken ct)
    {
        if (member.Status != CorporateUserStatus.Active)
        {
            throw new DomainException(ErrorCodes.Conflict, new { reason = "not_active", status = member.Status });
        }

        if (await IsLastActiveAdminAsync(member, ct))
        {
            throw new DomainException(ErrorCodes.Conflict, new { reason = "last_admin" });
        }

        var before = Snapshot(member);
        member.Status = CorporateUserStatus.Disabled;
        member.DisabledAt = clock.UtcNow;
        audit.Log("corporate_user.disable", "corporate_account", member.CorporateAccountId, before, Snapshot(member), actorRole);
    }

    public async Task EnableAsync(CorporateUser member, string? actorRole, CancellationToken ct)
    {
        if (member.Status != CorporateUserStatus.Disabled)
        {
            throw new DomainException(ErrorCodes.Conflict, new { reason = "not_disabled", status = member.Status });
        }

        if (member.UserId is { } userId)
        {
            await EnsureNotMemberElsewhereAsync(userId, member.Id, ct);
        }

        var before = Snapshot(member);
        member.Status = member.UserId is null ? CorporateUserStatus.Invited : CorporateUserStatus.Active;
        member.DisabledAt = null;
        audit.Log("corporate_user.enable", "corporate_account", member.CorporateAccountId, before, Snapshot(member), actorRole);
    }

    public async Task UpdateAsync(CorporateUser member, MemberSpec spec, string? actorRole, CancellationToken ct)
    {
        await ValidateSpecAsync(member.CorporateAccountId, spec, ct);
        var before = Snapshot(member);
        if (member.Role == CorporateRole.CorporateAdmin && spec.Role == CorporateRole.Employee && await IsLastActiveAdminAsync(member, ct))
        {
            throw new DomainException(ErrorCodes.Conflict, new { reason = "last_admin" });
        }

        var roleChanged = member.Role != spec.Role;
        member.FullName = CorporateRules.Clean(spec.FullName) ?? member.FullName;
        member.Role = spec.Role;
        member.EmployeeNumber = CorporateRules.Clean(spec.EmployeeNumber);
        member.Department = CorporateRules.Clean(spec.Department);
        member.CostCenterId = spec.CostCenterId;
        member.PolicyId = spec.PolicyId;
        member.MonthlyBudget = spec.MonthlyBudget;
        if (roleChanged && member.UserId is { } userId && member.Status == CorporateUserStatus.Active)
        {
            if (spec.Role == CorporateRole.CorporateAdmin)
            {
                var user = await db.Users.FirstAsync(u => u.Id == userId, ct);
                GrantAdminRole(user, clock.UtcNow);
            }
            else
            {
                await RevokeAdminRoleAsync(userId, ct);
            }
        }

        audit.Log("corporate_user.update", "corporate_account", member.CorporateAccountId, before, Snapshot(member), actorRole);
    }

    // ----- CSV import (POST /corporate/employees/import) -----

    /// <summary>
    /// Columns <c>phone_number,full_name,employee_number,department,cost_center_code,monthly_budget,role</c> (header required, UTF-8). Valid rows are invited, the others are reported with a
    /// reason code (<c>phone_invalid, full_name_required, duplicate_in_file, already_member, role_invalid, cost_center_unknown, monthly_budget_invalid, field_too_long</c>);
    /// <c>row</c> is the spreadsheet row (the header is row 1). Nothing is saved: the caller commits.
    /// </summary>
    public async Task<ImportResultDto> ImportAsync(CorporateAccount account, Stream csv, Guid invitedBy, CancellationToken ct)
    {
        var rows = CsvReader.Read(csv);
        if (rows.Count == 0)
        {
            throw new DomainException(ErrorCodes.ValidationFailed, new { file = "empty" });
        }

        var header = rows[0].Select(h => h.Trim().ToLowerInvariant()).ToList();
        if (!header.Contains("phone_number"))
        {
            throw new DomainException(ErrorCodes.ValidationFailed, new { file = "missing_column:phone_number" });
        }

        var data = rows.Skip(1).Where(r => r.Any(c => !string.IsNullOrWhiteSpace(c))).ToList();
        if (data.Count > _options.MaxImportRows)
        {
            throw new DomainException(ErrorCodes.ValidationFailed, new { file = $"too_many_rows:{_options.MaxImportRows}" });
        }

        string Cell(IReadOnlyList<string> row, string name)
        {
            var index = header.IndexOf(name);
            return index >= 0 && index < row.Count ? row[index].Trim() : string.Empty;
        }

        var existing = (await db.CorporateUsers.AsNoTracking().Where(m => m.CorporateAccountId == account.Id).Select(m => m.PhoneNumber).ToListAsync(ct)).ToHashSet();
        var costCenters = await db.CorporateCostCenters.AsNoTracking().Where(c => c.CorporateAccountId == account.Id).ToDictionaryAsync(c => c.Code.ToLowerInvariant(), c => c.Id, ct);
        var seen = new HashSet<string>();
        var skipped = new List<ImportSkippedDto>();
        var created = 0;
        for (var i = 0; i < data.Count; i++)
        {
            var row = data[i];
            var rowNumber = rows.IndexOf(row) + 1;
            string? reason = null;
            MemberSpec? spec = null;
            if (!PhoneNumber.TryNormalize(Cell(row, "phone_number"), out var phone))
            {
                reason = "phone_invalid";
            }
            else if (string.IsNullOrWhiteSpace(Cell(row, "full_name")))
            {
                reason = "full_name_required";
            }
            else if (!seen.Add(phone))
            {
                reason = "duplicate_in_file";
            }
            else if (existing.Contains(phone))
            {
                reason = "already_member";
            }
            else
            {
                var roleText = Cell(row, "role");
                var role = string.IsNullOrEmpty(roleText) ? CorporateRole.Employee : CorporateDtos.ParseRole(roleText);
                var budgetText = Cell(row, "monthly_budget");
                decimal? budget = null;
                Guid? costCenterId = null;
                if (role is null) reason = "role_invalid";
                else if (budgetText.Length > 0 && (!decimal.TryParse(budgetText, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) || parsed < 0 || decimal.Round(parsed, 2) != parsed)) reason = "monthly_budget_invalid";
                else if (Cell(row, "cost_center_code") is { Length: > 0 } code && !costCenters.TryGetValue(code.ToLowerInvariant(), out var costCenter)) reason = "cost_center_unknown";
                else if (Cell(row, "full_name").Length > 120 || Cell(row, "employee_number").Length > 40 || Cell(row, "department").Length > 80) reason = "field_too_long";
                else
                {
                    if (budgetText.Length > 0) budget = decimal.Parse(budgetText, NumberStyles.Number, CultureInfo.InvariantCulture);
                    if (Cell(row, "cost_center_code") is { Length: > 0 } c) costCenterId = costCenters[c.ToLowerInvariant()];
                    spec = new MemberSpec(phone, Cell(row, "full_name"), role!.Value, Cell(row, "employee_number"), Cell(row, "department"), costCenterId, null, budget);
                }
            }

            if (spec is null)
            {
                skipped.Add(new ImportSkippedDto(rowNumber, reason!));
                continue;
            }

            await InviteAsync(account, spec, invitedBy, AdminActor, ct);
            created++;
        }

        return new ImportResultDto(created, skipped);
    }

    private static object Snapshot(CorporateUser m) => new { memberId = m.Id, m.CorporateAccountId, m.PhoneNumber, m.Role, m.Status, m.EmployeeNumber, m.Department, m.CostCenterId, m.PolicyId, m.MonthlyBudget };
}

/// <summary>RFC 4180-style CSV reader (quoted fields, doubled quotes, CRLF, leading BOM).</summary>
public static class CsvReader
{
    public static List<List<string>> Read(Stream stream)
    {
        using var reader = new StreamReader(stream, new UTF8Encoding(false), detectEncodingFromByteOrderMarks: true);
        var text = reader.ReadToEnd();
        var rows = new List<List<string>>();
        var row = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (quoted)
            {
                if (c == '"' && i + 1 < text.Length && text[i + 1] == '"')
                {
                    field.Append('"');
                    i++;
                }
                else if (c == '"')
                {
                    quoted = false;
                }
                else
                {
                    field.Append(c);
                }

                continue;
            }

            switch (c)
            {
                case '"' when field.Length == 0:
                    quoted = true;
                    break;
                case ',':
                    row.Add(field.ToString());
                    field.Clear();
                    break;
                case '\r':
                    break;
                case '\n':
                    row.Add(field.ToString());
                    field.Clear();
                    rows.Add(row);
                    row = [];
                    break;
                default:
                    field.Append(c);
                    break;
            }
        }

        if (field.Length > 0 || row.Count > 0)
        {
            row.Add(field.ToString());
            rows.Add(row);
        }

        return rows;
    }
}
