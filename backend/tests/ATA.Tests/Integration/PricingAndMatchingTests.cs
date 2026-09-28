using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Api.Modules.Pricing;
using ATA.Domain.Matching;
using ATA.Domain.Pricing;
using ATA.Infrastructure.Persistence.Seed;
using ATA.Tests.Infrastructure;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ATA.Tests.Integration;

public class PricingAndMatchingTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    /// <summary>Distinct areas ≈15 km apart (beyond the 12 km maximum matching radius) so each test owns its zone and drivers.</summary>
    private static (decimal Lat, decimal Lng) Area(int index) => (24.25m + 0.13m * index, 46.95m + 0.04m * index);

    private static object Square((decimal Lat, decimal Lng) c, decimal half = 0.03m) => new[]
    {
        new[] { c.Lat - half, c.Lng - half }, new[] { c.Lat - half, c.Lng + half }, new[] { c.Lat + half, c.Lng + half }, new[] { c.Lat + half, c.Lng - half }, new[] { c.Lat - half, c.Lng - half },
    };

    [Fact]
    public async Task Zone_resolution_inside_outside_and_overlap_by_priority()
    {
        var area = Area(0);
        using var admin = await fixture.LoginAdminAsync();
        var a = await (await admin.PostAsJsonAsync("/api/v1/admin/zones", new
        {
            code = "test_zone_a", nameAr = "أ", nameEn = "A", polygon = Square(area), priority = 1,
            zoneCategorySettings = new[] { new { rideCategoryId = SeedIds.RideCategories.Economy, isEnabled = true, surgeCap = 1.3m } },
        })).ReadJsonAsync();
        Assert.Equal("test_zone_a", a.GetProperty("code").GetString());
        Assert.Equal(1.3m, a.GetProperty("zoneCategorySettings")[0].GetProperty("surgeCap").GetDecimal());
        Assert.Equal(area.Lat, a.GetProperty("centerLat").GetDecimal());
        var b = await (await admin.PostAsJsonAsync("/api/v1/admin/zones", new { code = "test_zone_b", nameAr = "ب", nameEn = "B", polygon = Square(area, 0.01m), priority = 5 })).ReadJsonAsync();
        var bId = Guid.Parse(b.GetProperty("id").GetString()!);

        var invalid = await admin.PostAsJsonAsync("/api/v1/admin/zones", new { code = "bad", nameAr = "x", nameEn = "x", polygon = new[] { new[] { 1.0, 2.0 } } });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);

        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var resolver = scope.ServiceProvider.GetRequiredService<ZoneResolver>();
            var now = fixture.Factory.Clock.UtcNow;
            Assert.Equal("test_zone_b", (await resolver.ResolveAsync(area.Lat, area.Lng, now, default))!.Code);
            Assert.Equal("test_zone_a", (await resolver.ResolveAsync(area.Lat + 0.02m, area.Lng, now, default))!.Code);
            Assert.Equal(Zone.CityDefaultCode, (await resolver.ResolveAsync(area.Lat + 0.05m, area.Lng, now, default))!.Code);
            Assert.Equal(Zone.CityDefaultCode, (await resolver.ResolveAsync(21.5m, 39.2m, now, default))!.Code, ignoreCase: true);
        }

        // Lowering B's priority below A's flips the overlap (the resolver cache is invalidated by the admin write).
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/v1/admin/zones/{bId}", new { priority = 0 })).StatusCode);
        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var resolver = scope.ServiceProvider.GetRequiredService<ZoneResolver>();
            Assert.Equal("test_zone_a", (await resolver.ResolveAsync(area.Lat, area.Lng, fixture.Factory.Clock.UtcNow, default))!.Code);
        }

        var (passenger, _) = await fixture.LoginAsync("passenger");
        var demand = await (await passenger.GetAsync($"/api/v1/pricing/demand?lat={area.Lat}&lng={area.Lng}")).ReadJsonAsync();
        Assert.Equal("test_zone_a", demand.GetProperty("zone").GetProperty("code").GetString());
        Assert.Equal("normal", demand.GetProperty("demand").GetProperty("code").GetString());

        var cityDefault = await fixture.Factory.WithDbAsync(db => db.Zones.FirstAsync(z => z.Code == Zone.CityDefaultCode));
        Assert.Equal(HttpStatusCode.Conflict, (await admin.DeleteAsync($"/api/v1/admin/zones/{cityDefault.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/admin/zones/{bId}")).StatusCode);
        var audits = await fixture.Factory.WithDbAsync(db => db.AuditLogs.Where(l => l.EntityType == "zone" && l.EntityId == bId).Select(l => l.Action).ToListAsync());
        Assert.Equal(["zone.create", "zone.update", "zone.delete"], audits);
    }

    [Fact]
    public async Task Rule_selection_formula_breakdown_rounding_and_time_multiplier()
    {
        var area = Area(1);
        using var admin = await fixture.LoginAdminAsync();
        var zone = await (await admin.PostAsJsonAsync("/api/v1/admin/zones", new { code = "test_zone_pricing", nameAr = "تسعير", nameEn = "Pricing", polygon = Square(area), priority = 1 })).ReadJsonAsync();
        var zoneId = Guid.Parse(zone.GetProperty("id").GetString()!);

        // Two zone rules for economy: the higher priority one carries an afternoon multiplier active at the fake clock (12:00 UTC = 15:00 Riyadh).
        var low = await admin.PostAsJsonAsync("/api/v1/admin/pricing-rules", new
        {
            rideCategoryId = SeedIds.RideCategories.Economy, zoneId, name = "low priority", baseFare = 50, perKm = 9, perMinute = 9, minFare = 100, priority = 0,
        });
        Assert.Equal(HttpStatusCode.Created, low.StatusCode);
        var created = await admin.PostAsJsonAsync("/api/v1/admin/pricing-rules", new
        {
            rideCategoryId = SeedIds.RideCategories.Economy, zoneId, name = "zone economy", baseFare = 10, perKm = 2, perMinute = 0.5, bookingFee = 3, serviceFeePercent = 10,
            minFare = 15, waitingPerMinute = 0.6, freeWaitingMinutes = 2, driverSharePercent = 75, priority = 1,
            timeMultipliers = new[] { new { dayOfWeek = (int?)null, fromTime = "14:00", toTime = "16:00", multiplier = 1.2, label = "peak_afternoon" } },
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var rule = await created.ReadJsonAsync();
        var ruleId = Guid.Parse(rule.GetProperty("id").GetString()!);
        Assert.Equal("14:00", rule.GetProperty("timeMultipliers")[0].GetProperty("fromTime").GetString());

        var (passenger, _) = await fixture.LoginAsync("passenger");
        var quoted = await passenger.PostAsJsonAsync("/api/v1/pricing/quote", new
        {
            pickup = new { lat = area.Lat, lng = area.Lng },
            dropoff = new { lat = area.Lat + 0.02m, lng = area.Lng + 0.02m },
            rideCategoryId = SeedIds.RideCategories.Economy, bookingType = "now",
        });
        // Quotes need coordinates only (the apps send lat/lng); names/addresses are required when creating the trip.
        Assert.Equal(HttpStatusCode.OK, quoted.StatusCode);
        var quote = await quoted.ReadJsonAsync();
        Assert.Equal("test_zone_pricing", quote.GetProperty("pickupZone").GetProperty("code").GetString());
        Assert.NotEqual(JsonValueKind.Null, quote.GetProperty("quoteId").ValueKind);
        Assert.Equal(fixture.Factory.Clock.UtcNow.AddMinutes(5), quote.GetProperty("expiresAt").GetDateTime().ToUniversalTime());
        var distance = quote.GetProperty("distanceMeters").GetInt32();
        var duration = quote.GetProperty("durationSeconds").GetInt32();
        var economy = quote.GetProperty("categories").EnumerateArray().Single(c => c.GetProperty("code").GetString() == "economy");

        var distanceFare = TripFlow.Round2(2m * distance / 1000m);
        var timeFare = TripFlow.Round2(0.5m * duration / 60m);
        var subtotal = Math.Max(10m + distanceFare + timeFare, 15m);
        var core = subtotal * 1.2m;
        var fare = core + 3m;
        var serviceFee = TripFlow.Round2(fare * 0.10m);
        var total = TripFlow.RoundToHalf(fare + serviceFee);
        Assert.Equal(total, economy.GetProperty("total").GetDecimal());
        Assert.Equal(0m, economy.GetProperty("total").GetDecimal() % 0.5m);
        Assert.Equal(TripFlow.Round2(core * 0.75m), economy.GetProperty("driverNetEarnings").GetDecimal());
        Assert.Equal(TripFlow.RoundToHalf(total * 0.7m), economy.GetProperty("offerMin").GetDecimal());
        Assert.Equal(TripFlow.RoundToHalf(total * 1.3m), economy.GetProperty("offerMax").GetDecimal());
        Assert.Equal("pricing_rule", economy.GetProperty("pricingSource").GetString());
        var breakdown = economy.GetProperty("breakdown");
        Assert.Equal(10m, breakdown.GetProperty("baseFare").GetDecimal());
        Assert.Equal(distanceFare, breakdown.GetProperty("distanceFare").GetDecimal());
        Assert.Equal(timeFare, breakdown.GetProperty("timeFare").GetDecimal());
        Assert.Equal(1.2m, breakdown.GetProperty("timeMultiplier").GetDecimal());
        Assert.Equal("peak_afternoon", breakdown.GetProperty("timeMultiplierLabel").GetString());
        Assert.Equal(1m, breakdown.GetProperty("demandMultiplier").GetDecimal());
        Assert.Equal(3m, breakdown.GetProperty("bookingFee").GetDecimal());
        Assert.Equal(serviceFee, breakdown.GetProperty("serviceFee").GetDecimal());
        Assert.False(breakdown.GetProperty("minFareApplied").GetBoolean());
        Assert.Equal("normal", economy.GetProperty("demand").GetProperty("code").GetString());
        var saver = quote.GetProperty("categories").EnumerateArray().Single(c => c.GetProperty("code").GetString() == "saver");
        Assert.Equal(1m, saver.GetProperty("breakdown").GetProperty("timeMultiplier").GetDecimal());

        // Simulation at 06:00 local: no afternoon multiplier; at 00:30 local the seeded night multiplier applies to the saver rule.
        var morning = await (await admin.PostAsJsonAsync("/api/v1/admin/pricing/simulate", new
        {
            pickup = new { name = "أ", address = "ب", lat = area.Lat, lng = area.Lng }, dropoff = new { name = "ج", address = "د", lat = area.Lat + 0.02m, lng = area.Lng + 0.02m },
            at = new DateTime(2026, 9, 28, 3, 0, 0, DateTimeKind.Utc),
        })).ReadJsonAsync();
        Assert.Equal(JsonValueKind.Null, morning.GetProperty("quoteId").ValueKind);
        Assert.Equal(1m, morning.GetProperty("categories").EnumerateArray().Single(c => c.GetProperty("code").GetString() == "economy").GetProperty("breakdown").GetProperty("timeMultiplier").GetDecimal());
        var night = await (await admin.PostAsJsonAsync("/api/v1/admin/pricing/simulate", new
        {
            pickup = new { name = "أ", address = "ب", lat = area.Lat, lng = area.Lng }, dropoff = new { name = "ج", address = "د", lat = area.Lat + 0.02m, lng = area.Lng + 0.02m },
            at = new DateTime(2026, 9, 28, 21, 30, 0, DateTimeKind.Utc),
        })).ReadJsonAsync();
        var nightSaver = night.GetProperty("categories").EnumerateArray().Single(c => c.GetProperty("code").GetString() == "saver").GetProperty("breakdown");
        Assert.Equal(1.15m, nightSaver.GetProperty("timeMultiplier").GetDecimal());
        Assert.Equal("night", nightSaver.GetProperty("timeMultiplierLabel").GetString());

        // Deactivating the zone rule falls back to the low-priority zone rule (min fare 100 applies).
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/v1/admin/pricing-rules/{ruleId}", new { isActive = false })).StatusCode);
        var fallback = await (await admin.PostAsJsonAsync("/api/v1/admin/pricing/simulate", new
        {
            pickup = new { name = "أ", address = "ب", lat = area.Lat, lng = area.Lng }, dropoff = new { name = "ج", address = "د", lat = area.Lat + 0.02m, lng = area.Lng + 0.02m },
        })).ReadJsonAsync();
        var lowEconomy = fallback.GetProperty("categories").EnumerateArray().Single(c => c.GetProperty("code").GetString() == "economy");
        Assert.Equal(50m, lowEconomy.GetProperty("breakdown").GetProperty("baseFare").GetDecimal());
        Assert.Equal(1m, lowEconomy.GetProperty("breakdown").GetProperty("timeMultiplier").GetDecimal());

        var rules = await (await admin.GetAsync($"/api/v1/admin/pricing-rules?zoneId={zoneId}")).ReadJsonAsync();
        Assert.Equal(2, rules.GetArrayLength());
        var audits = await fixture.Factory.WithDbAsync(db => db.AuditLogs.Where(l => l.EntityType == "pricing_rule" && l.EntityId == ruleId).Select(l => l.Action).ToListAsync());
        Assert.Equal(["pricing_rule.create", "pricing_rule.update"], audits);
    }

    [Fact]
    public async Task Demand_levels_from_snapshots_overrides_and_surge_cap()
    {
        var area = Area(2);
        using var admin = await fixture.LoginAdminAsync();
        var zone = await (await admin.PostAsJsonAsync("/api/v1/admin/zones", new
        {
            code = "test_zone_demand", nameAr = "طلب", nameEn = "Demand", polygon = Square(area), priority = 1,
            zoneCategorySettings = new[] { new { rideCategoryId = SeedIds.RideCategories.Economy, isEnabled = true, surgeCap = 1.3m } },
        })).ReadJsonAsync();
        var zoneId = Guid.Parse(zone.GetProperty("id").GetString()!);
        var demandRule = await admin.PostAsJsonAsync("/api/v1/admin/demand-rules", new { zoneId, windowMinutes = 10, thresholdModerate = 0.8, thresholdHigh = 1.5, thresholdVeryHigh = 2.5 });
        Assert.Equal(HttpStatusCode.Created, demandRule.StatusCode);

        using var anonymous = fixture.CreateClient();
        var adminAuth = await (await anonymous.PostAsJsonAsync("/api/v1/auth/admin/login", new { username = "admin", password = "Admin@12345" })).ReadJsonAsync();
        await using var adminHub = TripFlow.Hub(fixture, adminAuth.GetProperty("accessToken").GetString()!);
        var changed = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        adminHub.On<JsonElement>("DemandChanged", e =>
        {
            if (e.GetProperty("zoneCode").GetString() == "test_zone_demand") changed.TrySetResult(e);
        });
        await adminHub.StartAsync();

        // Three open requests and no driver in the zone → ratio 3 → very_high.
        for (var i = 0; i < 3; i++)
        {
            var (passenger, _) = await fixture.LoginAsync("passenger");
            Assert.Equal(HttpStatusCode.Created, (await passenger.PostAsJsonAsync("/api/v1/passenger/trips", TripFlow.Request(area))).StatusCode);
        }

        Assert.True(await fixture.Factory.RunDemandAsync() >= 1);
        var change = await changed.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal("very_high", change.GetProperty("code").GetString());
        Assert.Equal("normal", change.GetProperty("previousCode").GetString());
        Assert.Equal(3, change.GetProperty("requestsCount").GetInt32());

        var snapshot = await fixture.Factory.WithDbAsync(db => db.DemandSnapshots.Where(s => s.ZoneId == zoneId).OrderByDescending(s => s.ComputedAt).FirstAsync());
        Assert.Equal("very_high", snapshot.DemandLevelCode);
        Assert.Equal(3m, snapshot.Ratio);

        var current = await (await admin.GetAsync("/api/v1/admin/demand/current")).ReadJsonAsync();
        var zoneEntry = current.EnumerateArray().Single(z => z.GetProperty("code").GetString() == "test_zone_demand");
        var zoneLevel = zoneEntry.GetProperty("levels").EnumerateArray().Single(l => l.GetProperty("rideCategoryId").ValueKind == JsonValueKind.Null);
        Assert.Equal("very_high", zoneLevel.GetProperty("code").GetString());
        Assert.Equal(1.9m, zoneLevel.GetProperty("multiplier").GetDecimal());
        Assert.Equal("snapshot", zoneLevel.GetProperty("source").GetString());

        var (shopper, _) = await fixture.LoginAsync("passenger");
        var quote = await (await shopper.PostAsJsonAsync("/api/v1/pricing/quote", TripFlow.Route(area))).ReadJsonAsync();
        var economy = quote.GetProperty("categories").EnumerateArray().Single(c => c.GetProperty("code").GetString() == "economy");
        Assert.Equal("very_high", economy.GetProperty("demand").GetProperty("code").GetString());
        Assert.Equal(1.3m, economy.GetProperty("demand").GetProperty("multiplier").GetDecimal()); // capped by surge_cap
        Assert.Equal(1.3m, economy.GetProperty("breakdown").GetProperty("demandMultiplier").GetDecimal());
        var comfort = quote.GetProperty("categories").EnumerateArray().Single(c => c.GetProperty("code").GetString() == "comfort");
        Assert.Equal(1.9m, comfort.GetProperty("breakdown").GetProperty("demandMultiplier").GetDecimal());

        // A manual override beats the snapshot; ending it restores the snapshot level.
        var overridden = await admin.PostAsJsonAsync("/api/v1/admin/demand-overrides", new { zoneId, demandLevelCode = "moderate", reason = "حدث", endsAt = fixture.Factory.Clock.UtcNow.AddHours(1) });
        Assert.Equal(HttpStatusCode.Created, overridden.StatusCode);
        var overrideId = (await overridden.ReadJsonAsync()).GetProperty("id").GetString();
        quote = await (await shopper.PostAsJsonAsync("/api/v1/pricing/quote", TripFlow.Route(area))).ReadJsonAsync();
        Assert.Equal("moderate", quote.GetProperty("demand").GetProperty("code").GetString());
        Assert.Equal("override", quote.GetProperty("demand").GetProperty("source").GetString());
        Assert.Equal(1.2m, quote.GetProperty("categories").EnumerateArray().Single(c => c.GetProperty("code").GetString() == "economy").GetProperty("breakdown").GetProperty("demandMultiplier").GetDecimal());
        Assert.Single((await (await admin.GetAsync("/api/v1/admin/demand-overrides?active=true")).ReadJsonAsync()).EnumerateArray(), o => o.GetProperty("id").GetString() == overrideId);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/admin/demand-overrides/{overrideId}")).StatusCode);
        quote = await (await shopper.PostAsJsonAsync("/api/v1/pricing/quote", TripFlow.Route(area))).ReadJsonAsync();
        Assert.Equal("very_high", quote.GetProperty("demand").GetProperty("code").GetString());

        // Level multipliers are editable (only the multiplier) and the change is audited.
        var levels = await (await admin.GetAsync("/api/v1/admin/demand-levels")).ReadJsonAsync();
        Assert.Equal(4, levels.GetArrayLength());
        var high = levels.EnumerateArray().Single(l => l.GetProperty("code").GetString() == "high");
        var updated = await admin.PutAsJsonAsync($"/api/v1/admin/demand-levels/{high.GetProperty("id").GetString()}", new { multiplier = 1.55 });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal(1.55m, (await updated.ReadJsonAsync()).GetProperty("multiplier").GetDecimal());
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/v1/admin/demand-levels/{high.GetProperty("id").GetString()}", new { multiplier = 1.5 })).StatusCode);
        var audits = await fixture.Factory.WithDbAsync(db => db.AuditLogs.Where(l => l.EntityType == "demand_override" || l.EntityType == "demand_level" || l.EntityType == "demand_rule").Select(l => l.Action).ToListAsync());
        Assert.Contains("demand_override.create", audits);
        Assert.Contains("demand_override.end", audits);
        Assert.Contains("demand_level.update", audits);
        Assert.Contains("demand_rule.create", audits);
    }

    [Fact]
    public async Task Quote_locks_price_offer_range_is_enforced_and_expired_quote_is_rejected()
    {
        var area = Area(3);
        using var admin = await fixture.LoginAdminAsync();
        var zone = await (await admin.PostAsJsonAsync("/api/v1/admin/zones", new { code = "test_zone_quote", nameAr = "عرض", nameEn = "Quote", polygon = Square(area), priority = 1 })).ReadJsonAsync();
        var zoneId = Guid.Parse(zone.GetProperty("id").GetString()!);

        var (passenger, _) = await fixture.LoginAsync("passenger");
        var quote = await (await passenger.PostAsJsonAsync("/api/v1/pricing/quote", TripFlow.Route(area))).ReadJsonAsync();
        var quoteId = quote.GetProperty("quoteId").GetString()!;
        var economy = quote.GetProperty("categories").EnumerateArray().Single(c => c.GetProperty("code").GetString() == "economy");
        var total = economy.GetProperty("total").GetDecimal();
        Assert.Equal(quoteId, economy.GetProperty("quoteId").GetString());

        // Demand jumps after the quote: a trip created with the quote keeps the quoted price, a fresh request pays the surge.
        (await admin.PostAsJsonAsync("/api/v1/admin/demand-overrides", new { zoneId, demandLevelCode = "very_high", reason = "اختبار", endsAt = fixture.Factory.Clock.UtcNow.AddHours(2) })).EnsureSuccessStatusCode();
        var locked = await passenger.PostAsJsonAsync("/api/v1/passenger/trips", TripFlow.Request(area, quoteId: quoteId));
        Assert.Equal(HttpStatusCode.Created, locked.StatusCode);
        var lockedTrip = await locked.ReadJsonAsync();
        Assert.Equal(total, lockedTrip.GetProperty("estimatedFare").GetDecimal());
        var lockedId = Guid.Parse(lockedTrip.GetProperty("id").GetString()!);
        var used = await fixture.Factory.WithDbAsync(db => db.FareQuotes.FirstAsync(q => q.Id == Guid.Parse(quoteId)));
        Assert.Equal(lockedId, used.UsedTripId);

        var (other, _) = await fixture.LoginAsync("passenger");
        var surged = await (await other.PostAsJsonAsync("/api/v1/passenger/trips", TripFlow.Request(area))).ReadJsonAsync();
        Assert.True(surged.GetProperty("estimatedFare").GetDecimal() > total);
        Assert.NotNull(await fixture.Factory.WithDbAsync(db => db.FareQuotes.FirstOrDefaultAsync(q => q.UsedTripId == Guid.Parse(surged.GetProperty("id").GetString()!))));

        // The used quote cannot be reused.
        (await passenger.PostAsJsonAsync($"/api/v1/passenger/trips/{lockedId}/cancel", new { reasonCode = "changed_mind" })).EnsureSuccessStatusCode();
        var reused = await passenger.PostAsJsonAsync("/api/v1/passenger/trips", TripFlow.Request(area, quoteId: quoteId));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, reused.StatusCode);
        Assert.Equal("quote_expired", await reused.ErrorCodeAsync());

        // Offer your price: outside [offerMin, offerMax] → 422 with the bounds; inside → the offered price is the fare.
        var fresh = await (await passenger.PostAsJsonAsync("/api/v1/pricing/quote", TripFlow.Route(area))).ReadJsonAsync();
        var freshEconomy = fresh.GetProperty("categories").EnumerateArray().Single(c => c.GetProperty("code").GetString() == "economy");
        var offerMin = freshEconomy.GetProperty("offerMin").GetDecimal();
        var offerMax = freshEconomy.GetProperty("offerMax").GetDecimal();
        var tooHigh = await passenger.PostAsJsonAsync("/api/v1/passenger/trips", TripFlow.Request(area, quoteId: fresh.GetProperty("quoteId").GetString(), pricingMode: "offer", offeredPrice: offerMax + 10m));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, tooHigh.StatusCode);
        var error = (await tooHigh.ReadJsonAsync()).GetProperty("error");
        Assert.Equal("offer_out_of_range", error.GetProperty("code").GetString());
        Assert.Equal(offerMin, error.GetProperty("details").GetProperty("offerMin").GetDecimal());
        Assert.Equal(offerMax, error.GetProperty("details").GetProperty("offerMax").GetDecimal());
        var offered = await passenger.PostAsJsonAsync("/api/v1/passenger/trips", TripFlow.Request(area, quoteId: fresh.GetProperty("quoteId").GetString(), pricingMode: "offer", offeredPrice: offerMin));
        Assert.Equal(HttpStatusCode.Created, offered.StatusCode);
        var offeredTrip = await offered.ReadJsonAsync();
        Assert.Equal(offerMin, offeredTrip.GetProperty("estimatedFare").GetDecimal());
        (await passenger.PostAsJsonAsync($"/api/v1/passenger/trips/{offeredTrip.GetProperty("id").GetString()}/cancel", new { reasonCode = "changed_mind" })).EnsureSuccessStatusCode();

        // Expiry: 5 minutes.
        var expiring = await (await passenger.PostAsJsonAsync("/api/v1/pricing/quote", TripFlow.Route(area))).ReadJsonAsync();
        fixture.Factory.Clock.Advance(TimeSpan.FromMinutes(6));
        var expired = await passenger.PostAsJsonAsync("/api/v1/passenger/trips", TripFlow.Request(area, quoteId: expiring.GetProperty("quoteId").GetString()));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, expired.StatusCode);
        Assert.Equal("quote_expired", await expired.ErrorCodeAsync());
        var unknown = await passenger.PostAsJsonAsync("/api/v1/passenger/trips", TripFlow.Request(area, quoteId: Guid.NewGuid().ToString()));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, unknown.StatusCode);
        Assert.Equal("validation_failed", await unknown.ErrorCodeAsync());
    }

    [Fact]
    public async Task Scoring_prefers_higher_rated_closer_driver_and_records_attempts_and_candidates()
    {
        var area = Area(4);
        var (passenger, _) = await fixture.LoginAsync("passenger");
        var (best, _) = await fixture.LoginAsync("driver");
        var bestId = await TripFlow.ApproveDriverAsync(fixture, best, "الأفضل");
        await TripFlow.GoOnlineAsync(best, area.Lat + 0.0027m, area.Lng); // ≈300 m, rating 5.0
        var (nearest, _) = await fixture.LoginAsync("driver");
        var nearestId = await TripFlow.ApproveDriverAsync(fixture, nearest, "الأقرب");
        await TripFlow.GoOnlineAsync(nearest, area.Lat + 0.0009m, area.Lng); // ≈100 m, rating 3.5
        await fixture.Factory.WithDbAsync(db => db.Drivers.Where(d => d.Id == nearestId).ExecuteUpdateAsync(s => s.SetProperty(d => d.RatingAvg, 3.5m)));

        var created = await (await passenger.PostAsJsonAsync("/api/v1/passenger/trips", TripFlow.Request(area))).ReadJsonAsync();
        var tripId = created.GetProperty("id").GetString()!;
        await fixture.Factory.RunMatcherAsync();
        Assert.Equal(JsonValueKind.Null, (await (await nearest.GetAsync("/api/v1/driver/offers/active")).ReadJsonAsync()).ValueKind);
        var offer = await (await best.GetAsync("/api/v1/driver/offers/active")).ReadJsonAsync();
        Assert.Equal(JsonValueKind.Object, offer.ValueKind);
        Assert.Equal(1, offer.GetProperty("round").GetInt32());
        Assert.False(offer.GetProperty("passengerOffered").GetBoolean());

        using var admin = await fixture.LoginAdminAsync();
        var matching = await (await admin.GetAsync($"/api/v1/admin/trips/{tripId}/matching")).ReadJsonAsync();
        var attempt = Assert.Single(matching.GetProperty("attempts").EnumerateArray());
        Assert.Equal(1, attempt.GetProperty("round").GetInt32());
        Assert.Equal(5000, attempt.GetProperty("radiusMeters").GetInt32());
        Assert.Equal(2, attempt.GetProperty("candidatesCount").GetInt32());
        Assert.Equal(JsonValueKind.Null, attempt.GetProperty("outcome").ValueKind);
        var candidates = attempt.GetProperty("candidates").EnumerateArray().ToList();
        Assert.Equal(bestId.ToString(), candidates[0].GetProperty("driverId").GetString());
        Assert.Equal(1, candidates[0].GetProperty("rank").GetInt32());
        Assert.True(candidates[0].GetProperty("offered").GetBoolean());
        Assert.Equal(nearestId.ToString(), candidates[1].GetProperty("driverId").GetString());
        Assert.False(candidates[1].GetProperty("offered").GetBoolean());
        Assert.True(candidates[0].GetProperty("score").GetDecimal() > candidates[1].GetProperty("score").GetDecimal());
        Assert.True(candidates[0].GetProperty("distanceMeters").GetInt32() > candidates[1].GetProperty("distanceMeters").GetInt32());

        (await best.PostAsync($"/api/v1/driver/offers/{offer.GetProperty("id").GetString()}/accept", null)).EnsureSuccessStatusCode();
        matching = await (await admin.GetAsync($"/api/v1/admin/trips/{tripId}/matching")).ReadJsonAsync();
        attempt = Assert.Single(matching.GetProperty("attempts").EnumerateArray());
        Assert.Equal("assigned", attempt.GetProperty("outcome").GetString());
        Assert.Equal("accepted", attempt.GetProperty("candidates")[0].GetProperty("response").GetString());
        Assert.NotEqual(JsonValueKind.Null, attempt.GetProperty("finishedAt").ValueKind);
        Assert.Equal(0, matching.GetProperty("assignmentSeconds").GetInt32());
    }

    [Fact]
    public async Task Radius_expansion_finds_a_farther_driver_in_a_later_round()
    {
        var area = Area(5);
        var (passenger, _) = await fixture.LoginAsync("passenger");
        var (far, _) = await fixture.LoginAsync("driver");
        await TripFlow.ApproveDriverAsync(fixture, far, "بعيد");
        await TripFlow.GoOnlineAsync(far, area.Lat, area.Lng + 0.062m); // ≈6.3 km: outside 5 km, inside 7.5 km

        var created = await (await passenger.PostAsJsonAsync("/api/v1/passenger/trips", TripFlow.Request(area))).ReadJsonAsync();
        var tripId = Guid.Parse(created.GetProperty("id").GetString()!);
        await fixture.Factory.RunMatcherAsync();
        var offer = await (await far.GetAsync("/api/v1/driver/offers/active")).ReadJsonAsync();
        Assert.Equal(JsonValueKind.Object, offer.ValueKind);
        Assert.Equal(2, offer.GetProperty("round").GetInt32());
        Assert.InRange(offer.GetProperty("distanceToPickupMeters").GetInt32(), 5001, 7500);

        var attempts = await fixture.Factory.WithDbAsync(db => db.MatchingAttempts.Where(a => a.TripId == tripId).OrderBy(a => a.Round).ToListAsync());
        Assert.Equal(2, attempts.Count);
        Assert.Equal((5000, 0, MatchingOutcome.Exhausted), (attempts[0].RadiusMeters, attempts[0].CandidatesCount, attempts[0].Outcome));
        Assert.Equal((7500, 1, (MatchingOutcome?)null), (attempts[1].RadiusMeters, attempts[1].CandidatesCount, attempts[1].Outcome));

        // Rejection exhausts the round; the search widens to the maximum radius and then waits for the timeout.
        (await far.PostAsJsonAsync($"/api/v1/driver/offers/{offer.GetProperty("id").GetString()}/reject", new { reasonCode = "too_far" })).EnsureSuccessStatusCode();
        await fixture.Factory.RunMatcherAsync();
        attempts = await fixture.Factory.WithDbAsync(db => db.MatchingAttempts.Where(a => a.TripId == tripId).OrderBy(a => a.Round).ToListAsync());
        Assert.Equal([5000, 7500, 10000, 12000], attempts.Select(a => a.RadiusMeters).ToList());
        Assert.All(attempts, a => Assert.Equal(MatchingOutcome.Exhausted, a.Outcome));
        var rejected = await fixture.Factory.WithDbAsync(db => db.MatchingCandidates.FirstAsync(c => c.AttemptId == attempts[1].Id));
        Assert.Equal(CandidateResponse.Rejected, rejected.Response);

        fixture.Factory.Clock.Advance(TimeSpan.FromSeconds(121));
        await fixture.Factory.RunMatcherAsync();
        Assert.Equal("no_drivers", (await (await passenger.GetAsync($"/api/v1/passenger/trips/{tripId}")).ReadJsonAsync()).GetProperty("status").GetString());
    }

    [Fact]
    public async Task Admin_matching_settings_crud_and_stats()
    {
        var area = Area(6);
        using var admin = await fixture.LoginAdminAsync();
        var zone = await (await admin.PostAsJsonAsync("/api/v1/admin/zones", new { code = "test_zone_matching", nameAr = "مطابقة", nameEn = "Matching", polygon = Square(area), priority = 1 })).ReadJsonAsync();
        var zoneId = Guid.Parse(zone.GetProperty("id").GetString()!);

        var defaults = await (await admin.GetAsync("/api/v1/admin/matching-settings")).ReadJsonAsync();
        var global = defaults.EnumerateArray().Single(s => s.GetProperty("zoneId").ValueKind == JsonValueKind.Null && s.GetProperty("rideCategoryId").ValueKind == JsonValueKind.Null);
        Assert.Equal(0.35m, global.GetProperty("weights").GetProperty("distance").GetDecimal());
        Assert.Equal(8, global.GetProperty("maxCandidates").GetInt32());

        var created = await admin.PostAsJsonAsync("/api/v1/admin/matching-settings", new
        {
            zoneId, radiusMeters = 2000, maxRadiusMeters = 4000, radiusStepMeters = 1000, offerTimeoutSeconds = 15, searchTimeoutSeconds = 90, maxCandidates = 3,
            weights = new { distance = 0.5, eta = 0.1, rating = 0.2, acceptance = 0.1, cancellation = 0.05, tier = 0.05, favorite = 0 },
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var settings = await created.ReadJsonAsync();
        var settingsId = Guid.Parse(settings.GetProperty("id").GetString()!);
        Assert.Equal("test_zone_matching", settings.GetProperty("zoneCode").GetString());
        Assert.Equal(0.5m, settings.GetProperty("weights").GetProperty("distance").GetDecimal());
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync("/api/v1/admin/matching-settings", new { zoneId, radiusMeters = 3000 })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PostAsJsonAsync("/api/v1/admin/matching-settings", new { rideCategoryId = SeedIds.RideCategories.Saver, offerTimeoutSeconds = 1 })).StatusCode);

        // The zone settings drive matching in that zone: a driver 3 km away is only found in round 2 (2 km → 3 km).
        var (passenger, _) = await fixture.LoginAsync("passenger");
        var (driver, _) = await fixture.LoginAsync("driver");
        await TripFlow.ApproveDriverAsync(fixture, driver, "سائق المنطقة");
        await TripFlow.GoOnlineAsync(driver, area.Lat + 0.024m, area.Lng); // ≈2.7 km
        var trip = await (await passenger.PostAsJsonAsync("/api/v1/passenger/trips", TripFlow.Request(area))).ReadJsonAsync();
        await fixture.Factory.RunMatcherAsync();
        var offer = await (await driver.GetAsync("/api/v1/driver/offers/active")).ReadJsonAsync();
        Assert.Equal(2, offer.GetProperty("round").GetInt32());
        Assert.Equal(fixture.Factory.Clock.UtcNow.AddSeconds(15), offer.GetProperty("expiresAt").GetDateTime().ToUniversalTime());
        (await driver.PostAsync($"/api/v1/driver/offers/{offer.GetProperty("id").GetString()}/accept", null)).EnsureSuccessStatusCode();

        var updated = await admin.PutAsJsonAsync($"/api/v1/admin/matching-settings/{settingsId}", new { maxCandidates = 5, isActive = false });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal(5, (await updated.ReadJsonAsync()).GetProperty("maxCandidates").GetInt32());
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/api/v1/admin/matching-settings/{settingsId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/v1/admin/matching-settings/{settingsId}")).StatusCode);
        var audits = await fixture.Factory.WithDbAsync(db => db.AuditLogs.Where(l => l.EntityType == "matching_settings" && l.EntityId == settingsId).Select(l => l.Action).ToListAsync());
        Assert.Equal(["matching_settings.create", "matching_settings.update", "matching_settings.delete"], audits);

        var stats = await (await admin.GetAsync("/api/v1/admin/matching/stats?from=2026-09-28&to=2026-09-28")).ReadJsonAsync();
        Assert.True(stats.GetProperty("trips").GetInt32() >= 1);
        Assert.True(stats.GetProperty("assigned").GetInt32() >= 1);
        Assert.True(stats.GetProperty("offersSent").GetInt32() >= stats.GetProperty("offersAccepted").GetInt32());
        Assert.True(stats.GetProperty("offersAccepted").GetInt32() >= 1);
        Assert.InRange(stats.GetProperty("noDriversRate").GetDecimal(), 0m, 1m);
        Assert.InRange(stats.GetProperty("offerAcceptanceRate").GetDecimal(), 0m, 1m);
        Assert.True(stats.GetProperty("rounds").GetInt32() >= 2);
        Assert.True(stats.GetProperty("averageRoundsPerTrip").GetDecimal() >= 1m);
        Assert.NotEqual(JsonValueKind.Null, stats.GetProperty("averageAssignmentSeconds").ValueKind);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.GetAsync("/api/v1/admin/matching/stats?from=2026-09-29&to=2026-09-28")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await passenger.GetAsync("/api/v1/admin/matching/stats")).StatusCode);
    }
}
