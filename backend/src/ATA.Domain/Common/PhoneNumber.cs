using System.Text.RegularExpressions;

namespace ATA.Domain.Common;

/// <summary>Normalizes Saudi mobile numbers to E.164 (<c>+9665XXXXXXXX</c>).</summary>
public static partial class PhoneNumber
{
    [GeneratedRegex(@"^5\d{8}$")]
    private static partial Regex NationalSignificant();

    /// <summary>Accepts <c>05XXXXXXXX</c>, <c>5XXXXXXXX</c>, <c>9665XXXXXXXX</c>, <c>+9665XXXXXXXX</c>, <c>009665XXXXXXXX</c>.</summary>
    public static bool TryNormalize(string? input, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var digits = new string(input.Where(char.IsAsciiDigit).ToArray());
        if (input.Count(c => c == '+') > 1 || input.Any(c => !char.IsAsciiDigit(c) && c != '+' && c != ' ' && c != '-' && c != '(' && c != ')'))
        {
            return false;
        }

        if (digits.StartsWith("00966", StringComparison.Ordinal))
        {
            digits = digits[5..];
        }
        else if (digits.StartsWith("966", StringComparison.Ordinal))
        {
            digits = digits[3..];
        }
        else if (digits.StartsWith('0'))
        {
            digits = digits[1..];
        }

        if (!NationalSignificant().IsMatch(digits))
        {
            return false;
        }

        normalized = "+966" + digits;
        return true;
    }

    public static string Normalize(string? input) =>
        TryNormalize(input, out var normalized) ? normalized : throw new DomainException(ErrorCodes.PhoneInvalid);
}
