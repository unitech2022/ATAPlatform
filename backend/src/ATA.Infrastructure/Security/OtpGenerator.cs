using System.Security.Cryptography;
using System.Text;

namespace ATA.Infrastructure.Security;

public interface IOtpGenerator
{
    /// <summary>Generates a zero-padded numeric code of the given length.</summary>
    string Generate(int digits = 4);

    /// <summary>Hashes a code bound to its request id so that identical codes produce different hashes.</summary>
    string Hash(Guid requestId, string code);
}

public sealed class OtpGenerator : IOtpGenerator
{
    public string Generate(int digits = 4)
    {
        var max = (int)Math.Pow(10, digits);
        return RandomNumberGenerator.GetInt32(0, max).ToString().PadLeft(digits, '0');
    }

    public string Hash(Guid requestId, string code)
    {
        var bytes = HMACSHA256.HashData(requestId.ToByteArray(), Encoding.UTF8.GetBytes(code));
        return Convert.ToHexStringLower(bytes);
    }
}
