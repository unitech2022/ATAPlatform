using ATA.Domain.Common;
using ATA.Infrastructure.Security;

namespace ATA.Api.Common;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid UserId { get; }
    IReadOnlyCollection<string> Roles { get; }
    bool IsAdmin { get; }
    string? IpAddress { get; }
    string? DeviceId { get; }

    bool HasRole(string role);
}

/// <summary>Reads the caller's identity from the validated JWT claims.</summary>
public sealed class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private HttpContext Context => accessor.HttpContext ?? throw new InvalidOperationException("No active HTTP context.");

    public bool IsAuthenticated => Context.User.Identity?.IsAuthenticated == true;

    public Guid UserId
    {
        get
        {
            var sub = Context.User.FindFirst(AtaClaims.Subject)?.Value;
            return Guid.TryParse(sub, out var id) ? id : throw new DomainException(ErrorCodes.Unauthorized);
        }
    }

    public IReadOnlyCollection<string> Roles => Context.User.FindAll(AtaClaims.Roles).Select(c => c.Value).ToArray();

    public bool IsAdmin => HasRole(RoleNames.Admin) || HasRole(RoleNames.Operations);

    public string? IpAddress => Context.Connection.RemoteIpAddress?.ToString();

    public string? DeviceId => Context.Request.Headers.TryGetValue("X-Device-Id", out var v) ? v.ToString() : null;

    public bool HasRole(string role) => Context.User.HasClaim(AtaClaims.Roles, role);
}
