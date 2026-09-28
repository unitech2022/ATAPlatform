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

    public static Validator Booking(this Validator v, BookingType? bookingType, DateTime? scheduledAt, DateTime now)
    {
        var type = bookingType ?? BookingType.Now;
        return v
            .Rule("scheduledAt", type != BookingType.Scheduled || scheduledAt is not null, "required")
            .Rule("scheduledAt", scheduledAt is null || scheduledAt.Value.ToUniversalTime() > now.AddMinutes(10), "must be at least 10 minutes in the future")
            .Rule("scheduledAt", scheduledAt is null || scheduledAt.Value.ToUniversalTime() <= now.AddDays(30), "must be within 30 days");
    }

    public static GeoPoint Point(this PlaceRequest place) => new(place.Lat!.Value, place.Lng!.Value);
}
