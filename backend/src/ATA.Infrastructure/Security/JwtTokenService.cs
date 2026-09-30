using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ATA.Domain.Common;
using ATA.Domain.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;

namespace ATA.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string Section = "Jwt";
    public string Issuer { get; set; } = "ata";
    public string Audience { get; set; } = "ata-clients";
    public string Key { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 30;
}

public static class AtaClaims
{
    public const string Subject = "sub";
    public const string Phone = "phone";
    public const string Roles = "roles";
    public const string Name = "name";
    public const string Language = "lang";
    public const string Permissions = "perm";
    /// <summary>Corporate portal tokens: the <c>corporate_accounts.id</c> every <c>/corporate/*</c> query is scoped to (F19).</summary>
    public const string Corporate = "corp";
}

public sealed record AccessToken(string Token, int ExpiresInSeconds);

public sealed record OpaqueToken(string Value, string Hash);

public interface IJwtTokenService
{
    AccessToken CreateAccessToken(User user, IReadOnlyCollection<string>? permissions = null, Guid? corporateAccountId = null);

    /// <summary>Creates a cryptographically random opaque refresh token together with its SHA-256 hash.</summary>
    OpaqueToken CreateRefreshToken();

    string HashRefreshToken(string token);

    TimeSpan RefreshTokenLifetime { get; }
}

public sealed class JwtTokenService(IOptions<JwtOptions> options, IClock clock) : IJwtTokenService
{
    private readonly JwtOptions _options = options.Value;

    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(_options.RefreshTokenDays);

    /// <summary>
    /// A corporate portal token (<paramref name="corporateAccountId"/>, F19) carries only the <c>corporate_admin</c> role and the <c>corp</c> claim, so it can never
    /// be used against the rider, driver or admin APIs even when the same user also holds those roles.
    /// </summary>
    public AccessToken CreateAccessToken(User user, IReadOnlyCollection<string>? permissions = null, Guid? corporateAccountId = null)
    {
        var now = clock.UtcNow;
        var expires = now.AddMinutes(_options.AccessTokenMinutes);
        var claims = new List<Claim>
        {
            new(AtaClaims.Subject, user.Id.ToString()),
            new(AtaClaims.Phone, user.PhoneNumber),
            new(AtaClaims.Language, user.Language.ToString().ToLowerInvariant()),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()),
        };
        if (!string.IsNullOrWhiteSpace(user.FullName))
        {
            claims.Add(new Claim(AtaClaims.Name, user.FullName));
        }

        if (corporateAccountId is { } corporate)
        {
            claims.Add(new Claim(AtaClaims.Roles, RoleNames.CorporateAdmin));
            claims.Add(new Claim(AtaClaims.Corporate, corporate.ToString()));
        }
        else
        {
            claims.AddRange(user.Roles.Select(r => new Claim(AtaClaims.Roles, RoleNames.Of(r.Role))));
        }

        if (permissions is { Count: > 0 })
        {
            claims.AddRange(permissions.Select(p => new Claim(AtaClaims.Permissions, p)));
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(claims),
            NotBefore = now,
            IssuedAt = now,
            Expires = expires,
            SigningCredentials = new SigningCredentials(CreateKey(_options.Key), SecurityAlgorithms.HmacSha256),
        };
        var token = new JsonWebTokenHandler { SetDefaultTimesOnTokenCreation = false }.CreateToken(descriptor);
        return new AccessToken(token, _options.AccessTokenMinutes * 60);
    }

    public OpaqueToken CreateRefreshToken()
    {
        var value = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(48));
        return new OpaqueToken(value, HashRefreshToken(value));
    }

    public string HashRefreshToken(string token) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public static SymmetricSecurityKey CreateKey(string key) => new(Encoding.UTF8.GetBytes(key));
}

/// <summary>Snake_case role names used in JWT claims and authorization policies.</summary>
public static class RoleNames
{
    public const string Passenger = "passenger";
    public const string Driver = "driver";
    public const string Admin = "admin";
    public const string Operations = "operations";
    public const string CorporateAdmin = "corporate_admin";

    public static string Of(Role role) => role switch
    {
        Role.Passenger => Passenger,
        Role.Driver => Driver,
        Role.Admin => Admin,
        Role.Operations => Operations,
        Role.CorporateAdmin => CorporateAdmin,
        _ => throw new ArgumentOutOfRangeException(nameof(role)),
    };
}
