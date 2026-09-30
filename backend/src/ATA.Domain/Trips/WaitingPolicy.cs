using System.Text.Json;

namespace ATA.Domain.Trips;

/// <summary>
/// <c>trips.waiting_policy</c> (doc 11 §F17.6): the free waiting minutes and the per-minute waiting charge fixed when an airport trip is created;
/// they take precedence over the pricing rule in the waiting calculation and the F14 cancellation stages.
/// </summary>
public sealed record WaitingPolicy(int FreeMinutes, decimal PerMinute)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public string ToJson() => JsonSerializer.Serialize(this, Json);

    public static WaitingPolicy? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<WaitingPolicy>(json, Json);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
