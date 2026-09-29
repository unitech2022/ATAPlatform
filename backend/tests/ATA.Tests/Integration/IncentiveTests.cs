using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Api.Modules.Incentives;
using ATA.Domain.Cancellation;
using ATA.Domain.Common;
using ATA.Domain.Incentives;
using ATA.Domain.Notifications;
using ATA.Domain.Pricing;
using ATA.Domain.Wallet;
using ATA.Infrastructure.Persistence.Seed;
using ATA.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Integration;

public class IncentiveTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    private DateTime Now => fixture.Factory.Clock.UtcNow;

    private async Task<JsonElement> CreateIncentiveAsync(HttpClient admin, string name, Action<Dictionary<string, object?>>? configure = null)
    {
        var body = new Dictionary<string, object?>
        {
            ["nameAr"] = name, ["nameEn"] = name, ["type"] = "one_time", ["cityId"] = SeedIds.CityRiyadh, ["targetTrips"] = 2, ["rewardAmount"] = 50m,
            ["startsAt"] = Now.AddHours(-1), ["endsAt"] = Now.AddHours(1), ["isActive"] = true,
        };
        configure?.Invoke(body);
        var response = await admin.PostAsJsonAsync("/api/v1/admin/incentives", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.ReadJsonAsync();
    }

    private Task<List<DriverIncentiveProgress>> ProgressAsync(Guid driverId) =>
        fixture.Factory.WithDbAsync(db => db.DriverIncentiveProgress.AsNoTracking().Where(p => p.DriverId == driverId).ToListAsync());

    [Fact]
    public async Task Progress_counts_matching_trips_once_and_is_paid_to_the_wallet_after_the_delay()
    {
        var area = TripFlow.Area(0);
        using var admin = await fixture.LoginAdminAsync();
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var (driver, driverId) = await SafetyFlow.OnlineDriverAsync(fixture, area);

        var main = await CreateIncentiveAsync(admin, "رحلتان اقتصادي", b => { b["rideCategoryIds"] = new[] { SeedIds.RideCategories.Economy }; b["minTripFare"] = 1m; b["notifyOnPublish"] = true; });
        var inZone = await CreateIncentiveAsync(admin, "منطقة الرياض", b => { b["type"] = "zone_quest"; b["zoneIds"] = new[] { SeedIds.ZoneRiyadhDefault }; b["rewardAmount"] = 30m; });
        await CreateIncentiveAsync(admin, "مريح فقط", b => b["rideCategoryIds"] = new[] { SeedIds.RideCategories.Comfort });
        await CreateIncentiveAsync(admin, "مساءً فقط", b => { b["dailyFrom"] = "20:00"; b["dailyTo"] = "21:00"; });
        await CreateIncentiveAsync(admin, "الأحد فقط", b => b["daysOfWeek"] = new[] { 0 });
        await CreateIncentiveAsync(admin, "أجرة عالية", b => b["minTripFare"] = 500m);
        var farZone = await fixture.Factory.WithDbAsync(async db =>
        {
            var zone = new Zone { CityId = SeedIds.CityRiyadh, Code = "incentive_far_zone", NameAr = "بعيدة", NameEn = "Far", Polygon = "[[21.0,39.0],[21.0,39.1],[21.1,39.1],[21.1,39.0],[21.0,39.0]]", CenterLat = 21.05m, CenterLng = 39.05m, IsActive = false };
            db.Zones.Add(zone);
            await db.SaveChangesAsync();
            return zone.Id;
        });
        await CreateIncentiveAsync(admin, "منطقة أخرى", b => b["zoneIds"] = new[] { farZone });
        var longer = await CreateIncentiveAsync(admin, "خمس رحلات", b => b["targetTrips"] = 5);
        Assert.True(await fixture.Factory.WithDbAsync(db => db.Notifications.AnyAsync(n => n.UserId == driver.UserId && n.Type == NotificationTypes.IncentiveNew)));
        Assert.True(await fixture.Factory.WithDbAsync(db => db.AuditLogs.AnyAsync(a => a.Action == "incentive.create" && a.EntityId == Guid.Parse(main.GetProperty("id").GetString()!))));

        var first = await RewardsFlow.CompleteRideAsync(fixture, area, passenger, driver, RewardsFlow.Request(area));
        await RewardsFlow.CompleteRideAsync(fixture, area, passenger, driver, RewardsFlow.Request(area));
        Assert.Equal(0, await fixture.Factory.WithServiceAsync<IncentiveService, int>(s => s.RecordTripAsync(Guid.Parse(first), CancellationToken.None)));

        var progress = await ProgressAsync(driverId);
        var mainId = Guid.Parse(main.GetProperty("id").GetString()!);
        var zoneId = Guid.Parse(inZone.GetProperty("id").GetString()!);
        var longerId = Guid.Parse(longer.GetProperty("id").GetString()!);
        Assert.Equal(3, progress.Count);
        Assert.Equal(IncentiveProgressStatus.Achieved, progress.Single(p => p.IncentiveId == mainId).Status);
        Assert.Equal(2, progress.Single(p => p.IncentiveId == mainId).CompletedTrips);
        Assert.Equal(IncentiveProgressStatus.Achieved, progress.Single(p => p.IncentiveId == zoneId).Status);
        Assert.Equal(IncentiveProgressStatus.InProgress, progress.Single(p => p.IncentiveId == longerId).Status);
        var mainProgressId = progress.Single(p => p.IncentiveId == mainId).Id;
        Assert.Equal(2, await fixture.Factory.WithDbAsync(db => db.DriverIncentiveTrips.CountAsync(t => t.ProgressId == mainProgressId)));

        var active = await (await driver.Client.GetAsync("/api/v1/driver/incentives?status=active")).ReadJsonAsync();
        var mainView = active.EnumerateArray().First(i => i.GetProperty("id").GetString() == mainId.ToString());
        Assert.Equal(2, mainView.GetProperty("progress").GetProperty("completedTrips").GetInt32());
        Assert.Equal("achieved", mainView.GetProperty("progress").GetProperty("status").GetString());
        Assert.Equal(["economy"], mainView.GetProperty("rideCategoryCodes").EnumerateArray().Select(c => c.GetString()!).ToArray());
        var detail = await (await driver.Client.GetAsync($"/api/v1/driver/incentives/{zoneId}")).ReadJsonAsync();
        Assert.Equal(JsonValueKind.Array, detail.GetProperty("zonesPolygons").ValueKind);
        Assert.Single(detail.GetProperty("zones").EnumerateArray());

        // Not paid before period end + Incentives:PayoutDelayHours.
        Assert.Equal(0, await fixture.Factory.WithServiceAsync<IncentiveService, int>(s => s.PayoutDueAsync(CancellationToken.None)));
        var before = await SafetyFlow.WalletBalanceAsync(fixture, driver.UserId, WalletKind.Driver);
        fixture.Factory.Clock.Advance(TimeSpan.FromHours(3.1));
        Assert.Equal(2, await fixture.Factory.WithServiceAsync<IncentiveService, int>(s => s.PayoutDueAsync(CancellationToken.None)));
        Assert.Equal(before + 80m, await SafetyFlow.WalletBalanceAsync(fixture, driver.UserId, WalletKind.Driver));
        var paid = (await ProgressAsync(driverId)).Single(p => p.IncentiveId == mainId);
        Assert.Equal(IncentiveProgressStatus.Paid, paid.Status);
        Assert.Equal(50m, paid.RewardAmount);
        Assert.Equal(1.00m, paid.IncentiveMultiplier);
        var movement = await fixture.Factory.WithDbAsync(db => db.WalletTransactions.AsNoTracking().SingleAsync(t => t.IdempotencyKey == $"incentive:{paid.Id}"));
        Assert.Equal(TransactionType.Incentive, movement.Type);
        Assert.Equal(paid.WalletTransactionId, movement.Id);
        Assert.True(await fixture.Factory.WithDbAsync(db => db.LedgerEntries.AnyAsync(e => e.TransactionId == movement.Id && e.Account == LedgerAccounts.Incentives && e.Debit == 50m)));
        Assert.Equal(50m, await fixture.Factory.WithDbAsync(db => db.DriverIncentives.Where(i => i.Id == mainId).Select(i => i.SpentAmount).FirstAsync()));
        Assert.True(await fixture.Factory.WithDbAsync(db => db.Notifications.AnyAsync(n => n.UserId == driver.UserId && n.Type == NotificationTypes.IncentiveAchieved)));
        Assert.Equal(0, await fixture.Factory.WithServiceAsync<IncentiveService, int>(s => s.PayoutDueAsync(CancellationToken.None)));
        await PaymentFlow.AssertLedgerBalancedAsync(fixture);

        // IncentivePeriodJob: the period ended below the target → expired.
        await fixture.Factory.WithServiceAsync<IncentiveService, int>(s => s.RunPeriodsAsync(CancellationToken.None));
        Assert.Equal(IncentiveProgressStatus.Expired, (await ProgressAsync(driverId)).Single(p => p.IncentiveId == longerId).Status);
    }

    [Fact]
    public async Task Payout_is_reduced_by_the_reliability_multiplier_and_voided_when_the_budget_is_exhausted()
    {
        using var admin = await fixture.LoginAdminAsync();
        var (reduced, reducedId) = await SafetyFlow.OnlineDriverAsync(fixture, TripFlow.Area(1));
        var (normal, normalId) = await SafetyFlow.OnlineDriverAsync(fixture, TripFlow.Area(1));
        var reward = await CreateIncentiveAsync(admin, "مكافأة 40", b => { b["rewardAmount"] = 40m; b["startsAt"] = Now.AddDays(-3); b["endsAt"] = Now.AddDays(-1); });
        var budgeted = await CreateIncentiveAsync(admin, "ميزانية صغيرة", b => { b["budgetAmount"] = 10m; b["startsAt"] = Now.AddDays(-3); b["endsAt"] = Now.AddDays(-1); });
        await fixture.Factory.WithDbAsync(async db =>
        {
            db.ReliabilityProfiles.Add(new ReliabilityProfile { UserId = reduced.UserId, Role = Role.Driver, WindowDays = 30, RestrictionLevel = RestrictionLevel.IncentivesReduced, LastComputedAt = Now });
            foreach (var (incentive, driverId) in new[] { (reward, reducedId), (budgeted, normalId) })
            {
                db.DriverIncentiveProgress.Add(new DriverIncentiveProgress
                {
                    IncentiveId = Guid.Parse(incentive.GetProperty("id").GetString()!), DriverId = driverId, PeriodStart = Now.AddDays(-3), PeriodEnd = Now.AddDays(-1),
                    CompletedTrips = 2, Status = IncentiveProgressStatus.Achieved, AchievedAt = Now.AddDays(-2),
                });
            }

            return await db.SaveChangesAsync();
        });

        await fixture.Factory.WithServiceAsync<IncentiveService, int>(s => s.PayoutDueAsync(CancellationToken.None));
        var halved = (await ProgressAsync(reducedId)).Single();
        Assert.Equal(IncentiveProgressStatus.Paid, halved.Status);
        Assert.Equal(0.50m, halved.IncentiveMultiplier);
        Assert.Equal(20m, halved.RewardAmount);
        Assert.Equal(20m, await SafetyFlow.WalletBalanceAsync(fixture, reduced.UserId, WalletKind.Driver));
        var view = await (await reduced.Client.GetAsync($"/api/v1/driver/incentives/{reward.GetProperty("id").GetString()}")).ReadJsonAsync();
        Assert.Equal(0.50m, view.GetProperty("rewardMultiplier").GetDecimal());
        Assert.Equal(20m, view.GetProperty("effectiveRewardAmount").GetDecimal());
        Assert.Equal(20m, view.GetProperty("progress").GetProperty("rewardAmount").GetDecimal());
        var voided = (await ProgressAsync(normalId)).Single();
        Assert.Equal(IncentiveProgressStatus.Voided, voided.Status);
        Assert.Equal("budget_exhausted", voided.VoidedReason);
        Assert.Equal(0m, await SafetyFlow.WalletBalanceAsync(fixture, normal.UserId, WalletKind.Driver));
    }

    [Fact]
    public async Task Opt_in_participant_cap_refund_removal_admin_void_and_crud_are_enforced_and_audited()
    {
        var area = TripFlow.Area(2);
        using var admin = await fixture.LoginAdminAsync();
        var passenger = await SafetyFlow.PassengerAsync(fixture);
        var (driver, driverId) = await SafetyFlow.OnlineDriverAsync(fixture, area);
        var (other, _) = await SafetyFlow.OnlineDriverAsync(fixture, TripFlow.Area(3));
        var quest = await CreateIncentiveAsync(admin, "بالاشتراك", b => { b["requiresOptIn"] = true; b["maxParticipants"] = 1; b["targetTrips"] = 3; });
        var questId = quest.GetProperty("id").GetString()!;

        await RewardsFlow.CompleteRideAsync(fixture, area, passenger, driver, RewardsFlow.Request(area));
        Assert.DoesNotContain(await ProgressAsync(driverId), p => p.IncentiveId == Guid.Parse(questId));

        var joined = await driver.Client.PostAsync($"/api/v1/driver/incentives/{questId}/opt-in", null);
        Assert.Equal(HttpStatusCode.OK, joined.StatusCode);
        var joinedBody = await joined.ReadJsonAsync();
        Assert.True(joinedBody.GetProperty("optedIn").GetBoolean());
        Assert.Equal(0, joinedBody.GetProperty("progress").GetProperty("completedTrips").GetInt32());
        var full = await other.Client.PostAsync($"/api/v1/driver/incentives/{questId}/opt-in", null);
        Assert.Equal(HttpStatusCode.Conflict, full.StatusCode);
        Assert.Equal("incentive_opt_in_closed", await full.ErrorCodeAsync());

        var counted = await RewardsFlow.CompleteRideAsync(fixture, area, passenger, driver, RewardsFlow.Request(area));
        Assert.Equal(1, (await ProgressAsync(driverId)).Single(p => p.IncentiveId == Guid.Parse(questId)).CompletedTrips);

        // A fully refunded trip leaves unpaid progress.
        var fare = (await RewardsFlow.TripAsync(fixture, counted)).FinalFare!.Value;
        var refund = await admin.PostAsJsonAsync($"/api/v1/admin/trips/{counted}/refunds", new { amount = fare, reasonCode = "goodwill", reason = "تعويض كامل" });
        Assert.Equal("succeeded", (await refund.ReadJsonAsync()).GetProperty("status").GetString());
        var afterRefund = (await ProgressAsync(driverId)).Single(p => p.IncentiveId == Guid.Parse(questId));
        Assert.Equal(0, afterRefund.CompletedTrips);

        var progressList = await (await admin.GetAsync($"/api/v1/admin/incentives/{questId}/progress")).ReadJsonAsync();
        var row = Assert.Single(progressList.GetProperty("items").EnumerateArray());
        Assert.Equal("in_progress", row.GetProperty("status").GetString());
        var voided = await admin.PostAsJsonAsync($"/api/v1/admin/incentive-progress/{row.GetProperty("id").GetString()}/void", new { reason = "اشتباه احتيال" });
        Assert.Equal("voided", (await voided.ReadJsonAsync()).GetProperty("status").GetString());
        var twice = await admin.PostAsJsonAsync($"/api/v1/admin/incentive-progress/{row.GetProperty("id").GetString()}/void", new { reason = "again" });
        Assert.Equal(HttpStatusCode.Conflict, twice.StatusCode);
        var perDriver = await (await admin.GetAsync($"/api/v1/admin/drivers/{driverId}/incentives")).ReadJsonAsync();
        Assert.Contains(perDriver.EnumerateArray(), p => p.GetProperty("incentiveId").GetString() == questId && p.GetProperty("status").GetString() == "voided");

        var detail = await (await admin.GetAsync($"/api/v1/admin/incentives/{questId}")).ReadJsonAsync();
        Assert.Equal(1, detail.GetProperty("participantsCount").GetInt32());
        Assert.Equal("active", detail.GetProperty("status").GetString());
        var update = await admin.PutAsJsonAsync($"/api/v1/admin/incentives/{questId}", new
        {
            nameAr = "بالاشتراك 2", nameEn = "Opt-in 2", type = "weekly", cityId = SeedIds.CityRiyadh, targetTrips = 4, rewardAmount = 60, startsAt = Now.AddHours(-1), endsAt = Now.AddDays(7),
            daysOfWeek = new[] { 4, 5 }, dailyFrom = "16:00", dailyTo = "23:59", requiresOptIn = true, maxParticipants = 1,
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.ReadJsonAsync();
        Assert.Equal("16:00", updated.GetProperty("dailyFrom").GetString());
        Assert.Equal([4, 5], updated.GetProperty("daysOfWeek").EnumerateArray().Select(d => d.GetInt32()).ToArray());
        var badTime = await admin.PutAsJsonAsync($"/api/v1/admin/incentives/{questId}", new { nameAr = "x", nameEn = "x", type = "daily", cityId = SeedIds.CityRiyadh, targetTrips = 1, rewardAmount = 1, startsAt = Now, endsAt = Now.AddDays(1), dailyFrom = "25:00", dailyTo = "26:00" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, badTime.StatusCode);
        var deactivated = await (await admin.PostAsync($"/api/v1/admin/incentives/{questId}/deactivate", null)).ReadJsonAsync();
        Assert.Equal("inactive", deactivated.GetProperty("status").GetString());
        var closed = await driver.Client.PostAsync($"/api/v1/driver/incentives/{questId}/opt-in", null);
        Assert.Equal("incentive_opt_in_closed", await closed.ErrorCodeAsync());
        var list = await (await admin.GetAsync("/api/v1/admin/incentives?status=inactive")).ReadJsonAsync();
        Assert.Contains(list.GetProperty("items").EnumerateArray(), i => i.GetProperty("id").GetString() == questId);
        var actions = await fixture.Factory.WithDbAsync(db => db.AuditLogs.Where(a => a.EntityId == Guid.Parse(questId) || a.EntityType == "incentive_progress").Select(a => a.Action).ToListAsync());
        Assert.Contains("incentive.create", actions);
        Assert.Contains("incentive.update", actions);
        Assert.Contains("incentive.deactivate", actions);
        Assert.Contains("incentive_progress.void", actions);
        Assert.Equal(HttpStatusCode.Forbidden, (await driver.Client.GetAsync("/api/v1/admin/incentives")).StatusCode);
    }
}
