using System.Text.Json;
using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Domain.Common;
using ATA.Domain.Pricing;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Pricing;

/// <summary>Admin CRUD for zones, pricing rules, demand levels/rules/overrides (all audited) and the per-zone demand overview.</summary>
public sealed class PricingAdminService(AtaDbContext db, ZoneResolver zones, ZoneCache zoneCache, DemandService demand, DemandCache demandCache, AuditService audit, ICurrentUser currentUser, IClock clock)
{
    // ----- zones -----

    public async Task<List<ZoneDto>> ListZonesAsync(CancellationToken ct)
    {
        var rows = await db.Zones.AsNoTracking().Include(z => z.CategorySettings).OrderByDescending(z => z.Priority).ThenBy(z => z.Code).ToListAsync(ct);
        var codes = await CategoryCodesAsync(ct);
        return rows.Select(z => ToDto(z, codes)).ToList();
    }

    public async Task<ZoneDto> GetZoneAsync(Guid id, CancellationToken ct) =>
        ToDto(Guard.NotFound(await db.Zones.AsNoTracking().Include(z => z.CategorySettings).FirstOrDefaultAsync(z => z.Id == id, ct)), await CategoryCodesAsync(ct));

    public async Task<ZoneDto> CreateZoneAsync(ZoneUpsertRequest request, CancellationToken ct)
    {
        var v = new Validator()
            .Require(nameof(request.Code), request.Code, 40)
            .Require(nameof(request.NameAr), request.NameAr, 80)
            .Require(nameof(request.NameEn), request.NameEn, 80)
            .Rule(nameof(request.Polygon), request.Polygon is not null, "required");
        ValidateZone(v, request);
        v.ThrowIfInvalid();

        var code = request.Code!.Trim().ToLowerInvariant();
        var cityId = request.CityId ?? await db.Cities.Where(c => c.IsActive).OrderBy(c => c.Code).Select(c => c.Id).FirstAsync(ct);
        v.Rule(nameof(request.CityId), await db.Cities.AnyAsync(c => c.Id == cityId, ct), "unknown city").ThrowIfInvalid();
        if (await db.Zones.AnyAsync(z => z.Code == code, ct))
        {
            throw new DomainException(ErrorCodes.Conflict, new { code = "already exists" });
        }

        var ring = GeoPolygon.Parse(request.Polygon!.Value.GetRawText());
        var centroid = ring.Centroid();
        var zone = new Zone
        {
            CityId = cityId, Code = code, NameAr = request.NameAr!.Trim(), NameEn = request.NameEn!.Trim(), Polygon = request.Polygon.Value.GetRawText(),
            CenterLat = request.CenterLat ?? (decimal)centroid.Lat, CenterLng = request.CenterLng ?? (decimal)centroid.Lng, Priority = request.Priority ?? 0, IsActive = request.IsActive ?? true,
            OperatingHours = NormalizeOperatingHours(request.OperatingHours),
        };
        await ApplyCategorySettingsAsync(zone, request.ZoneCategorySettings, ct);
        db.Zones.Add(zone);
        var codes = await CategoryCodesAsync(ct);
        audit.Log("zone.create", "zone", zone.Id, null, ToDto(zone, codes));
        await db.SaveChangesAsync(ct);
        zoneCache.Invalidate();
        return ToDto(zone, codes);
    }

    public async Task<ZoneDto> UpdateZoneAsync(Guid id, ZoneUpsertRequest request, CancellationToken ct)
    {
        var v = new Validator();
        ValidateZone(v, request);
        v.ThrowIfInvalid();
        var zone = Guard.NotFound(await db.Zones.Include(z => z.CategorySettings).FirstOrDefaultAsync(z => z.Id == id, ct));
        var codes = await CategoryCodesAsync(ct);
        var before = ToDto(zone, codes);
        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var code = request.Code.Trim().ToLowerInvariant();
            if (code != zone.Code && await db.Zones.AnyAsync(z => z.Code == code, ct))
            {
                throw new DomainException(ErrorCodes.Conflict, new { code = "already exists" });
            }

            zone.Code = code;
        }

        if (request.CityId is { } cityId)
        {
            v.Rule(nameof(request.CityId), await db.Cities.AnyAsync(c => c.Id == cityId, ct), "unknown city").ThrowIfInvalid();
            zone.CityId = cityId;
        }

        if (!string.IsNullOrWhiteSpace(request.NameAr)) zone.NameAr = request.NameAr.Trim();
        if (!string.IsNullOrWhiteSpace(request.NameEn)) zone.NameEn = request.NameEn.Trim();
        if (request.Polygon is { } polygon)
        {
            zone.Polygon = polygon.GetRawText();
            if (request.CenterLat is null && request.CenterLng is null)
            {
                var centroid = GeoPolygon.Parse(zone.Polygon).Centroid();
                zone.CenterLat = (decimal)centroid.Lat;
                zone.CenterLng = (decimal)centroid.Lng;
            }
        }

        if (request.CenterLat is not null) zone.CenterLat = request.CenterLat.Value;
        if (request.CenterLng is not null) zone.CenterLng = request.CenterLng.Value;
        if (request.Priority is not null) zone.Priority = request.Priority.Value;
        if (request.IsActive is not null) zone.IsActive = request.IsActive.Value;
        if (request.OperatingHours is not null) zone.OperatingHours = NormalizeOperatingHours(request.OperatingHours);
        await ApplyCategorySettingsAsync(zone, request.ZoneCategorySettings, ct);
        audit.Log("zone.update", "zone", zone.Id, before, ToDto(zone, codes));
        await db.SaveChangesAsync(ct);
        zoneCache.Invalidate();
        demandCache.Invalidate();
        return ToDto(zone, codes);
    }

    public async Task DeleteZoneAsync(Guid id, CancellationToken ct)
    {
        var zone = Guard.NotFound(await db.Zones.Include(z => z.CategorySettings).FirstOrDefaultAsync(z => z.Id == id, ct));
        if (zone.IsCityDefault)
        {
            throw new DomainException(ErrorCodes.Conflict, new { code = "the city default zone cannot be deleted; deactivate other zones instead" });
        }

        db.Zones.Remove(zone);
        audit.Log("zone.delete", "zone", zone.Id, ToDto(zone, await CategoryCodesAsync(ct)), null);
        await db.SaveChangesAsync(ct);
        zoneCache.Invalidate();
        demandCache.Invalidate();
    }

    private static void ValidateZone(Validator v, ZoneUpsertRequest request)
    {
        v.Rule(nameof(request.Code), request.Code is null || request.Code.Trim().All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '_'), "lowercase letters, digits and underscores only")
         .Rule(nameof(request.CenterLat), request.CenterLat is null or (>= -90 and <= 90), "out of range")
         .Rule(nameof(request.CenterLng), request.CenterLng is null or (>= -180 and <= 180), "out of range");
        if (request.Polygon is { } polygon)
        {
            v.Rule(nameof(request.Polygon), polygon.ValueKind == JsonValueKind.Array && GeoPolygon.TryParse(polygon.GetRawText(), out _), "must be [[lat,lng],…] with at least 3 points");
        }

        if (request.OperatingHours is { } hours && hours.ValueKind is not JsonValueKind.Null)
        {
            try
            {
                OperatingSchedule.Parse(hours.GetRawText());
            }
            catch (DomainException)
            {
                v.Fail(nameof(request.OperatingHours), "must be [{day:0-6, from:\"HH:mm\", to:\"HH:mm\"}]");
            }
        }

        if (request.ZoneCategorySettings is { } settings)
        {
            for (var i = 0; i < settings.Count; i++)
            {
                v.Require($"zoneCategorySettings[{i}].rideCategoryId", settings[i].RideCategoryId)
                 .Rule($"zoneCategorySettings[{i}].surgeCap", settings[i].SurgeCap is null or (>= 1 and <= 10), "must be between 1 and 10");
            }
        }
    }

    private static string? NormalizeOperatingHours(JsonElement? hours) =>
        hours is null || hours.Value.ValueKind is JsonValueKind.Null || OperatingSchedule.Parse(hours.Value.GetRawText()).IsAlwaysOpen ? null : hours.Value.GetRawText();

    private async Task ApplyCategorySettingsAsync(Zone zone, List<ZoneCategorySettingRequest>? settings, CancellationToken ct)
    {
        if (settings is null)
        {
            return;
        }

        var ids = settings.Select(s => s.RideCategoryId!.Value).Distinct().ToList();
        var known = await db.RideCategories.AsNoTracking().Where(c => ids.Contains(c.Id)).Select(c => c.Id).ToListAsync(ct);
        new Validator().Rule("zoneCategorySettings", ids.All(known.Contains), "unknown ride category").ThrowIfInvalid();
        foreach (var stale in zone.CategorySettings.Where(s => !ids.Contains(s.RideCategoryId)).ToList())
        {
            zone.CategorySettings.Remove(stale);
            db.ZoneCategorySettings.Remove(stale);
        }

        foreach (var item in settings)
        {
            var current = zone.CategorySettings.FirstOrDefault(s => s.RideCategoryId == item.RideCategoryId);
            if (current is null)
            {
                current = new ZoneCategorySetting { ZoneId = zone.Id, RideCategoryId = item.RideCategoryId!.Value };
                zone.CategorySettings.Add(current);
            }

            if (item.IsEnabled is not null) current.IsEnabled = item.IsEnabled.Value;
            if (item.SurgeCap is not null) current.SurgeCap = item.SurgeCap.Value;
        }
    }

    private static ZoneDto ToDto(Zone z, Dictionary<Guid, string> codes) => new(
        z.Id, z.CityId, z.Code, z.NameAr, z.NameEn, JsonSerializer.Deserialize<JsonElement>(z.Polygon), z.CenterLat, z.CenterLng, z.Priority, z.IsActive,
        z.OperatingHours is null ? null : JsonSerializer.Deserialize<JsonElement>(z.OperatingHours),
        z.CategorySettings.OrderBy(s => s.RideCategoryId).Select(s => new ZoneCategorySettingDto(s.RideCategoryId, codes.GetValueOrDefault(s.RideCategoryId), s.IsEnabled, s.SurgeCap)).ToList(),
        z.CreatedAt, z.UpdatedAt);

    // ----- pricing rules -----

    public async Task<List<PricingRuleDto>> ListPricingRulesAsync(Guid? rideCategoryId, Guid? zoneId, CancellationToken ct)
    {
        var query = db.PricingRules.AsNoTracking().Include(r => r.TimeMultipliers).AsQueryable();
        if (rideCategoryId is not null) query = query.Where(r => r.RideCategoryId == rideCategoryId);
        if (zoneId is not null) query = query.Where(r => r.ZoneId == zoneId);
        var rows = await query.OrderBy(r => r.RideCategoryId).ThenByDescending(r => r.ZoneId != null).ThenByDescending(r => r.Priority).ToListAsync(ct);
        var codes = await CategoryCodesAsync(ct);
        var zoneCodes = await ZoneCodesAsync(ct);
        return rows.Select(r => ToDto(r, codes, zoneCodes)).ToList();
    }

    public async Task<PricingRuleDto> GetPricingRuleAsync(Guid id, CancellationToken ct) =>
        ToDto(Guard.NotFound(await db.PricingRules.AsNoTracking().Include(r => r.TimeMultipliers).FirstOrDefaultAsync(r => r.Id == id, ct)), await CategoryCodesAsync(ct), await ZoneCodesAsync(ct));

    public async Task<PricingRuleDto> CreatePricingRuleAsync(PricingRuleUpsertRequest request, CancellationToken ct)
    {
        var v = new Validator()
            .Require(nameof(request.RideCategoryId), request.RideCategoryId)
            .Require(nameof(request.Name), request.Name, 120)
            .Require(nameof(request.BaseFare), request.BaseFare)
            .Require(nameof(request.PerKm), request.PerKm)
            .Require(nameof(request.PerMinute), request.PerMinute)
            .Require(nameof(request.MinFare), request.MinFare);
        ValidatePricingRule(v, request);
        v.ThrowIfInvalid();
        await EnsureReferencesAsync(request.RideCategoryId, request.ZoneId, ct);

        var rule = new PricingRule
        {
            RideCategoryId = request.RideCategoryId!.Value, ZoneId = request.ZoneId, Name = request.Name!.Trim(),
            EffectiveFrom = (request.EffectiveFrom ?? clock.UtcNow).ToUniversalTime(),
        };
        ApplyPricingRule(rule, request);
        foreach (var multiplier in request.TimeMultipliers ?? [])
        {
            rule.TimeMultipliers.Add(ToEntity(rule.Id, multiplier));
        }

        db.PricingRules.Add(rule);
        var codes = await CategoryCodesAsync(ct);
        var zoneCodes = await ZoneCodesAsync(ct);
        audit.Log("pricing_rule.create", "pricing_rule", rule.Id, null, ToDto(rule, codes, zoneCodes));
        await db.SaveChangesAsync(ct);
        return ToDto(rule, codes, zoneCodes);
    }

    public async Task<PricingRuleDto> UpdatePricingRuleAsync(Guid id, PricingRuleUpsertRequest request, CancellationToken ct)
    {
        var v = new Validator();
        ValidatePricingRule(v, request);
        v.ThrowIfInvalid();
        var rule = Guard.NotFound(await db.PricingRules.Include(r => r.TimeMultipliers).FirstOrDefaultAsync(r => r.Id == id, ct));
        var codes = await CategoryCodesAsync(ct);
        var zoneCodes = await ZoneCodesAsync(ct);
        var before = ToDto(rule, codes, zoneCodes);
        if (request.RideCategoryId is not null || request.ZoneId is not null)
        {
            await EnsureReferencesAsync(request.RideCategoryId, request.ZoneId, ct);
            if (request.RideCategoryId is not null) rule.RideCategoryId = request.RideCategoryId.Value;
            if (request.ZoneId is not null) rule.ZoneId = request.ZoneId;
        }

        if (!string.IsNullOrWhiteSpace(request.Name)) rule.Name = request.Name.Trim();
        if (request.EffectiveFrom is not null) rule.EffectiveFrom = request.EffectiveFrom.Value.ToUniversalTime();
        ApplyPricingRule(rule, request);
        if (request.TimeMultipliers is not null)
        {
            db.PricingTimeMultipliers.RemoveRange(rule.TimeMultipliers);
            rule.TimeMultipliers.Clear();
            foreach (var multiplier in request.TimeMultipliers)
            {
                rule.TimeMultipliers.Add(ToEntity(rule.Id, multiplier));
            }
        }

        audit.Log("pricing_rule.update", "pricing_rule", rule.Id, before, ToDto(rule, codes, zoneCodes));
        await db.SaveChangesAsync(ct);
        return ToDto(rule, codes, zoneCodes);
    }

    public async Task DeletePricingRuleAsync(Guid id, CancellationToken ct)
    {
        var rule = Guard.NotFound(await db.PricingRules.Include(r => r.TimeMultipliers).FirstOrDefaultAsync(r => r.Id == id, ct));
        db.PricingRules.Remove(rule);
        audit.Log("pricing_rule.delete", "pricing_rule", rule.Id, ToDto(rule, await CategoryCodesAsync(ct), await ZoneCodesAsync(ct)), null);
        await db.SaveChangesAsync(ct);
    }

    private static void ValidatePricingRule(Validator v, PricingRuleUpsertRequest request)
    {
        v.Rule(nameof(request.BaseFare), request.BaseFare is null or >= 0, "must be positive")
         .Rule(nameof(request.PerKm), request.PerKm is null or >= 0, "must be positive")
         .Rule(nameof(request.PerMinute), request.PerMinute is null or >= 0, "must be positive")
         .Rule(nameof(request.BookingFee), request.BookingFee is null or >= 0, "must be positive")
         .Rule(nameof(request.ServiceFeePercent), request.ServiceFeePercent is null or (>= 0 and <= 100), "must be between 0 and 100")
         .Rule(nameof(request.MinFare), request.MinFare is null or >= 0, "must be positive")
         .Rule(nameof(request.WaitingPerMinute), request.WaitingPerMinute is null or >= 0, "must be positive")
         .Rule(nameof(request.FreeWaitingMinutes), request.FreeWaitingMinutes is null or (>= 0 and <= 60), "must be between 0 and 60")
         .Rule(nameof(request.CancellationFee), request.CancellationFee is null or >= 0, "must be positive")
         .Rule(nameof(request.DriverSharePercent), request.DriverSharePercent is null or (>= 0 and <= 100), "must be between 0 and 100")
         .Rule(nameof(request.EffectiveTo), request.EffectiveTo is null || request.EffectiveFrom is null || request.EffectiveTo > request.EffectiveFrom, "must be after effectiveFrom");
        if (request.TimeMultipliers is { } multipliers)
        {
            for (var i = 0; i < multipliers.Count; i++)
            {
                var m = multipliers[i];
                v.Rule($"timeMultipliers[{i}].dayOfWeek", m.DayOfWeek is null or <= 6, "must be 0-6")
                 .Rule($"timeMultipliers[{i}].fromTime", TimeOnly.TryParse(m.FromTime, out _), "must be HH:mm")
                 .Rule($"timeMultipliers[{i}].toTime", TimeOnly.TryParse(m.ToTime, out _), "must be HH:mm")
                 .Rule($"timeMultipliers[{i}].multiplier", m.Multiplier is > 0 and <= 10, "must be between 0 and 10")
                 .Require($"timeMultipliers[{i}].label", m.Label, 40);
            }
        }
    }

    private static void ApplyPricingRule(PricingRule rule, PricingRuleUpsertRequest request)
    {
        if (request.BaseFare is not null) rule.BaseFare = request.BaseFare.Value;
        if (request.PerKm is not null) rule.PerKm = request.PerKm.Value;
        if (request.PerMinute is not null) rule.PerMinute = request.PerMinute.Value;
        if (request.BookingFee is not null) rule.BookingFee = request.BookingFee.Value;
        if (request.ServiceFeePercent is not null) rule.ServiceFeePercent = request.ServiceFeePercent.Value;
        if (request.MinFare is not null) rule.MinFare = request.MinFare.Value;
        if (request.WaitingPerMinute is not null) rule.WaitingPerMinute = request.WaitingPerMinute.Value;
        else if (rule.WaitingPerMinute == 0m && request.PerMinute is not null) rule.WaitingPerMinute = request.PerMinute.Value;
        if (request.FreeWaitingMinutes is not null) rule.FreeWaitingMinutes = request.FreeWaitingMinutes.Value;
        if (request.CancellationFee is not null) rule.CancellationFee = request.CancellationFee.Value;
        if (request.DriverSharePercent is not null) rule.DriverSharePercent = request.DriverSharePercent.Value;
        if (request.EffectiveTo is not null) rule.EffectiveTo = request.EffectiveTo.Value.ToUniversalTime();
        if (request.Priority is not null) rule.Priority = request.Priority.Value;
        if (request.IsActive is not null) rule.IsActive = request.IsActive.Value;
    }

    private static PricingTimeMultiplier ToEntity(Guid ruleId, TimeMultiplierRequest m) => new()
    {
        PricingRuleId = ruleId, DayOfWeek = m.DayOfWeek, FromTime = TimeOnly.Parse(m.FromTime!), ToTime = TimeOnly.Parse(m.ToTime!), Multiplier = m.Multiplier!.Value, Label = m.Label!.Trim(),
    };

    private static PricingRuleDto ToDto(PricingRule r, Dictionary<Guid, string> codes, Dictionary<Guid, string> zoneCodes) => new(
        r.Id, r.RideCategoryId, codes.GetValueOrDefault(r.RideCategoryId), r.ZoneId, r.ZoneId is { } z ? zoneCodes.GetValueOrDefault(z) : null, r.Name,
        r.BaseFare, r.PerKm, r.PerMinute, r.BookingFee, r.ServiceFeePercent, r.MinFare, r.WaitingPerMinute, r.FreeWaitingMinutes, r.CancellationFee, r.DriverSharePercent,
        r.EffectiveFrom, r.EffectiveTo, r.Priority, r.IsActive,
        r.TimeMultipliers.OrderBy(m => m.DayOfWeek ?? -1).ThenBy(m => m.FromTime).Select(m => new TimeMultiplierDto(m.Id, m.DayOfWeek, m.FromTime.ToString("HH:mm"), m.ToTime.ToString("HH:mm"), m.Multiplier, m.Label)).ToList(),
        r.CreatedAt, r.UpdatedAt);

    // ----- demand levels -----

    public async Task<List<DemandLevelDto>> ListDemandLevelsAsync(CancellationToken ct) =>
        (await demand.LevelsAsync(ct)).Select(ToDto).ToList();

    /// <summary>Only the multiplier is editable; codes, names and colours are fixed by the product.</summary>
    public async Task<DemandLevelDto> UpdateDemandLevelAsync(Guid id, DemandLevelUpdateRequest request, CancellationToken ct)
    {
        new Validator().Require(nameof(request.Multiplier), request.Multiplier).Rule(nameof(request.Multiplier), request.Multiplier is null or (>= 1 and <= 10), "must be between 1 and 10").ThrowIfInvalid();
        var level = Guard.NotFound(await db.DemandLevels.FirstOrDefaultAsync(l => l.Id == id, ct));
        var before = ToDto(level);
        level.Multiplier = request.Multiplier!.Value;
        audit.Log("demand_level.update", "demand_level", level.Id, before, ToDto(level));
        await db.SaveChangesAsync(ct);
        demandCache.Invalidate();
        return ToDto(level);
    }

    private static DemandLevelDto ToDto(DemandLevel l) => new(l.Id, l.Code, l.NameAr, l.NameEn, l.Multiplier, l.Color, l.SortOrder);

    // ----- demand rules -----

    public async Task<List<DemandRuleDto>> ListDemandRulesAsync(CancellationToken ct)
    {
        var rows = await db.DemandRules.AsNoTracking().OrderByDescending(r => r.ZoneId != null).ThenBy(r => r.CreatedAt).ToListAsync(ct);
        var codes = await CategoryCodesAsync(ct);
        var zoneCodes = await ZoneCodesAsync(ct);
        return rows.Select(r => ToDto(r, codes, zoneCodes)).ToList();
    }

    public async Task<DemandRuleDto> CreateDemandRuleAsync(DemandRuleUpsertRequest request, CancellationToken ct)
    {
        var v = new Validator()
            .Require(nameof(request.ThresholdModerate), request.ThresholdModerate)
            .Require(nameof(request.ThresholdHigh), request.ThresholdHigh)
            .Require(nameof(request.ThresholdVeryHigh), request.ThresholdVeryHigh);
        ValidateDemandRule(v, request);
        v.ThrowIfInvalid();
        await EnsureReferencesAsync(request.RideCategoryId, request.ZoneId, ct);
        var rule = new DemandRule { ZoneId = request.ZoneId, RideCategoryId = request.RideCategoryId };
        ApplyDemandRule(rule, request);
        db.DemandRules.Add(rule);
        var codes = await CategoryCodesAsync(ct);
        var zoneCodes = await ZoneCodesAsync(ct);
        audit.Log("demand_rule.create", "demand_rule", rule.Id, null, ToDto(rule, codes, zoneCodes));
        await db.SaveChangesAsync(ct);
        demandCache.Invalidate();
        return ToDto(rule, codes, zoneCodes);
    }

    public async Task<DemandRuleDto> UpdateDemandRuleAsync(Guid id, DemandRuleUpsertRequest request, CancellationToken ct)
    {
        var v = new Validator();
        ValidateDemandRule(v, request);
        v.ThrowIfInvalid();
        var rule = Guard.NotFound(await db.DemandRules.FirstOrDefaultAsync(r => r.Id == id, ct));
        var codes = await CategoryCodesAsync(ct);
        var zoneCodes = await ZoneCodesAsync(ct);
        var before = ToDto(rule, codes, zoneCodes);
        await EnsureReferencesAsync(request.RideCategoryId, request.ZoneId, ct);
        if (request.ZoneId is not null) rule.ZoneId = request.ZoneId;
        if (request.RideCategoryId is not null) rule.RideCategoryId = request.RideCategoryId;
        ApplyDemandRule(rule, request);
        audit.Log("demand_rule.update", "demand_rule", rule.Id, before, ToDto(rule, codes, zoneCodes));
        await db.SaveChangesAsync(ct);
        demandCache.Invalidate();
        return ToDto(rule, codes, zoneCodes);
    }

    public async Task DeleteDemandRuleAsync(Guid id, CancellationToken ct)
    {
        var rule = Guard.NotFound(await db.DemandRules.FirstOrDefaultAsync(r => r.Id == id, ct));
        db.DemandRules.Remove(rule);
        audit.Log("demand_rule.delete", "demand_rule", rule.Id, ToDto(rule, await CategoryCodesAsync(ct), await ZoneCodesAsync(ct)), null);
        await db.SaveChangesAsync(ct);
        demandCache.Invalidate();
    }

    private static void ValidateDemandRule(Validator v, DemandRuleUpsertRequest request)
    {
        v.Rule(nameof(request.Metric), request.Metric is null || request.Metric == DemandMetrics.RequestsPerDriver, $"must be {DemandMetrics.RequestsPerDriver}")
         .Rule(nameof(request.WindowMinutes), request.WindowMinutes is null or (>= 1 and <= 120), "must be between 1 and 120")
         .Rule(nameof(request.ThresholdModerate), request.ThresholdModerate is null or >= 0, "must be positive")
         .Rule(nameof(request.ThresholdHigh), request.ThresholdHigh is null || request.ThresholdModerate is null || request.ThresholdHigh > request.ThresholdModerate, "must be above thresholdModerate")
         .Rule(nameof(request.ThresholdVeryHigh), request.ThresholdVeryHigh is null || request.ThresholdHigh is null || request.ThresholdVeryHigh > request.ThresholdHigh, "must be above thresholdHigh");
    }

    private static void ApplyDemandRule(DemandRule rule, DemandRuleUpsertRequest request)
    {
        if (request.Metric is not null) rule.Metric = request.Metric;
        if (request.WindowMinutes is not null) rule.WindowMinutes = request.WindowMinutes.Value;
        if (request.ThresholdModerate is not null) rule.ThresholdModerate = request.ThresholdModerate.Value;
        if (request.ThresholdHigh is not null) rule.ThresholdHigh = request.ThresholdHigh.Value;
        if (request.ThresholdVeryHigh is not null) rule.ThresholdVeryHigh = request.ThresholdVeryHigh.Value;
        if (request.IsActive is not null) rule.IsActive = request.IsActive.Value;
    }

    private static DemandRuleDto ToDto(DemandRule r, Dictionary<Guid, string> codes, Dictionary<Guid, string> zoneCodes) => new(
        r.Id, r.ZoneId, r.ZoneId is { } z ? zoneCodes.GetValueOrDefault(z) : null, r.RideCategoryId, r.RideCategoryId is { } c ? codes.GetValueOrDefault(c) : null,
        r.Metric, r.WindowMinutes, r.ThresholdModerate, r.ThresholdHigh, r.ThresholdVeryHigh, r.IsActive, r.CreatedAt, r.UpdatedAt);

    // ----- demand overrides -----

    public async Task<List<DemandOverrideDto>> ListDemandOverridesAsync(bool activeOnly, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var query = db.DemandOverrides.AsNoTracking();
        if (activeOnly) query = query.Where(o => o.StartsAt <= now && o.EndsAt > now);
        var rows = await query.OrderByDescending(o => o.StartsAt).ToListAsync(ct);
        var levels = (await demand.LevelsAsync(ct)).ToDictionary(l => l.Id, l => l.Code);
        var codes = await CategoryCodesAsync(ct);
        var zoneCodes = await ZoneCodesAsync(ct);
        return rows.Select(o => ToDto(o, levels, codes, zoneCodes, now)).ToList();
    }

    public async Task<DemandOverrideDto> CreateDemandOverrideAsync(DemandOverrideUpsertRequest request, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var v = new Validator()
            .Require(nameof(request.ZoneId), request.ZoneId)
            .Require(nameof(request.Reason), request.Reason, 255)
            .Require(nameof(request.EndsAt), request.EndsAt)
            .Rule("demandLevel", request.DemandLevelId is not null || !string.IsNullOrWhiteSpace(request.DemandLevelCode), "demandLevelId or demandLevelCode is required");
        v.ThrowIfInvalid();
        var level = await ResolveLevelAsync(request, ct);
        var startsAt = (request.StartsAt ?? now).ToUniversalTime();
        var endsAt = request.EndsAt!.Value.ToUniversalTime();
        v.Rule(nameof(request.EndsAt), endsAt > startsAt && endsAt > now, "must be in the future and after startsAt").ThrowIfInvalid();
        await EnsureReferencesAsync(request.RideCategoryId, request.ZoneId, ct);

        var entry = new DemandOverride
        {
            ZoneId = request.ZoneId!.Value, RideCategoryId = request.RideCategoryId, DemandLevelId = level.Id, Reason = request.Reason!.Trim(),
            StartsAt = startsAt, EndsAt = endsAt, CreatedBy = currentUser.UserId,
        };
        db.DemandOverrides.Add(entry);
        var dto = ToDto(entry, new Dictionary<Guid, string> { [level.Id] = level.Code }, await CategoryCodesAsync(ct), await ZoneCodesAsync(ct), now);
        audit.Log("demand_override.create", "demand_override", entry.Id, null, dto);
        await db.SaveChangesAsync(ct);
        demandCache.Invalidate();
        return dto;
    }

    public async Task<DemandOverrideDto> UpdateDemandOverrideAsync(Guid id, DemandOverrideUpsertRequest request, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var entry = Guard.NotFound(await db.DemandOverrides.FirstOrDefaultAsync(o => o.Id == id, ct));
        var levels = (await demand.LevelsAsync(ct)).ToDictionary(l => l.Id, l => l.Code);
        var codes = await CategoryCodesAsync(ct);
        var zoneCodes = await ZoneCodesAsync(ct);
        var before = ToDto(entry, levels, codes, zoneCodes, now);
        if (request.DemandLevelId is not null || !string.IsNullOrWhiteSpace(request.DemandLevelCode))
        {
            entry.DemandLevelId = (await ResolveLevelAsync(request, ct)).Id;
        }

        if (!string.IsNullOrWhiteSpace(request.Reason)) entry.Reason = request.Reason.Trim();
        if (request.StartsAt is not null) entry.StartsAt = request.StartsAt.Value.ToUniversalTime();
        if (request.EndsAt is not null) entry.EndsAt = request.EndsAt.Value.ToUniversalTime();
        new Validator().Rule(nameof(request.EndsAt), entry.EndsAt > entry.StartsAt, "must be after startsAt").ThrowIfInvalid();
        audit.Log("demand_override.update", "demand_override", entry.Id, before, ToDto(entry, levels, codes, zoneCodes, now));
        await db.SaveChangesAsync(ct);
        demandCache.Invalidate();
        return ToDto(entry, levels, codes, zoneCodes, now);
    }

    /// <summary>Ends an override immediately (kept for history when it already started, removed when it never took effect).</summary>
    public async Task DeleteDemandOverrideAsync(Guid id, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var entry = Guard.NotFound(await db.DemandOverrides.FirstOrDefaultAsync(o => o.Id == id, ct));
        var levels = (await demand.LevelsAsync(ct)).ToDictionary(l => l.Id, l => l.Code);
        var before = ToDto(entry, levels, await CategoryCodesAsync(ct), await ZoneCodesAsync(ct), now);
        if (entry.StartsAt <= now)
        {
            entry.EndsAt = now;
            audit.Log("demand_override.end", "demand_override", entry.Id, before, new { endsAt = now });
        }
        else
        {
            db.DemandOverrides.Remove(entry);
            audit.Log("demand_override.delete", "demand_override", entry.Id, before, null);
        }

        await db.SaveChangesAsync(ct);
        demandCache.Invalidate();
    }

    private async Task<DemandLevel> ResolveLevelAsync(DemandOverrideUpsertRequest request, CancellationToken ct)
    {
        var levels = await demand.LevelsAsync(ct);
        var level = request.DemandLevelId is { } id
            ? levels.FirstOrDefault(l => l.Id == id)
            : levels.FirstOrDefault(l => l.Code == request.DemandLevelCode!.Trim().ToLowerInvariant());
        new Validator().Rule("demandLevel", level is not null, "unknown demand level").ThrowIfInvalid();
        return level!;
    }

    private static DemandOverrideDto ToDto(DemandOverride o, Dictionary<Guid, string> levels, Dictionary<Guid, string> codes, Dictionary<Guid, string> zoneCodes, DateTime now) => new(
        o.Id, o.ZoneId, zoneCodes.GetValueOrDefault(o.ZoneId), o.RideCategoryId, o.RideCategoryId is { } c ? codes.GetValueOrDefault(c) : null,
        o.DemandLevelId, levels.GetValueOrDefault(o.DemandLevelId, "?"), o.Reason, o.StartsAt, o.EndsAt, o.CreatedBy, o.IsActiveAt(now), o.CreatedAt);

    // ----- current demand -----

    /// <summary>Current level per zone (whole zone, plus each category a rule or override targets) with the reading behind it.</summary>
    public async Task<List<DemandCurrentZoneDto>> CurrentDemandAsync(Language lang, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var zoneList = await zones.AllAsync(ct);
        var rules = await demand.RulesAsync(ct);
        var codes = await CategoryCodesAsync(ct);
        var activeOverrides = await db.DemandOverrides.AsNoTracking().Where(o => o.StartsAt <= now && o.EndsAt > now).Select(o => new { o.ZoneId, o.RideCategoryId }).ToListAsync(ct);
        var result = new List<DemandCurrentZoneDto>(zoneList.Count);
        foreach (var zone in zoneList.OrderByDescending(z => z.Priority).ThenBy(z => z.Code))
        {
            var categoryKeys = rules.Where(r => r.ZoneId == null || r.ZoneId == zone.Id).Select(r => r.RideCategoryId)
                .Concat(activeOverrides.Where(o => o.ZoneId == zone.Id).Select(o => o.RideCategoryId))
                .Append(null).Distinct().OrderBy(k => k is null ? 0 : 1).ToList();
            var entries = new List<DemandCurrentEntryDto>(categoryKeys.Count);
            foreach (var key in categoryKeys)
            {
                var reading = await demand.ReadAsync(zone, key, now, ct);
                var latest = await demand.LatestSnapshotAsync(zone.Id, key, now, ct);
                entries.Add(new DemandCurrentEntryDto(key, key is { } c ? codes.GetValueOrDefault(c) : null, reading.Code, lang.Pick(reading.NameAr, reading.NameEn), reading.Multiplier, reading.Color,
                    reading.Source, latest?.Ratio, latest?.RequestsCount, latest?.OnlineDrivers, latest?.ComputedAt));
            }

            result.Add(new DemandCurrentZoneDto(zone.Id, zone.Code, lang.Pick(zone.NameAr, zone.NameEn), zone.CenterLat, zone.CenterLng, entries));
        }

        return result;
    }

    // ----- helpers -----

    private async Task EnsureReferencesAsync(Guid? rideCategoryId, Guid? zoneId, CancellationToken ct)
    {
        var v = new Validator();
        if (rideCategoryId is { } c) v.Rule("rideCategoryId", await db.RideCategories.AnyAsync(x => x.Id == c, ct), "unknown ride category");
        if (zoneId is { } z) v.Rule("zoneId", await db.Zones.AnyAsync(x => x.Id == z, ct), "unknown zone");
        v.ThrowIfInvalid();
    }

    private Task<Dictionary<Guid, string>> CategoryCodesAsync(CancellationToken ct) => db.RideCategories.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.Code, ct);

    private Task<Dictionary<Guid, string>> ZoneCodesAsync(CancellationToken ct) => db.Zones.AsNoTracking().ToDictionaryAsync(z => z.Id, z => z.Code, ct);
}
