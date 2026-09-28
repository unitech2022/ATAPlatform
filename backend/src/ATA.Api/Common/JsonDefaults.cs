using System.Text.Json;
using System.Text.Json.Serialization;
using ATA.Domain.Common;

namespace ATA.Api.Common;

/// <summary>JSON conventions shared by the HTTP pipeline and internal serialization (audit snapshots, notification data).</summary>
public static class JsonDefaults
{
    public static readonly JsonSerializerOptions Options = Configure(new JsonSerializerOptions(JsonSerializerDefaults.Web));

    public static JsonSerializerOptions Configure(JsonSerializerOptions options)
    {
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
        options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
        return options;
    }
}

/// <summary>Parses snake_case enum values from query strings case-insensitively (<c>under_review</c> → <c>UnderReview</c>).</summary>
public static class QueryEnum
{
    public static TEnum? Parse<TEnum>(string? value, string parameterName) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Enum.TryParse<TEnum>(value.Replace("_", string.Empty), ignoreCase: true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed
            : throw new DomainException(ErrorCodes.ValidationFailed, new Dictionary<string, string>
            {
                [parameterName] = "must be one of: " + string.Join('|', Enum.GetNames<TEnum>().Select(n => JsonNamingPolicy.SnakeCaseLower.ConvertName(n))),
            });
    }
}
