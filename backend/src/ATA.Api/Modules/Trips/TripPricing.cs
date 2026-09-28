using ATA.Domain.Catalog;
using ATA.Domain.Common;

namespace ATA.Api.Modules.Trips;

public readonly record struct GeoPoint(decimal Lat, decimal Lng);

public sealed record RouteEstimate(int DistanceMeters, int DurationSeconds);

public sealed record FareQuote(decimal Fare, decimal DriverNetEarnings);

/// <summary>Route estimation and fare calculation; <see cref="FlatPricing"/> is the F8 stand-in for the F10 pricing engine.</summary>
public interface IPricingService
{
    RouteEstimate EstimateRoute(GeoPoint pickup, IReadOnlyList<GeoPoint> stops, GeoPoint dropoff);

    /// <summary>Seconds needed to drive <paramref name="distanceMeters"/> at the assumed average speed.</summary>
    int EtaSeconds(double distanceMeters);

    FareQuote Quote(RideCategory category, int distanceMeters, int durationSeconds, int waitingSeconds = 0);
}

/// <summary>
/// <c>fare = baseFare + perKm × km + perMinute × min + bookingFee</c> (never below <c>minFare</c>), with distance = Haversine × 1.3
/// and duration at 30 km/h until a maps provider is integrated. Billable waiting minutes are charged at <c>perMinute</c>.
/// </summary>
public sealed class FlatPricing : IPricingService
{
    public const double RoadFactor = 1.3;
    public const double AverageSpeedKmh = 30;
    private const double MetersPerSecond = AverageSpeedKmh * 1000 / 3600;

    public RouteEstimate EstimateRoute(GeoPoint pickup, IReadOnlyList<GeoPoint> stops, GeoPoint dropoff)
    {
        var meters = 0d;
        var previous = pickup;
        foreach (var point in stops.Append(dropoff))
        {
            meters += Geo.HaversineMeters(previous.Lat, previous.Lng, point.Lat, point.Lng);
            previous = point;
        }

        var distance = (int)Math.Round(meters * RoadFactor);
        return new RouteEstimate(distance, EtaSeconds(distance));
    }

    public int EtaSeconds(double distanceMeters) => (int)Math.Ceiling(Math.Max(0, distanceMeters) / MetersPerSecond);

    public FareQuote Quote(RideCategory category, int distanceMeters, int durationSeconds, int waitingSeconds = 0)
    {
        var km = distanceMeters / 1000m;
        var minutes = (durationSeconds + Math.Max(0, waitingSeconds)) / 60m;
        var fare = category.BaseFare + category.PerKm * km + category.PerMinute * minutes + category.BookingFee;
        fare = Math.Max(category.MinFare, decimal.Round(fare, 2, MidpointRounding.AwayFromZero));
        var driverNet = decimal.Round(fare * category.DriverSharePercent / 100m, 2, MidpointRounding.AwayFromZero);
        return new FareQuote(fare, driverNet);
    }
}
