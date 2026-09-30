using System.Security.Cryptography;
using System.Text;
using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Domain.Common;
using ATA.Domain.Corporate;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Corporate;

/// <summary>
/// <c>/corporate/api-keys</c> (doc 12 §F19.4, future extensibility): only with <c>Corporate:ApiKeysEnabled=true</c> (otherwise <c>404</c>). A key is returned in full once
/// (<c>ata_live_&lt;prefix&gt;&lt;secret&gt;</c>); only its prefix and SHA-256 are stored. No authentication scheme consumes keys in v1.
/// </summary>
public sealed class CorporateApiKeyService(AtaDbContext db, IClock clock, AuditService audit, IOptions<CorporateOptions> options)
{
    public static readonly string[] AllScopes = ["bookings:read", "bookings:write", "reports:read"];
    private const string Alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

    private void EnsureEnabled()
    {
        if (!options.Value.ApiKeysEnabled)
        {
            throw new DomainException(ErrorCodes.NotFound);
        }
    }

    public async Task<IReadOnlyList<CorporateApiKeyDto>> ListAsync(Guid accountId, CancellationToken ct)
    {
        EnsureEnabled();
        return (await db.CorporateApiKeys.AsNoTracking().Where(k => k.CorporateAccountId == accountId).OrderByDescending(k => k.CreatedAt).ToListAsync(ct)).Select(k => ToDto(k)).ToList();
    }

    public async Task<CorporateApiKeyDto> CreateAsync(Guid accountId, Guid createdBy, CreateApiKeyRequest request, CancellationToken ct)
    {
        EnsureEnabled();
        new Validator()
            .Require(nameof(request.Name), request.Name, 120)
            .Rule(nameof(request.Scopes), request.Scopes is null || request.Scopes.All(AllScopes.Contains), $"must be among {string.Join('|', AllScopes)}")
            .Rule(nameof(request.ExpiresAt), request.ExpiresAt is null || request.ExpiresAt > clock.UtcNow, "must be in the future")
            .ThrowIfInvalid();
        var prefix = Random(8);
        var key = $"ata_live_{prefix}{Random(32)}";
        var entity = new CorporateApiKey
        {
            CorporateAccountId = accountId, Name = request.Name!.Trim(), KeyPrefix = prefix, KeyHash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(key))),
            Scopes = CorporateDtos.Scopes(request.Scopes is { Count: > 0 } ? request.Scopes : AllScopes), ExpiresAt = request.ExpiresAt?.ToUniversalTime(), CreatedBy = createdBy,
        };
        db.CorporateApiKeys.Add(entity);
        audit.Log("corporate_api_key.create", "corporate_account", accountId, null, new { keyId = entity.Id, entity.Name, entity.KeyPrefix, entity.Scopes }, CorporateMembershipService.AdminActor);
        await db.SaveChangesAsync(ct);
        return ToDto(entity, key);
    }

    public async Task RevokeAsync(Guid accountId, Guid id, CancellationToken ct)
    {
        EnsureEnabled();
        var key = Guard.NotFound(await db.CorporateApiKeys.FirstOrDefaultAsync(k => k.Id == id && k.CorporateAccountId == accountId, ct));
        key.RevokedAt ??= clock.UtcNow;
        audit.Log("corporate_api_key.revoke", "corporate_account", accountId, null, new { keyId = key.Id, key.KeyPrefix }, CorporateMembershipService.AdminActor);
        await db.SaveChangesAsync(ct);
    }

    private static CorporateApiKeyDto ToDto(CorporateApiKey k, string? full = null) => new(k.Id, k.Name, k.KeyPrefix,
        System.Text.Json.JsonSerializer.Deserialize<string[]>(k.Scopes) ?? [], k.LastUsedAt, k.ExpiresAt, k.RevokedAt, k.CreatedAt, full);

    private static string Random(int length) => string.Create(length, 0, (span, _) =>
    {
        for (var i = 0; i < span.Length; i++)
        {
            span[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }
    });
}
