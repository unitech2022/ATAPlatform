using System.Security.Cryptography;
using System.Text;

namespace ATA.Infrastructure.Security;

/// <summary>RFC 6238 TOTP (HMAC-SHA1, 6 digits, 30-second steps) with RFC 4648 Base32 secrets — the parameters every authenticator app supports.</summary>
public static class Totp
{
    public const int Digits = 6;
    public const int PeriodSeconds = 30;
    public const int SecretBytes = 20;
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static string NewSecret() => Base32Encode(RandomNumberGenerator.GetBytes(SecretBytes));

    public static long StepAt(DateTime utc) => new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)).ToUnixTimeSeconds() / PeriodSeconds;

    public static string Code(string base32Secret, long step)
    {
        var counter = new byte[8];
        for (var i = 7; i >= 0; i--)
        {
            counter[i] = (byte)(step & 0xFF);
            step >>= 8;
        }

        var hash = HMACSHA1.HashData(Base32Decode(base32Secret), counter);
        var offset = hash[^1] & 0x0F;
        var binary = ((hash[offset] & 0x7F) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | hash[offset + 3];
        return (binary % 1_000_000).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Checks <paramref name="code"/> against the steps <c>now−1 … now+1</c> and returns the matching step, or null. Steps at or before
    /// <paramref name="lastUsedStep"/> never match (a code cannot be replayed).
    /// </summary>
    public static long? Verify(string base32Secret, string? code, DateTime nowUtc, long? lastUsedStep, int window = 1)
    {
        var normalized = code?.Replace(" ", string.Empty, StringComparison.Ordinal).Trim();
        if (normalized is not { Length: Digits } || !normalized.All(char.IsAsciiDigit))
        {
            return null;
        }

        var current = StepAt(nowUtc);
        for (var step = current - window; step <= current + window; step++)
        {
            if (lastUsedStep is { } last && step <= last)
            {
                continue;
            }

            if (CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Code(base32Secret, step)), Encoding.ASCII.GetBytes(normalized)))
            {
                return step;
            }
        }

        return null;
    }

    public static string Base32Encode(ReadOnlySpan<byte> data)
    {
        var sb = new StringBuilder((data.Length * 8 + 4) / 5);
        int buffer = 0, bits = 0;
        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bits += 8;
            while (bits >= 5)
            {
                sb.Append(Alphabet[(buffer >> (bits - 5)) & 31]);
                bits -= 5;
            }
        }

        if (bits > 0)
        {
            sb.Append(Alphabet[(buffer << (5 - bits)) & 31]);
        }

        return sb.ToString();
    }

    public static byte[] Base32Decode(string value)
    {
        var clean = value.Trim().TrimEnd('=').Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
        var output = new List<byte>(clean.Length * 5 / 8);
        int buffer = 0, bits = 0;
        foreach (var c in clean)
        {
            var index = Alphabet.IndexOf(c);
            if (index < 0)
            {
                throw new FormatException("Invalid Base32 character.");
            }

            buffer = (buffer << 5) | index;
            bits += 5;
            if (bits >= 8)
            {
                output.Add((byte)((buffer >> (bits - 8)) & 0xFF));
                bits -= 8;
            }
        }

        return output.ToArray();
    }
}
