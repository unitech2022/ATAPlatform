using System.Security.Cryptography;
using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Api.Modules.Rbac;
using ATA.Domain.Common;
using ATA.Domain.Identity;
using ATA.Domain.Rbac;
using ATA.Infrastructure.Persistence;
using ATA.Infrastructure.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Identity;

/// <summary><c>Admin:*</c> keys of doc 12 §F20.8 (the seed account keys <c>Admin:Username</c>/<c>Password</c>/<c>PhoneNumber</c> live in the same section).</summary>
public sealed class AdminOptions
{
    public const string Section = "Admin";

    /// <summary>Every admin must enrol TOTP (<c>false</c> in <c>appsettings.Development.json</c> so the seed admin signs in with the password only).</summary>
    public bool MfaRequired { get; set; } = true;
    public int MfaTokenMinutes { get; set; } = 5;
    public int MaxFailedLogins { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
    /// <summary>Wrong MFA codes accepted for one login before <c>429 mfa_locked</c> (a new password login starts over).</summary>
    public int MaxMfaAttempts { get; set; } = 5;
    public int AccessTokenMinutes { get; set; } = 15;
    public int SessionAbsoluteHours { get; set; } = 12;
    public int SessionIdleMinutes { get; set; } = 30;
    /// <summary><c>AdminSessionCleanupJob</c> (hourly); <c>false</c> in tests (they call <c>RunOnceAsync</c>).</summary>
    public bool JobsEnabled { get; set; } = true;
}

public enum MfaStage { Verify, Enroll }

/// <summary>
/// The short-lived <c>mfaToken</c> (doc 12 §F20.4): a Data Protection payload with the admin account id, the stage (<c>verify</c>/<c>enroll</c>) and an expiry
/// checked against <see cref="IClock"/> (<c>Admin:MfaTokenMinutes</c>). Anything unreadable, expired or of the other stage → <c>401 unauthorized</c>.
/// </summary>
public sealed class MfaTokenService(IDataProtectionProvider protection, IClock clock, IOptions<AdminOptions> options)
{
    private readonly IDataProtector _protector = protection.CreateProtector("ATA.Admin.MfaToken.v1");

    public string Create(Guid adminAccountId, MfaStage stage)
    {
        var expires = clock.UtcNow.AddMinutes(options.Value.MfaTokenMinutes);
        return _protector.Protect($"{adminAccountId:N}|{stage}|{expires.Ticks}|{Guid.NewGuid():N}");
    }

    public Guid Read(string? token, MfaStage stage)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new DomainException(ErrorCodes.Unauthorized);
        }

        string payload;
        try
        {
            payload = _protector.Unprotect(token.Trim());
        }
        catch (CryptographicException)
        {
            throw new DomainException(ErrorCodes.Unauthorized);
        }
        catch (FormatException)
        {
            throw new DomainException(ErrorCodes.Unauthorized);
        }

        var parts = payload.Split('|');
        if (parts.Length != 4 || !Guid.TryParseExact(parts[0], "N", out var accountId) || parts[1] != stage.ToString()
            || !long.TryParse(parts[2], out var ticks) || new DateTime(ticks, DateTimeKind.Utc) <= clock.UtcNow)
        {
            throw new DomainException(ErrorCodes.Unauthorized);
        }

        return accountId;
    }
}

/// <summary>TOTP secrets are stored encrypted with Data Protection in <c>admin_accounts.mfa_secret</c>.</summary>
public sealed class MfaSecretProtector(IDataProtectionProvider protection)
{
    private readonly IDataProtector _protector = protection.CreateProtector("ATA.Admin.MfaSecret.v1");

    public string Protect(string base32Secret) => _protector.Protect(base32Secret);

    public string Unprotect(string stored) => _protector.Unprotect(stored);
}

/// <summary>Password policy (doc 12 §F20.4): at least 12 characters with an upper-case letter, a lower-case letter, a digit and a symbol.</summary>
public static class PasswordPolicy
{
    public const int MinLength = 12;
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lower = "abcdefghijkmnpqrstuvwxyz";
    private const string DigitChars = "23456789";
    private const string Symbols = "!@#$%^&*-_=+?";

    /// <summary>Throws <c>422 password_policy_violation { rules: [...] }</c> listing every failed rule (<c>min_length:12</c>, <c>uppercase</c>, <c>lowercase</c>, <c>digit</c>, <c>symbol</c>).</summary>
    public static void Validate(string? password)
    {
        var value = password ?? string.Empty;
        var failed = new List<string>();
        if (value.Length < MinLength) failed.Add($"min_length:{MinLength}");
        if (!value.Any(char.IsUpper)) failed.Add("uppercase");
        if (!value.Any(char.IsLower)) failed.Add("lowercase");
        if (!value.Any(char.IsDigit)) failed.Add("digit");
        if (!value.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c))) failed.Add("symbol");
        if (value.Length > 128) failed.Add("max_length:128");
        if (failed.Count > 0)
        {
            throw new DomainException(ErrorCodes.PasswordPolicyViolation, new { rules = failed });
        }
    }

    /// <summary>A random 16-character temporary password that satisfies the policy.</summary>
    public static string GenerateTemporary()
    {
        const string all = Upper + Lower + DigitChars + Symbols;
        var chars = new List<char>
        {
            Upper[RandomNumberGenerator.GetInt32(Upper.Length)], Lower[RandomNumberGenerator.GetInt32(Lower.Length)],
            DigitChars[RandomNumberGenerator.GetInt32(DigitChars.Length)], Symbols[RandomNumberGenerator.GetInt32(Symbols.Length)],
        };
        while (chars.Count < 16)
        {
            chars.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);
        }

        return new string(chars.OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue)).ToArray());
    }
}

/// <summary>TOTP verification with the replay guard, the per-login failure counter and the single-use recovery codes.</summary>
public sealed class AdminMfaService(AtaDbContext db, IPasswordHasher hasher, MfaSecretProtector secrets, IClock clock, IOptions<AdminOptions> options, AuditService audit)
{
    public const int RecoveryCodeCount = 10;
    private const string RecoveryAlphabet = "abcdefghijklmnopqrstuvwxyz234567";

    public static string OtpAuthUri(string username, string secret) =>
        $"otpauth://totp/ATA%20Admin:{Uri.EscapeDataString(username)}?secret={secret}&issuer=ATA%20Admin&digits={Totp.Digits}&period={Totp.PeriodSeconds}";

    /// <summary><c>429 mfa_locked</c> once <c>Admin:MaxMfaAttempts</c> wrong codes were entered since the password step.</summary>
    public void EnsureNotLocked(AdminAccount account)
    {
        if (account.MfaFailedCount >= options.Value.MaxMfaAttempts)
        {
            throw new DomainException(ErrorCodes.MfaLocked);
        }
    }

    /// <summary>Accepts the code of step now±1 newer than <c>mfa_last_step</c> and records its step; false otherwise.</summary>
    public bool TryAcceptTotp(AdminAccount account, string? code)
    {
        if (account.MfaSecret is null)
        {
            return false;
        }

        var step = Totp.Verify(secrets.Unprotect(account.MfaSecret), code, clock.UtcNow, account.MfaLastStep);
        if (step is null)
        {
            return false;
        }

        account.MfaLastStep = step;
        return true;
    }

    /// <summary>Marks the matching unused recovery code as used; false when none matches.</summary>
    public async Task<bool> TryUseRecoveryCodeAsync(AdminAccount account, string? recoveryCode, CancellationToken ct)
    {
        var normalized = NormalizeRecoveryCode(recoveryCode);
        if (normalized is null)
        {
            return false;
        }

        var codes = await db.AdminRecoveryCodes.Where(c => c.AdminAccountId == account.Id && c.UsedAt == null).ToListAsync(ct);
        var match = codes.FirstOrDefault(c => hasher.Verify(normalized, c.CodeHash));
        if (match is null)
        {
            return false;
        }

        match.UsedAt = clock.UtcNow;
        return true;
    }

    public Task<int> RemainingRecoveryCodesAsync(Guid adminAccountId, CancellationToken ct) =>
        db.AdminRecoveryCodes.CountAsync(c => c.AdminAccountId == adminAccountId && c.UsedAt == null, ct);

    /// <summary>
    /// Counts a wrong code (audit <c>admin.mfa_failed</c>), saves, and throws <c>400 mfa_invalid { attemptsLeft }</c> — or <c>429 mfa_locked</c> for the last attempt.
    /// </summary>
    public async Task<Exception> FailAsync(AdminAccount account, CancellationToken ct)
    {
        account.MfaFailedCount++;
        var left = Math.Max(0, options.Value.MaxMfaAttempts - account.MfaFailedCount);
        audit.Log("admin.mfa_failed", "admin_account", account.Id, null, new { attemptsLeft = left }, RoleNames.Admin, account.UserId);
        await db.SaveChangesAsync(ct);
        return left == 0 ? new DomainException(ErrorCodes.MfaLocked) : new DomainException(ErrorCodes.MfaInvalid, new { attemptsLeft = left });
    }

    /// <summary>Replaces the account's recovery codes with 10 new ones (hashed) and returns the plain codes (shown once).</summary>
    public async Task<IReadOnlyList<string>> RegenerateRecoveryCodesAsync(AdminAccount account, CancellationToken ct)
    {
        db.AdminRecoveryCodes.RemoveRange(await db.AdminRecoveryCodes.Where(c => c.AdminAccountId == account.Id).ToListAsync(ct));
        var codes = new List<string>(RecoveryCodeCount);
        while (codes.Count < RecoveryCodeCount)
        {
            var code = NewRecoveryCode();
            if (!codes.Contains(code))
            {
                codes.Add(code);
                db.AdminRecoveryCodes.Add(new AdminRecoveryCode { AdminAccountId = account.Id, CodeHash = hasher.Hash(code) });
            }
        }

        return codes;
    }

    /// <summary>Clears the secret, the enrolment and the recovery codes (the next login asks for a new enrolment when MFA is required).</summary>
    public async Task ResetAsync(AdminAccount account, CancellationToken ct)
    {
        account.MfaEnabled = false;
        account.MfaSecret = null;
        account.MfaEnrolledAt = null;
        account.MfaLastStep = null;
        account.MfaFailedCount = 0;
        db.AdminRecoveryCodes.RemoveRange(await db.AdminRecoveryCodes.Where(c => c.AdminAccountId == account.Id).ToListAsync(ct));
    }

    private static string NewRecoveryCode()
    {
        Span<char> chars = stackalloc char[9];
        for (var i = 0; i < 9; i++)
        {
            chars[i] = i == 4 ? '-' : RecoveryAlphabet[RandomNumberGenerator.GetInt32(RecoveryAlphabet.Length)];
        }

        return new string(chars);
    }

    private static string? NormalizeRecoveryCode(string? value)
    {
        var compact = value?.Replace("-", string.Empty, StringComparison.Ordinal).Replace(" ", string.Empty, StringComparison.Ordinal).Trim().ToLowerInvariant();
        return compact is { Length: 8 } ? $"{compact[..4]}-{compact[4..]}" : null;
    }
}

/// <summary>
/// Issues admin sessions (doc 12 §F20.4): a <c>refresh_tokens</c> row of kind <c>admin</c> with <c>absolute_expires_at</c> (login + <c>Admin:SessionAbsoluteHours</c>,
/// kept by every rotation together with the session start and user agent) and an access token of <c>Admin:AccessTokenMinutes</c> carrying the session id,
/// the permissions derived from the account's roles and — for a temporary password — the <c>pwdc</c> claim.
/// </summary>
public sealed class AdminSessionIssuer(AtaDbContext db, IJwtTokenService tokens, IClock clock, ICurrentUser currentUser, IOptions<AdminOptions> options)
{
    public async Task<AuthResponse> IssueAsync(User user, AdminAccount account, RefreshToken? previous, CancellationToken ct)
    {
        var o = options.Value;
        var now = clock.UtcNow;
        var permissions = await AdminPermissions.OfAccountAsync(db, account.Id, ct);
        var refresh = tokens.CreateRefreshToken();
        var absolute = previous is null ? now.AddHours(o.SessionAbsoluteHours) : previous.AbsoluteExpiresAt ?? previous.CreatedAt.AddHours(o.SessionAbsoluteHours);
        var lifetimeEnd = now + tokens.RefreshTokenLifetime;
        var entity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refresh.Hash,
            DeviceId = previous?.DeviceId ?? currentUser.DeviceId,
            SessionKind = SessionKind.Admin,
            ExpiresAt = absolute < lifetimeEnd ? absolute : lifetimeEnd,
            AbsoluteExpiresAt = absolute,
            LastUsedAt = now,
            UserAgent = previous?.UserAgent ?? currentUser.UserAgent,
            CreatedByIp = currentUser.IpAddress,
            // The session keeps its start time across rotations (sessions list: createdAt).
            CreatedAt = previous?.CreatedAt ?? now,
        };
        db.RefreshTokens.Add(entity);
        if (previous is not null)
        {
            previous.RevokedAt = now;
            previous.ReplacedById = entity.Id;
        }

        var access = tokens.CreateAccessToken(user, permissions, null, new AdminTokenContext(entity.Id, o.AccessTokenMinutes, account.MustChangePassword));
        return new AuthResponse(access.Token, access.ExpiresInSeconds, refresh.Value, entity.ExpiresAt, false, UserDto.From(user), null, permissions)
        {
            MustChangePassword = account.MustChangePassword,
        };
    }

    /// <summary>Whether an admin session may still be refreshed: not past its absolute end and used within <c>Admin:SessionIdleMinutes</c>.</summary>
    public bool IsAlive(RefreshToken token, DateTime now)
    {
        var o = options.Value;
        var absolute = token.AbsoluteExpiresAt ?? token.CreatedAt.AddHours(o.SessionAbsoluteHours);
        var lastUsed = token.LastUsedAt ?? token.CreatedAt;
        return token.IsActive(now) && now < absolute && now - lastUsed <= TimeSpan.FromMinutes(o.SessionIdleMinutes);
    }
}
