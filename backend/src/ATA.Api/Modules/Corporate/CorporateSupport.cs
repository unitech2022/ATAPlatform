using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ATA.Api.Common;
using ATA.Domain.Common;
using ATA.Domain.Corporate;

namespace ATA.Api.Modules.Corporate;

/// <summary>Field rules and small helpers shared by the corporate services.</summary>
public static partial class CorporateRules
{
    [GeneratedRegex(@"^\d{10}$")]
    private static partial Regex CrPattern();

    [GeneratedRegex(@"^3\d{13}3$")]
    private static partial Regex VatPattern();

    [GeneratedRegex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$")]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"^\+[1-9]\d{7,14}$")]
    private static partial Regex E164Pattern();

    [GeneratedRegex(@"^([01]\d|2[0-3]):[0-5]\d$")]
    private static partial Regex TimePattern();

    public static bool IsCrNumber(string? value) => value is not null && CrPattern().IsMatch(value);

    public static bool IsVatNumber(string? value) => value is not null && VatPattern().IsMatch(value);

    public static bool IsEmail(string? value) => value is { Length: <= 254 } && EmailPattern().IsMatch(value);

    public static bool IsTime(string? value) => value is not null && TimePattern().IsMatch(value);

    /// <summary>Saudi numbers are normalised to <c>+9665XXXXXXXX</c>; any other well-formed E.164 number is kept (company contact phones may be landlines or foreign).</summary>
    public static bool TryNormalizePhone(string? input, out string normalized)
    {
        normalized = string.Empty;
        if (PhoneNumber.TryNormalize(input, out normalized))
        {
            return true;
        }

        var trimmed = input?.Trim().Replace(" ", string.Empty);
        if (trimmed is not null && E164Pattern().IsMatch(trimmed))
        {
            normalized = trimmed;
            return true;
        }

        return false;
    }

    public static string NewToken() => Microsoft.IdentityModel.Tokens.Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(24));

    public static string HashToken(string token) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    /// <summary>Start (inclusive) and end (exclusive) of the Riyadh calendar month containing <paramref name="utc"/>, as UTC instants.</summary>
    public static (DateTime Start, DateTime End) MonthBounds(DateTime utc)
    {
        var local = Formats.RiyadhDate(utc);
        var first = new DateOnly(local.Year, local.Month, 1);
        return (Formats.RiyadhMidnightUtc(first), Formats.RiyadhMidnightUtc(first.AddMonths(1)));
    }

    /// <summary>Riyadh-local day range <c>[from, to]</c> (inclusive days) as UTC instants; <c>null</c> bounds stay open.</summary>
    public static (DateTime? From, DateTime? ToExclusive) DayRange(DateOnly? from, DateOnly? to) =>
        (from is { } f ? Formats.RiyadhMidnightUtc(f) : null, to is { } t ? Formats.RiyadhMidnightUtc(t.AddDays(1)) : null);

    public static NationalAddress? ParseAddress(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<NationalAddress>(json, JsonDefaults.Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static string? SerializeAddress(NationalAddress? address) => address is null ? null : JsonSerializer.Serialize(address, JsonDefaults.Options);

    public static NationalAddress? ToAddress(NationalAddressRequest? request) => request is null
        ? null
        : new NationalAddress(Clean(request.BuildingNumber), Clean(request.Street), Clean(request.District), Clean(request.City), Clean(request.PostalCode), Clean(request.AdditionalNumber),
            string.IsNullOrWhiteSpace(request.CountryCode) ? "SA" : request.CountryCode.Trim().ToUpperInvariant());

    /// <summary>Single-line national address for invoices: <c>building street, district, city postal-additional, SA</c>.</summary>
    public static string FormatAddress(NationalAddress? address)
    {
        if (address is null)
        {
            return string.Empty;
        }

        var parts = new[]
        {
            string.Join(' ', new[] { address.BuildingNumber, address.Street }.Where(p => !string.IsNullOrWhiteSpace(p))),
            address.District,
            string.Join(' ', new[] { address.City, string.Join('-', new[] { address.PostalCode, address.AdditionalNumber }.Where(p => !string.IsNullOrWhiteSpace(p))) }.Where(p => !string.IsNullOrWhiteSpace(p))),
            address.CountryCode,
        };
        return string.Join(", ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    public static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static decimal Round2(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    public static string FirstName(string? fullName) => fullName?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
}
