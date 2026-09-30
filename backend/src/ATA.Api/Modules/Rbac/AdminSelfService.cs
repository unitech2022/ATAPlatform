using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Api.Modules.Identity;
using ATA.Domain.Common;
using ATA.Domain.Identity;
using ATA.Infrastructure.Persistence;
using ATA.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Rbac;

/// <summary>Shared reads of the RBAC module: role references per account and the live admin sessions of a user.</summary>
public sealed class AdminRbacQueries(AtaDbContext db, AdminSessionIssuer sessions, IClock clock)
{
    public async Task<Dictionary<Guid, List<RoleRefDto>>> RoleRefsAsync(IReadOnlyCollection<Guid> accountIds, Language lang, CancellationToken ct)
    {
        var rows = await (from ar in db.AdminAccountRoles.AsNoTracking()
                          join r in db.Roles.AsNoTracking() on ar.RoleId equals r.Id
                          where accountIds.Contains(ar.AdminAccountId)
                          select new { ar.AdminAccountId, r.Id, r.Code, r.NameAr, r.NameEn }).ToListAsync(ct);
        return rows.GroupBy(r => r.AdminAccountId)
            .ToDictionary(g => g.Key, g => g.OrderBy(r => r.Code).Select(r => new RoleRefDto(r.Id, r.Code, lang.Pick(r.NameAr, r.NameEn))).ToList());
    }

    /// <summary>Admin sessions of <paramref name="userId"/> that can still be refreshed (not revoked, not idle, not past the absolute end).</summary>
    public async Task<List<AdminSessionDto>> SessionsAsync(Guid userId, Guid? currentSessionId, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var tokens = await db.RefreshTokens.AsNoTracking()
            .Where(t => t.UserId == userId && t.SessionKind == SessionKind.Admin && t.RevokedAt == null && t.ExpiresAt > now).ToListAsync(ct);
        return tokens.Where(t => sessions.IsAlive(t, now))
            .OrderByDescending(t => t.LastUsedAt ?? t.CreatedAt)
            .Select(t => new AdminSessionDto(t.Id, t.UserAgent, t.CreatedByIp, t.CreatedAt, t.LastUsedAt, t.Id == currentSessionId)).ToList();
    }

    /// <summary>Revokes every refresh token of the user (all kinds, doc 12 §F20.3); <paramref name="except"/> keeps the current session.</summary>
    public Task<int> RevokeAllAsync(Guid userId, Guid? except, CancellationToken ct)
    {
        var now = clock.UtcNow;
        return db.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null && (except == null || t.Id != except))
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
    }
}

/// <summary><c>/admin/me*</c> (doc 12 §F20.5): any admin — profile with permissions, password change, recovery codes and own sessions.</summary>
public sealed class AdminSelfService(
    AtaDbContext db, ICurrentUser currentUser, IPasswordHasher hasher, IClock clock, AdminMfaService mfa, MfaSecretProtector secrets, AdminRbacQueries queries, AuditService audit)
{
    public async Task<AdminMeResponse> GetAsync(Language lang, CancellationToken ct)
    {
        var account = await CurrentAccountAsync(tracking: false, ct);
        var roles = await queries.RoleRefsAsync([account.Id], lang, ct);
        var permissions = await AdminPermissions.OfAccountAsync(db, account.Id, ct);
        return new AdminMeResponse(account.Id, account.UserId, account.Username, account.User!.FullName, roles.GetValueOrDefault(account.Id) ?? [], permissions,
            account.MfaEnabled, account.MustChangePassword, account.OnDuty);
    }

    /// <summary>
    /// Verifies the current password (<c>400 invalid_credentials</c> — not 401, the session is fine), applies the policy, clears <c>must_change_password</c> and
    /// revokes the user's other sessions (the current one stays valid; audit <c>admin.password_changed</c>).
    /// </summary>
    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.CurrentPassword), request.CurrentPassword, 256)
            .Require(nameof(request.NewPassword), request.NewPassword, 256)
            .ThrowIfInvalid();

        var account = await CurrentAccountAsync(tracking: true, ct);
        if (!hasher.Verify(request.CurrentPassword!, account.PasswordHash))
        {
            throw new DomainException(ErrorCodes.InvalidCredentials, new { field = "currentPassword" }, StatusCodes.Status400BadRequest);
        }

        PasswordPolicy.Validate(request.NewPassword);
        if (request.NewPassword == request.CurrentPassword)
        {
            throw new DomainException(ErrorCodes.PasswordPolicyViolation, new { rules = new[] { "different_from_current" } });
        }

        account.PasswordHash = hasher.Hash(request.NewPassword!);
        account.MustChangePassword = false;
        account.PasswordChangedAt = clock.UtcNow;
        audit.Log("admin.password_changed", "admin_account", account.Id, null, new { account.PasswordChangedAt });
        await db.SaveChangesAsync(ct);
        await queries.RevokeAllAsync(account.UserId, currentUser.SessionId, ct);
    }

    /// <summary>A valid TOTP code replaces the recovery codes with 10 new ones (<c>409 conflict</c> without MFA).</summary>
    public async Task<RecoveryCodesResponse> RegenerateRecoveryCodesAsync(RecoveryCodesRequest request, CancellationToken ct)
    {
        new Validator().Require(nameof(request.Code), request.Code, 16).ThrowIfInvalid();
        var account = await CurrentAccountAsync(tracking: true, ct);
        if (!account.MfaEnabled || account.MfaSecret is null)
        {
            throw new DomainException(ErrorCodes.Conflict, new { reason = "mfa_not_enabled" });
        }

        mfa.EnsureNotLocked(account);
        if (!mfa.TryAcceptTotp(account, request.Code))
        {
            throw await mfa.FailAsync(account, ct);
        }

        account.MfaFailedCount = 0;
        var codes = await mfa.RegenerateRecoveryCodesAsync(account, ct);
        audit.Log("admin.recovery_codes_regenerated", "admin_account", account.Id);
        await db.SaveChangesAsync(ct);
        return new RecoveryCodesResponse(codes);
    }

    /// <summary>
    /// <c>POST /admin/me/mfa/enroll</c> (dashboard addition): voluntary enrolment of a signed-in admin — a new pending secret (<c>409 conflict { reason:
    /// "mfa_already_enabled" }</c> once MFA is on; reset it through an admin first).
    /// </summary>
    public async Task<AdminMfaEnrollResponse> StartEnrollmentAsync(CancellationToken ct)
    {
        var account = await CurrentAccountAsync(tracking: true, ct);
        if (account.MfaEnabled)
        {
            throw new DomainException(ErrorCodes.Conflict, new { reason = "mfa_already_enabled" });
        }

        var secret = ATA.Infrastructure.Security.Totp.NewSecret();
        account.MfaSecret = secrets.Protect(secret);
        account.MfaLastStep = null;
        await db.SaveChangesAsync(ct);
        return new AdminMfaEnrollResponse(secret, AdminMfaService.OtpAuthUri(account.Username, secret));
    }

    /// <summary><c>POST /admin/me/mfa/enroll/confirm</c>: the first valid code activates MFA → 10 recovery codes (shown once).</summary>
    public async Task<RecoveryCodesResponse> ConfirmEnrollmentAsync(RecoveryCodesRequest request, CancellationToken ct)
    {
        new Validator().Require(nameof(request.Code), request.Code, 16).ThrowIfInvalid();
        var account = await CurrentAccountAsync(tracking: true, ct);
        if (account.MfaEnabled)
        {
            throw new DomainException(ErrorCodes.Conflict, new { reason = "mfa_already_enabled" });
        }

        if (account.MfaSecret is null)
        {
            throw new DomainException(ErrorCodes.Conflict, new { reason = "enrollment_not_started" });
        }

        mfa.EnsureNotLocked(account);
        if (!mfa.TryAcceptTotp(account, request.Code))
        {
            throw await mfa.FailAsync(account, ct);
        }

        account.MfaEnabled = true;
        account.MfaEnrolledAt = clock.UtcNow;
        account.MfaFailedCount = 0;
        var codes = await mfa.RegenerateRecoveryCodesAsync(account, ct);
        audit.Log("admin.mfa_enrolled", "admin_account", account.Id, null, new { account.MfaEnrolledAt });
        await db.SaveChangesAsync(ct);
        return new RecoveryCodesResponse(codes);
    }

    public Task<List<AdminSessionDto>> SessionsAsync(CancellationToken ct) => queries.SessionsAsync(currentUser.UserId, currentUser.SessionId, ct);

    public async Task RevokeSessionAsync(Guid sessionId, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var token = await db.RefreshTokens.FirstOrDefaultAsync(t => t.Id == sessionId && t.UserId == userId && t.SessionKind == SessionKind.Admin, ct)
            ?? throw new DomainException(ErrorCodes.NotFound);
        token.RevokedAt ??= clock.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task RevokeOtherSessionsAsync(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var current = currentUser.SessionId;
        var now = clock.UtcNow;
        await db.RefreshTokens.Where(t => t.UserId == userId && t.SessionKind == SessionKind.Admin && t.RevokedAt == null && (current == null || t.Id != current))
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
    }

    private async Task<AdminAccount> CurrentAccountAsync(bool tracking, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var query = db.AdminAccounts.Include(a => a.User).Where(a => a.UserId == userId);
        return await (tracking ? query : query.AsNoTracking()).FirstOrDefaultAsync(ct) ?? throw new DomainException(ErrorCodes.NotFound);
    }
}
