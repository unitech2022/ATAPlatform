using ATA.Domain.Ratings;
using ATA.Domain.Reporting;
using ATA.Domain.Trips;
using ATA.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Integration;

/// <summary><c>ReportSnapshotJob</c> (doc 12 §F20.8): yesterday + the trailing recompute days, upserted (never duplicated), late rows picked up.</summary>
public sealed class ReportSnapshotJobTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Job_snapshots_yesterday_and_the_trailing_days_upserts_and_recomputes_late_rows()
    {
        var data = await fixture.Factory.WithDbAsync(ReportData.SeedAsync);
        var clock = fixture.Factory.Clock;
        // 2026-09-22 02:00 Riyadh: yesterday = 09-21, trailing 3 days → 09-18 … 09-21.
        clock.Set(new DateTime(2026, 9, 21, 23, 0, 0, DateTimeKind.Utc));
        var rows = await fixture.Factory.RunReportSnapshotJobAsync();
        Assert.True(rows > 4 * 32);

        async Task<List<DateOnly>> DaysAsync() => await fixture.Factory.WithDbAsync(db =>
            db.ReportSnapshots.Where(s => s.ScopeKey == ReportSnapshot.AllScope).Select(s => s.SnapshotDate).Distinct().OrderBy(d => d).ToListAsync());
        Assert.Equal([new DateOnly(2026, 9, 18), new DateOnly(2026, 9, 19), ReportData.Day, new DateOnly(2026, 9, 21)], await DaysAsync());
        async Task<ReportSnapshot> RowAsync(DateOnly day, string metric) => await fixture.Factory.WithDbAsync(db =>
            db.ReportSnapshots.AsNoTracking().SingleAsync(s => s.SnapshotDate == day && s.ScopeKey == ReportSnapshot.AllScope && s.MetricCode == metric));
        Assert.Equal(3m, (await RowAsync(ReportData.Day, "completed_trips")).Value);
        Assert.Equal(1m, (await RowAsync(new DateOnly(2026, 9, 21), "completed_trips")).Value);
        Assert.Equal(4.5m, (await RowAsync(ReportData.Day, "customer_rating")).Value);

        // Running twice the same day replaces the rows (UNIQUE(snapshot_date, scope_key, metric_code)).
        var count = await fixture.Factory.WithDbAsync(db => db.ReportSnapshots.CountAsync());
        Assert.Equal(rows, count);
        Assert.Equal(rows, await fixture.Factory.RunReportSnapshotJobAsync());
        Assert.Equal(count, await fixture.Factory.WithDbAsync(db => db.ReportSnapshots.CountAsync()));
        Assert.False(await fixture.Factory.WithDbAsync(db => db.ReportSnapshots.GroupBy(s => new { s.SnapshotDate, s.ScopeKey, s.MetricCode }).AnyAsync(g => g.Count() > 1)));

        // Late rows: a rating dated 09-20 (inside the next run's trailing window) and one dated 09-18 (outside it).
        var t9 = await fixture.Factory.WithDbAsync(db => db.Trips.Where(t => t.CompletedAt >= new DateTime(2026, 9, 21, 0, 0, 0, DateTimeKind.Utc)).Select(t => t.Id).SingleAsync());
        var t0 = await fixture.Factory.WithDbAsync(db => db.Trips.Where(t => t.CompletedAt < new DateTime(2026, 9, 16, 0, 0, 0, DateTimeKind.Utc) && t.Status == TripStatus.Completed).Select(t => t.Id).SingleAsync());
        await fixture.Factory.WithDbAsync(async db =>
        {
            db.Ratings.Add(new Rating { TripId = t9, RaterUserId = data.P1.Id, RaterRole = RatingRole.Passenger, RateeUserId = data.D1User.Id, RateeRole = RatingRole.Driver, Stars = 3, CreatedAt = ReportData.At(20, 20) });
            db.Ratings.Add(new Rating { TripId = t0, RaterUserId = data.P3.Id, RaterRole = RatingRole.Passenger, RateeUserId = data.D2User.Id, RateeRole = RatingRole.Driver, Stars = 1, CreatedAt = ReportData.At(18, 12) });
            await db.SaveChangesAsync();
            return true;
        });

        clock.Advance(TimeSpan.FromDays(1));
        await fixture.Factory.RunReportSnapshotJobAsync();
        Assert.Equal(4m, (await RowAsync(ReportData.Day, "customer_rating")).Value);
        var stale = await RowAsync(new DateOnly(2026, 9, 18), "customer_rating");
        Assert.Equal((0m, 0m), (stale.Numerator!.Value, stale.Denominator!.Value));
        Assert.Contains(new DateOnly(2026, 9, 22), await DaysAsync());
    }
}
