using System.Text.Json;
using ATA.Domain.Common;

namespace ATA.Domain.Pricing;

/// <summary>A polygon service area (<c>zones</c>); the city's <c>city_default</c> zone catches every point outside all other zones.</summary>
public class Zone : AuditableEntity
{
    public const string CityDefaultCode = "city_default";

    public Guid CityId { get; set; }
    public required string Code { get; set; }
    public required string NameAr { get; set; }
    public required string NameEn { get; set; }
    /// <summary>Closed ring as <c>[[lat,lng],…]</c> JSON.</summary>
    public required string Polygon { get; set; }
    public decimal CenterLat { get; set; }
    public decimal CenterLng { get; set; }
    /// <summary>Highest priority wins when zones overlap.</summary>
    public int Priority { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary><c>[{day:0-6, from:"06:00", to:"23:59"}]</c> JSON or null for always open.</summary>
    public string? OperatingHours { get; set; }

    public ICollection<ZoneCategorySetting> CategorySettings { get; set; } = [];

    public bool IsCityDefault => Code == CityDefaultCode;

    public GeoPolygon Ring() => GeoPolygon.Parse(Polygon);

    /// <summary>Whether the zone operates at the given local time; zones without operating hours are always open.</summary>
    public bool IsOpenAt(DateTime localTime) => OperatingSchedule.Parse(OperatingHours).IsOpenAt(localTime);
}

/// <summary>Per-zone switches for a ride category (<c>zone_category_settings</c>).</summary>
public class ZoneCategorySetting : Entity
{
    public const decimal DefaultSurgeCap = 2.5m;

    public Guid ZoneId { get; set; }
    public Guid RideCategoryId { get; set; }
    public bool IsEnabled { get; set; } = true;
    /// <summary>Upper bound for the demand multiplier applied in this zone and category.</summary>
    public decimal SurgeCap { get; set; } = DefaultSurgeCap;
}

/// <summary>A closed ring of WGS-84 points with ray-casting containment.</summary>
public sealed class GeoPolygon
{
    private readonly (double Lat, double Lng)[] _points;

    private GeoPolygon((double Lat, double Lng)[] points)
    {
        _points = points;
        MinLat = points.Min(p => p.Lat);
        MaxLat = points.Max(p => p.Lat);
        MinLng = points.Min(p => p.Lng);
        MaxLng = points.Max(p => p.Lng);
    }

    public double MinLat { get; }
    public double MaxLat { get; }
    public double MinLng { get; }
    public double MaxLng { get; }

    public IReadOnlyList<(double Lat, double Lng)> Points => _points;

    /// <summary>Parses <c>[[lat,lng],…]</c>; the ring may or may not repeat its first point at the end.</summary>
    public static GeoPolygon Parse(string json)
    {
        var raw = JsonSerializer.Deserialize<double[][]>(json) ?? throw new DomainException(ErrorCodes.ValidationFailed, new { polygon = "invalid" });
        var points = raw.Where(p => p.Length >= 2).Select(p => (p[0], p[1])).ToList();
        if (points.Count > 1 && points[0] == points[^1])
        {
            points.RemoveAt(points.Count - 1);
        }

        if (points.Count < 3)
        {
            throw new DomainException(ErrorCodes.ValidationFailed, new { polygon = "must have at least 3 distinct points" });
        }

        return new GeoPolygon(points.ToArray());
    }

    public static bool TryParse(string? json, out GeoPolygon? polygon)
    {
        polygon = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            polygon = Parse(json);
            return true;
        }
        catch (Exception ex) when (ex is JsonException or DomainException)
        {
            return false;
        }
    }

    /// <summary>Ray casting (even-odd rule) with a bounding-box pre-check.</summary>
    public bool Contains(double lat, double lng)
    {
        if (lat < MinLat || lat > MaxLat || lng < MinLng || lng > MaxLng)
        {
            return false;
        }

        var inside = false;
        for (int i = 0, j = _points.Length - 1; i < _points.Length; j = i++)
        {
            var (latI, lngI) = _points[i];
            var (latJ, lngJ) = _points[j];
            var crosses = (lngI > lng) != (lngJ > lng)
                          && lat < (latJ - latI) * (lng - lngI) / (lngJ - lngI) + latI;
            if (crosses)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    public bool Contains(decimal lat, decimal lng) => Contains((double)lat, (double)lng);

    public (double Lat, double Lng) Centroid() => (_points.Average(p => p.Lat), _points.Average(p => p.Lng));
}

/// <summary>Weekly operating windows (<c>day</c> 0 = Sunday … 6 = Saturday, local time).</summary>
public sealed class OperatingSchedule
{
    public sealed record Window(int Day, TimeOnly From, TimeOnly To);

    private static readonly OperatingSchedule Always = new([]);
    private readonly Window[] _windows;

    private OperatingSchedule(Window[] windows) => _windows = windows;

    public IReadOnlyList<Window> Windows => _windows;

    public bool IsAlwaysOpen => _windows.Length == 0;

    public static OperatingSchedule Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Always;
        }

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new DomainException(ErrorCodes.ValidationFailed, new { operatingHours = "must be an array" });
        }

        var windows = new List<Window>();
        foreach (var item in doc.RootElement.EnumerateArray())
        {
            if (!item.TryGetProperty("day", out var day) || !day.TryGetInt32(out var d) || d is < 0 or > 6
                || !item.TryGetProperty("from", out var from) || !TimeOnly.TryParse(from.GetString(), out var f)
                || !item.TryGetProperty("to", out var to) || !TimeOnly.TryParse(to.GetString(), out var t))
            {
                throw new DomainException(ErrorCodes.ValidationFailed, new { operatingHours = "each item needs day (0-6), from and to (HH:mm)" });
            }

            windows.Add(new Window(d, f, t));
        }

        return windows.Count == 0 ? Always : new OperatingSchedule(windows.ToArray());
    }

    public bool IsOpenAt(DateTime localTime)
    {
        if (IsAlwaysOpen)
        {
            return true;
        }

        var day = (int)localTime.DayOfWeek;
        var time = TimeOnly.FromDateTime(localTime);
        return _windows.Any(w => w.Day == day && time >= w.From && time <= w.To);
    }
}
