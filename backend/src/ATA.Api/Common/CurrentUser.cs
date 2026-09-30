using ATA.Domain.Common;
using ATA.Infrastructure.Security;

namespace ATA.Api.Common;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid UserId { get; }
    IReadOnlyCollection<string> Roles { get; }
    bool IsAdmin { get; }
    /// <summary>F19: the <c>corp</c> claim of a corporate portal token.</summary>
    Guid? CorporateAccountId { get; }
    string? IpAddress { get; }
    string? DeviceId { get; }
    /// <summary>F20: the <c>sid</c> claim of an admin token (the refresh token of the current session).</summary>
    Guid? SessionId { get; }
    string? UserAgent { get; }

    bool HasRole(string role);

    /// <summary>Admin permission from the JWT <c>perm</c> claims (<c>*</c> grants everything).</summary>
    bool HasPermission(string permission);
}

/// <summary>Reads the caller's identity from the validated JWT claims.</summary>
public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private HttpContext Context => accessor.HttpContext ?? throw new InvalidOperationException("No active HTTP context.");

    public bool IsAuthenticated => accessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public Guid UserId
    {
        get
        {
            var sub = Context.User.FindFirst(AtaClaims.Subject)?.Value;
            return Guid.TryParse(sub, out var id) ? id : throw new DomainException(ErrorCodes.Unauthorized);
        }
    }

    public IReadOnlyCollection<string> Roles => accessor.HttpContext?.User.FindAll(AtaClaims.Roles).Select(c => c.Value).ToArray() ?? [];

    public bool IsAdmin => HasRole(RoleNames.Admin) || HasRole(RoleNames.Operations);

    public Guid? CorporateAccountId => Guid.TryParse(accessor.HttpContext?.User.FindFirst(AtaClaims.Corporate)?.Value, out var id) ? id : null;

    public string? IpAddress => accessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? DeviceId => Context.Request.Headers.TryGetValue("X-Device-Id", out var v) ? v.ToString() : null;

    public Guid? SessionId => Guid.TryParse(accessor.HttpContext?.User.FindFirst(AtaClaims.Session)?.Value, out var id) ? id : null;

    public string? UserAgent
    {
        get
        {
            var value = accessor.HttpContext?.Request.Headers.UserAgent.ToString();
            return string.IsNullOrWhiteSpace(value) ? null : value.Length > 255 ? value[..255] : value;
        }
    }

    public bool HasRole(string role) => Context.User.HasClaim(AtaClaims.Roles, role);

    public bool HasPermission(string permission) =>
        Context.User.HasClaim(AtaClaims.Permissions, "*") || Context.User.HasClaim(AtaClaims.Permissions, permission);
}
