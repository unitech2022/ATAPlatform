using ATA.Api.Common;
using ATA.Domain.Trips;

namespace ATA.Api.Modules.Trips;

internal static class TripValidation
{
    /// <summary>
    /// Validates a place. Quotes only need coordinates (<paramref name="requireLabels"/> = false);
    /// creating a trip also needs the display name and address.
    /// </summary>
    public static Validator Place(this Validator v, string prefix, PlaceRequest? place, bool required, bool requireLabels = true)
    {
        if (place is null)
        {
            return required ? v.Fail(prefix, "required") : v;
        }

        if (requireLabels)
        {
            v.Require($"{prefix}.name", place.Name, 120).Require($"{prefix}.address", place.Address, 500);
        }

        return v
            .Require($"{prefix}.lat", place.Lat)
            .Require($"{prefix}.lng", place.Lng)
            .Rule($"{prefix}.lat", place.Lat is null or (>= -90 and <= 90), "out of range")
            .Rule($"{prefix}.lng", place.Lng is null or (>= -180 and <= 180), "out of range");
    }

    public static Validator Route(this Validator v, PlaceRequest? pickup, PlaceRequest? dropoff, List<PlaceRequest>? stops, bool requireLabels = true)
    {
        v.Place("pickup", pickup, required: true, requireLabels).Place("dropoff", dropoff, required: true, requireLabels);
        if (stops is not null)
        {
            v.Rule("stops", stops.Count <= 5, "must be at most 5");
            for (var i = 0; i < stops.Count; i++)
            {
                v.Place($"stops[{i}]", stops[i], required: true, requireLabels);
            }
        }

        return v;
    }

    /// <summary>
    /// A scheduled booking needs <c>scheduledAt</c>. The window (<c>max_days_ahead</c> measured from the booking time, <c>min_lead_minutes</c>) is enforced by
    /// <c>ScheduleRuleProvider.EnsureWindow</c> with the applicable <c>scheduled_ride_rules</c> row (F17 replaces the F8 10-minute / 30-day limits).
    /// </summary>
    public static Validator Booking(this Validator v, BookingType? bookingType, DateTime? scheduledAt) =>
        v.Rule("scheduledAt", (bookingType ?? BookingType.Now) != BookingType.Scheduled || scheduledAt is not null, "required");

    public static GeoPoint Point(this PlaceRequest place) => new(place.Lat!.Value, place.Lng!.Value);
}
