using ATA.Domain.Common;
using Microsoft.Net.Http.Headers;

namespace ATA.Api.Common;

/// <summary>Resolves the request language from <c>Accept-Language</c> (Arabic is the default).</summary>
public static class Localization
{
    public static Language GetLanguage(this HttpContext context)
    {
        var header = context.Request.Headers.AcceptLanguage.ToString();
        if (string.IsNullOrWhiteSpace(header))
        {
            return Language.Ar;
        }

        var preferred = header
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(v => StringWithQualityHeaderValue.TryParse(v, out var parsed) ? parsed : null)
            .OfType<StringWithQualityHeaderValue>()
            .OrderByDescending(v => v.Quality ?? 1)
            .Select(v => v.Value.ToString())
            .FirstOrDefault(v => v.StartsWith("ar", StringComparison.OrdinalIgnoreCase) || v.StartsWith("en", StringComparison.OrdinalIgnoreCase));

        return preferred is not null && preferred.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? Language.En : Language.Ar;
    }

    public static string Pick(this Language language, string ar, string en) => language == Language.En ? en : ar;

    public static string? PickOptional(this Language language, string? ar, string? en) => language == Language.En ? en : ar;
}
