using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Domain.Catalog;
using ATA.Infrastructure.Persistence.Seed;
using ATA.Tests.Infrastructure;

namespace ATA.Tests.Integration;

public class FavoriteStatsTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Stats_report_favorite_requests_fallbacks_discount_usage_and_top_drivers_with_date_and_city_filters()
    {
        var area = TripFlow.Area(0);
        using var admin = await fixture.LoginAdminAsync();
        var empty = await (await admin.GetAsync("/api/v1/admin/favorites/stats")).ReadJsonAsync();
        Assert.Equal(0, empty.GetProperty("favoriteRequests").GetInt32());
        Assert.Equal(0m, empty.GetProperty("favoriteBookingRate").GetDecimal());
        Assert.Empty(empty.GetProperty("topDrivers").EnumerateArray());

        // Trips: the first ride (normal), a favourite trip accepted + completed with a discount, a favourite request the driver rejected, a normal trip.
        var (passenger, driver, driverId, _) = await FavoritesFlow.PassengerWithFavoriteAsync(fixture, area);
        var acceptedId = await RewardsFlow.CompleteRideAsync(fixture, area, passenger, driver, FavoritesFlow.Request(area, driverId));
        var accepted = await RewardsFlow.TripAsync(fixture, acceptedId);
        Assert.True(accepted.DiscountTotal > 0);

        var rejectedId = (await FavoritesFlow.RequestAsync(passenger.Client, FavoritesFlow.Request(area, driverId))).GetProperty("id").GetString()!;
        await fixture.Factory.RunMatcherAsync();
        var offer = await FavoritesFlow.ActiveOfferAsync(driver.Client);
        (await driver.Client.PostAsJsonAsync($"/api/v1/driver/offers/{offer.GetProperty("id").GetString()}/reject", new { })).EnsureSuccessStatusCode();
        await fixture.Factory.RunMatcherAsync();
        (await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{rejectedId}/cancel", new { reasonCode = "changed_mind" })).EnsureSuccessStatusCode();
        await RewardsFlow.CompleteRideAsync(fixture, area, passenger, driver, RewardsFlow.Request(area));

        var stats = await (await admin.GetAsync("/api/v1/admin/favorites/stats")).ReadJsonAsync();
        Assert.Equal(2, stats.GetProperty("favoriteRequests").GetInt32());
        Assert.Equal(1, stats.GetProperty("accepted").GetInt32());
        Assert.Equal(1, stats.GetProperty("fallback").GetInt32());
        // favoriteBookingRate = favourite requests / all trips requested (4 trips: 2 normal completed, 1 accepted favourite, 1 rejected favourite).
        Assert.Equal(0.5m, stats.GetProperty("favoriteBookingRate").GetDecimal());
        Assert.Equal(1, stats.GetProperty("discountUsageCount").GetInt32());
        Assert.Equal(accepted.DiscountTotal, stats.GetProperty("discountTotal").GetDecimal());
        var top = Assert.Single(stats.GetProperty("topDrivers").EnumerateArray());
        Assert.Equal(driverId.ToString(), top.GetProperty("driverId").GetString());
        Assert.Equal("محمد العتيبي", top.GetProperty("name").GetString());
        Assert.Equal(1, top.GetProperty("favoritesCount").GetInt32());
        Assert.Equal(1, top.GetProperty("favoriteTrips").GetInt32());

        // Range and city filters.
        var today = "2026-09-28";
        var sameDay = await (await admin.GetAsync($"/api/v1/admin/favorites/stats?from={today}&to={today}")).ReadJsonAsync();
        Assert.Equal(2, sameDay.GetProperty("favoriteRequests").GetInt32());
        var later = await (await admin.GetAsync("/api/v1/admin/favorites/stats?from=2026-09-29&to=2026-10-05")).ReadJsonAsync();
        Assert.Equal(0, later.GetProperty("favoriteRequests").GetInt32());
        Assert.Equal(0, later.GetProperty("discountUsageCount").GetInt32());
        Assert.Equal(0m, later.GetProperty("discountTotal").GetDecimal());
        var riyadh = await (await admin.GetAsync($"/api/v1/admin/favorites/stats?cityId={SeedIds.CityRiyadh}")).ReadJsonAsync();
        Assert.Equal(2, riyadh.GetProperty("favoriteRequests").GetInt32());
        Assert.Single(riyadh.GetProperty("topDrivers").EnumerateArray());
        var jeddah = await fixture.Factory.WithDbAsync(async db =>
        {
            var city = new City { Code = "jeddah_fav", NameAr = "جدة", NameEn = "Jeddah", CenterLat = 21.5m, CenterLng = 39.2m };
            db.Cities.Add(city);
            await db.SaveChangesAsync();
            return city.Id;
        });
        var other = await (await admin.GetAsync($"/api/v1/admin/favorites/stats?cityId={jeddah}")).ReadJsonAsync();
        Assert.Equal(0, other.GetProperty("favoriteRequests").GetInt32());
        Assert.Empty(other.GetProperty("topDrivers").EnumerateArray());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.GetAsync($"/api/v1/admin/favorites/stats?cityId={Guid.NewGuid()}")).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.GetAsync("/api/v1/admin/favorites/stats?from=2026-10-05&to=2026-09-29")).StatusCode);
    }
}
