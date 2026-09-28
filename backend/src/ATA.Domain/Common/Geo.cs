namespace ATA.Domain.Common;

/// <summary>Great-circle helpers on WGS-84 coordinates.</summary>
public static class Geo
{
    public const double EarthRadiusMeters = 6_371_000d;

    public static double HaversineMeters(double lat1, double lng1, double lat2, double lng2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLng = ToRadians(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return EarthRadiusMeters * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    public static double HaversineMeters(decimal lat1, decimal lng1, decimal lat2, decimal lng2) =>
        HaversineMeters((double)lat1, (double)lng1, (double)lat2, (double)lng2);

    /// <summary>Latitude/longitude deltas that bound a circle of <paramref name="radiusMeters"/> around a point (for SQL pre-filtering).</summary>
    public static (decimal DeltaLat, decimal DeltaLng) BoundingBox(decimal lat, double radiusMeters)
    {
        var deltaLat = radiusMeters / 111_320d;
        var cos = Math.Max(0.01, Math.Cos(ToRadians((double)lat)));
        var deltaLng = radiusMeters / (111_320d * cos);
        return ((decimal)deltaLat, (decimal)deltaLng);
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180d;
}
