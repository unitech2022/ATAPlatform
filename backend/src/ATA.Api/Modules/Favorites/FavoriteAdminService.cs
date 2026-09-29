using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Api.Modules.Pricing;
using ATA.Api.Modules.Promotions;
using ATA.Domain.Common;
using ATA.Domain.Favorites;
using ATA.Domain.Trips;
using ATA.Domain.Wallet;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Favorites;

/// <summary>Admin console of the favourite-driver discount rules and the favourites KPIs (<c>favorites.manage</c>); rule writes are audited (<c>favorite_discount_rule.create|update|delete</c>).</summary>
public sealed class FavoriteAdminService(AtaDbContext db, IClock clock, ICurrentUser currentUser, AuditService audit, ZoneResolver zones)
{
    private const string EntityType = "favorite_discount_rule";
    private const int TopDriversCount = 10;

    public static string StatusOf(FavoriteDriverDiscountRule r, DateTime now) =>
        !r.IsActive ? "inactive" : r.ValidFrom > now ? "scheduled" : r.ValidTo is { } to && to < now ? "expired" : "active";

    public async Task<IReadOnlyList<FavoriteDiscountRuleDto>> ListAsync(CancellationToken ct)
    {
        var rows = await db.FavoriteDriverDiscountRules.AsNoTracking().OrderByDescending(r => r.Priority).ThenByDescending(r => r.CreatedAt).ThenBy(r => r.Id).ToListAsync(ct);
        var names = await CreatorNamesAsync(rows, ct);
        return rows.Select(r => ToDto(r, names)).ToList();
    }

    public async Task<FavoriteDiscountRuleDto> GetAsync(Guid id, CancellationToken ct)
    {
        var rule = Guard.NotFound(await db.FavoriteDriverDiscountRules.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct));
        return ToDto(rule, await CreatorNamesAsync([rule], ct));
    }

    public async Task<FavoriteDiscountRuleDto> CreateAsync(FavoriteDiscountRuleUpsertRequest request, CancellationToken ct)
    {
        await ValidateAsync(request, ct);
        var rule = new FavoriteDriverDiscountRule { Name = string.Empty, CreatedBy = currentUser.UserId };
        Apply(rule, request);
        db.FavoriteDriverDiscountRules.Add(rule);
        audit.Log("favorite_discount_rule.create", EntityType, rule.Id, null, Snapshot(rule));
        await db.SaveChangesAsync(ct);
        return ToDto(rule, await CreatorNamesAsync([rule], ct));
    }

    public async Task<FavoriteDiscountRuleDto> UpdateAsync(Guid id, FavoriteDiscountRuleUpsertRequest request, CancellationToken ct)
    {
        var rule = Guard.NotFound(await db.FavoriteDriverDiscountRules.FirstOrDefaultAsync(r => r.Id == id, ct));
        await ValidateAsync(request, ct);
        var before = Snapshot(rule);
        Apply(rule, request);
        audit.Log("favorite_discount_rule.update", EntityType, rule.Id, before, Snapshot(rule));
        await db.SaveChangesAsync(ct);
        return ToDto(rule, await CreatorNamesAsync([rule], ct));
    }

    /// <summary>A rule pinned by trips is only deactivated (their receipts keep pointing at it); an unused rule is removed.</summary>
    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var rule = Guard.NotFound(await db.FavoriteDriverDiscountRules.FirstOrDefaultAsync(r => r.Id == id, ct));
        var before = Snapshot(rule);
        if (await db.Trips.AnyAsync(t => t.FavoriteDiscountRuleId == id, ct))
        {
            rule.IsActive = false;
            audit.Log("favorite_discount_rule.delete", EntityType, rule.Id, before, new { deactivated = true, isActive = false });
        }
        else
        {
            db.FavoriteDriverDiscountRules.Remove(rule);
            audit.Log("favorite_discount_rule.delete", EntityType, rule.Id, before, null);
        }

        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// <c>GET /admin/favorites/stats</c> over trips requested in the range (no range = all time; <c>cityId</c> filters by the pickup's zone city):
    /// <c>favoriteRequests</c> = trips with a <c>favorite_driver_id</c>; <c>accepted</c> = the favourite took the trip; <c>fallback</c> = requests that fell back to
    /// normal matching (<c>unavailable</c>, <c>rejected</c>, <c>expired</c>); <c>favoriteBookingRate</c> = favoriteRequests / all trips requested; the discount figures
    /// come from the <c>discount_favorite_driver</c> ledger postings; <c>topDrivers</c> = most saved drivers with their completed favourite trips.
    /// </summary>
    public async Task<FavoriteStatsDto> StatsAsync(DateOnly? from, DateOnly? to, Guid? cityId, CancellationToken ct)
    {
        new Validator().Rule("from", from is null || to is null || from <= to, "must be on or before 'to'").ThrowIfInvalid();
        if (cityId is { } city && !await db.Cities.AnyAsync(c => c.Id == city, ct))
        {
            throw new DomainException(ErrorCodes.ValidationFailed, new Dictionary<string, string> { ["cityId"] = "unknown city" });
        }

        var query = db.Trips.AsNoTracking().AsQueryable();
        if (from is { } f)
        {
            var fromAt = f.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(t => t.RequestedAt >= fromAt);
        }

        if (to is { } t0)
        {
            var toAt = t0.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(t => t.RequestedAt < toAt);
        }

        var trips = await query.Select(t => new { t.Id, t.FavoriteDriverId, t.FavoriteStatus, t.Status, t.DriverId, t.PickupLat, t.PickupLng, t.RequestedAt }).ToListAsync(ct);
        if (cityId is { } cityFilter)
        {
            var all = await zones.AllAsync(ct);
            trips = trips.Where(t => zones.Resolve(all, t.PickupLat, t.PickupLng, t.RequestedAt) is { } zone && zone.CityId == cityFilter).ToList();
        }

        var favorites = trips.Where(t => t.FavoriteDriverId != null).ToList();
        var discountRows = new List<decimal>();
        foreach (var chunk in favorites.Where(t => t.Status == TripStatus.Completed).Select(t => t.Id).Chunk(500))
        {
            var ids = chunk.ToList();
            discountRows.AddRange(await (from e in db.LedgerEntries.AsNoTracking() join j in db.LedgerJournals.AsNoTracking() on e.JournalId equals j.Id
                                         where e.Account == LedgerAccounts.DiscountFavoriteDriver && j.Type == JournalType.TripDiscount && ids.Contains(j.ReferenceId!.Value)
                                         select e.Debit).ToListAsync(ct));
        }

        var favoriteTrips = favorites.Where(t => t.Status == TripStatus.Completed && t.DriverId == t.FavoriteDriverId).GroupBy(t => t.DriverId!.Value).ToDictionary(g => g.Key, g => g.Count());
        var counts = db.FavoriteDrivers.AsNoTracking().AsQueryable();
        var saved = await (cityId is { } c
            ? from fd in counts join d in db.Drivers.AsNoTracking() on fd.DriverId equals d.Id where d.CityId == c group fd by fd.DriverId into g select new { DriverId = g.Key, Count = g.Count() }
            : from fd in counts group fd by fd.DriverId into g select new { DriverId = g.Key, Count = g.Count() }).ToListAsync(ct);
        var top = saved.OrderByDescending(x => x.Count).ThenByDescending(x => favoriteTrips.GetValueOrDefault(x.DriverId)).ThenBy(x => x.DriverId).Take(TopDriversCount).ToList();
        var topIds = top.Select(x => x.DriverId).ToList();
        var names = await (from d in db.Drivers.AsNoTracking() join u in db.Users.AsNoTracking() on d.UserId equals u.Id where topIds.Contains(d.Id) select new { d.Id, u.FullName })
            .ToDictionaryAsync(x => x.Id, x => x.FullName, ct);

        return new FavoriteStatsDto(
            favorites.Count,
            favorites.Count(t => t.FavoriteStatus == FavoriteStatus.Accepted),
            favorites.Count(t => t.FavoriteStatus is FavoriteStatus.Unavailable or FavoriteStatus.Rejected or FavoriteStatus.Expired),
            trips.Count == 0 ? 0m : decimal.Round((decimal)favorites.Count / trips.Count, 4, MidpointRounding.AwayFromZero),
            discountRows.Count, discountRows.Sum(),
            top.Select(x => new FavoriteTopDriverDto(x.DriverId, names.GetValueOrDefault(x.DriverId), x.Count, favoriteTrips.GetValueOrDefault(x.DriverId))).ToList());
    }

    private async Task ValidateAsync(FavoriteDiscountRuleUpsertRequest r, CancellationToken ct)
    {
        var v = new Validator()
            .Require(nameof(r.Name), r.Name, FavoriteDriverDiscountRule.NameMaxLength)
            .Require(nameof(r.DiscountPercent), r.DiscountPercent)
            .Rule(nameof(r.DiscountPercent), r.DiscountPercent is null or (>= FavoriteDriverDiscountRule.MinPercent and <= FavoriteDriverDiscountRule.MaxPercent), "must be between 1 and 50")
            .Rule(nameof(r.DiscountPercent), r.DiscountPercent is null || decimal.Round(r.DiscountPercent.Value, 2) == r.DiscountPercent.Value, "at most 2 decimal places")
            .Require(nameof(r.MaxDiscountAmount), r.MaxDiscountAmount)
            .Rule(nameof(r.MaxDiscountAmount), r.MaxDiscountAmount is null or > 0, "must be positive")
            .Rule(nameof(r.MaxDiscountAmount), r.MaxDiscountAmount is null || decimal.Round(r.MaxDiscountAmount.Value, 2) == r.MaxDiscountAmount.Value, "at most 2 decimal places")
            .Rule(nameof(r.MinFare), r.MinFare is null or >= 0, "must be positive")
            .Rule(nameof(r.Priority), r.Priority is null or >= 0, "must be positive")
            .Require(nameof(r.ValidFrom), r.ValidFrom)
            .Rule(nameof(r.ValidTo), r.ValidFrom is null || r.ValidTo is null || r.ValidTo > r.ValidFrom, "must be after validFrom");
        v.ThrowIfInvalid();
        if (r.RideCategoryIds is { Count: > 0 } categories)
        {
            var known = await db.RideCategories.CountAsync(c => categories.Contains(c.Id), ct);
            v.Rule(nameof(r.RideCategoryIds), known == categories.Distinct().Count(), "unknown ride category");
        }

        if (r.ZoneIds is { Count: > 0 } zoneIds)
        {
            var known = await db.Zones.CountAsync(z => zoneIds.Contains(z.Id), ct);
            v.Rule(nameof(r.ZoneIds), known == zoneIds.Distinct().Count(), "unknown zone");
        }

        v.Rule(nameof(r.BookingTypes), r.BookingTypes is null || r.BookingTypes.All(Enum.IsDefined), "must be now|scheduled");
        v.ThrowIfInvalid();
    }

    private static void Apply(FavoriteDriverDiscountRule rule, FavoriteDiscountRuleUpsertRequest r)
    {
        rule.Name = r.Name!.Trim();
        rule.DiscountPercent = r.DiscountPercent!.Value;
        rule.MaxDiscountAmount = r.MaxDiscountAmount!.Value;
        rule.MinFare = r.MinFare;
        rule.StackableWithPromotions = r.StackableWithPromotions ?? false;
        rule.ValidFrom = r.ValidFrom!.Value.ToUniversalTime();
        rule.ValidTo = r.ValidTo?.ToUniversalTime();
        rule.RideCategoryIds = JsonLists.Serialize(r.RideCategoryIds);
        rule.ZoneIds = JsonLists.Serialize(r.ZoneIds);
        rule.BookingTypes = JsonLists.Serialize(r.BookingTypes);
        rule.Priority = r.Priority ?? 0;
        rule.IsActive = r.IsActive ?? true;
    }

    private static object Snapshot(FavoriteDriverDiscountRule r) => new
    {
        r.Name, r.DiscountPercent, r.MaxDiscountAmount, r.MinFare, r.StackableWithPromotions, r.ValidFrom, r.ValidTo, r.RideCategoryIds, r.ZoneIds, r.BookingTypes, r.Priority, r.IsActive,
    };

    private async Task<Dictionary<Guid, string?>> CreatorNamesAsync(IEnumerable<FavoriteDriverDiscountRule> rules, CancellationToken ct)
    {
        var ids = rules.Where(r => r.CreatedBy != null).Select(r => r.CreatedBy!.Value).Distinct().ToList();
        return ids.Count == 0 ? [] : (await db.Users.AsNoTracking().Where(u => ids.Contains(u.Id)).Select(u => new { u.Id, u.FullName }).ToListAsync(ct)).ToDictionary(u => u.Id, u => u.FullName);
    }

    private FavoriteDiscountRuleDto ToDto(FavoriteDriverDiscountRule r, IReadOnlyDictionary<Guid, string?> creatorNames) =>
        new(r.Id, r.Name, r.DiscountPercent, r.MaxDiscountAmount, r.MinFare, r.StackableWithPromotions, r.ValidFrom, r.ValidTo, JsonLists.Parse<Guid>(r.RideCategoryIds),
            JsonLists.Parse<Guid>(r.ZoneIds), JsonLists.Parse<BookingType>(r.BookingTypes), r.Priority, r.IsActive, StatusOf(r, clock.UtcNow), r.CreatedAt, r.UpdatedAt,
            r.CreatedBy is { } by ? creatorNames.GetValueOrDefault(by) : null);
}
