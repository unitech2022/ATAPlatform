namespace ATA.Api.Modules.Rbac;

public sealed record RoleRefDto(Guid Id, string Code, string Name);

/// <summary><c>GET /admin/me</c> (doc 12 §F20.5): permissions are re-read from the roles (<c>["*"]</c> for a super admin).</summary>
public sealed record AdminMeResponse(
    Guid AdminAccountId, Guid UserId, string Username, string? FullName, IReadOnlyList<RoleRefDto> Roles, IReadOnlyList<string> Permissions,
    bool MfaEnabled, bool MustChangePassword, bool OnDuty);

public sealed record ChangePasswordRequest(string? CurrentPassword, string? NewPassword);

public sealed record RecoveryCodesRequest(string? Code);

public sealed record RecoveryCodesResponse(IReadOnlyList<string> RecoveryCodes);

public sealed record AdminSessionDto(Guid Id, string? UserAgent, string? IpAddress, DateTime CreatedAt, DateTime? LastUsedAt, bool Current);

public sealed record AdminUserDto(
    Guid Id, Guid UserId, string Username, string? FullName, string PhoneNumber, IReadOnlyList<RoleRefDto> Roles, bool IsActive, bool MfaEnabled,
    DateTime? LastLoginAt, bool OnDuty, DateTime? LockedUntil);

/// <summary><c>GET /admin/admin-users/{id}</c> (added for the dashboard detail page): the list fields plus security state, permissions and active sessions.</summary>
public sealed record AdminUserDetailDto(
    Guid Id, Guid UserId, string Username, string? FullName, string PhoneNumber, IReadOnlyList<RoleRefDto> Roles, bool IsActive, bool MfaEnabled,
    DateTime? LastLoginAt, bool OnDuty, DateTime? LockedUntil, bool MustChangePassword, DateTime? MfaEnrolledAt, DateTime? PasswordChangedAt, int FailedLoginCount,
    DateTime CreatedAt, IReadOnlyList<string> Permissions, IReadOnlyList<AdminSessionDto> Sessions);

public sealed record CreateAdminUserRequest(string? Username, string? FullName, string? PhoneNumber, IReadOnlyList<Guid>? RoleIds, string? TemporaryPassword);

public sealed record CreateAdminUserResponse(AdminUserDto AdminUser, string TemporaryPassword);

public sealed record UpdateAdminUserRequest(string? FullName, IReadOnlyList<Guid>? RoleIds);

public sealed record TemporaryPasswordResponse(string TemporaryPassword);

public sealed record PermissionDto(string Code, string Module, string Name, string NameAr, string NameEn, string? Description);

public sealed record RoleDto(
    Guid Id, string Code, string Name, string NameAr, string NameEn, string? Description, bool IsSystem, IReadOnlyList<string> PermissionCodes, int UserCount,
    DateTime CreatedAt, DateTime UpdatedAt);

public sealed record RoleUpsertRequest(string? Code, string? NameAr, string? NameEn, string? Description, IReadOnlyList<string>? PermissionCodes);
