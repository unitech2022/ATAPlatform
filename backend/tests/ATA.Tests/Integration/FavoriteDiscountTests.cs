using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Domain.Catalog;
using ATA.Domain.Pricing;
using ATA.Domain.Promotions;
using ATA.Domain.Trips;
using ATA.Domain.Wallet;
using ATA.Infrastructure.Persistence.Seed;
using ATA.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Integration;

public class FavoriteDiscountTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private static readonly DateTime Now = new(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Admin_manages_discount_rules_with_validation_audit_permissions_and_delete_semantics()
    {
        using var admin = await fixture.LoginAdminAsync();
        var seeded = await (await admin.GetAsync("/api/v1/admin/favorite-discount-rules")).ReadJsonAsync();
        var seededRule = seeded.EnumerateArray().First(r => r.GetProperty("name").GetString() == "خصم الكابتن المفضل");
        Assert.Equal(10m, seededRule.GetProperty("discountPercent").GetDecimal());
        Assert.Equal(10m, seededRule.GetProperty("maxDiscountAmount").GetDecimal());
        Assert.False(seededRule.GetProperty("stackableWithPromotions").GetBoolean());
        Assert.Equal("active", seededRule.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, seededRule.GetProperty("rideCategoryIds").ValueKind);
        Assert.Equal(JsonValueKind.Null, seededRule.GetProperty("zoneIds").ValueKind);
        Assert.Equal(JsonValueKind.Null, seededRule.GetProperty("bookingTypes").ValueKind);
        Assert.Equal(JsonValueKind.Null, seededRule.GetProperty("validTo").ValueKind);
        Assert.Equal(JsonValueKind.Null, seededRule.GetProperty("minFare").ValueKind);
        Assert.True(seededRule.GetProperty("isActive").GetBoolean());

        var created = await FavoritesFlow.CreateRuleAsync(admin, "قاعدة تجريبية", b =>
        {
            b["discountPercent"] = 15.5m;
            b["maxDiscountAmount"] = 12.25m;
            b["minFare"] = 20m;
            b["stackableWithPromotions"] = true;
            b["validTo"] = Now.AddDays(30);
            b["rideCategoryIds"] = new[] { SeedIds.RideCategories.Economy, SeedIds.RideCategories.Comfort };
            b["zoneIds"] = new[] { SeedIds.ZoneRiyadhDefault };
            b["bookingTypes"] = new[] { "now", "scheduled" };
            b["priority"] = 3;
        });
        var id = created.GetProperty("id").GetString();
        Assert.Equal(15.5m, created.GetProperty("discountPercent").GetDecimal());
        Assert.Equal(12.25m, created.GetProperty("maxDiscountAmount").GetDecimal());
        Assert.Equal(20m, created.GetProperty("minFare").GetDecimal());
        Assert.True(created.GetProperty("stackableWithPromotions").GetBoolean());
        Assert.Equal(2, created.GetProperty("rideCategoryIds").GetArrayLength());
        Assert.Equal(["now", "scheduled"], created.GetProperty("bookingTypes").EnumerateArray().Select(x => x.GetString()!).ToArray());
        Assert.Equal(3, created.GetProperty("priority").GetInt32());
        Assert.Equal("active", created.GetProperty("status").GetString());
        Assert.False(string.IsNullOrEmpty(created.GetProperty("createdByName").GetString()));
        Assert.Equal(id, (await (await admin.GetAsync($"/api/v1/admin/favorite-discount-rules/{id}")).ReadJsonAsync()).GetProperty("id").GetString());
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/v1/admin/favorite-discount-rules/{Guid.NewGuid()}")).StatusCode);

        // Validation: percent 1–50 with at most 2 decimals, positive cap, ordered window, non-negative priority, known categories/zones.
        var invalid = await admin.PostAsJsonAsync("/api/v1/admin/favorite-discount-rules", new
        {
            name = " ", discountPercent = 51, maxDiscountAmount = 0, minFare = -1, validFrom = Now, validTo = Now.AddDays(-1), priority = -1,
            rideCategoryIds = new[] { Guid.NewGuid() }, zoneIds = new[] { Guid.NewGuid() },
        });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
        var details = (await invalid.ReadJsonAsync()).GetProperty("error").GetProperty("details");
        foreach (var field in new[] { "name", "discountPercent", "maxDiscountAmount", "minFare", "validTo", "priority" })
        {
            Assert.True(details.TryGetProperty(field, out _), field);
        }

        var badCategory = await admin.PostAsJsonAsync("/api/v1/admin/favorite-discount-rules", new { name = "x", discountPercent = 5, maxDiscountAmount = 5, validFrom = Now, rideCategoryIds = new[] { Guid.NewGuid() } });
        Assert.True((await badCategory.ReadJsonAsync()).GetProperty("error").GetProperty("details").TryGetProperty("rideCategoryIds", out _));
        var badZone = await admin.PostAsJsonAsync("/api/v1/admin/favorite-discount-rules", new { name = "x", discountPercent = 5, maxDiscountAmount = 5, validFrom = Now, zoneIds = new[] { Guid.NewGuid() } });
        Assert.True((await badZone.ReadJsonAsync()).GetProperty("error").GetProperty("details").TryGetProperty("zoneIds", out _));
        foreach (var percent in new[] { 0m, 0.99m, 50.01m, 10.555m })
        {
            var response = await admin.PostAsJsonAsync("/api/v1/admin/favorite-discount-rules", new { name = "x", discountPercent = percent, maxDiscountAmount = 5, validFrom = Now });
            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
            Assert.True((await response.ReadJsonAsync()).GetProperty("error").GetProperty("details").TryGetProperty("discountPercent", out _), percent.ToString());
        }

        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync("/api/v1/admin/favorite-discount-rules", new { name = "edge 1", discountPercent = 1, maxDiscountAmount = 0.01, validFrom = Now })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync("/api/v1/admin/favorite-discount-rules", new { name = "edge 50", discountPercent = 50, maxDiscountAmount = 5, validFrom = Now, isActive = false })).StatusCode);

        // Full replacement; isActive toggles (the dashboard re-saves the rule); lists cleared with null.
        var updated = await admin.PutAsJsonAsync($"/api/v1/admin/favorite-discount-rules/{id}", new
        {
            name = "قاعدة معدّلة", discountPercent = 20, maxDiscountAmount = 8, validFrom = Now.AddDays(-1), validTo = (DateTime?)null, priority = 4, isActive = false, stackableWithPromotions = false,
            rideCategoryIds = (Guid[]?)null, zoneIds = (Guid[]?)null, bookingTypes = (string[]?)null, minFare = (decimal?)null,
        });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var body = await updated.ReadJsonAsync();
        Assert.Equal("inactive", body.GetProperty("status").GetString());
        Assert.False(body.GetProperty("isActive").GetBoolean());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("rideCategoryIds").ValueKind);
        Assert.Equal(JsonValueKind.Null, body.GetProperty("minFare").ValueKind);
        Assert.Equal(JsonValueKind.Null, body.GetProperty("validTo").ValueKind);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PutAsJsonAsync($"/api/v1/admin/favorite-discount-rules/{Guid.NewGuid()}", new { name = "x", discountPercent = 5, maxDiscountAmount = 5, validFrom = Now })).StatusCode);
        var expired = await FavoritesFlow.CreateRuleAsync(admin, "منتهية", b => { b["validFrom"] = Now.AddDays(-10); b["validTo"] = Now.AddDays(-1); });
        var scheduled = await FavoritesFlow.CreateRuleAsync(admin, "قادمة", b => b["validFrom"] = Now.AddDays(5));
        Assert.Equal("expired", expired.GetProperty("status").GetString());
        Assert.Equal("scheduled", scheduled.GetProperty("status").GetString());

        // Unused rules are deleted; every write is audited.
        await FavoritesFlow.DeleteRuleAsync(admin, created);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/v1/admin/favorite-discount-rules/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync($"/api/v1/admin/favorite-discount-rules/{id}")).StatusCode);
        var actions = await fixture.Factory.WithDbAsync(db => db.AuditLogs.Where(a => a.EntityType == "favorite_discount_rule" && a.EntityId == Guid.Parse(id!)).OrderBy(a => a.CreatedAt).Select(a => a.Action).ToListAsync());
        Assert.Equal(["favorite_discount_rule.create", "favorite_discount_rule.update", "favorite_discount_rule.delete"], actions);
        Assert.True(await fixture.Factory.WithDbAsync(db => db.AuditLogs.AnyAsync(a => a.Action == "favorite_discount_rule.create" && a.AfterJson != null && a.AfterJson.Contains("15.5"))));

        // Permissions: passengers and admins without favorites.manage are refused.
        var (passenger, _) = await fixture.LoginAsync("passenger");
        Assert.Equal(HttpStatusCode.Forbidden, (await passenger.GetAsync("/api/v1/admin/favorite-discount-rules")).StatusCode);
        var limited = await FavoritesFlow.LimitedAdminAsync(fixture, "trips_only", "[\"trips.view\"]");
        Assert.Equal(HttpStatusCode.Forbidden, (await limited.GetAsync("/api/v1/admin/favorite-discount-rules")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await limited.PostAsJsonAsync("/api/v1/admin/favorite-discount-rules", new { name = "x", discountPercent = 5, maxDiscountAmount = 5, validFrom = Now })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await limited.GetAsync("/api/v1/admin/favorites/stats")).StatusCode);
        var allowed = await FavoritesFlow.LimitedAdminAsync(fixture, "favorites_only", "[\"favorites.manage\"]");
        Assert.Equal(HttpStatusCode.OK, (await allowed.GetAsync("/api/v1/admin/favorite-discount-rules")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await allowed.GetAsync("/api/v1/admin/favorites/stats")).StatusCode);

        foreach (var leftover in new[] { expired, scheduled })
        {
            await FavoritesFlow.DeleteRuleAsync(admin, leftover);
        }

        foreach (var row in (await (await admin.GetAsync("/api/v1/admin/favorite-discount-rules")).ReadJsonAsync()).EnumerateArray().Where(r => r.GetProperty("name").GetString()!.StartsWith("edge")))
        {
            await FavoritesFlow.DeleteRuleAsync(admin, row);
        }
    }

    [Fact]
    public async Task Rule_selection_follows_priority_then_larger_percent_then_newest_and_honours_window_category_zone_booking_type_and_min_fare()
    {
        var area = TripFlow.Area(1);
        using var admin = await fixture.LoginAdminAsync();
        var (passenger, _, driverId, _) = await FavoritesFlow.PassengerWithFavoriteAsync(fixture, area);
        var created = new List<JsonElement>();

        async Task<(decimal Economy, decimal Comfort, decimal EconomyBase, decimal ComfortBase)> QuoteAsync(bool scheduled = false)
        {
            object body = scheduled
                ? new
                {
                    pickup = new { name = "المنزل", address = "x", lat = area.Lat, lng = area.Lng }, dropoff = new { name = "العمل", address = "y", lat = area.Lat + 0.05m, lng = area.Lng + 0.05m },
                    stops = Array.Empty<object>(), bookingType = "scheduled", scheduledAt = fixture.Factory.Clock.UtcNow.AddHours(2), favoriteDriverId = driverId,
                }
                : RewardsFlow.Quote(area, favoriteDriverId: driverId);
            var quote = await (await passenger.Client.PostAsJsonAsync("/api/v1/pricing/quote", body)).ReadJsonAsync();
            async Task<decimal> BaseAsync(string code)
            {
                var quoteId = Guid.Parse(quote.GetProperty("categories").EnumerateArray().First(c => c.GetProperty("code").GetString() == code).GetProperty("quoteId").GetString()!);
                return await fixture.Factory.WithDbAsync(db => db.FareQuotes.AsNoTracking().Where(q => q.Id == quoteId).Select(q => q.BaseAmount).SingleAsync());
            }

            return (FavoritesFlow.QuoteDiscount(quote), FavoritesFlow.QuoteDiscount(quote, "comfort"), await BaseAsync("economy"), await BaseAsync("comfort"));
        }

        async Task<JsonElement> RuleAsync(string name, Action<Dictionary<string, object?>> configure)
        {
            var rule = await FavoritesFlow.CreateRuleAsync(admin, name, configure, fixture.Factory.Clock.UtcNow);
            created.Add(rule);
            return rule;
        }

        static decimal Pct(decimal fare, decimal percent, decimal cap) => Math.Min(TripFlow.Round2(fare * percent / 100m), cap);

        // Only the seeded rule: 10 % up to 10 SAR for every category.
        var baseline = await QuoteAsync();
        Assert.Equal(Pct(baseline.EconomyBase, 10m, 10m), baseline.Economy);
        Assert.Equal(Pct(baseline.ComfortBase, 10m, 10m), baseline.Comfort);

        // Equal priority: the larger percent wins, then the newest rule.
        var comfortOnly = new[] { SeedIds.RideCategories.Comfort };
        await RuleAsync("A 20%", b => { b["priority"] = 5; b["discountPercent"] = 20m; b["maxDiscountAmount"] = 100m; b["rideCategoryIds"] = comfortOnly; });
        var afterA = await QuoteAsync();
        Assert.Equal(Pct(afterA.ComfortBase, 20m, 100m), afterA.Comfort);
        Assert.Equal(baseline.Economy, afterA.Economy);
        await RuleAsync("B 30%", b => { b["priority"] = 5; b["discountPercent"] = 30m; b["maxDiscountAmount"] = 100m; b["rideCategoryIds"] = comfortOnly; });
        var afterB = await QuoteAsync();
        Assert.Equal(Pct(afterB.ComfortBase, 30m, 100m), afterB.Comfort);
        fixture.Factory.Clock.Advance(TimeSpan.FromSeconds(5));
        await RuleAsync("C 30% newer", b => { b["priority"] = 5; b["discountPercent"] = 30m; b["maxDiscountAmount"] = 5m; b["rideCategoryIds"] = comfortOnly; });
        var afterC = await QuoteAsync();
        Assert.Equal(5m, afterC.Comfort);
        Assert.Equal(baseline.Economy, afterC.Economy);

        // Higher priority but not applicable: min fare, expired, not yet valid, inactive, scheduled-only, another zone.
        var otherZone = await fixture.Factory.WithDbAsync(async db =>
        {
            var zone = new Zone { CityId = SeedIds.CityRiyadh, Code = "fav_test_zone", NameAr = "منطقة", NameEn = "Zone", Polygon = "[[24.0,46.0],[24.0,46.1],[24.1,46.1],[24.1,46.0],[24.0,46.0]]", CenterLat = 24.05m, CenterLng = 46.05m, IsActive = false };
            db.Zones.Add(zone);
            await db.SaveChangesAsync();
            return zone.Id;
        });
        var now = fixture.Factory.Clock.UtcNow;
        await RuleAsync("min fare", b => { b["priority"] = 9; b["discountPercent"] = 50m; b["maxDiscountAmount"] = 100m; b["minFare"] = 10000m; });
        await RuleAsync("expired", b => { b["priority"] = 9; b["discountPercent"] = 50m; b["maxDiscountAmount"] = 100m; b["validFrom"] = now.AddDays(-10); b["validTo"] = now.AddDays(-1); });
        await RuleAsync("future", b => { b["priority"] = 9; b["discountPercent"] = 50m; b["maxDiscountAmount"] = 100m; b["validFrom"] = now.AddDays(1); });
        await RuleAsync("inactive", b => { b["priority"] = 9; b["discountPercent"] = 50m; b["maxDiscountAmount"] = 100m; b["isActive"] = false; });
        await RuleAsync("scheduled only", b => { b["priority"] = 9; b["discountPercent"] = 50m; b["maxDiscountAmount"] = 100m; b["bookingTypes"] = new[] { "scheduled" }; });
        await RuleAsync("other zone", b => { b["priority"] = 9; b["discountPercent"] = 50m; b["maxDiscountAmount"] = 100m; b["zoneIds"] = new[] { otherZone }; });
        var ignored = await QuoteAsync();
        Assert.Equal(baseline.Economy, ignored.Economy);
        Assert.Equal(5m, ignored.Comfort);

        // The scheduled-only rule applies to a scheduled quote.
        var scheduled = await QuoteAsync(scheduled: true);
        Assert.Equal(Pct(scheduled.EconomyBase, 50m, 100m), scheduled.Economy);

        // Zone restriction that matches the pickup zone, top priority, both categories.
        await RuleAsync("riyadh zone", b => { b["priority"] = 10; b["discountPercent"] = 50m; b["maxDiscountAmount"] = 100m; b["zoneIds"] = new[] { SeedIds.ZoneRiyadhDefault }; b["stackableWithPromotions"] = true; });
        var matched = await QuoteAsync();
        Assert.Equal(Pct(matched.EconomyBase, 50m, 100m), matched.Economy);
        Assert.Equal(Pct(matched.ComfortBase, 50m, 100m), matched.Comfort);
        // The passenger sees the stackable flag of the rule that applies.
        var available = await (await passenger.Client.GetAsync($"/api/v1/passenger/favorite-drivers/available?lat={area.Lat}&lng={area.Lng}&rideCategoryId={SeedIds.RideCategories.Economy}")).ReadJsonAsync();
        Assert.Equal(50m, Assert.Single(available.EnumerateArray()).GetProperty("discount").GetProperty("percent").GetDecimal());

        foreach (var rule in created)
        {
            await FavoritesFlow.DeleteRuleAsync(admin, rule);
        }

        Assert.Equal(baseline.Economy, (await QuoteAsync()).Economy);
    }

    [Fact]
    public async Task Discounts_stack_with_promotions_only_when_both_are_stackable_otherwise_the_larger_wins_and_the_cap_applies()
    {
        var area = TripFlow.Area(2);
        using var admin = await fixture.LoginAdminAsync();
        var (passenger, driver, driverId, _) = await FavoritesFlow.PassengerWithFavoriteAsync(fixture, area);

        async Task<(Trip Trip, decimal Core, decimal Base)> RideAsync(string? promoCode = null)
        {
            var tripId = await RewardsFlow.CompleteRideAsync(fixture, area, passenger, driver, FavoritesFlow.Request(area, driverId, promoCode: promoCode));
            var trip = await RewardsFlow.TripAsync(fixture, tripId);
            var (core, baseFare) = FavoritesFlow.FareOf(trip);
            return (trip, core, baseFare);
        }

        Task<List<string>> JournalSourcesAsync(Guid tripId) => fixture.Factory.WithDbAsync(db =>
            db.LedgerJournals.AsNoTracking().Where(j => j.ReferenceId == tripId && j.Type == JournalType.TripDiscount).Select(j => j.IdempotencyKey!).ToListAsync());

        Task<Promotion> PromoAsync(string code) => fixture.Factory.WithDbAsync(db => db.Promotions.AsNoTracking().SingleAsync(p => p.Code == code));
        Task<PromotionRedemption> RedemptionAsync(Guid tripId) => fixture.Factory.WithDbAsync(db => db.PromotionRedemptions.AsNoTracking().SingleAsync(r => r.TripId == tripId));

        // The quote explains a promotion that loses against the larger favourite discount (`not_stacked`), and shows no favourite line when the promotion wins.
        await RewardsFlow.CreatePromotionAsync(admin, "QSMALL02", b => { b["value"] = 2m; b["perUserLimit"] = 5; });
        await RewardsFlow.CreatePromotionAsync(admin, "QBIG0800", b => { b["value"] = 8m; b["perUserLimit"] = 5; });
        var losing = await (await passenger.Client.PostAsJsonAsync("/api/v1/pricing/quote", RewardsFlow.Quote(area, "QSMALL02", favoriteDriverId: driverId))).ReadJsonAsync();
        Assert.True(losing.GetProperty("promotion").GetProperty("valid").GetBoolean());
        Assert.Equal("not_stacked", losing.GetProperty("promotion").GetProperty("reason").GetString());
        Assert.True(FavoritesFlow.QuoteDiscount(losing) > 2m);
        var winning = await (await passenger.Client.PostAsJsonAsync("/api/v1/pricing/quote", RewardsFlow.Quote(area, "QBIG0800", favoriteDriverId: driverId))).ReadJsonAsync();
        Assert.True(winning.GetProperty("promotion").GetProperty("valid").GetBoolean());
        Assert.Equal(JsonValueKind.Null, winning.GetProperty("promotion").GetProperty("reason").ValueKind);
        Assert.Equal(0m, FavoritesFlow.QuoteDiscount(winning));
        Assert.False(winning.GetProperty("favoriteDiscountConditional").GetBoolean());
        var winningLine = Assert.Single(winning.GetProperty("categories").EnumerateArray().First(c => c.GetProperty("code").GetString() == "economy").GetProperty("breakdown").GetProperty("discounts").EnumerateArray());
        Assert.Equal("promotion", winningLine.GetProperty("source").GetString());

        // 1) Non-stackable promotion smaller than the favourite discount: the favourite wins and the code is released (not_stacked).
        await RewardsFlow.CreatePromotionAsync(admin, "SMALL002", b => { b["value"] = 2m; b["perUserLimit"] = 5; });
        var small = await RideAsync("SMALL002");
        var favoriteAmount = Math.Min(TripFlow.Round2(small.Base * 0.10m), 10m);
        Assert.True(favoriteAmount > 2m);
        Assert.Equal(favoriteAmount, small.Trip.DiscountTotal);
        Assert.Equal(TripFlow.RoundToHalf(small.Base - favoriteAmount), small.Trip.FinalFare);
        Assert.Equal(["favorite_driver"], JsonDocument.Parse(small.Trip.FareBreakdown!).RootElement.GetProperty("discounts").EnumerateArray().Select(d => d.GetProperty("source").GetString()!).ToArray());
        var released = await RedemptionAsync(small.Trip.Id);
        Assert.Equal(RedemptionStatus.Released, released.Status);
        Assert.Equal(RedemptionReleaseReason.NotStacked, released.ReleaseReason);
        Assert.Equal(0, (await PromoAsync("SMALL002")).UsageCount);
        Assert.Equal([$"trip:{small.Trip.Id}:discount:favorite_driver"], await JournalSourcesAsync(small.Trip.Id));
        Assert.Equal(TripFlow.Round2(small.Core * 0.80m), small.Trip.DriverEarnings);

        // 2) Non-stackable promotion larger than the favourite discount: the promotion wins, no favourite line, the favourite trip stays accepted without a discount.
        await RewardsFlow.CreatePromotionAsync(admin, "BIG00800", b => { b["value"] = 8m; b["perUserLimit"] = 5; });
        var big = await RideAsync("BIG00800");
        Assert.Equal(8m, big.Trip.DiscountTotal);
        Assert.Equal(["promotion"], JsonDocument.Parse(big.Trip.FareBreakdown!).RootElement.GetProperty("discounts").EnumerateArray().Select(d => d.GetProperty("source").GetString()!).ToArray());
        Assert.Equal(RedemptionStatus.Applied, (await RedemptionAsync(big.Trip.Id)).Status);
        Assert.Equal([$"trip:{big.Trip.Id}:discount:promotion"], await JournalSourcesAsync(big.Trip.Id));
        Assert.Equal(FavoriteStatus.Accepted, big.Trip.FavoriteStatus);
        Assert.False((await FavoritesFlow.TripAsync(passenger.Client, big.Trip.Id.ToString())).GetProperty("favorite").GetProperty("discountApplied").GetBoolean());
        Assert.Equal(TripFlow.Round2(big.Core * 0.80m), big.Trip.DriverEarnings);

        // 3) The maximum discount caps the percentage (50 % of the fare would be far above 4 SAR).
        var capped = await FavoritesFlow.CreateRuleAsync(admin, "سقف 4", b => { b["priority"] = 20; b["discountPercent"] = 50m; b["maxDiscountAmount"] = 4m; }, fixture.Factory.Clock.UtcNow);
        var cap = await RideAsync();
        Assert.True(TripFlow.Round2(cap.Base * 0.50m) > 4m);
        Assert.Equal(4m, cap.Trip.DiscountTotal);
        Assert.Equal(TripFlow.RoundToHalf(cap.Base - 4m), cap.Trip.FinalFare);
        Assert.Equal(capped.GetProperty("id").GetString(), cap.Trip.FavoriteDiscountRuleId.ToString());
        // A rule pinned by a trip is only deactivated when deleted (its receipts keep pointing at it).
        await FavoritesFlow.DeleteRuleAsync(admin, capped);
        var deactivated = await (await admin.GetAsync($"/api/v1/admin/favorite-discount-rules/{capped.GetProperty("id").GetString()}")).ReadJsonAsync();
        Assert.False(deactivated.GetProperty("isActive").GetBoolean());
        Assert.True(await fixture.Factory.WithDbAsync(db => db.AuditLogs.AnyAsync(a => a.Action == "favorite_discount_rule.delete" && a.EntityId == Guid.Parse(capped.GetProperty("id").GetString()!) && a.AfterJson!.Contains("deactivated"))));

        // 4) Both stackable: the promotion and the favourite discount add up, two journals, balanced ledger.
        var stackable = await FavoritesFlow.CreateRuleAsync(admin, "قابل للجمع", b => { b["priority"] = 30; b["discountPercent"] = 10m; b["maxDiscountAmount"] = 10m; b["stackableWithPromotions"] = true; }, fixture.Factory.Clock.UtcNow);
        await RewardsFlow.CreatePromotionAsync(admin, "STACK003", b => { b["value"] = 3m; b["perUserLimit"] = 5; b["isStackable"] = true; });
        var both = await RideAsync("STACK003");
        var stackedFavorite = Math.Min(TripFlow.Round2(both.Base * 0.10m), 10m);
        Assert.Equal(3m + stackedFavorite, both.Trip.DiscountTotal);
        Assert.Equal(TripFlow.RoundToHalf(both.Base - 3m - stackedFavorite), both.Trip.FinalFare);
        var lines = JsonDocument.Parse(both.Trip.FareBreakdown!).RootElement.GetProperty("discounts").EnumerateArray().ToList();
        Assert.Equal(["promotion", "favorite_driver"], lines.Select(l => l.GetProperty("source").GetString()!).ToArray());
        Assert.Equal([3m, stackedFavorite], lines.Select(l => l.GetProperty("amount").GetDecimal()).ToArray());
        Assert.Equal(RedemptionStatus.Applied, (await RedemptionAsync(both.Trip.Id)).Status);
        Assert.Equal(2, (await JournalSourcesAsync(both.Trip.Id)).Count);
        var cash = await fixture.Factory.WithDbAsync(db => db.LedgerEntries.AsNoTracking()
            .Where(e => db.LedgerJournals.Any(j => j.ReferenceId == both.Trip.Id && j.Type == JournalType.TripDiscount && j.Id == e.JournalId) && e.Account == LedgerAccounts.CashCollected).SumAsync(e => e.Credit));
        Assert.Equal(both.Trip.DiscountTotal, cash);
        var favoriteDebit = await fixture.Factory.WithDbAsync(db => db.LedgerEntries.AsNoTracking().Where(e => e.Account == LedgerAccounts.DiscountFavoriteDriver).SumAsync(e => e.Debit));
        Assert.Equal(favoriteAmount + 4m + stackedFavorite, favoriteDebit);
        await PaymentFlow.AssertLedgerBalancedAsync(fixture);
        Assert.Equal(TripFlow.Round2(both.Core * 0.80m), both.Trip.DriverEarnings);
        await FavoritesFlow.DeleteRuleAsync(admin, stackable);
    }

    [Fact]
    public async Task Favorite_requests_with_offer_your_price_get_no_discount()
    {
        var area = TripFlow.Area(3);
        var (passenger, driver, driverId, _) = await FavoritesFlow.PassengerWithFavoriteAsync(fixture, area);
        var quote = await (await passenger.Client.PostAsJsonAsync("/api/v1/pricing/quote", RewardsFlow.Quote(area))).ReadJsonAsync();
        var economy = quote.GetProperty("categories").EnumerateArray().First(c => c.GetProperty("code").GetString() == "economy");
        var offered = economy.GetProperty("total").GetDecimal();

        var tripId = await RewardsFlow.CompleteRideAsync(fixture, area, passenger, driver,
            FavoritesFlow.Request(area, driverId, pricingMode: "offer", offeredPrice: offered, quoteId: quote.GetProperty("quoteId").GetString()));
        var trip = await RewardsFlow.TripAsync(fixture, tripId);
        Assert.Equal(FavoriteStatus.Accepted, trip.FavoriteStatus);
        Assert.Null(trip.FavoriteDiscountRuleId);
        Assert.Equal(offered, trip.EstimatedFare);
        Assert.Equal(offered, trip.FinalFare);
        Assert.Equal(0m, trip.DiscountTotal);
        Assert.False(await fixture.Factory.WithDbAsync(db => db.LedgerJournals.AnyAsync(j => j.ReferenceId == trip.Id && j.Type == JournalType.TripDiscount)));
        Assert.False((await FavoritesFlow.TripAsync(passenger.Client, tripId)).GetProperty("favorite").GetProperty("discountApplied").GetBoolean());
    }
}
