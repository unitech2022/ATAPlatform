using ATA.Domain.Common;

namespace ATA.Domain.Identity;

public enum OtpPurpose { Login }

public class OtpRequest : Entity
{
    public const int DefaultMaxAttempts = 5;

    public required string PhoneNumber { get; set; }
    public OtpPurpose Purpose { get; set; } = OtpPurpose.Login;
    public required string CodeHash { get; set; }
    public int Attempts { get; set; }
    public int MaxAttempts { get; set; } = DefaultMaxAttempts;
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public string? IpAddress { get; set; }

    public int AttemptsLeft => Math.Max(0, MaxAttempts - Attempts);

    /// <summary>Applies one verification attempt. Throws otp_expired / otp_locked / otp_invalid; marks consumed on success.</summary>
    public void Verify(bool hashMatches, DateTime now)
    {
        if (ConsumedAt is not null || now >= ExpiresAt)
        {
            throw new DomainException(ErrorCodes.OtpExpired);
        }

        if (Attempts >= MaxAttempts)
        {
            throw new DomainException(ErrorCodes.OtpLocked, new { attemptsLeft = 0 });
        }

        Attempts++;
        if (!hashMatches)
        {
            throw new DomainException(Attempts >= MaxAttempts ? ErrorCodes.OtpLocked : ErrorCodes.OtpInvalid, new { attemptsLeft = AttemptsLeft });
        }

        ConsumedAt = now;
    }
}
