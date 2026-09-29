using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence.Seed;
using ATA.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Integration;

public class FavoriteDriverTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Favorites_are_added_only_after_a_completed_trip_with_duplicates_removal_photo_and_driver_count()
    {
        var area = TripFlow.Area(0);
        var (passenger, driver, driverId, firstTripId) = await FavoritesFlow.PassengerWithFavoriteAsync(fixture, area);

        // The favourite was added by driver id: the shared completed trip becomes the source trip.
        var list = await (await passenger.Client.GetAsync("/api/v1/passenger/favorite-drivers")).ReadJsonAsync();
        var favorite = Assert.Single(list.EnumerateArray());
        Assert.Equal(driverId.ToString(), favorite.GetProperty("driverId").GetString());
        Assert.Equal("محمد", favorite.GetProperty("firstName").GetString());
        Assert.Equal($"/api/v1/passenger/favorite-drivers/{driverId}/photo", favorite.GetProperty("photoUrl").GetString());
        Assert.Equal(5m, favorite.GetProperty("ratingAvg").GetDecimal());
        Assert.Equal("Toyota", favorite.GetProperty("vehicle").GetProperty("make").GetString());
        Assert.Equal("Camry", favorite.GetProperty("vehicle").GetProperty("model").GetString());
        Assert.Equal("economy", favorite.GetProperty("rideCategoryCode").GetString());
        Assert.Equal(1, favorite.GetProperty("tripsTogether").GetInt32());
        Assert.NotEqual(JsonValueKind.Null, favorite.GetProperty("lastTripAt").ValueKind);
        Assert.Equal(firstTripId, (await fixture.Factory.WithDbAsync(db => db.FavoriteDrivers.AsNoTracking().SingleAsync(f => f.DriverId == driverId))).SourceTripId.ToString());

        var duplicate = await passenger.Client.PostAsJsonAsync("/api/v1/passenger/favorite-drivers", new { driverId });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("favorite_exists", await duplicate.ErrorCodeAsync());
        var duplicateByTrip = await passenger.Client.PostAsJsonAsync("/api/v1/passenger/favorite-drivers", new { tripId = firstTripId });
        Assert.Equal("favorite_exists", await duplicateByTrip.ErrorCodeAsync());

        // Someone who never rode with the driver cannot save them (also with an unknown driver id, without revealing existence).
        var stranger = await SafetyFlow.PassengerAsync(fixture);
        var notEligible = await stranger.Client.PostAsJsonAsync("/api/v1/passenger/favorite-drivers", new { driverId });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, notEligible.StatusCode);
        Assert.Equal("favorite_not_eligible", await notEligible.ErrorCodeAsync());
        Assert.Equal("favorite_not_eligible", await (await stranger.Client.PostAsJsonAsync("/api/v1/passenger/favorite-drivers", new { driverId = Guid.NewGuid() })).ErrorCodeAsync());
        // A trip of another passenger is not found; an unfinished trip of the passenger is not eligible.
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.Client.PostAsJsonAsync("/api/v1/passenger/favorite-drivers", new { tripId = firstTripId })).StatusCode);
        var openTrip = await FavoritesFlow.RequestAsync(stranger.Client, RewardsFlow.Request(TripFlow.Area(9)));
        Assert.Equal("favorite_not_eligible", await (await stranger.Client.PostAsJsonAsync("/api/v1/passenger/favorite-drivers", new { tripId = openTrip.GetProperty("id").GetString() })).ErrorCodeAsync());
        (await stranger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{openTrip.GetProperty("id").GetString()}/cancel", new { reasonCode = "changed_mind" })).EnsureSuccessStatusCode();
        // Exactly one of driverId / tripId.
        var both = await passenger.Client.PostAsJsonAsync("/api/v1/passenger/favorite-drivers", new { driverId, tripId = firstTripId });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, both.StatusCode);
        Assert.Equal("validation_failed", await both.ErrorCodeAsync());
        Assert.Equal("validation_failed", await (await passenger.Client.PostAsJsonAsync("/api/v1/passenger/favorite-drivers", new { })).ErrorCodeAsync());

        // Drivers only see how many passengers saved them; the photo is served to the passenger who saved the driver only.
        Assert.Equal(1, (await (await driver.Client.GetAsync("/api/v1/driver/favorites/count")).ReadJsonAsync()).GetProperty("count").GetInt32());
        Assert.Equal(HttpStatusCode.Forbidden, (await passenger.Client.GetAsync("/api/v1/driver/favorites/count")).StatusCode);
        var photo = await passenger.Client.GetAsync($"/api/v1/passenger/favorite-drivers/{driverId}/photo");
        Assert.Equal(HttpStatusCode.OK, photo.StatusCode);
        Assert.NotEmpty(await photo.Content.ReadAsByteArrayAsync());
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.Client.GetAsync($"/api/v1/passenger/favorite-drivers/{driverId}/photo")).StatusCode);

        // Remove, remove again (404), add again from the trip history (tripId).
        Assert.Equal(HttpStatusCode.NoContent, (await passenger.Client.DeleteAsync($"/api/v1/passenger/favorite-drivers/{driverId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await passenger.Client.DeleteAsync($"/api/v1/passenger/favorite-drivers/{driverId}")).StatusCode);
        Assert.Empty((await (await passenger.Client.GetAsync("/api/v1/passenger/favorite-drivers")).ReadJsonAsync()).EnumerateArray());
        Assert.Equal(0, (await (await driver.Client.GetAsync("/api/v1/driver/favorites/count")).ReadJsonAsync()).GetProperty("count").GetInt32());
        var again = await passenger.Client.PostAsJsonAsync("/api/v1/passenger/favorite-drivers", new { tripId = firstTripId });
        Assert.Equal(HttpStatusCode.Created, again.StatusCode);
        Assert.Equal(driverId.ToString(), (await again.ReadJsonAsync()).GetProperty("driverId").GetString());

        Assert.Equal(HttpStatusCode.Unauthorized, (await fixture.CreateClient().GetAsync("/api/v1/passenger/favorite-drivers")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await driver.Client.GetAsync("/api/v1/passenger/favorite-drivers")).StatusCode);
    }

    [Fact]
    public async Task Available_favorites_exclude_offline_busy_far_and_other_category_drivers_and_carry_eta_and_discount()
    {
        var area = TripFlow.Area(1);
        var passenger = await SafetyFlow.PassengerAsync(fixture);

        // Busy: the only driver around is on a trip with another passenger.
        var busyRide = await SafetyFlow.AssignedRideAsync(fixture, area);
        // Online and near (eligible), offline, and online but ~10 km away (outside the 5 km radius).
        var (near, nearId) = await SafetyFlow.OnlineDriverAsync(fixture, (area.Lat + 0.009m, area.Lng), "خالد الشمري");
        var (offline, offlineId) = await SafetyFlow.OnlineDriverAsync(fixture, area, "سعد القحطاني");
        (await offline.Client.PutAsJsonAsync("/api/v1/driver/status", new { isOnline = false })).EnsureSuccessStatusCode();
        var (_, farId) = await SafetyFlow.OnlineDriverAsync(fixture, (area.Lat + 0.09m, area.Lng), "ماجد الحربي");
        foreach (var id in new[] { busyRide.DriverId, nearId, offlineId, farId })
        {
            await FavoritesFlow.SaveDirectlyAsync(fixture, passenger.UserId, id);
        }

        var url = $"/api/v1/passenger/favorite-drivers/available?lat={area.Lat}&lng={area.Lng}&rideCategoryId={SeedIds.RideCategories.Economy}";
        var available = await (await passenger.Client.GetAsync(url)).ReadJsonAsync();
        var entry = Assert.Single(available.EnumerateArray());
        Assert.Equal(nearId.ToString(), entry.GetProperty("driverId").GetString());
        Assert.Equal("خالد", entry.GetProperty("firstName").GetString());
        Assert.Equal(3, entry.GetProperty("etaMinutes").GetInt32());
        Assert.Equal(10m, entry.GetProperty("discount").GetProperty("percent").GetDecimal());
        Assert.Equal(10m, entry.GetProperty("discount").GetProperty("maxAmount").GetDecimal());
        Assert.False(entry.GetProperty("discount").GetProperty("stackableWithPromotions").GetBoolean());
        Assert.Equal("Toyota", entry.GetProperty("vehicle").GetProperty("make").GetString());
        Assert.False(entry.TryGetProperty("lat", out _));
        Assert.False(entry.TryGetProperty("lng", out _));
        Assert.False(entry.TryGetProperty("latitude", out _));

        // Another vehicle category (no upgrade) and a pickup far from everybody: nobody is available.
        Assert.Empty((await (await passenger.Client.GetAsync($"/api/v1/passenger/favorite-drivers/available?lat={area.Lat}&lng={area.Lng}&rideCategoryId={SeedIds.RideCategories.Comfort}")).ReadJsonAsync()).EnumerateArray());
        Assert.Empty((await (await passenger.Client.GetAsync($"/api/v1/passenger/favorite-drivers/available?lat={area.Lat + 0.5m}&lng={area.Lng}&rideCategoryId={SeedIds.RideCategories.Economy}")).ReadJsonAsync()).EnumerateArray());

        // Nobody stale: after 10 minutes without a location update the driver is not available any more.
        fixture.Factory.Clock.Advance(TimeSpan.FromMinutes(10));
        Assert.Empty((await (await passenger.Client.GetAsync(url)).ReadJsonAsync()).EnumerateArray());
        await FavoritesFlow.MoveDriverAsync(near.Client, area.Lat + 0.009m, area.Lng);
        Assert.Single((await (await passenger.Client.GetAsync(url)).ReadJsonAsync()).EnumerateArray());

        // Validation and roles.
        var missing = await passenger.Client.GetAsync("/api/v1/passenger/favorite-drivers/available?lat=24.7");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, missing.StatusCode);
        var details = (await missing.ReadJsonAsync()).GetProperty("error").GetProperty("details");
        Assert.True(details.TryGetProperty("lng", out _));
        Assert.True(details.TryGetProperty("rideCategoryId", out _));
        Assert.Empty((await (await (await SafetyFlow.PassengerAsync(fixture)).Client.GetAsync(url)).ReadJsonAsync()).EnumerateArray());
        Assert.Equal(HttpStatusCode.Forbidden, (await near.Client.GetAsync(url)).StatusCode);
    }

    [Fact]
    public async Task Trip_request_and_quote_need_a_saved_favorite_and_the_quote_shows_the_conditional_discount()
    {
        var area = TripFlow.Area(2);
        var (passenger, _, driverId, _) = await FavoritesFlow.PassengerWithFavoriteAsync(fixture, area);
        var stranger = await SafetyFlow.PassengerAsync(fixture);

        // A driver who is not one of the passenger's favourites: 422 validation_failed { favoriteDriverId: "not_favorite" } on the request and on the quote.
        var refused = await stranger.Client.PostAsJsonAsync("/api/v1/passenger/trips", FavoritesFlow.Request(area, driverId));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, refused.StatusCode);
        var error = (await refused.ReadJsonAsync()).GetProperty("error");
        Assert.Equal("validation_failed", error.GetProperty("code").GetString());
        Assert.Equal("not_favorite", error.GetProperty("details").GetProperty("favoriteDriverId").GetString());
        var refusedQuote = await stranger.Client.PostAsJsonAsync("/api/v1/pricing/quote", RewardsFlow.Quote(area, favoriteDriverId: driverId));
        Assert.Equal("not_favorite", (await refusedQuote.ReadJsonAsync()).GetProperty("error").GetProperty("details").GetProperty("favoriteDriverId").GetString());
        Assert.Equal("null", (await (await stranger.Client.GetAsync("/api/v1/passenger/trips/active")).Content.ReadAsStringAsync()).Trim());

        // The quote discounts the favourite's trip "assuming acceptance"; without a favourite nothing changes; stored quotes keep the undiscounted price.
        var plain = await (await passenger.Client.PostAsJsonAsync("/api/v1/pricing/quote", RewardsFlow.Quote(area))).ReadJsonAsync();
        Assert.False(plain.GetProperty("favoriteDiscountConditional").GetBoolean());
        var quote = await (await passenger.Client.PostAsJsonAsync("/api/v1/pricing/quote", RewardsFlow.Quote(area, favoriteDriverId: driverId))).ReadJsonAsync();
        Assert.True(quote.GetProperty("favoriteDiscountConditional").GetBoolean());
        var economy = quote.GetProperty("categories").EnumerateArray().First(c => c.GetProperty("code").GetString() == "economy");
        var plainEconomy = plain.GetProperty("categories").EnumerateArray().First(c => c.GetProperty("code").GetString() == "economy");
        var quoteRow = await fixture.Factory.WithDbAsync(db => db.FareQuotes.AsNoTracking().SingleAsync(q => q.Id == Guid.Parse(economy.GetProperty("quoteId").GetString()!)));
        var expected = Math.Min(TripFlow.Round2(quoteRow.BaseAmount * 0.10m), 10m);
        Assert.Equal(expected, FavoritesFlow.QuoteDiscount(quote));
        Assert.Equal(expected, economy.GetProperty("breakdown").GetProperty("discount").GetDecimal());
        Assert.Equal(plainEconomy.GetProperty("total").GetDecimal(), economy.GetProperty("totalBeforeDiscount").GetDecimal());
        Assert.Equal(TripFlow.RoundToHalf(quoteRow.BaseAmount - expected), economy.GetProperty("total").GetDecimal());
        Assert.Equal(plainEconomy.GetProperty("total").GetDecimal(), quoteRow.Total);
        var line = Assert.Single(economy.GetProperty("breakdown").GetProperty("discounts").EnumerateArray());
        Assert.Equal("خصم الكابتن المفضل", line.GetProperty("label").GetString());
        // The driver-earnings figure shown to drivers is not affected.
        Assert.Equal(plainEconomy.GetProperty("driverNetEarnings").GetDecimal(), economy.GetProperty("driverNetEarnings").GetDecimal());
        // Alias of the quote endpoint.
        var alias = await (await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips/estimate", RewardsFlow.Quote(area, favoriteDriverId: driverId))).ReadJsonAsync();
        Assert.True(alias.GetProperty("favoriteDiscountConditional").GetBoolean());

        // Removing the favourite blocks new favourite requests but the request that was made keeps its favourite (the running trip is untouched).
        var request = await FavoritesFlow.RequestAsync(passenger.Client, FavoritesFlow.Request(area, driverId));
        Assert.Equal(driverId.ToString(), request.GetProperty("favorite").GetProperty("driverId").GetString());
        var tripId = request.GetProperty("id").GetString()!;
        (await passenger.Client.DeleteAsync($"/api/v1/passenger/favorite-drivers/{driverId}")).EnsureSuccessStatusCode();
        var after = await FavoritesFlow.TripAsync(passenger.Client, tripId);
        Assert.Equal(driverId.ToString(), after.GetProperty("favorite").GetProperty("driverId").GetString());
        Assert.NotEqual("not_favorite", after.GetProperty("favorite").GetProperty("status").GetString());
        (await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{tripId}/cancel", new { reasonCode = "changed_mind" })).EnsureSuccessStatusCode();
        var noLongerFavorite = await passenger.Client.PostAsJsonAsync("/api/v1/passenger/trips", FavoritesFlow.Request(area, driverId));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, noLongerFavorite.StatusCode);

        // Trips without a favourite carry `favorite: null`.
        var normal = await FavoritesFlow.RequestAsync(passenger.Client, RewardsFlow.Request(area));
        Assert.Equal(JsonValueKind.Null, normal.GetProperty("favorite").ValueKind);
        (await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{normal.GetProperty("id").GetString()}/cancel", new { reasonCode = "changed_mind" })).EnsureSuccessStatusCode();
    }
}

public class FavoriteLimitTests(FavoriteLimitFixture fixture) : IClassFixture<FavoriteLimitFixture>
{
    [Fact]
    public async Task Saving_more_favorites_than_Favorites_MaxPerPassenger_is_refused_but_a_duplicate_is_still_a_conflict()
    {
        var area = TripFlow.Area(0);
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var (first, firstId) = await SafetyFlow.OnlineDriverAsync(fixture, area, "أحمد الدوسري");
        var firstTrip = await RewardsFlow.CompleteRideAsync(fixture, area, passenger, first, RewardsFlow.Request(area));
        await FavoritesFlow.AddFavoriteAsync(passenger.Client, firstId);

        // A second driver: online after the first ride so the passenger rides with them too.
        (await first.Client.PutAsJsonAsync("/api/v1/driver/status", new { isOnline = false })).EnsureSuccessStatusCode();
        var (second, secondId) = await SafetyFlow.OnlineDriverAsync(fixture, area, "فهد المطيري");
        var secondTrip = await RewardsFlow.CompleteRideAsync(fixture, area, passenger, second, RewardsFlow.Request(area));

        var limit = await passenger.Client.PostAsJsonAsync("/api/v1/passenger/favorite-drivers", new { tripId = secondTrip });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, limit.StatusCode);
        Assert.Equal("favorites_limit", await limit.ErrorCodeAsync());
        Assert.Equal("favorites_limit", await (await passenger.Client.PostAsJsonAsync("/api/v1/passenger/favorite-drivers", new { driverId = secondId })).ErrorCodeAsync());
        Assert.Equal("favorite_exists", await (await passenger.Client.PostAsJsonAsync("/api/v1/passenger/favorite-drivers", new { tripId = firstTrip })).ErrorCodeAsync());

        // Removing one frees the slot.
        (await passenger.Client.DeleteAsync($"/api/v1/passenger/favorite-drivers/{firstId}")).EnsureSuccessStatusCode();
        Assert.Equal(secondId.ToString(), (await FavoritesFlow.AddFavoriteAsync(passenger.Client, secondId)).GetProperty("driverId").GetString());
    }
}
