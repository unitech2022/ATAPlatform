using System.Text.Json.Serialization;
using ATA.Domain.Common;
using ATA.Domain.Drivers;
using ATA.Domain.Identity;
using ATA.Infrastructure.Security;

namespace ATA.Api.Modules.Identity;

public sealed record OtpRequestRequest(string? PhoneNumber, Role? Role, Language? Language);

public sealed record OtpRequestResponse(Guid RequestId, string PhoneNumber, int ExpiresInSeconds, int ResendAfterSeconds, string? DevCode);

public sealed record DeviceInfo(string? DeviceId, DevicePlatform? Platform, string? DeviceName, string? PushToken, string? AppVersion);

public sealed record OtpVerifyRequest(Guid? RequestId, string? PhoneNumber, string? Code, Role? Role, DeviceInfo? Device);

public sealed record RefreshRequest(string? RefreshToken);

public sealed record LogoutRequest(string? RefreshToken);

public sealed record AdminLoginRequest(string? Username, string? Password);

public sealed record UserDto(
    Guid Id,
    string PhoneNumber,
    string? FullName,
    Language Language,
    Gender Gender,
    IReadOnlyList<string> Roles,
    DateTime? TermsAcceptedAt,
    DateTime CreatedAt)
{
    public static UserDto From(User user) => new(
        user.Id,
        user.PhoneNumber,
        user.FullName,
        user.Language,
        user.Gender,
        user.Roles.OrderBy(r => r.GrantedAt).Select(r => RoleNames.Of(r.Role)).ToArray(),
        user.TermsAcceptedAt,
        user.CreatedAt);
}

public sealed record DriverSummaryDto(string ApplicationNumber, ApplicationStatus ApplicationStatus)
{
    public static DriverSummaryDto From(DriverProfile driver) => new(driver.ApplicationNumber, driver.ApplicationStatus);
}

public sealed record AuthResponse(
    string AccessToken,
    int AccessTokenExpiresIn,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    bool IsNewUser,
    UserDto User,
    DriverSummaryDto? Driver,
    IReadOnlyList<string>? Permissions)
{
    /// <summary>F20 admin sessions only (omitted for app / corporate sessions): a temporary password must be changed first.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? MustChangePassword { get; init; }

    /// <summary>F20: unused recovery codes after <c>/auth/admin/mfa/verify</c>.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? RecoveryCodesRemaining { get; init; }
}

// ----- F20 admin login with MFA (doc 12 §F20.5) -----

/// <summary><c>200 { mfaRequired: true, mfaToken, methods: ["totp", "recovery_code"] }</c>: the password was right, a TOTP or recovery code must follow.</summary>
public sealed record AdminMfaChallengeResponse(bool MfaRequired, string MfaToken, IReadOnlyList<string> Methods);

/// <summary><c>200 { mfaEnrollmentRequired: true, mfaToken }</c>: MFA is mandatory and the account has not enrolled yet.</summary>
public sealed record AdminMfaEnrollmentResponse(bool MfaEnrollmentRequired, string MfaToken);

public sealed record AdminMfaVerifyRequest(string? MfaToken, string? Code, string? RecoveryCode);

public sealed record AdminMfaEnrollRequest(string? MfaToken);

public sealed record AdminMfaEnrollResponse(string Secret, string OtpauthUri);

public sealed record AdminMfaEnrollConfirmRequest(string? MfaToken, string? Code);

public sealed record AdminMfaEnrollConfirmResponse(IReadOnlyList<string> RecoveryCodes, AuthResponse Auth);

public sealed record MeResponse(UserDto User, MePassengerDto? Passenger, MeDriverDto? Driver);

public sealed record MePassengerDto(decimal RatingAvg, int RatingCount, bool PreferFemaleDriver, PaymentMethodKind DefaultPaymentMethod, string MemberSince);

public sealed record MeDriverDto(string ApplicationNumber, ApplicationStatus ApplicationStatus, bool IsOnline, DriverTier Tier);

public sealed record UpdateMeRequest(string? FullName, Language? Language, Gender? Gender, bool? AcceptTerms);

public sealed record NotificationPreferencesDto(bool Trips, bool Wallet, bool Safety, bool Offers);

public sealed record RegisterDeviceRequest(string? DeviceId, DevicePlatform? Platform, string? DeviceName, string? PushToken, string? AppVersion);
