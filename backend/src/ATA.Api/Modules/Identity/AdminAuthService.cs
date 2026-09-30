using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Domain.Common;
using ATA.Domain.Identity;
using ATA.Infrastructure.Persistence;
using ATA.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Identity;

/// <summary>
/// Admin login (doc 12 §F20.4 / §F20.5): password with lockout, then — when enrolled or required — a TOTP / recovery-code step or the mandatory enrolment,
/// each continued with a short-lived <c>mfaToken</c>. Security events go to <c>audit_logs</c> with the caller's IP.
/// </summary>
public sealed class AdminAuthService(
    AtaDbContext db, IPasswordHasher hasher, IClock clock, IOptions<AdminOptions> options, MfaTokenService mfaTokens, MfaSecretProtector secrets,
    AdminMfaService mfa, AdminSessionIssuer sessions, AuditService audit)
{
    public static readonly IReadOnlyList<string> MfaMethods = ["totp", "recovery_code"];

    /// <summary>
    /// <c>AuthResponse</c> (+ <c>mustChangePassword</c>) without MFA, <see cref="AdminMfaChallengeResponse"/> or <see cref="AdminMfaEnrollmentResponse"/>;
    /// <c>401 invalid_credentials</c>, <c>429 account_locked { retryAfterSeconds }</c> after <c>Admin:MaxFailedLogins</c> wrong passwords, <c>403 account_suspended</c> when disabled.
    /// </summary>
    public async Task<object> LoginAsync(AdminLoginRequest request, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.Username), request.Username, 64)
            .Require(nameof(request.Password), request.Password, 256)
            .ThrowIfInvalid();

        var o = options.Value;
        var username = request.Username!.Trim();
        var account = await db.AdminAccounts.Include(a => a.User).FirstOrDefaultAsync(a => a.Username == username, ct);
        if (account is null)
        {
            audit.Log("admin.login_failed", "admin_account", null, null, new { username, reason = "unknown_username" }, RoleNames.Admin);
            await db.SaveChangesAsync(ct);
            throw new DomainException(ErrorCodes.InvalidCredentials);
        }

        var now = clock.UtcNow;
        if (account.IsLockedAt(now))
        {
            throw Locked(account, now);
        }

        if (!hasher.Verify(request.Password!, account.PasswordHash))
        {
            account.FailedLoginCount++;
            var locked = account.FailedLoginCount >= o.MaxFailedLogins;
            if (locked)
            {
                account.LockedUntil = now.AddMinutes(o.LockoutMinutes);
                account.FailedLoginCount = 0;
            }

            audit.Log("admin.login_failed", "admin_account", account.Id, null, new { username, reason = "invalid_password", locked }, RoleNames.Admin);
            await db.SaveChangesAsync(ct);
            throw locked ? Locked(account, now) : new DomainException(ErrorCodes.InvalidCredentials);
        }

        account.FailedLoginCount = 0;
        account.LockedUntil = null;
        if (!account.IsActive)
        {
            await db.SaveChangesAsync(ct);
            throw new DomainException(ErrorCodes.AccountSuspended, new { status = "disabled" });
        }

        account.User!.EnsureActive();
        if (account.MfaEnabled && account.MfaSecret is not null)
        {
            account.MfaFailedCount = 0;
            await db.SaveChangesAsync(ct);
            return new AdminMfaChallengeResponse(true, mfaTokens.Create(account.Id, MfaStage.Verify), MfaMethods);
        }

        if (o.MfaRequired)
        {
            account.MfaFailedCount = 0;
            await db.SaveChangesAsync(ct);
            return new AdminMfaEnrollmentResponse(true, mfaTokens.Create(account.Id, MfaStage.Enroll));
        }

        return await CompleteAsync(account, "password", null, ct);
    }

    /// <summary><c>POST /auth/admin/mfa/verify</c>: a TOTP code or a recovery code → <c>AuthResponse</c> (+ <c>recoveryCodesRemaining</c>).</summary>
    public async Task<AuthResponse> VerifyAsync(AdminMfaVerifyRequest request, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.MfaToken), request.MfaToken, 2048)
            .Rule(nameof(request.Code), !string.IsNullOrWhiteSpace(request.Code) || !string.IsNullOrWhiteSpace(request.RecoveryCode), "code or recoveryCode is required")
            .Rule(nameof(request.Code), request.Code is null || request.Code.Length <= 16, "max_length:16")
            .Rule(nameof(request.RecoveryCode), request.RecoveryCode is null || request.RecoveryCode.Length <= 32, "max_length:32")
            .ThrowIfInvalid();

        var account = await LoadAsync(mfaTokens.Read(request.MfaToken, MfaStage.Verify), ct);
        if (!account.MfaEnabled || account.MfaSecret is null)
        {
            throw new DomainException(ErrorCodes.Unauthorized);
        }

        mfa.EnsureNotLocked(account);
        string method;
        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            method = "totp";
            if (!mfa.TryAcceptTotp(account, request.Code))
            {
                throw await mfa.FailAsync(account, ct);
            }
        }
        else
        {
            method = "recovery_code";
            if (!await mfa.TryUseRecoveryCodeAsync(account, request.RecoveryCode, ct))
            {
                throw await mfa.FailAsync(account, ct);
            }
        }

        account.MfaFailedCount = 0;
        await db.SaveChangesAsync(ct);
        var remaining = await mfa.RemainingRecoveryCodesAsync(account.Id, ct);
        return await CompleteAsync(account, method, remaining, ct);
    }

    /// <summary><c>POST /auth/admin/mfa/enroll</c>: a new 20-byte Base32 secret (stored encrypted, not active until confirmed) and its <c>otpauth://</c> URI.</summary>
    public async Task<AdminMfaEnrollResponse> EnrollAsync(AdminMfaEnrollRequest request, CancellationToken ct)
    {
        new Validator().Require(nameof(request.MfaToken), request.MfaToken, 2048).ThrowIfInvalid();
        var account = await LoadAsync(mfaTokens.Read(request.MfaToken, MfaStage.Enroll), ct);
        if (account.MfaEnabled)
        {
            throw new DomainException(ErrorCodes.Unauthorized);
        }

        var secret = Totp.NewSecret();
        account.MfaSecret = secrets.Protect(secret);
        account.MfaLastStep = null;
        await db.SaveChangesAsync(ct);
        return new AdminMfaEnrollResponse(secret, AdminMfaService.OtpAuthUri(account.Username, secret));
    }

    /// <summary><c>POST /auth/admin/mfa/enroll/confirm</c>: the first valid code activates MFA → 10 recovery codes (shown once) + <c>AuthResponse</c>.</summary>
    public async Task<AdminMfaEnrollConfirmResponse> ConfirmEnrollmentAsync(AdminMfaEnrollConfirmRequest request, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.MfaToken), request.MfaToken, 2048)
            .Require(nameof(request.Code), request.Code, 16)
            .ThrowIfInvalid();

        var account = await LoadAsync(mfaTokens.Read(request.MfaToken, MfaStage.Enroll), ct);
        if (account.MfaEnabled)
        {
            throw new DomainException(ErrorCodes.Unauthorized);
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

        var now = clock.UtcNow;
        account.MfaEnabled = true;
        account.MfaEnrolledAt = now;
        account.MfaFailedCount = 0;
        var codes = await mfa.RegenerateRecoveryCodesAsync(account, ct);
        audit.Log("admin.mfa_enrolled", "admin_account", account.Id, null, new { account.MfaEnrolledAt }, RoleNames.Admin, account.UserId);
        var auth = await CompleteAsync(account, "mfa_enrollment", codes.Count, ct);
        return new AdminMfaEnrollConfirmResponse(codes, auth);
    }

    private async Task<AdminAccount> LoadAsync(Guid accountId, CancellationToken ct)
    {
        var account = await db.AdminAccounts.Include(a => a.User).FirstOrDefaultAsync(a => a.Id == accountId, ct) ?? throw new DomainException(ErrorCodes.Unauthorized);
        if (!account.IsActive)
        {
            throw new DomainException(ErrorCodes.AccountSuspended, new { status = "disabled" });
        }

        account.User!.EnsureActive();
        return account;
    }

    private async Task<AuthResponse> CompleteAsync(AdminAccount account, string method, int? recoveryCodesRemaining, CancellationToken ct)
    {
        var now = clock.UtcNow;
        account.LastLoginAt = now;
        account.User!.LastLoginAt = now;
        audit.Log("admin.login", "admin_account", account.Id, null, new { method }, RoleNames.Admin, account.UserId);
        var response = await sessions.IssueAsync(account.User, account, null, ct);
        await db.SaveChangesAsync(ct);
        return response with { RecoveryCodesRemaining = recoveryCodesRemaining };
    }

    private static DomainException Locked(AdminAccount account, DateTime now) =>
        new(ErrorCodes.AccountLocked, new { retryAfterSeconds = (int)Math.Ceiling((account.LockedUntil!.Value - now).TotalSeconds) });
}
