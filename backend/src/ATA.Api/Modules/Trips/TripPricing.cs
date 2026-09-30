using ATA.Api.Modules.Pricing;
using ATA.Domain.Catalog;
using ATA.Domain.Common;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Trips;

public readonly record struct GeoPoint(decimal Lat, decimal Lng);

public sealed record RouteEstimate(int DistanceMeters, int DurationSeconds);

/// <summary>
/// Inputs of a fare calculation. <paramref name="At"/> is the pickup time (UTC) used for time multipliers and demand; a locked demand
/// multiplier (from the quote the trip was created with) can be supplied so the final fare keeps the multiplier the passenger accepted.
/// <paramref name="WaitingPerMinute"/> (F17 airport waiting policy) replaces the pricing rule's per-minute waiting charge.
/// </summary>
public sealed record FareRequest(
    RideCategory Category,
    GeoPoint Pickup,
    GeoPoint? Dropoff,
    int DistanceMeters,
    int DurationSeconds,
    DateTime At,
    int WaitingSeconds = 0,
    DemandReading? LockedDemand = null,
    decimal? WaitingPerMinute = null);

/// <summary>Route estimation and fare calculation; <see cref="RulePricingService"/> is the F10 engine, <see cref="FlatPricing"/> its fallback.</summary>
public interface IPricingService
{
    RouteEstimate EstimateRoute(GeoPoint pickup, IReadOnlyList<GeoPoint> stops, GeoPoint dropoff);

    /// <summary>Seconds needed to drive <paramref name="distanceMeters"/> at the assumed average speed.</summary>
    int EtaSeconds(double distanceMeters);

    Task<FareCalculation> CalculateAsync(FareRequest request, CancellationToken ct);

    /// <summary>Free waiting minutes at the pickup for this category/zone (from the pricing rule, else <c>Trips:FreeWaitingMinutes</c>).</summary>
    Task<int> FreeWaitingMinutesAsync(RideCategory category, GeoPoint pickup, DateTime at, CancellationToken ct);

    /// <summary>Per-minute waiting charge for this category/zone (from the pricing rule, else the category's per-minute rate).</summary>
    Task<decimal> WaitingPerMinuteAsync(RideCategory category, GeoPoint pickup, DateTime at, CancellationToken ct);
}

/// <summary>
/// F8 pricing kept as the fallback when no pricing rule matches: <c>fare = baseFare + perKm × km + perMinute × min + bookingFee</c>
/// (never below <c>minFare</c>) from the <c>ride_categories</c> columns, with distance = Haversine × 1.3 and duration at 30 km/h until a
/// maps provider is integrated. Billable waiting minutes are charged at <c>perMinute</c>.
/// </summary>
public sealed class FlatPricing(IOptions<PricingOptions> pricingOptions, IOptions<TripOptions> tripOptions) : IPricingService
{
    public const double RoadFactor = 1.3;
    public const double AverageSpeedKmh = 30;
    private const double MetersPerSecond = AverageSpeedKmh * 1000 / 3600;
    private readonly PricingOptions _pricing = pricingOptions.Value;
    private readonly TripOptions _trips = tripOptions.Value;

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

    public Task<FareCalculation> CalculateAsync(FareRequest request, CancellationToken ct) => Task.FromResult(Calculate(request));

    public Task<int> FreeWaitingMinutesAsync(RideCategory category, GeoPoint pickup, DateTime at, CancellationToken ct) => Task.FromResult(_trips.FreeWaitingMinutes);

    public Task<decimal> WaitingPerMinuteAsync(RideCategory category, GeoPoint pickup, DateTime at, CancellationToken ct) => Task.FromResult(category.PerMinute);

    public FareCalculation Calculate(FareRequest request)
    {
        var category = request.Category;
        var km = request.DistanceMeters / 1000m;
        var minutes = request.DurationSeconds / 60m;
        var waitingMinutes = Math.Max(0, request.WaitingSeconds) / 60m;
        var distanceFare = PricingMath.Round2(category.PerKm * km);
        var timeFare = PricingMath.Round2(category.PerMinute * minutes);
        var waitingFare = PricingMath.Round2((request.WaitingPerMinute ?? category.PerMinute) * waitingMinutes);
        var fare = category.BaseFare + distanceFare + timeFare + waitingFare + category.BookingFee;
        var minApplied = fare < category.MinFare;
        fare = Math.Max(category.MinFare, PricingMath.Round2(fare));
        var driverNet = PricingMath.Round2(fare * category.DriverSharePercent / 100m);
        var breakdown = new FareBreakdown(category.BaseFare, distanceFare, timeFare, waitingFare, minApplied, 1m, null, 1m, category.BookingFee, 0m, 0m);
        return new FareCalculation(
            fare, driverNet, category.DriverSharePercent,
            PricingMath.RoundToHalf(fare * _pricing.OfferMinPercent / 100m), PricingMath.RoundToHalf(fare * _pricing.OfferMaxPercent / 100m),
            breakdown, request.LockedDemand ?? DemandReading.Neutral(), null, null, null, FareCalculation.SourceFallback)
        {
            Base = fare,
            ShareBase = fare,
        };
    }
}
