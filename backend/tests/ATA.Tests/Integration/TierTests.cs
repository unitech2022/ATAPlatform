using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Domain.Drivers;
using ATA.Domain.Notifications;
using ATA.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Integration;

public class TierTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private async Task<JsonElement> RuleAsync(HttpClient admin, string tier) =>
        (await (await admin.GetAsync("/api/v1/admin/driver-tier-rules")).ReadJsonAsync()).EnumerateArray().First(r => r.GetProperty("tier").GetString() == tier);

    private static object RuleBody(JsonElement rule, Action<Dictionary<string, object?>> change)
    {
        var body = new Dictionary<string, object?>
        {
            ["minCompletedTrips"] = rule.GetProperty("minCompletedTrips").GetInt32(), ["minRatingAvg"] = rule.GetProperty("minRatingAvg").GetDecimal(),
            ["minAcceptanceRate"] = rule.GetProperty("minAcceptanceRate").GetDecimal(), ["maxCancellationRate"] = rule.GetProperty("maxCancellationRate").GetDecimal(),
            ["commissionDiscountPercent"] = rule.GetProperty("commissionDiscountPercent").GetDecimal(), ["matchingNorm"] = rule.GetProperty("matchingNorm").GetDecimal(),
            ["benefitsAr"] = rule.GetProperty("benefitsAr").GetString(), ["benefitsEn"] = rule.GetProperty("benefitsEn").GetString(), ["sortOrder"] = rule.GetProperty("sortOrder").GetInt32(),
        };
        change(body);
        return body;
    }

    [Fact]
    public async Task Weekly_recalculation_promotes_exactly_on_the_thresholds_with_history_notification_and_driver_view()
    {
        using var admin = await fixture.LoginAdminAsync();
        var rules = await (await admin.GetAsync("/api/v1/admin/driver-tier-rules")).ReadJsonAsync();
        Assert.Equal(["bronze", "silver", "gold", "platinum"], rules.EnumerateArray().Select(r => r.GetProperty("tier").GetString()!).ToArray());
        var silver = await RuleAsync(admin, "silver");
        Assert.Equal(60, silver.GetProperty("minCompletedTrips").GetInt32());
        Assert.Equal(5m, silver.GetProperty("commissionDiscountPercent").GetDecimal());

        // Silver now needs exactly what a fresh driver with one trip has: 1 trip, 5.00 rating, 100 % acceptance, 0 % cancellations.
        var updated = await admin.PutAsJsonAsync($"/api/v1/admin/driver-tier-rules/{silver.GetProperty("id").GetString()}",
            RuleBody(silver, b => { b["minCompletedTrips"] = 1; b["minRatingAvg"] = 5.00m; b["minAcceptanceRate"] = 1m; b["maxCancellationRate"] = 0m; }));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var invalid = await admin.PutAsJsonAsync($"/api/v1/admin/driver-tier-rules/{silver.GetProperty("id").GetString()}", RuleBody(silver, b => b["minAcceptanceRate"] = 1.5m));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
        Assert.True(await fixture.Factory.WithDbAsync(db => db.AuditLogs.AnyAsync(a => a.Action == "driver_tier_rule.update")));

        var area = TripFlow.Area(0);
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var (driver, driverId) = await SafetyFlow.OnlineDriverAsync(fixture, area);
        var (_, idleId) = await SafetyFlow.OnlineDriverAsync(fixture, TripFlow.Area(5));
        await RewardsFlow.CompleteRideAsync(fixture, area, passenger, driver, RewardsFlow.Request(area));

        var recalc = await admin.PostAsync("/api/v1/admin/driver-tiers/recalculate", null);
        Assert.Equal(HttpStatusCode.Accepted, recalc.StatusCode);
        Assert.True((await recalc.ReadJsonAsync()).GetProperty("changed").GetInt32() >= 1);
        Assert.Equal(DriverTier.Silver, await fixture.Factory.WithDbAsync(db => db.Drivers.Where(d => d.Id == driverId).Select(d => d.Tier).FirstAsync()));
        Assert.Equal(DriverTier.Bronze, await fixture.Factory.WithDbAsync(db => db.Drivers.Where(d => d.Id == idleId).Select(d => d.Tier).FirstAsync()));

        var history = await (await admin.GetAsync($"/api/v1/admin/drivers/{driverId}/tier-history")).ReadJsonAsync();
        var entry = Assert.Single(history.EnumerateArray());
        Assert.Equal("bronze", entry.GetProperty("fromTier").GetString());
        Assert.Equal("silver", entry.GetProperty("toTier").GetString());
        Assert.Equal("weekly_recalc", entry.GetProperty("reason").GetString());
        Assert.Equal(1, entry.GetProperty("metrics").GetProperty("completedTrips").GetInt32());
        Assert.True(await fixture.Factory.WithDbAsync(db => db.Notifications.AnyAsync(n => n.UserId == driver.UserId && n.Type == NotificationTypes.DriverTierChanged && n.BodyAr.Contains("فضي"))));

        var view = await (await driver.Client.GetAsync("/api/v1/driver/tier")).ReadJsonAsync();
        Assert.Equal("silver", view.GetProperty("tier").GetString());
        Assert.Equal("gold", view.GetProperty("nextTier").GetString());
        Assert.Equal(28, view.GetProperty("periodDays").GetInt32());
        Assert.Equal(1, view.GetProperty("metrics").GetProperty("completedTrips").GetInt32());
        Assert.Equal(150, view.GetProperty("nextRequirements").GetProperty("minCompletedTrips").GetInt32());
        Assert.Equal(5m, view.GetProperty("benefits").GetProperty("commissionDiscountPercent").GetDecimal());
        Assert.Equal(new DateTime(2026, 10, 4, 0, 0, 0, DateTimeKind.Utc), view.GetProperty("recalculatesAt").GetDateTime().ToUniversalTime());

        var rulesAfter = await (await admin.GetAsync("/api/v1/admin/driver-tier-rules")).ReadJsonAsync();
        Assert.True(rulesAfter.EnumerateArray().First(r => r.GetProperty("tier").GetString() == "silver").GetProperty("driversCount").GetInt32() >= 1);

        // Restore the seeded silver thresholds for the other tests of the class.
        (await admin.PutAsJsonAsync($"/api/v1/admin/driver-tier-rules/{silver.GetProperty("id").GetString()}", RuleBody(silver, _ => { }))).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Tier_commission_discount_raises_the_driver_share_in_offers_and_at_completion()
    {
        var area = TripFlow.Area(1);
        using var admin = await fixture.LoginAdminAsync();
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var (driver, driverId) = await SafetyFlow.OnlineDriverAsync(fixture, area);

        var set = await admin.PostAsJsonAsync($"/api/v1/admin/drivers/{driverId}/tier", new { tier = "gold", reason = "مكافأة الأداء" });
        Assert.Equal(HttpStatusCode.OK, set.StatusCode);
        var setBody = await set.ReadJsonAsync();
        Assert.Equal("admin", setBody.GetProperty("reason").GetString());
        Assert.Equal("مكافأة الأداء", setBody.GetProperty("note").GetString());
        Assert.False(string.IsNullOrEmpty(setBody.GetProperty("actorName").GetString()));
        Assert.True(await fixture.Factory.WithDbAsync(db => db.AuditLogs.AnyAsync(a => a.Action == "driver.tier_set" && a.EntityId == driverId)));

        var quote = await (await passenger.Client.PostAsJsonAsync("/api/v1/pricing/quote", RewardsFlow.Quote(area))).ReadJsonAsync();
        var economy = quote.GetProperty("categories").EnumerateArray().First(c => c.GetProperty("code").GetString() == "economy");
        var quotedNet = economy.GetProperty("driverNetEarnings").GetDecimal();
        var created = await (await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips", RewardsFlow.Request(area, quoteId: quote.GetProperty("quoteId").GetString()))).ReadJsonAsync();
        var tripId = created.GetProperty("id").GetString()!;
        await fixture.Factory.RunMatcherAsync();
        var offer = await (await driver.Client.GetAsync("/api/v1/driver/offers/active")).ReadJsonAsync();
        // Gold: 10 % of the commission → 80 % becomes 82 %.
        Assert.Equal(TripFlow.Round2(quotedNet * 82m / 80m), offer.GetProperty("driverNetEarnings").GetDecimal());
        (await driver.Client.PostAsync($"/api/v1/driver/offers/{offer.GetProperty("id").GetString()}/accept", null)).EnsureSuccessStatusCode();
        var pin = (await (await passenger.Client.GetAsync($"/api/v1/passenger/trips/{tripId}")).ReadJsonAsync()).GetProperty("pin").GetString()!;
        await TripFlow.DriveAsync(driver.Client, tripId, pin);
        (await driver.Client.PostAsJsonAsync($"/api/v1/driver/trips/{tripId}/complete", new { })).EnsureSuccessStatusCode();

        var trip = await RewardsFlow.TripAsync(fixture, tripId);
        var core = TripFlow.EconomyCore(trip.FinalDistanceM!.Value, trip.FinalDurationS!.Value, trip.WaitingSeconds);
        Assert.Equal(TripFlow.Round2(core * 0.82m), trip.DriverEarnings);
        Assert.Equal(10m, trip.TierCommissionDiscountPercent);
        var earnings = await (await driver.Client.GetAsync($"/api/v1/driver/trips/{tripId}/earnings")).ReadJsonAsync();
        Assert.Equal(10m, earnings.GetProperty("tierCommissionDiscountPercent").GetDecimal());
        Assert.Equal(trip.DriverEarnings, earnings.GetProperty("driverEarnings").GetDecimal());
        await PaymentFlow.AssertLedgerBalancedAsync(fixture);
    }

    [Fact]
    public async Task Matcher_ranks_candidates_by_the_tier_rule_matching_norm()
    {
        var area = TripFlow.Area(2);
        using var admin = await fixture.LoginAdminAsync();
        var (_, goldId) = await SafetyFlow.OnlineDriverAsync(fixture, area, "سائق ذهبي");
        var (_, bronzeId) = await SafetyFlow.OnlineDriverAsync(fixture, area, "سائق برونزي");
        (await admin.PostAsJsonAsync($"/api/v1/admin/drivers/{goldId}/tier", new { tier = "gold", reason = "test" })).EnsureSuccessStatusCode();

        async Task<Guid> FirstRankedAsync()
        {
            var passenger = await SafetyFlow.PassengerAsync(fixture);
            var trip = await (await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips", RewardsFlow.Request(area))).ReadJsonAsync();
            var tripId = Guid.Parse(trip.GetProperty("id").GetString()!);
            await fixture.Factory.RunMatcherAsync();
            var ranked = await fixture.Factory.WithDbAsync(db => (from a in db.MatchingAttempts join c in db.MatchingCandidates on a.Id equals c.AttemptId
                                                                   where a.TripId == tripId orderby c.Rank select new { c.DriverId, c.Score }).ToListAsync());
            Assert.Equal(2, ranked.Count);
            Assert.NotEqual(ranked[0].Score, ranked[1].Score);
            (await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{tripId}/cancel", new { reasonCode = "changed_mind" })).EnsureSuccessStatusCode();
            return ranked[0].DriverId;
        }

        Assert.Equal(goldId, await FirstRankedAsync());

        var gold = await RuleAsync(admin, "gold");
        var bronze = await RuleAsync(admin, "bronze");
        (await admin.PutAsJsonAsync($"/api/v1/admin/driver-tier-rules/{gold.GetProperty("id").GetString()}", RuleBody(gold, b => b["matchingNorm"] = 0m))).EnsureSuccessStatusCode();
        (await admin.PutAsJsonAsync($"/api/v1/admin/driver-tier-rules/{bronze.GetProperty("id").GetString()}", RuleBody(bronze, b => b["matchingNorm"] = 1m))).EnsureSuccessStatusCode();
        Assert.Equal(bronzeId, await FirstRankedAsync());

        (await admin.PutAsJsonAsync($"/api/v1/admin/driver-tier-rules/{gold.GetProperty("id").GetString()}", RuleBody(gold, _ => { }))).EnsureSuccessStatusCode();
        (await admin.PutAsJsonAsync($"/api/v1/admin/driver-tier-rules/{bronze.GetProperty("id").GetString()}", RuleBody(bronze, _ => { }))).EnsureSuccessStatusCode();
        Assert.Equal(0.75m, (await RuleAsync(admin, "gold")).GetProperty("matchingNorm").GetDecimal());
    }
}
