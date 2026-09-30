using System.Text.Json;
using ATA.Api.Common;
using ATA.Domain.Common;
using ATA.Api.Modules.Corporate;
using ATA.Domain.Corporate;
using ATA.Domain.Drivers;
using ATA.Domain.Identity;
using ATA.Domain.Notifications;
using ATA.Domain.Passengers;
using ATA.Infrastructure.Persistence;
using ATA.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Identity;

/// <summary>Login by OTP, refresh-token rotation, logout and admin login.</summary>
public sealed class AuthService(
    AtaDbContext db, OtpService otp, IJwtTokenService tokens, IPasswordHasher passwordHasher, IClock clock, ICurrentUser currentUser, CorporateMembershipService membership)
{
    public async Task<OtpRequestResponse> RequestOtpAsync(OtpRequestRequest request, Language fallbackLanguage, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.PhoneNumber), request.PhoneNumber)
            .Rule(nameof(request.Role), request.Role is null or Role.Passenger or Role.Driver or Role.CorporateAdmin, "must be passenger, driver or corporate_admin")
            .ThrowIfInvalid();

        // F19: the code is sent for corporate_admin too, whatever the phone number is (membership is only checked on verify, so the request never reveals it).
        var phone = PhoneNumber.Normalize(request.PhoneNumber);
        return await otp.RequestAsync(phone, request.Language ?? fallbackLanguage, ct);
    }

    public async Task<AuthResponse> VerifyOtpAsync(OtpVerifyRequest request, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.RequestId), request.RequestId)
            .Require(nameof(request.PhoneNumber), request.PhoneNumber)
            .Require(nameof(request.Code), request.Code, 8)
            .Require(nameof(request.Role), request.Role)
            .Rule(nameof(request.Role), request.Role is null or Role.Passenger or Role.Driver or Role.CorporateAdmin, "must be passenger, driver or corporate_admin")
            .ThrowIfInvalid();

        var phone = PhoneNumber.Normalize(request.PhoneNumber);
        var role = request.Role!.Value;
        await otp.VerifyAsync(request.RequestId!.Value, phone, request.Code!.Trim(), ct);
        if (role == Role.CorporateAdmin)
        {
            return await VerifyCorporateAdminAsync(request, phone, ct);
        }

        var now = clock.UtcNow;
        var user = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone, ct);
        var isNewUser = user is null;
        if (user is null)
        {
            user = new User { PhoneNumber = phone, PhoneVerifiedAt = now };
            db.Users.Add(user);
        }

        user.EnsureActive();
        user.PhoneVerifiedAt ??= now;
        user.LastLoginAt = now;

        if (!user.HasRole(role))
        {
            user.Roles.Add(new UserRole { UserId = user.Id, Role = role, GrantedAt = now });
        }

        var driver = await EnsureProfileAsync(user, role, ct);
        await UpsertDeviceAsync(user.Id, request.Device, ct);

        var (response, _) = IssueTokens(user, driver, isNewUser, request.Device?.DeviceId, null);
        await db.SaveChangesAsync(ct);
        return response;
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, CancellationToken ct)
    {
        new Validator().Require(nameof(request.RefreshToken), request.RefreshToken, 512).ThrowIfInvalid();

        var hash = tokens.HashRefreshToken(request.RefreshToken!);
        var existing = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        var now = clock.UtcNow;
        if (existing is null || !existing.IsActive(now))
        {
            throw new DomainException(ErrorCodes.Unauthorized);
        }

        var user = await db.Users.FirstAsync(u => u.Id == existing.UserId, ct);
        user.EnsureActive();

        var driver = await db.Drivers.FirstOrDefaultAsync(d => d.UserId == user.Id, ct);
        var permissions = await LoadPermissionsAsync(user, ct);
        // F19: a corporate session stays a corporate session (same `corp` claim) only while the user is still an active admin of a company that is not closed.
        Guid? corporateAccountId = null;
        if (existing.SessionKind == SessionKind.Corporate)
        {
            corporateAccountId = await (from m in db.CorporateUsers
                                        join a in db.CorporateAccounts on m.CorporateAccountId equals a.Id
                                        where m.UserId == user.Id && m.Role == CorporateRole.CorporateAdmin && m.Status == CorporateUserStatus.Active && a.Status != CorporateAccountStatus.Closed
                                        select (Guid?)a.Id).FirstOrDefaultAsync(ct);
            if (corporateAccountId is null)
            {
                throw new DomainException(ErrorCodes.Unauthorized);
            }
        }

        var (response, replacement) = IssueTokens(user, driver, false, existing.DeviceId, permissions, existing.SessionKind, corporateAccountId);

        existing.RevokedAt = now;
        existing.ReplacedById = replacement.Id;
        await db.SaveChangesAsync(ct);
        return response;
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken ct)
    {
        new Validator().Require(nameof(request.RefreshToken), request.RefreshToken, 512).ThrowIfInvalid();
        var hash = tokens.HashRefreshToken(request.RefreshToken!);
        var existing = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (existing is not null && existing.RevokedAt is null)
        {
            existing.RevokedAt = clock.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    public async Task<AuthResponse> AdminLoginAsync(AdminLoginRequest request, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.Username), request.Username, 64)
            .Require(nameof(request.Password), request.Password, 256)
            .ThrowIfInvalid();

        var account = await db.AdminAccounts.Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Username == request.Username!.Trim(), ct);
        if (account is null || !account.IsActive || !passwordHasher.Verify(request.Password!, account.PasswordHash))
        {
            throw new DomainException(ErrorCodes.InvalidCredentials);
        }

        var user = account.User!;
        user.EnsureActive();
        var now = clock.UtcNow;
        account.LastLoginAt = now;
        user.LastLoginAt = now;

        var permissions = ParsePermissions(account.Permissions);
        var (response, _) = IssueTokens(user, null, false, currentUser.DeviceId, permissions, SessionKind.Admin);
        await db.SaveChangesAsync(ct);
        return response;
    }

    /// <summary>
    /// OTP login to the corporate portal (doc 12 §F19.3): needs a <c>corporate_admin</c> membership (invited or active) of a pending / active / suspended company —
    /// <c>403 corporate_not_member</c> otherwise, <c>403 corporate_account_inactive</c> for a closed one. A first sign-in accepts the invitation implicitly
    /// (<c>410 invitation_expired</c> when it lapsed, <c>409 corporate_member_elsewhere</c> when the user administers another company). The token carries the
    /// <c>corp</c> claim and only the <c>corporate_admin</c> role; the refresh token is a <c>corporate</c> session.
    /// </summary>
    private async Task<AuthResponse> VerifyCorporateAdminAsync(OtpVerifyRequest request, string phone, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var candidates = await (from m in db.CorporateUsers
                                join a in db.CorporateAccounts on m.CorporateAccountId equals a.Id
                                where m.PhoneNumber == phone && m.Role == CorporateRole.CorporateAdmin && (m.Status == CorporateUserStatus.Invited || m.Status == CorporateUserStatus.Active)
                                select new { Member = m, Account = a }).ToListAsync(ct);
        var chosen = candidates.Where(c => c.Account.Status != CorporateAccountStatus.Closed)
            .OrderBy(c => c.Member.Status == CorporateUserStatus.Active ? 0 : 1).ThenByDescending(c => c.Member.CreatedAt).FirstOrDefault();
        if (chosen is null)
        {
            throw new DomainException(candidates.Count > 0 ? ErrorCodes.CorporateAccountInactive : ErrorCodes.CorporateNotMember);
        }

        var member = chosen.Member;
        var user = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone, ct);
        var isNewUser = user is null;
        if (user is null)
        {
            user = new User { PhoneNumber = phone, PhoneVerifiedAt = now };
            db.Users.Add(user);
        }

        user.EnsureActive();
        user.PhoneVerifiedAt ??= now;
        user.LastLoginAt = now;
        user.FullName ??= member.FullName;
        if (member.Status == CorporateUserStatus.Invited)
        {
            var invitations = await db.CorporateInvitations.Where(i => i.CorporateUserId == member.Id && i.AcceptedAt == null && i.DeclinedAt == null && i.RevokedAt == null).ToListAsync(ct);
            if (invitations.Count == 0)
            {
                throw new DomainException(ErrorCodes.CorporateNotMember);
            }

            if (invitations.All(i => i.IsExpiredAt(now)))
            {
                throw new DomainException(ErrorCodes.InvitationExpired, new { expiresAt = invitations.Max(i => i.ExpiresAt) });
            }

            await membership.ActivateAsync(member, user, now, "corporate_user.accept", CorporateMembershipService.AdminActor, ct);
        }
        else if (member.UserId != user.Id)
        {
            throw new DomainException(ErrorCodes.CorporateNotMember);
        }
        else if (!user.HasRole(Role.CorporateAdmin))
        {
            user.Roles.Add(new UserRole { UserId = user.Id, Role = Role.CorporateAdmin, GrantedAt = now });
        }

        await EnsureProfileAsync(user, Role.CorporateAdmin, ct);
        await UpsertDeviceAsync(user.Id, request.Device, ct);
        var (response, _) = IssueTokens(user, null, isNewUser, request.Device?.DeviceId, null, SessionKind.Corporate, chosen.Account.Id);
        await db.SaveChangesAsync(ct);
        return response;
    }

    public async Task RevokeAllAsync(Guid userId, CancellationToken ct)
    {
        var now = clock.UtcNow;
        await db.RefreshTokens.Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
    }

    private async Task<DriverProfile?> EnsureProfileAsync(User user, Role role, CancellationToken ct)
    {
        if (role == Role.Passenger && !await db.Passengers.AnyAsync(p => p.UserId == user.Id, ct))
        {
            db.Passengers.Add(new PassengerProfile { UserId = user.Id });
        }

        if (!await db.NotificationPreferences.AnyAsync(p => p.UserId == user.Id, ct))
        {
            db.NotificationPreferences.Add(new NotificationPreference { UserId = user.Id });
        }

        var driver = await db.Drivers.FirstOrDefaultAsync(d => d.UserId == user.Id, ct);
        if (role == Role.Driver && driver is null)
        {
            driver = new DriverProfile { UserId = user.Id, ApplicationNumber = await NewApplicationNumberAsync(ct) };
            db.Drivers.Add(driver);
        }

        return driver;
    }

    private async Task<string> NewApplicationNumberAsync(CancellationToken ct)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var candidate = $"ATA-{Random.Shared.Next(10000, 100000)}";
            if (!await db.Drivers.AnyAsync(d => d.ApplicationNumber == candidate, ct)
                && db.Drivers.Local.All(d => d.ApplicationNumber != candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Could not allocate a unique application number.");
    }

    private async Task UpsertDeviceAsync(Guid userId, DeviceInfo? device, CancellationToken ct)
    {
        if (device is null || string.IsNullOrWhiteSpace(device.DeviceId))
        {
            return;
        }

        var existing = await db.UserDevices.FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceId == device.DeviceId, ct);
        if (existing is null)
        {
            existing = new UserDevice { UserId = userId, DeviceId = device.DeviceId };
            db.UserDevices.Add(existing);
        }

        existing.Platform = device.Platform ?? existing.Platform;
        existing.DeviceName = device.DeviceName ?? existing.DeviceName;
        existing.PushToken = device.PushToken ?? existing.PushToken;
        existing.AppVersion = device.AppVersion ?? existing.AppVersion;
        existing.LastSeenAt = clock.UtcNow;
    }

    private async Task<IReadOnlyList<string>?> LoadPermissionsAsync(User user, CancellationToken ct)
    {
        if (!user.HasRole(Role.Admin) && !user.HasRole(Role.Operations))
        {
            return null;
        }

        var json = await db.AdminAccounts.Where(a => a.UserId == user.Id && a.IsActive).Select(a => a.Permissions).FirstOrDefaultAsync(ct);
        return json is null ? null : ParsePermissions(json);
    }

    private static IReadOnlyList<string> ParsePermissions(string json) =>
        JsonSerializer.Deserialize<string[]>(json) ?? [];

    private (AuthResponse Response, RefreshToken Token) IssueTokens(
        User user, DriverProfile? driver, bool isNewUser, string? deviceId, IReadOnlyList<string>? permissions, SessionKind sessionKind = SessionKind.App, Guid? corporateAccountId = null)
    {
        var access = tokens.CreateAccessToken(user, permissions, corporateAccountId);
        var refresh = tokens.CreateRefreshToken();
        var now = clock.UtcNow;
        var entity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refresh.Hash,
            DeviceId = deviceId,
            SessionKind = sessionKind,
            ExpiresAt = now + tokens.RefreshTokenLifetime,
            CreatedByIp = currentUser.IpAddress,
            CreatedAt = now,
        };
        db.RefreshTokens.Add(entity);

        var response = new AuthResponse(
            access.Token,
            access.ExpiresInSeconds,
            refresh.Value,
            entity.ExpiresAt,
            isNewUser,
            UserDto.From(user),
            driver is null ? null : DriverSummaryDto.From(driver),
            permissions);
        return (response, entity);
    }
}
