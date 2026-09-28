using ATA.Api.Common;
using ATA.Domain.Common;
using ATA.Domain.Identity;
using ATA.Infrastructure.Persistence;
using ATA.Infrastructure.Security;
using ATA.Infrastructure.Sms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Identity;

public sealed class OtpOptions
{
    public const string Section = "Otp";
    public bool DevMode { get; set; }
    public int Digits { get; set; } = 4;
    public int ExpiryMinutes { get; set; } = 5;
    public int MaxAttempts { get; set; } = OtpRequest.DefaultMaxAttempts;
    public int ResendAfterSeconds { get; set; } = 60;
    public int MaxRequestsPerWindow { get; set; } = 3;
    public int WindowMinutes { get; set; } = 10;
}

/// <summary>OTP issuance and verification: hashed 4-digit codes, 5-minute expiry, 5 attempts, resend cooldown and per-phone quota.</summary>
public sealed class OtpService(AtaDbContext db, IOtpGenerator generator, ISmsSender sms, IClock clock, IOptions<OtpOptions> options, ICurrentUser currentUser)
{
    private readonly OtpOptions _options = options.Value;

    public async Task<OtpRequestResponse> RequestAsync(string phoneNumber, Language language, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var windowStart = now.AddMinutes(-_options.WindowMinutes);
        var recent = await db.OtpRequests
            .Where(o => o.PhoneNumber == phoneNumber && o.CreatedAt >= windowStart)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => o.CreatedAt)
            .ToListAsync(ct);

        if (recent.Count >= _options.MaxRequestsPerWindow)
        {
            var retryAfter = (int)Math.Ceiling((recent[^1].AddMinutes(_options.WindowMinutes) - now).TotalSeconds);
            throw new DomainException(ErrorCodes.RateLimited, new { retryAfterSeconds = Math.Max(1, retryAfter) });
        }

        if (recent.Count > 0 && (now - recent[0]).TotalSeconds < _options.ResendAfterSeconds)
        {
            var retryAfter = (int)Math.Ceiling(_options.ResendAfterSeconds - (now - recent[0]).TotalSeconds);
            throw new DomainException(ErrorCodes.RateLimited, new { retryAfterSeconds = Math.Max(1, retryAfter) });
        }

        var code = generator.Generate(_options.Digits);
        var request = new OtpRequest
        {
            PhoneNumber = phoneNumber,
            Purpose = OtpPurpose.Login,
            CodeHash = string.Empty,
            MaxAttempts = _options.MaxAttempts,
            ExpiresAt = now.AddMinutes(_options.ExpiryMinutes),
            IpAddress = currentUser.IpAddress,
            CreatedAt = now,
        };
        request.CodeHash = generator.Hash(request.Id, code);
        db.OtpRequests.Add(request);
        await db.SaveChangesAsync(ct);

        if (!_options.DevMode)
        {
            var text = language == Language.En
                ? $"Your ATA verification code is {code}. Valid for {_options.ExpiryMinutes} minutes."
                : $"رمز التحقق في ATA هو {code}. صالح لمدة {_options.ExpiryMinutes} دقائق.";
            await sms.SendAsync(phoneNumber, text, ct);
        }

        return new OtpRequestResponse(request.Id, phoneNumber, _options.ExpiryMinutes * 60, _options.ResendAfterSeconds, _options.DevMode ? code : null);
    }

    /// <summary>Validates the code and consumes the request. Throws otp_invalid / otp_expired / otp_locked.</summary>
    public async Task VerifyAsync(Guid requestId, string phoneNumber, string code, CancellationToken ct)
    {
        var request = await db.OtpRequests.FirstOrDefaultAsync(o => o.Id == requestId && o.PhoneNumber == phoneNumber, ct)
            ?? throw new DomainException(ErrorCodes.OtpExpired);

        var matches = string.Equals(request.CodeHash, generator.Hash(request.Id, code), StringComparison.Ordinal);
        try
        {
            request.Verify(matches, clock.UtcNow);
        }
        finally
        {
            await db.SaveChangesAsync(ct);
        }
    }
}
