using ATA.Api.Modules.Payments;
using ATA.Api.Modules.Pricing;
using ATA.Domain.Common;
using ATA.Domain.Drivers;
using ATA.Domain.Matching;
using ATA.Domain.Trips;
using ATA.Domain.Wallet;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Trips.Matching;

/// <summary>
/// What to search for. <paramref name="RadiusMeters"/> overrides the settings radius (used by the expanding rounds); <paramref name="TripId"/>
/// excludes drivers already offered the trip and <paramref name="PassengerId"/> enables the favourite-driver bonus.
/// </summary>
public sealed record MatchCriteria(
    decimal PickupLat,
    decimal PickupLng,
    Guid RideCategoryId,
    bool PreferFemaleDriver,
    IReadOnlyCollection<Guid> ExcludedDriverIds,
    int? RadiusMeters = null,
    Guid? PassengerId = null,
    Guid? TripId = null);

public sealed record DriverCandidate(Guid DriverId, Guid UserId, Guid VehicleId, int DistanceMeters, int EtaSeconds, decimal Score);

/// <summary>Finds eligible drivers for a pickup, best score first (<see cref="ScoringMatcher"/> is the F9 engine).</summary>
public interface IMatcher
{
    Task<IReadOnlyList<DriverCandidate>> FindCandidatesAsync(MatchCriteria criteria, CancellationToken ct);
}

/// <summary>F16 hook: the passenger's favourite drivers get the <c>favorite</c> score bonus. No favourites exist before F16.</summary>
public interface IFavoriteDriverProvider
{
    Task<IReadOnlySet<Guid>> FavoriteDriverIdsAsync(Guid passengerId, CancellationToken ct);
}

public sealed class NoFavoriteDrivers : IFavoriteDriverProvider
{
    public Task<IReadOnlySet<Guid>> FavoriteDriverIdsAsync(Guid passengerId, CancellationToken ct) => Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());
}

/// <summary><paramref name="MatchingFactor"/> multiplies the final score (F14 <c>deprioritize_factor</c>; 1 when not deprioritised).</summary>
public sealed record DriverReliability(decimal AcceptanceRate, decimal CancellationRate, bool IsBlocked, decimal MatchingFactor = 1m)
{
    public static readonly DriverReliability Neutral = new(1m, 0m, false);
}

/// <summary>F14 hook: acceptance/cancellation rates over the last 30 days and temporary blocks. The default reads the F8 counters and trips.</summary>
public interface IDriverReliabilityProvider
{
    Task<IReadOnlyDictionary<Guid, DriverReliability>> GetAsync(IReadOnlyCollection<Guid> driverIds, DateTime now, CancellationToken ct);
}

public sealed class CounterDriverReliability(AtaDbContext db) : IDriverReliabilityProvider
{
    private static readonly TimeSpan Window = TimeSpan.FromDays(30);

    public async Task<IReadOnlyDictionary<Guid, DriverReliability>> GetAsync(IReadOnlyCollection<Guid> driverIds, DateTime now, CancellationToken ct)
    {
        var ids = driverIds.ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, DriverReliability>();
        }

        var counters = await db.Drivers.AsNoTracking().Where(d => ids.Contains(d.Id)).Select(d => new { d.Id, d.AcceptanceCount, d.RejectionCount }).ToListAsync(ct);
        var since = now - Window;
        var trips = await db.Trips.AsNoTracking()
            .Where(t => t.DriverId != null && ids.Contains(t.DriverId!.Value) && t.AssignedAt >= since)
            .GroupBy(t => t.DriverId!.Value)
            .Select(g => new { DriverId = g.Key, Assigned = g.Count(), Cancelled = g.Count(t => t.Status == TripStatus.Cancelled && t.CancelledBy == CancelledBy.Driver) })
            .ToListAsync(ct);
        var cancellations = trips.ToDictionary(t => t.DriverId, t => t.Assigned == 0 ? 0m : (decimal)t.Cancelled / t.Assigned);
        return counters.ToDictionary(
            c => c.Id,
            c =>
            {
                var offers = c.AcceptanceCount + c.RejectionCount;
                var acceptance = offers == 0 ? 1m : (decimal)c.AcceptanceCount / offers;
                return new DriverReliability(acceptance, cancellations.GetValueOrDefault(c.Id), false);
            });
    }
}

/// <summary>Effective matching parameters for a zone/category (a <c>matching_settings</c> row or the <c>Matching:*</c> defaults).</summary>
public sealed record ResolvedMatchingSettings(
    Guid? SettingsId,
    int RadiusMeters,
    int MaxRadiusMeters,
    int RadiusStepMeters,
    int OfferTimeoutSeconds,
    int SearchTimeoutSeconds,
    int MaxCandidates,
    MatchingWeights Weights,
    bool AllowCategoryUpgrade,
    bool PreferFavoriteDriver);

/// <summary>Process-wide cache of active <c>matching_settings</c>; invalidated by the admin endpoints.</summary>
public sealed class MatchingSettingsCache
{
    private IReadOnlyList<MatchingSettings>? _rows;

    public IReadOnlyList<MatchingSettings>? Rows
    {
        get => Volatile.Read(ref _rows);
        set => Volatile.Write(ref _rows, value);
    }

    public void Invalidate() => Rows = null;
}

/// <summary>Resolves settings zone+category → zone → category → global → <c>Matching:*</c> configuration.</summary>
public sealed class MatchingSettingsProvider(AtaDbContext db, MatchingSettingsCache cache, IOptions<MatchingOptions> options)
{
    private readonly MatchingOptions _options = options.Value;

    public async Task<ResolvedMatchingSettings> ResolveAsync(Guid? zoneId, Guid rideCategoryId, CancellationToken ct)
    {
        var rows = cache.Rows;
        if (rows is null)
        {
            rows = await db.MatchingSettings.AsNoTracking().Where(m => m.IsActive).ToListAsync(ct);
            cache.Rows = rows;
        }

        var row = rows
            .Where(m => (m.ZoneId == null || m.ZoneId == zoneId) && (m.RideCategoryId == null || m.RideCategoryId == rideCategoryId))
            .OrderByDescending(m => (m.ZoneId != null ? 2 : 0) + (m.RideCategoryId != null ? 1 : 0))
            .ThenByDescending(m => m.UpdatedAt)
            .FirstOrDefault();
        if (row is null)
        {
            return new ResolvedMatchingSettings(null, _options.RadiusMeters, _options.MaxRadiusMeters, _options.RadiusStepMeters, _options.OfferTimeoutSeconds,
                _options.SearchTimeoutSeconds, _options.MaxCandidates, MatchingWeights.Default, _options.AllowUpgrade, true);
        }

        return new ResolvedMatchingSettings(row.Id, row.RadiusMeters, row.MaxRadiusMeters, row.RadiusStepMeters, row.OfferTimeoutSeconds, row.SearchTimeoutSeconds,
            row.MaxCandidates, row.ParseWeights(), row.AllowCategoryUpgrade || _options.AllowUpgrade, row.PreferFavoriteDriver);
    }
}

/// <summary>
/// The F9 matcher. Geographic search (bounding box in SQL, Haversine in memory) inside the round's radius, eligibility filters (online,
/// approved, free, fresh location, category match or upgrade, gender, zone allows the category, no expired documents, not blocked, not
/// already offered the trip) and a weighted score in [0, 1]:
/// <c>distance: 1 − d/maxRadius · eta: 1 − eta/900 · rating: (rating − 3)/2 · acceptance rate · 1 − cancellation rate · tier · favourite</c>.
/// </summary>
public sealed class ScoringMatcher(
    AtaDbContext db,
    IPricingService pricing,
    ZoneResolver zones,
    MatchingSettingsProvider settings,
    IFavoriteDriverProvider favorites,
    IDriverReliabilityProvider reliability,
    IOptions<MatchingOptions> options,
    IOptions<PayoutsOptions> payouts,
    IClock clock,
    Incentives.TierRuleProvider tierRules) : IMatcher
{
    private const double MaxEtaSeconds = 900d;
    private readonly MatchingOptions _options = options.Value;

    public async Task<IReadOnlyList<DriverCandidate>> FindCandidatesAsync(MatchCriteria criteria, CancellationToken ct)
    {
        var category = await db.RideCategories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == criteria.RideCategoryId, ct);
        if (category is null)
        {
            return [];
        }

        var now = clock.UtcNow;
        var zone = await zones.ResolveAsync(criteria.PickupLat, criteria.PickupLng, now, ct);
        if (zone is not null && !zone.AllowsCategory(category.Id))
        {
            return [];
        }

        var resolved = await settings.ResolveAsync(zone?.Id, category.Id, ct);
        var radius = criteria.RadiusMeters ?? resolved.RadiusMeters;
        var categoryIds = resolved.AllowCategoryUpgrade
            ? await db.RideCategories.AsNoTracking().Where(c => c.IsActive && c.SortOrder >= category.SortOrder).Select(c => c.Id).ToListAsync(ct)
            : [category.Id];

        var (deltaLat, deltaLng) = Geo.BoundingBox(criteria.PickupLat, radius);
        var minLat = criteria.PickupLat - deltaLat;
        var maxLat = criteria.PickupLat + deltaLat;
        var minLng = criteria.PickupLng - deltaLng;
        var maxLng = criteria.PickupLng + deltaLng;
        var freshAfter = now.AddSeconds(-_options.LocationMaxAgeSeconds);
        var excluded = criteria.ExcludedDriverIds.ToList();
        if (criteria.TripId is { } tripId)
        {
            excluded.AddRange(await db.TripOffers.AsNoTracking().Where(o => o.TripId == tripId).Select(o => o.DriverId).ToListAsync(ct));
        }

        var query = from loc in db.DriverLocations.AsNoTracking()
                    join d in db.Drivers.AsNoTracking() on loc.DriverId equals d.Id
                    join v in db.Vehicles.AsNoTracking() on d.Id equals v.DriverId
                    where loc.IsOnline && d.IsOnline && d.ApplicationStatus == ApplicationStatus.Approved && d.CurrentTripId == null
                          && v.IsActive && categoryIds.Contains(v.RideCategoryId)
                          && loc.UpdatedAt >= freshAfter
                          && loc.Lat >= minLat && loc.Lat <= maxLat && loc.Lng >= minLng && loc.Lng <= maxLng
                          && !excluded.Contains(d.Id)
                    select new { d.Id, d.UserId, VehicleId = v.Id, loc.Lat, loc.Lng, d.Gender, d.RatingAvg, d.Tier };

        if (criteria.PreferFemaleDriver)
        {
            query = query.Where(x => x.Gender == Gender.Female);
        }

        var rows = await query.ToListAsync(ct);
        var inRange = rows
            .Select(r => (Row: r, Distance: Geo.HaversineMeters(criteria.PickupLat, criteria.PickupLng, r.Lat, r.Lng)))
            .Where(x => x.Distance <= radius)
            .ToList();
        if (inRange.Count == 0)
        {
            return [];
        }

        var ids = inRange.Select(x => x.Row.Id).ToList();
        var today = DateOnly.FromDateTime(now);
        var expiredDocuments = (await db.DriverDocuments.AsNoTracking()
            .Where(doc => ids.Contains(doc.DriverId) && doc.ExpiresAt != null && doc.ExpiresAt < today && doc.Status != DocumentStatus.Rejected)
            .Select(doc => doc.DriverId).ToListAsync(ct)).ToHashSet();
        var reliabilities = await reliability.GetAsync(ids, now, ct);
        // F11: drivers whose cash debt exceeds Payouts:MaxCashDebt are not offered trips.
        var userIds = inRange.Select(x => x.Row.UserId).ToList();
        var debtFloor = -payouts.Value.MaxCashDebt;
        var indebted = (await db.Wallets.AsNoTracking()
            .Where(w => w.Kind == WalletKind.Driver && userIds.Contains(w.UserId) && w.Balance < debtFloor)
            .Select(w => w.UserId).ToListAsync(ct)).ToHashSet();
        var favoriteIds = criteria.PassengerId is { } passengerId && resolved.PreferFavoriteDriver
            ? await favorites.FavoriteDriverIdsAsync(passengerId, ct)
            : new HashSet<Guid>();

        // F15: norm_tier comes from driver_tier_rules.matching_norm.
        var tierNorms = await tierRules.MatchingNormsAsync(ct);
        var weights = resolved.Weights;
        var maxRadius = Math.Max(resolved.MaxRadiusMeters, radius);
        var candidates = new List<DriverCandidate>(inRange.Count);
        foreach (var (row, distance) in inRange)
        {
            var stats = reliabilities.GetValueOrDefault(row.Id, DriverReliability.Neutral);
            if (expiredDocuments.Contains(row.Id) || stats.IsBlocked || indebted.Contains(row.UserId))
            {
                continue;
            }

            var eta = pricing.EtaSeconds(distance * FlatPricing.RoadFactor);
            var normTier = tierNorms.TryGetValue(row.Tier, out var norm) ? norm : Domain.Incentives.TierMath.DefaultNorm(row.Tier);
            var score = decimal.Round(Score(weights, distance, maxRadius, eta, row.RatingAvg, stats, normTier, favoriteIds.Contains(row.Id)) * Math.Clamp(stats.MatchingFactor, 0m, 1m), 4, MidpointRounding.AwayFromZero);
            candidates.Add(new DriverCandidate(row.Id, row.UserId, row.VehicleId, (int)Math.Round(distance), eta, score));
        }

        return candidates.OrderByDescending(c => c.Score).ThenBy(c => c.DistanceMeters).ToList();
    }

    public static decimal Score(MatchingWeights w, double distanceMeters, double maxRadiusMeters, int etaSeconds, decimal rating, DriverReliability stats, DriverTier tier, bool favorite) =>
        Score(w, distanceMeters, maxRadiusMeters, etaSeconds, rating, stats, Domain.Incentives.TierMath.DefaultNorm(tier), favorite);

    /// <summary><paramref name="normTier"/> is the tier's <c>matching_norm</c> (F15).</summary>
    public static decimal Score(MatchingWeights w, double distanceMeters, double maxRadiusMeters, int etaSeconds, decimal rating, DriverReliability stats, decimal normTier, bool favorite)
    {
        var normDistance = Clamp(1m - (decimal)(distanceMeters / maxRadiusMeters));
        var normEta = Clamp(1m - (decimal)(etaSeconds / MaxEtaSeconds));
        var normRating = Clamp((rating - 3m) / 2m);
        var normAcceptance = Clamp(stats.AcceptanceRate);
        var normCancellation = Clamp(1m - stats.CancellationRate);
        normTier = Clamp(normTier);
        var normFavorite = favorite ? 1m : 0m;
        var sum = w.Distance * normDistance + w.Eta * normEta + w.Rating * normRating + w.Acceptance * normAcceptance
                  + w.Cancellation * normCancellation + w.Tier * normTier + w.Favorite * normFavorite;
        return decimal.Round(sum / w.Sum, 4, MidpointRounding.AwayFromZero);
    }

    private static decimal Clamp(decimal value) => Math.Clamp(value, 0m, 1m);
}
