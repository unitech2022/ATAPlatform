using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Api.Modules.Ratings;
using ATA.Domain.Notifications;
using ATA.Domain.Ratings;
using ATA.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Integration;

/// <summary>API host with a small low-average threshold and an abusive-word list (F15 rating tests).</summary>
public sealed class RatingFixture() : ApiFixture(new Dictionary<string, string?>
{
    ["Ratings:MinCountForAverageFlag"] = "3",
    ["Ratings:AbusiveWords:0"] = "badword",
});

public class RatingTests(RatingFixture fixture) : IClassFixture<RatingFixture>
{
    [Fact]
    public async Task Both_parties_rate_once_with_tags_and_the_averages_pending_list_summary_and_reminders_follow()
    {
        var area = TripFlow.Area(0);
        var passenger = await SafetyFlow.PassengerAsync(fixture, "نورة سالم");
        var (driver, _) = await SafetyFlow.OnlineDriverAsync(fixture, area, "خالد الدوسري");
        var tripId = await RewardsFlow.CompleteRideAsync(fixture, area, passenger, driver, RewardsFlow.Request(area));

        var tags = await (await passenger.Client.GetAsync("/api/v1/catalog/rating-tags?target=driver")).ReadJsonAsync();
        Assert.Equal(["driving", "cleanliness", "behaviour", "navigation", "vehicle_condition"], tags.EnumerateArray().Select(t => t.GetProperty("code").GetString()!).ToArray());
        Assert.Equal("القيادة", tags[0].GetProperty("name").GetString());

        var pending = await (await passenger.Client.GetAsync("/api/v1/passenger/ratings/pending")).ReadJsonAsync();
        var item = Assert.Single(pending.EnumerateArray());
        Assert.Equal(tripId, item.GetProperty("tripId").GetString());
        Assert.Equal("خالد", item.GetProperty("counterpartName").GetString());

        var before = await (await passenger.Client.GetAsync($"/api/v1/passenger/trips/{tripId}")).ReadJsonAsync();
        Assert.True(before.GetProperty("canRate").GetBoolean());
        Assert.Equal(JsonValueKind.Null, before.GetProperty("myRating").ValueKind);

        var invalidTag = await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{tripId}/rating", new { stars = 5, tags = new[] { "punctuality" } });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalidTag.StatusCode);
        Assert.Equal("invalid", (await invalidTag.ReadJsonAsync()).GetProperty("error").GetProperty("details").GetProperty("tags").GetString());

        var rated = await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{tripId}/rating", new { stars = 5, tags = new[] { "driving", "cleanliness" }, comment = "كابتن ممتاز" });
        Assert.Equal(HttpStatusCode.Created, rated.StatusCode);
        var rating = await rated.ReadJsonAsync();
        Assert.Equal(5, rating.GetProperty("stars").GetInt32());
        Assert.Equal(tripId, rating.GetProperty("tripId").GetString());

        var twice = await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{tripId}/rating", new { stars = 4 });
        Assert.Equal(HttpStatusCode.Conflict, twice.StatusCode);
        Assert.Equal("rating_exists", await twice.ErrorCodeAsync());

        var driverRated = await driver.Client.PostAsJsonAsync($"/api/v1/driver/trips/{tripId}/rating", new { stars = 4, tags = new[] { "punctuality" } });
        Assert.Equal(HttpStatusCode.Created, driverRated.StatusCode);

        var driverProfile = await fixture.Factory.WithDbAsync(db => db.Drivers.AsNoTracking().FirstAsync(d => d.UserId == driver.UserId));
        Assert.Equal(5.00m, driverProfile.RatingAvg);
        Assert.Equal(1, driverProfile.RatingCount);
        var passengerProfile = await fixture.Factory.WithDbAsync(db => db.Passengers.AsNoTracking().FirstAsync(p => p.UserId == passenger.UserId));
        Assert.Equal(4.00m, passengerProfile.RatingAvg);
        Assert.Equal(1, passengerProfile.RatingCount);

        var after = await (await passenger.Client.GetAsync($"/api/v1/passenger/trips/{tripId}")).ReadJsonAsync();
        Assert.False(after.GetProperty("canRate").GetBoolean());
        Assert.Equal(5, after.GetProperty("myRating").GetProperty("stars").GetInt32());
        Assert.Empty((await (await passenger.Client.GetAsync("/api/v1/passenger/ratings/pending")).ReadJsonAsync()).EnumerateArray());
        var history = await (await passenger.Client.GetAsync("/api/v1/passenger/trips?status=completed")).ReadJsonAsync();
        var historyItem = history.GetProperty("items")[0];
        Assert.Equal("خالد", historyItem.GetProperty("driverName").GetString());
        Assert.Equal(5, historyItem.GetProperty("myRating").GetProperty("stars").GetInt32());
        Assert.False(historyItem.GetProperty("canRate").GetBoolean());
        var driverHistory = await (await driver.Client.GetAsync("/api/v1/driver/trips?status=completed")).ReadJsonAsync();
        Assert.Equal(4, driverHistory.GetProperty("items")[0].GetProperty("myRating").GetProperty("stars").GetInt32());
        Assert.Equal("نورة", driverHistory.GetProperty("items")[0].GetProperty("passengerName").GetString());

        var summary = await (await driver.Client.GetAsync("/api/v1/driver/ratings/summary")).ReadJsonAsync();
        Assert.Equal(5.00m, summary.GetProperty("ratingAvg").GetDecimal());
        Assert.Equal(1, summary.GetProperty("distribution").GetProperty("5").GetInt32());
        Assert.Equal(0, summary.GetProperty("distribution").GetProperty("1").GetInt32());
        Assert.Contains(summary.GetProperty("topTags").EnumerateArray(), t => t.GetProperty("code").GetString() == "driving" && t.GetProperty("positive").GetBoolean());
        var comment = Assert.Single(summary.GetProperty("recentComments").EnumerateArray());
        Assert.Equal("كابتن ممتاز", comment.GetProperty("comment").GetString());
        Assert.Matches(@"^\d{4}-W\d{2}$", comment.GetProperty("week").GetString()!);
        var raw = summary.GetRawText();
        Assert.DoesNotContain("نورة", raw);
        Assert.DoesNotContain("T-", raw);
        Assert.DoesNotContain(tripId, raw);

        using var admin = await fixture.LoginAdminAsync();
        var list = await (await admin.GetAsync($"/api/v1/admin/ratings?userId={driver.UserId}")).ReadJsonAsync();
        Assert.Equal(2, list.GetProperty("total").GetInt32());
        var byPassenger = await (await admin.GetAsync($"/api/v1/admin/ratings?userId={driver.UserId}&raterRole=passenger")).ReadJsonAsync();
        var row = Assert.Single(byPassenger.GetProperty("items").EnumerateArray());
        Assert.Equal("نورة سالم", row.GetProperty("raterName").GetString());
        Assert.Equal("خالد الدوسري", row.GetProperty("rateeName").GetString());
        Assert.Equal("visible", row.GetProperty("status").GetString());
        var detail = await (await admin.GetAsync($"/api/v1/admin/trips/{tripId}")).ReadJsonAsync();
        Assert.Equal(2, detail.GetProperty("ratings").GetArrayLength());

        // rating.reminder: nothing before 30 minutes; the trip is already rated by both parties afterwards → no reminder.
        var other = await RewardsFlow.CompleteRideAsync(fixture, area, passenger, driver, RewardsFlow.Request(area));
        Task<int> RemindersAsync(string id) => fixture.Factory.WithDbAsync(db => db.Notifications.CountAsync(n => n.Type == NotificationTypes.RatingReminder && n.Data!.Contains(id)));
        await fixture.Factory.WithServiceAsync<RatingService, int>(s => s.SendRemindersAsync(CancellationToken.None));
        Assert.Equal(0, await RemindersAsync(other));
        fixture.Factory.Clock.Advance(TimeSpan.FromMinutes(31));
        await fixture.Factory.WithServiceAsync<RatingService, int>(s => s.SendRemindersAsync(CancellationToken.None));
        Assert.Equal(2, await RemindersAsync(other));
        Assert.Equal(0, await RemindersAsync(tripId));
        await fixture.Factory.WithServiceAsync<RatingService, int>(s => s.SendRemindersAsync(CancellationToken.None));
        Assert.Equal(2, await RemindersAsync(other));
    }

    [Fact]
    public async Task Rating_is_refused_for_strangers_unfinished_trips_and_after_the_72_hour_window()
    {
        var area = TripFlow.Area(1);
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var (driver, driverId) = await SafetyFlow.OnlineDriverAsync(fixture, area);
        var ride = await SafetyFlow.NextRideAsync(fixture, area, passenger, driver, driverId);

        var early = await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{ride.TripId}/rating", new { stars = 5 });
        Assert.Equal(HttpStatusCode.Conflict, early.StatusCode);
        Assert.Equal("conflict", await early.ErrorCodeAsync());

        await SafetyFlow.CompleteAsync(ride);
        var stranger = await SafetyFlow.PassengerAsync(fixture);
        var forbidden = await stranger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{ride.TripId}/rating", new { stars = 5 });
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        var invalidStars = await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{ride.TripId}/rating", new { stars = 6 });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalidStars.StatusCode);

        fixture.Factory.Clock.Advance(TimeSpan.FromHours(73));
        // Access tokens follow the fake clock: sign in again.
        var (passengerClient, _) = await fixture.LoginAsync("passenger", passenger.Phone);
        var (driverClient, _) = await fixture.LoginAsync("driver", driver.Phone);
        var late = await passengerClient.PostAsJsonAsync($"/api/v1/passenger/trips/{ride.TripId}/rating", new { stars = 5 });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, late.StatusCode);
        Assert.Equal("rating_window_closed", await late.ErrorCodeAsync());
        var lateDriver = await driverClient.PostAsJsonAsync($"/api/v1/driver/trips/{ride.TripId}/rating", new { stars = 5 });
        Assert.Equal("rating_window_closed", await lateDriver.ErrorCodeAsync());
        var trip = await (await passengerClient.GetAsync($"/api/v1/passenger/trips/{ride.TripId}")).ReadJsonAsync();
        Assert.False(trip.GetProperty("canRate").GetBoolean());
    }

    [Fact]
    public async Task Low_ratings_raise_flags_an_abusive_comment_is_hidden_and_hiding_recomputes_the_weighted_average()
    {
        var area = TripFlow.Area(2);
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var (driver, driverId) = await SafetyFlow.OnlineDriverAsync(fixture, area);
        var trips = new List<string>();
        foreach (var (stars, comment) in new[] { (5, (string?)null), (4, "رحلة جيدة"), (2, "BadWord driver") })
        {
            var tripId = await RewardsFlow.CompleteRideAsync(fixture, area, passenger, driver, RewardsFlow.Request(area));
            trips.Add(tripId);
            fixture.Factory.Clock.Advance(TimeSpan.FromMinutes(1));
            var rated = await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{tripId}/rating", new { stars, comment });
            Assert.Equal(HttpStatusCode.Created, rated.StatusCode);
        }

        // Newest first [2, 4, 5] with weights 1, 0.8333, 0.6667 → 8.6667 / 2.5 = 3.47.
        var profile = await fixture.Factory.WithDbAsync(db => db.Drivers.AsNoTracking().FirstAsync(d => d.Id == driverId));
        Assert.Equal(3.47m, profile.RatingAvg);
        Assert.Equal(3, profile.RatingCount);

        var flags = await fixture.Factory.WithDbAsync(db => db.RatingFlags.AsNoTracking().ToListAsync());
        Assert.Single(flags, f => f.Type == RatingFlagType.LowRating && f.UserId == driver.UserId && f.Value == 2m);
        Assert.Single(flags, f => f.Type == RatingFlagType.LowAverage && f.UserId == driver.UserId && f.Value == 3.47m);
        Assert.Single(flags, f => f.Type == RatingFlagType.AbusiveComment && f.UserId == passenger.UserId);
        var abusive = await fixture.Factory.WithDbAsync(db => db.Ratings.AsNoTracking().FirstAsync(r => r.TripId == Guid.Parse(trips[2])));
        Assert.True(abusive.CommentHidden);
        Assert.Equal(RatingStatus.Visible, abusive.Status);
        var summary = await (await driver.Client.GetAsync("/api/v1/driver/ratings/summary")).ReadJsonAsync();
        Assert.DoesNotContain(summary.GetProperty("recentComments").EnumerateArray(), c => c.GetProperty("comment").GetString()!.Contains("BadWord"));

        // Another low rating: still one open low_average flag per driver; the daily job adds nothing either.
        var fourth = await RewardsFlow.CompleteRideAsync(fixture, area, passenger, driver, RewardsFlow.Request(area));
        fixture.Factory.Clock.Advance(TimeSpan.FromMinutes(1));
        (await passenger.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{fourth}/rating", new { stars = 1 })).EnsureSuccessStatusCode();
        Assert.Equal(0, await fixture.Factory.WithServiceAsync<RatingService, int>(s => s.FlagLowAveragesAsync(CancellationToken.None)));
        Assert.Equal(1, await fixture.Factory.WithDbAsync(db => db.RatingFlags.CountAsync(f => f.UserId == driver.UserId && f.Type == RatingFlagType.LowAverage)));

        using var admin = await fixture.LoginAdminAsync();
        var lowId = await fixture.Factory.WithDbAsync(db => db.Ratings.Where(r => r.TripId == Guid.Parse(trips[2])).Select(r => r.Id).FirstAsync());
        var fourthId = await fixture.Factory.WithDbAsync(db => db.Ratings.Where(r => r.TripId == Guid.Parse(fourth)).Select(r => r.Id).FirstAsync());
        (await admin.PostAsJsonAsync($"/api/v1/admin/ratings/{fourthId}/hide", new { reason = "test" })).EnsureSuccessStatusCode();
        var hidden = await admin.PostAsJsonAsync($"/api/v1/admin/ratings/{lowId}/hide", new { reason = "شكوى غير صحيحة" });
        Assert.Equal(HttpStatusCode.OK, hidden.StatusCode);
        var hiddenBody = await hidden.ReadJsonAsync();
        Assert.Equal("hidden", hiddenBody.GetProperty("status").GetString());
        Assert.True(hiddenBody.GetProperty("flagged").GetBoolean());
        // Visible newest first [4, 5] with weights 1, 0.75 → 7.75 / 1.75 = 4.43.
        Assert.Equal(4.43m, await fixture.Factory.WithDbAsync(db => db.Drivers.Where(d => d.Id == driverId).Select(d => d.RatingAvg).FirstAsync()));
        Assert.Equal(2, await fixture.Factory.WithDbAsync(db => db.Drivers.Where(d => d.Id == driverId).Select(d => d.RatingCount).FirstAsync()));
        (await admin.PostAsync($"/api/v1/admin/ratings/{lowId}/unhide", null)).EnsureSuccessStatusCode();
        Assert.Equal(3.47m, await fixture.Factory.WithDbAsync(db => db.Drivers.Where(d => d.Id == driverId).Select(d => d.RatingAvg).FirstAsync()));
        Assert.True(await fixture.Factory.WithDbAsync(db => db.AuditLogs.AnyAsync(a => a.Action == "rating.hide" && a.EntityId == lowId)));
        Assert.True(await fixture.Factory.WithDbAsync(db => db.AuditLogs.AnyAsync(a => a.Action == "rating.unhide" && a.EntityId == lowId)));

        var hiddenList = await (await admin.GetAsync($"/api/v1/admin/ratings?status=hidden&userId={driver.UserId}")).ReadJsonAsync();
        Assert.Equal(fourthId.ToString(), Assert.Single(hiddenList.GetProperty("items").EnumerateArray()).GetProperty("id").GetString());
        var flaggedList = await (await admin.GetAsync($"/api/v1/admin/ratings?flagged=true&userId={driver.UserId}")).ReadJsonAsync();
        Assert.True(flaggedList.GetProperty("total").GetInt32() >= 2);

        var openFlags = await (await admin.GetAsync("/api/v1/admin/rating-flags?status=open&type=low_average")).ReadJsonAsync();
        var lowAverage = Assert.Single(openFlags.GetProperty("items").EnumerateArray(), f => f.GetProperty("userId").GetString() == driver.UserId.ToString());
        Assert.Equal(driverId.ToString(), lowAverage.GetProperty("driverId").GetString());
        var flagId = lowAverage.GetProperty("id").GetString();
        var reviewed = await (await admin.PostAsJsonAsync($"/api/v1/admin/rating-flags/{flagId}/review", new { action = "suspension_review", note = "متابعة الجودة" })).ReadJsonAsync();
        Assert.Equal("actioned", reviewed.GetProperty("status").GetString());
        Assert.Equal("suspension_review", reviewed.GetProperty("action").GetString());
        var again = await admin.PostAsJsonAsync($"/api/v1/admin/rating-flags/{flagId}/review", new { action = "dismiss" });
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.True(await fixture.Factory.WithDbAsync(db => db.AuditLogs.AnyAsync(a => a.Action == "rating_flag.review" && a.EntityId == Guid.Parse(flagId!))));
        Assert.True(await fixture.Factory.WithDbAsync(db => db.AuditLogs.AnyAsync(a => a.Action == "driver.suspension_review" && a.EntityId == driverId)));
        // The review never suspends automatically.
        Assert.Equal(ATA.Domain.Drivers.ApplicationStatus.Approved, await fixture.Factory.WithDbAsync(db => db.Drivers.Where(d => d.Id == driverId).Select(d => d.ApplicationStatus).FirstAsync()));
    }
}
