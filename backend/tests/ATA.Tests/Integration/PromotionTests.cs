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

public class PromotionTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Admin_manages_promotions_with_code_rules_status_filters_and_audit()
    {
        using var admin = await fixture.LoginAdminAsync();
        var created = await RewardsFlow.CreatePromotionAsync(admin, "summer26", b =>
        {
            b["type"] = "percent";
            b["value"] = 15m;
            b["maxDiscount"] = 10m;
            b["rideCategoryIds"] = new[] { SeedIds.RideCategories.Economy };
            b["paymentMethods"] = new[] { "card", "wallet" };
        });
        var id = created.GetProperty("id").GetString();
        Assert.Equal("SUMMER26", created.GetProperty("code").GetString());
        Assert.Equal("active", created.GetProperty("status").GetString());
        Assert.Equal(["card", "wallet"], created.GetProperty("paymentMethods").EnumerateArray().Select(x => x.GetString()!).ToArray());

        var duplicate = await admin.PostAsJsonAsync("/api/v1/admin/promotions", new { code = "SUMMER26", nameAr = "x", nameEn = "x", type = "fixed", value = 5, validFrom = "2026-09-01T00:00:00Z", validTo = "2026-12-01T00:00:00Z" });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var badCode = await admin.PostAsJsonAsync("/api/v1/admin/promotions", new { code = "A!", nameAr = "x", nameEn = "x", type = "percent", value = 150, validFrom = "2026-09-01T00:00:00Z", validTo = "2026-12-01T00:00:00Z" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, badCode.StatusCode);
        var details = (await badCode.ReadJsonAsync()).GetProperty("error").GetProperty("details");
        Assert.True(details.TryGetProperty("code", out _));
        Assert.True(details.TryGetProperty("value", out _));

        await RewardsFlow.CreatePromotionAsync(admin, "LATER26", b => b["validFrom"] = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc));
        var scheduled = await (await admin.GetAsync("/api/v1/admin/promotions?status=scheduled")).ReadJsonAsync();
        Assert.Contains(scheduled.GetProperty("items").EnumerateArray(), p => p.GetProperty("code").GetString() == "LATER26");
        Assert.DoesNotContain(scheduled.GetProperty("items").EnumerateArray(), p => p.GetProperty("code").GetString() == "SUMMER26");
        var search = await (await admin.GetAsync("/api/v1/admin/promotions?search=summer")).ReadJsonAsync();
        Assert.Equal("SUMMER26", Assert.Single(search.GetProperty("items").EnumerateArray()).GetProperty("code").GetString());

        var update = await admin.PutAsJsonAsync($"/api/v1/admin/promotions/{id}", new
        {
            code = "SUMMER26", nameAr = "صيف 26", nameEn = "Summer 26", type = "percent", value = 20, maxDiscount = 12, validFrom = "2026-09-01T00:00:00Z",
            validTo = "2026-12-01T00:00:00Z", perUserLimit = 2, isActive = false,
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.ReadJsonAsync();
        Assert.Equal("inactive", updated.GetProperty("status").GetString());
        Assert.Equal(20m, updated.GetProperty("value").GetDecimal());
        Assert.Equal(JsonValueKind.Null, updated.GetProperty("rideCategoryIds").ValueKind);

        var activated = await (await admin.PostAsync($"/api/v1/admin/promotions/{id}/activate", null)).ReadJsonAsync();
        Assert.True(activated.GetProperty("isActive").GetBoolean());
        var deactivated = await (await admin.PostAsync($"/api/v1/admin/promotions/{id}/deactivate", null)).ReadJsonAsync();
        Assert.False(deactivated.GetProperty("isActive").GetBoolean());
        var inactive = await (await admin.GetAsync("/api/v1/admin/promotions?status=inactive")).ReadJsonAsync();
        Assert.Contains(inactive.GetProperty("items").EnumerateArray(), p => p.GetProperty("code").GetString() == "SUMMER26");

        var stats = await (await admin.GetAsync($"/api/v1/admin/promotions/{id}/stats")).ReadJsonAsync();
        Assert.Equal(0, stats.GetProperty("reserved").GetInt32());
        var actions = await fixture.Factory.WithDbAsync(db => db.AuditLogs.Where(a => a.EntityType == "promotion" && a.EntityId == Guid.Parse(id!)).Select(a => a.Action).ToListAsync());
        Assert.Equal(["promotion.activate", "promotion.create", "promotion.deactivate", "promotion.update"], actions.Order().ToArray());

        var (passenger, _) = await fixture.LoginAsync("passenger");
        Assert.Equal(HttpStatusCode.Forbidden, (await passenger.GetAsync("/api/v1/admin/promotions")).StatusCode);
    }

    [Fact]
    public async Task Validation_reports_the_first_failure_in_order_and_every_eligibility_reason()
    {
        var area = TripFlow.Area(1);
        using var admin = await fixture.LoginAdminAsync();
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var quote = await (await passenger.Client.PostAsJsonAsync("/api/v1/pricing/quote", RewardsFlow.Quote(area))).ReadJsonAsync();
        var quoteId = quote.GetProperty("quoteId").GetString();
        var quotedTotal = quote.GetProperty("categories").EnumerateArray().First(c => c.GetProperty("code").GetString() == "economy").GetProperty("total").GetDecimal();

        async Task<HttpResponseMessage> ValidateAsync(string code, string paymentMethod = "cash", string bookingType = "now", bool withQuote = true) =>
            await passenger.Client.PostAsJsonAsync("/api/v1/passenger/promotions/validate", new { code, quoteId = withQuote ? quoteId : null, paymentMethod, bookingType });

        var unknown = await ValidateAsync("NOPE1234");
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
        Assert.Equal("promo_not_found", await unknown.ErrorCodeAsync());

        await RewardsFlow.CreatePromotionAsync(admin, "OFFLINE1", b => b["isActive"] = false);
        Assert.Equal("promo_not_found", await (await ValidateAsync("OFFLINE1")).ErrorCodeAsync());

        // Expired and restricted to another category: the date check comes first.
        await RewardsFlow.CreatePromotionAsync(admin, "OLDCODE1", b =>
        {
            b["validFrom"] = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
            b["validTo"] = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
            b["rideCategoryIds"] = new[] { SeedIds.RideCategories.Comfort };
        });
        var expired = await ValidateAsync("OLDCODE1");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, expired.StatusCode);
        Assert.Equal("promo_expired", await expired.ErrorCodeAsync());
        await RewardsFlow.CreatePromotionAsync(admin, "SOON2026", b => b["validFrom"] = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc));
        Assert.Equal("promo_expired", await (await ValidateAsync("SOON2026")).ErrorCodeAsync());

        // Used up (and also restricted to another category): the usage check comes before eligibility.
        var total = await RewardsFlow.CreatePromotionAsync(admin, "TOTAL001", b => { b["totalUsageLimit"] = 1; b["rideCategoryIds"] = new[] { SeedIds.RideCategories.Comfort }; });
        await fixture.Factory.WithDbAsync(db => db.Promotions.Where(p => p.Id == Guid.Parse(total.GetProperty("id").GetString()!)).ExecuteUpdateAsync(s => s.SetProperty(p => p.UsageCount, 1)));
        var usedUp = await ValidateAsync("TOTAL001");
        await RewardsFlow.AssertErrorAsync(usedUp, "promo_usage_limit_reached", scope: "total");
        await RewardsFlow.CreatePromotionAsync(admin, "BUDGET01", b => { b["value"] = 5m; b["budgetAmount"] = 3m; });
        var budget = await ValidateAsync("BUDGET01");
        await RewardsFlow.AssertErrorAsync(budget, "promo_usage_limit_reached", scope: "budget");

        var otherCity = await fixture.Factory.WithDbAsync(async db =>
        {
            var city = new City { Code = "jeddah_test", NameAr = "جدة", NameEn = "Jeddah", CenterLat = 21.5m, CenterLng = 39.2m };
            db.Cities.Add(city);
            var zone = new Zone { CityId = SeedIds.CityRiyadh, Code = "promo_test_zone", NameAr = "منطقة", NameEn = "Zone", Polygon = "[[24.0,46.0],[24.0,46.1],[24.1,46.1],[24.1,46.0],[24.0,46.0]]", CenterLat = 24.05m, CenterLng = 46.05m, IsActive = false };
            db.Zones.Add(zone);
            await db.SaveChangesAsync();
            return (CityId: city.Id, ZoneId: zone.Id);
        });

        var reasons = new (string Code, Action<Dictionary<string, object?>> Configure, string Reason, string PaymentMethod, string BookingType)[]
        {
            ("CITY0001", b => b["cityId"] = otherCity.CityId, "city", "cash", "now"),
            ("CATEG001", b => b["rideCategoryIds"] = new[] { SeedIds.RideCategories.Comfort }, "category", "cash", "now"),
            ("ZONE0001", b => b["zoneIds"] = new[] { otherCity.ZoneId }, "zone", "cash", "now"),
            ("CARDONLY", b => b["paymentMethods"] = new[] { "card" }, "payment_method", "cash", "now"),
            ("SCHEDULE", b => b["bookingTypes"] = new[] { "scheduled" }, "booking_type", "cash", "now"),
            ("MINFARE1", b => b["minFare"] = 500m, "min_fare", "cash", "now"),
        };
        foreach (var r in reasons)
        {
            await RewardsFlow.CreatePromotionAsync(admin, r.Code, r.Configure);
            var response = await ValidateAsync(r.Code, r.PaymentMethod, r.BookingType);
            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
            await RewardsFlow.AssertErrorAsync(response, "promo_not_eligible", reason: r.Reason);
        }

        // new_users_only: an account older than new_user_days.
        await RewardsFlow.CreatePromotionAsync(admin, "NEWBIE01", b => { b["newUsersOnly"] = true; b["newUserDays"] = 7; });
        await fixture.Factory.WithDbAsync(db => db.Users.Where(u => u.Id == passenger.UserId).ExecuteUpdateAsync(s => s.SetProperty(u => u.CreatedAt, new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc))));
        Assert.Equal("new_users_only", await RewardsFlow.ErrorReasonAsync(await ValidateAsync("NEWBIE01")));

        // Valid codes: amounts computed on the quoted base.
        await RewardsFlow.CreatePromotionAsync(admin, "GOOD0005", b => b["value"] = 5m);
        var valid = await (await ValidateAsync("good0005")).ReadJsonAsync();
        Assert.True(valid.GetProperty("valid").GetBoolean());
        Assert.Equal("GOOD0005", valid.GetProperty("promotion").GetProperty("code").GetString());
        Assert.Equal(5m, valid.GetProperty("discountAmount").GetDecimal());
        Assert.Equal(quotedTotal, valid.GetProperty("totalBefore").GetDecimal());
        Assert.InRange(valid.GetProperty("totalAfter").GetDecimal(), quotedTotal - 5.5m, quotedTotal - 4.5m);
        var withoutQuote = await (await ValidateAsync("GOOD0005", withQuote: false)).ReadJsonAsync();
        Assert.Equal(JsonValueKind.Null, withoutQuote.GetProperty("discountAmount").ValueKind);

        await RewardsFlow.CreatePromotionAsync(admin, "PCT50CAP", b => { b["type"] = "percent"; b["value"] = 50m; b["maxDiscount"] = 5m; });
        Assert.Equal(5m, (await (await ValidateAsync("PCT50CAP")).ReadJsonAsync()).GetProperty("discountAmount").GetDecimal());
        await RewardsFlow.CreatePromotionAsync(admin, "FREEBOOK", b => { b["type"] = "free_booking_fee"; b["value"] = null; });
        Assert.Equal(2m, (await (await ValidateAsync("FREEBOOK")).ReadJsonAsync()).GetProperty("discountAmount").GetDecimal());
        await RewardsFlow.CreatePromotionAsync(admin, "BIG00500", b => b["value"] = 500m);
        var big = await (await ValidateAsync("BIG00500")).ReadJsonAsync();
        Assert.Equal(0m, big.GetProperty("totalAfter").GetDecimal());

        // The quote never fails on a code: it reports validity per the requested category and discounts the categories it applies to.
        var promoQuote = await (await passenger.Client.PostAsJsonAsync("/api/v1/pricing/quote", RewardsFlow.Quote(area, "GOOD0005"))).ReadJsonAsync();
        Assert.True(promoQuote.GetProperty("promotion").GetProperty("valid").GetBoolean());
        var economy = promoQuote.GetProperty("categories").EnumerateArray().First(c => c.GetProperty("code").GetString() == "economy");
        Assert.Equal(5m, economy.GetProperty("breakdown").GetProperty("discount").GetDecimal());
        var line = Assert.Single(economy.GetProperty("breakdown").GetProperty("discounts").EnumerateArray());
        Assert.Equal("promotion", line.GetProperty("source").GetString());
        Assert.Equal("GOOD0005", line.GetProperty("reference").GetString());
        var badQuote = await (await passenger.Client.PostAsJsonAsync("/api/v1/pricing/quote", RewardsFlow.Quote(area, "CATEG001"))).ReadJsonAsync();
        Assert.False(badQuote.GetProperty("promotion").GetProperty("valid").GetBoolean());
        Assert.Equal("category", badQuote.GetProperty("promotion").GetProperty("reason").GetString());
        var comfort = badQuote.GetProperty("categories").EnumerateArray().First(c => c.GetProperty("code").GetString() == "comfort");
        Assert.Equal(5m, comfort.GetProperty("breakdown").GetProperty("discount").GetDecimal());
        var unknownQuote = await (await passenger.Client.PostAsJsonAsync("/api/v1/pricing/quote", RewardsFlow.Quote(area, "NOPE1234"))).ReadJsonAsync();
        Assert.Equal("promo_not_found", unknownQuote.GetProperty("promotion").GetProperty("reason").GetString());

        // No promotions with "offer your price".
        var offer = await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips", RewardsFlow.Request(area, "GOOD0005", pricingMode: "offer", offeredPrice: quotedTotal, quoteId: quoteId));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, offer.StatusCode);
        await RewardsFlow.AssertErrorAsync(offer, "promo_not_eligible", reason: "pricing_mode");

        var publicList = await (await passenger.Client.GetAsync("/api/v1/passenger/promotions")).ReadJsonAsync();
        var codes = publicList.EnumerateArray().Select(p => p.GetProperty("code").GetString()).ToList();
        Assert.Contains("GOOD0005", codes);
        Assert.Contains("WELCOME", codes);
        Assert.DoesNotContain("OFFLINE1", codes);
        Assert.DoesNotContain("NEWBIE01", codes);
        var welcome = publicList.EnumerateArray().First(p => p.GetProperty("code").GetString() == "WELCOME");
        Assert.True(welcome.GetProperty("firstTripOnly").GetBoolean());
        Assert.Equal(JsonValueKind.Null, welcome.GetProperty("rideCategoryCodes").ValueKind);
    }

    [Fact]
    public async Task Reservations_count_against_limits_and_are_released_on_cancellation_and_no_drivers()
    {
        var area = TripFlow.Area(2);
        using var admin = await fixture.LoginAdminAsync();
        var promo = await RewardsFlow.CreatePromotionAsync(admin, "LIMIT001", b => { b["totalUsageLimit"] = 1; b["perUserLimit"] = 1; });
        var promoId = Guid.Parse(promo.GetProperty("id").GetString()!);
        Task<Promotion> PromoAsync() => fixture.Factory.WithDbAsync(db => db.Promotions.AsNoTracking().FirstAsync(p => p.Id == promoId));

        var first = await SafetyFlow.PassengerAsync(fixture);
        var second = await SafetyFlow.PassengerAsync(fixture);
        var created = await first.Client.PostAsJsonAsync("/api/v1/passenger/trips", RewardsFlow.Request(area, "limit001"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var trip = await created.ReadJsonAsync();
        var tripId = trip.GetProperty("id").GetString()!;
        Assert.Equal("LIMIT001", trip.GetProperty("promotion").GetProperty("code").GetString());
        Assert.Equal("reserved", trip.GetProperty("promotion").GetProperty("status").GetString());
        Assert.Equal(5m, trip.GetProperty("promotion").GetProperty("reservedAmount").GetDecimal());
        Assert.Equal(1, (await PromoAsync()).UsageCount);

        var refused = await second.Client.PostAsJsonAsync("/api/v1/passenger/trips", RewardsFlow.Request(area, "LIMIT001"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, refused.StatusCode);
        Assert.Equal("promo_usage_limit_reached", await refused.ErrorCodeAsync());
        Assert.Equal("null", (await (await second.Client.GetAsync("/api/v1/passenger/trips/active")).Content.ReadAsStringAsync()).Trim());

        (await first.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{tripId}/cancel", new { reasonCode = "changed_mind" })).EnsureSuccessStatusCode();
        var released = await fixture.Factory.WithDbAsync(db => db.PromotionRedemptions.AsNoTracking().FirstAsync(r => r.TripId == Guid.Parse(tripId)));
        Assert.Equal(RedemptionStatus.Released, released.Status);
        Assert.Equal(RedemptionReleaseReason.TripCancelled, released.ReleaseReason);
        Assert.Equal(0, (await PromoAsync()).UsageCount);

        // The code is free again: the second passenger reserves it, nobody is around → no_drivers releases it.
        var retry = await second.Client.PostAsJsonAsync("/api/v1/passenger/trips", RewardsFlow.Request(area, "LIMIT001"));
        Assert.Equal(HttpStatusCode.Created, retry.StatusCode);
        var retryId = (await retry.ReadJsonAsync()).GetProperty("id").GetString()!;
        await fixture.Factory.RunMatcherAsync();
        fixture.Factory.Clock.Advance(TimeSpan.FromSeconds(130));
        await fixture.Factory.RunMatcherAsync();
        var noDrivers = await RewardsFlow.TripAsync(fixture, retryId);
        Assert.Equal(TripStatus.NoDrivers, noDrivers.Status);
        var releasedAgain = await fixture.Factory.WithDbAsync(db => db.PromotionRedemptions.AsNoTracking().FirstAsync(r => r.TripId == Guid.Parse(retryId)));
        Assert.Equal(RedemptionReleaseReason.NoDrivers, releasedAgain.ReleaseReason);
        Assert.Equal(0, (await PromoAsync()).UsageCount);

        var redemptions = await (await admin.GetAsync($"/api/v1/admin/promotions/{promoId}/redemptions?status=released")).ReadJsonAsync();
        Assert.Equal(2, redemptions.GetProperty("total").GetInt32());
        Assert.All(redemptions.GetProperty("items").EnumerateArray(), r => Assert.Contains("****", r.GetProperty("phoneMasked").GetString()));
        var all = await (await admin.GetAsync($"/api/v1/admin/promotion-redemptions?promotionId={promoId}&from=2026-09-28&to=2026-09-28")).ReadJsonAsync();
        Assert.Equal(2, all.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Completion_applies_the_discount_to_the_fare_receipt_and_ledger_while_driver_earnings_stay_unchanged()
    {
        var area = TripFlow.Area(3);
        using var admin = await fixture.LoginAdminAsync();
        var promo = await RewardsFlow.CreatePromotionAsync(admin, "RIDE0005", b => { b["value"] = 5m; b["perUserLimit"] = 1; });
        var promoId = promo.GetProperty("id").GetString();
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var (driver, _) = await SafetyFlow.OnlineDriverAsync(fixture, area);
        await TripFlow.TopupAsync(passenger.Client, 200m);

        var quote = await (await passenger.Client.PostAsJsonAsync("/api/v1/pricing/quote", RewardsFlow.Quote(area, "RIDE0005"))).ReadJsonAsync();
        var economy = quote.GetProperty("categories").EnumerateArray().First(c => c.GetProperty("code").GetString() == "economy");
        var tripId = await RewardsFlow.CompleteRideAsync(fixture, area, passenger, driver,
            RewardsFlow.Request(area, "RIDE0005", "wallet", quote.GetProperty("quoteId").GetString()));
        var trip = await RewardsFlow.TripAsync(fixture, tripId);
        Assert.Equal(economy.GetProperty("total").GetDecimal(), trip.EstimatedFare);
        Assert.Equal(5m, trip.DiscountTotal);

        var breakdown = JsonDocument.Parse(trip.FareBreakdown!).RootElement;
        decimal D(string name) => breakdown.GetProperty(name).GetDecimal();
        var core = D("baseFare") + D("distanceFare") + D("timeFare") + D("waitingFare") + D("minFareAdjustment");
        var baseFare = core + D("bookingFee") + D("serviceFee");
        Assert.Equal(TripFlow.RoundToHalf(baseFare - 5m), trip.FinalFare);
        Assert.Equal(TripFlow.Round2(core * 0.80m), trip.DriverEarnings);
        Assert.Equal(5m, D("discount"));
        Assert.Equal("RIDE0005", breakdown.GetProperty("discounts")[0].GetProperty("reference").GetString());

        var passengerView = await (await passenger.Client.GetAsync($"/api/v1/passenger/trips/{tripId}")).ReadJsonAsync();
        Assert.Equal("applied", passengerView.GetProperty("promotion").GetProperty("status").GetString());
        Assert.Equal(5m, passengerView.GetProperty("promotion").GetProperty("discountAmount").GetDecimal());
        Assert.Equal(5m, passengerView.GetProperty("discountTotal").GetDecimal());

        var receipt = await (await passenger.Client.GetAsync($"/api/v1/passenger/trips/{tripId}/receipt")).ReadJsonAsync();
        var discountLine = Assert.Single(receipt.GetProperty("lines").EnumerateArray(), l => l.GetProperty("code").GetString() == "discount");
        Assert.Equal(-5m, discountLine.GetProperty("amount").GetDecimal());
        Assert.Equal("promotion", discountLine.GetProperty("source").GetString());
        Assert.Equal("خصم RIDE0005", discountLine.GetProperty("label").GetString());
        Assert.Equal(5m, receipt.GetProperty("discountTotal").GetDecimal());
        Assert.Equal(trip.FinalFare, receipt.GetProperty("total").GetDecimal());
        Assert.Equal(trip.FinalFare, receipt.GetProperty("lines").EnumerateArray().Sum(l => l.GetProperty("amount").GetDecimal()));

        // Ledger: the passenger pays the discounted fare, the driver keeps the full share, the platform books the discount.
        Assert.Equal(200m - trip.FinalFare!.Value, await SafetyFlow.WalletBalanceAsync(fixture, passenger.UserId, WalletKind.Passenger));
        Assert.Equal(trip.DriverEarnings, await SafetyFlow.WalletBalanceAsync(fixture, driver.UserId, WalletKind.Driver));
        var journal = await fixture.Factory.WithDbAsync(db => db.LedgerJournals.AsNoTracking().SingleAsync(j => j.ReferenceId == trip.Id && j.Type == JournalType.TripDiscount));
        Assert.Equal($"trip:{trip.Id}:discount:promotion", journal.IdempotencyKey);
        var entries = await fixture.Factory.WithDbAsync(db => db.LedgerEntries.AsNoTracking().Where(e => e.JournalId == journal.Id).ToListAsync());
        Assert.Equal(5m, entries.Single(e => e.Account == LedgerAccounts.DiscountPromotion).Debit);
        Assert.Equal(5m, entries.Single(e => e.Account == LedgerAccounts.TripRevenue).Credit);
        await PaymentFlow.AssertLedgerBalancedAsync(fixture);

        var stored = await fixture.Factory.WithDbAsync(db => db.Promotions.AsNoTracking().FirstAsync(p => p.Id == Guid.Parse(promoId!)));
        Assert.Equal(5m, stored.SpentAmount);
        Assert.Equal(1, stored.UsageCount);
        var redemption = await fixture.Factory.WithDbAsync(db => db.PromotionRedemptions.AsNoTracking().FirstAsync(r => r.TripId == trip.Id));
        Assert.Equal(RedemptionStatus.Applied, redemption.Status);
        Assert.Equal(5m, redemption.DiscountAmount);

        // Per-user limit: the applied redemption counts.
        var again = await passenger.Client.PostAsJsonAsync("/api/v1/passenger/promotions/validate", new { code = "RIDE0005" });
        await RewardsFlow.AssertErrorAsync(again, "promo_usage_limit_reached", scope: "user");

        // WELCOME (seeded, first trip only) no longer applies once a trip was completed.
        var welcome = await passenger.Client.PostAsJsonAsync("/api/v1/passenger/promotions/validate", new { code = "WELCOME" });
        await RewardsFlow.AssertErrorAsync(welcome, "promo_not_eligible", reason: "first_trip_only");

        var stats = await (await admin.GetAsync($"/api/v1/admin/promotions/{promoId}/stats")).ReadJsonAsync();
        Assert.Equal(1, stats.GetProperty("applied").GetInt32());
        Assert.Equal(5m, stats.GetProperty("totalDiscount").GetDecimal());
        Assert.Equal(1, stats.GetProperty("uniqueUsers").GetInt32());
        Assert.Equal(1, stats.GetProperty("firstTripConversions").GetInt32());
        var detail = await (await admin.GetAsync($"/api/v1/admin/trips/{tripId}")).ReadJsonAsync();
        Assert.Equal("RIDE0005", detail.GetProperty("promotion").GetProperty("code").GetString());
        Assert.Equal(5m, detail.GetProperty("discounts")[0].GetProperty("amount").GetDecimal());
        var locked = await admin.PutAsJsonAsync($"/api/v1/admin/promotions/{promoId}", new { code = "RIDE0006", nameAr = "x", nameEn = "x", type = "fixed", value = 5, validFrom = "2026-09-01T00:00:00Z", validTo = "2026-12-01T00:00:00Z" });
        Assert.Equal(HttpStatusCode.Conflict, locked.StatusCode);

        // The final fare no longer reaches min_fare → released (not_eligible_at_completion), no discount.
        await RewardsFlow.CreatePromotionAsync(admin, "MINFARE2", b => { b["value"] = 3m; b["minFare"] = 20m; });
        var shortTrip = await RewardsFlow.CompleteRideAsync(fixture, area, passenger, driver, RewardsFlow.Request(area, "MINFARE2", "wallet"),
            new { finalDistanceMeters = 500, finalDurationSeconds = 60 });
        var shortRow = await RewardsFlow.TripAsync(fixture, shortTrip);
        Assert.Equal(0m, shortRow.DiscountTotal);
        var shortRedemption = await fixture.Factory.WithDbAsync(db => db.PromotionRedemptions.AsNoTracking().FirstAsync(r => r.TripId == shortRow.Id));
        Assert.Equal(RedemptionReleaseReason.NotEligibleAtCompletion, shortRedemption.ReleaseReason);
        Assert.False(await fixture.Factory.WithDbAsync(db => db.LedgerJournals.AnyAsync(j => j.ReferenceId == shortRow.Id && j.Type == JournalType.TripDiscount)));

        // A fixed code larger than a cash fare: the fare drops to 0 (MinPayableFare 0), the driver is still paid, the discount credits cash_collected.
        await RewardsFlow.CreatePromotionAsync(admin, "HUGE0500", b => b["value"] = 500m);
        var driverBefore = await SafetyFlow.WalletBalanceAsync(fixture, driver.UserId, WalletKind.Driver);
        var freeTrip = await RewardsFlow.CompleteRideAsync(fixture, area, passenger, driver, RewardsFlow.Request(area, "HUGE0500", "cash"));
        var freeRow = await RewardsFlow.TripAsync(fixture, freeTrip);
        Assert.Equal(0m, freeRow.FinalFare);
        Assert.True(freeRow.DiscountTotal > 0);
        Assert.Equal(driverBefore + freeRow.DriverEarnings!.Value, await SafetyFlow.WalletBalanceAsync(fixture, driver.UserId, WalletKind.Driver));
        var freeJournal = await fixture.Factory.WithDbAsync(db => db.LedgerJournals.AsNoTracking().SingleAsync(j => j.ReferenceId == freeRow.Id && j.Type == JournalType.TripDiscount));
        Assert.True(await fixture.Factory.WithDbAsync(db => db.LedgerEntries.AnyAsync(e => e.JournalId == freeJournal.Id && e.Account == LedgerAccounts.CashCollected && e.Credit == freeRow.DiscountTotal)));
        await PaymentFlow.AssertLedgerBalancedAsync(fixture);

        // Card trip: the discounted fare is captured and the discount is booked against trip_revenue.
        await RewardsFlow.CreatePromotionAsync(admin, "CARD0004", b => b["value"] = 4m);
        var cardId = await PaymentFlow.AddCardAsync(passenger.Client, "tok_sandbox_visa");
        var cardRequest = new
        {
            pickup = new { name = "المنزل", address = "شارع الملك فهد", lat = area.Lat, lng = area.Lng },
            dropoff = new { name = "العمل", address = "طريق الملك عبدالله", lat = area.Lat + 0.05m, lng = area.Lng + 0.05m },
            stops = Array.Empty<object>(), rideCategoryId = SeedIds.RideCategories.Economy, bookingType = "now", paymentMethod = "card", paymentMethodId = cardId,
            pricingMode = "fixed", promoCode = "CARD0004",
        };
        var cardTrip = await RewardsFlow.CompleteRideAsync(fixture, area, passenger, driver, cardRequest);
        var cardRow = await RewardsFlow.TripAsync(fixture, cardTrip);
        Assert.Equal(4m, cardRow.DiscountTotal);
        Assert.Equal(ATA.Domain.Common.PaymentMethodKind.Card, cardRow.PaymentMethod);
        var captured = await fixture.Factory.WithDbAsync(db => db.Payments.AsNoTracking().Where(p => p.TripId == cardRow.Id && p.Status == ATA.Domain.Payments.PaymentStatus.Captured).Select(p => p.CapturedAmount).SingleAsync());
        Assert.Equal(cardRow.FinalFare, captured);
        var cardJournal = await fixture.Factory.WithDbAsync(db => db.LedgerJournals.AsNoTracking().SingleAsync(j => j.ReferenceId == cardRow.Id && j.Type == JournalType.TripDiscount));
        Assert.True(await fixture.Factory.WithDbAsync(db => db.LedgerEntries.AnyAsync(e => e.JournalId == cardJournal.Id && e.Account == LedgerAccounts.TripRevenue && e.Credit == 4m)));
        await PaymentFlow.AssertLedgerBalancedAsync(fixture);
    }
}
