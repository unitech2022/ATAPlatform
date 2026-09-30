using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Domain.Common;
using ATA.Domain.Reporting;
using ATA.Infrastructure.Locking;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Reporting;

/// <summary>
/// Daily KPI snapshots (doc 12 §F20.8): every metric of a Riyadh day for every scope with activity (<c>all</c> always, zero values included). A day is
/// recomputed as a whole — its previous rows are replaced in one transaction — so running twice never duplicates rows (UNIQUE(snapshot_date, scope_key,
/// metric_code)). The current Riyadh day and later are never stored: the reports compute them live.
/// </summary>
public sealed class ReportSnapshotService(AtaDbContext db, KpiCalculator calculator, IClock clock, IOptions<ReportsOptions> options, AuditService audit)
{
    private const int ChunkDays = 31;

    public DateOnly Today => Formats.RiyadhDate(clock.UtcNow);

    /// <summary><c>ReportSnapshotJob</c>: yesterday plus the <c>Reports:RecomputeTrailingDays</c> days before it.</summary>
    public Task<int> RunDailyAsync(CancellationToken ct)
    {
        var yesterday = Today.AddDays(-1);
        return RebuildAsync(yesterday.AddDays(-Math.Max(0, options.Value.RecomputeTrailingDays)), yesterday, ct);
    }

    /// <summary><c>POST /admin/reports/snapshots/rebuild</c>: validates the range (≤ <c>Reports:MaxRangeDays</c>, days before today) and rebuilds it synchronously.</summary>
    public async Task<SnapshotRebuildResponse> RebuildRequestedAsync(SnapshotRebuildRequest request, CancellationToken ct)
    {
        new Validator().Require(nameof(request.From), request.From).Require(nameof(request.To), request.To)
            .Rule(nameof(request.To), request.From is null || request.To is null || request.To >= request.From, "must not be before from")
            .ThrowIfInvalid();
        var from = request.From!.Value;
        var to = request.To!.Value;
        ReportRange.EnsureWithin(from, to, options.Value.MaxRangeDays);
        var yesterday = Today.AddDays(-1);
        if (to > yesterday) to = yesterday;
        if (from > to)
        {
            throw new DomainException(ErrorCodes.ValidationFailed, new Dictionary<string, string> { ["from"] = "only days before today can be snapshotted" });
        }

        var rows = await RebuildAsync(from, to, ct);
        audit.Log("report_snapshots.rebuild", "report_snapshot", null, null, new { from, to, rows });
        await db.SaveChangesAsync(ct);
        return new SnapshotRebuildResponse(from, to, to.DayNumber - from.DayNumber + 1, rows);
    }

    public async Task<int> RebuildAsync(DateOnly from, DateOnly to, CancellationToken ct)
    {
        var written = 0;
        for (var chunkStart = from; chunkStart <= to; chunkStart = chunkStart.AddDays(ChunkDays))
        {
            var chunkEnd = chunkStart.AddDays(ChunkDays - 1) < to ? chunkStart.AddDays(ChunkDays - 1) : to;
            var computation = await calculator.ComputeAsync(chunkStart, chunkEnd, d => d, KpiCodes.AllCodes, ct);
            var now = clock.UtcNow;
            var rows = new List<ReportSnapshot>();
            for (var day = chunkStart; day <= chunkEnd; day = day.AddDays(1))
            {
                foreach (var definition in KpiCodes.All)
                {
                    rows.Add(Row(day, KpiScope.All, definition, computation.Get(day, KpiScope.All.Key, definition.Code), now));
                }
            }

            foreach (var ((day, scopeKey), metrics) in computation.Cells.Where(c => c.Key.Scope != KpiScope.All.Key))
            {
                var scope = computation.Scopes[scopeKey];
                foreach (var (code, acc) in metrics)
                {
                    rows.Add(Row(day, scope, KpiCodes.ByCode[code], acc, now));
                }
            }

            var chunkFrom = chunkStart;
            var chunkTo = chunkEnd;
            await db.InTransactionAsync(async () =>
            {
                await db.ReportSnapshots.Where(s => s.SnapshotDate >= chunkFrom && s.SnapshotDate <= chunkTo).ExecuteDeleteAsync(ct);
                db.ReportSnapshots.AddRange(rows);
                await db.SaveChangesAsync(ct);
            }, ct);
            db.ChangeTracker.Clear();
            written += rows.Count;
        }

        return written;
    }

    private static ReportSnapshot Row(DateOnly day, KpiScope scope, KpiDefinition definition, KpiAccumulator? acc, DateTime now)
    {
        var cell = KpiCell.From(definition, acc);
        return new ReportSnapshot
        {
            SnapshotDate = day, ScopeKey = scope.Key, CityId = scope.CityId, ZoneId = scope.ZoneId, RideCategoryId = scope.RideCategoryId, MetricCode = definition.Code,
            Value = cell.Value ?? 0m, Numerator = cell.Numerator, Denominator = cell.Denominator, ComputedAt = now,
        };
    }
}

public static class ReportRange
{
    /// <summary><c>422 report_range_too_large { maxDays }</c> for more than <paramref name="maxDays"/> days (inclusive).</summary>
    public static void EnsureWithin(DateOnly from, DateOnly to, int maxDays)
    {
        if (to.DayNumber - from.DayNumber + 1 > maxDays)
        {
            throw new DomainException(ErrorCodes.ReportRangeTooLarge, new { maxDays });
        }
    }
}

/// <summary>
/// <c>ReportSnapshotJob</c> (doc 12 §F20.8): once a day after 01:30 Riyadh, snapshots yesterday and recomputes the trailing days. Off with
/// <c>Reports:JobsEnabled=false</c> (tests call <see cref="RunOnceAsync"/>).
/// </summary>
public sealed class ReportSnapshotJob(IServiceScopeFactory scopes, IDistributedLock locks, IOptions<ReportsOptions> options, IClock clock, ILogger<ReportSnapshotJob> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.JobsEnabled)
        {
            return;
        }

        DateOnly? lastRun = null;
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                var local = Formats.ToRiyadh(clock.UtcNow);
                var today = DateOnly.FromDateTime(local);
                var due = local.Hour > options.Value.SnapshotHourLocal
                          || (local.Hour == options.Value.SnapshotHourLocal && local.Minute >= options.Value.SnapshotMinuteLocal);
                if (due && lastRun != today)
                {
                    lastRun = today;
                    var rows = await RunOnceAsync(stoppingToken);
                    logger.LogInformation("Report snapshots written: {Rows}", rows);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Report snapshot job failed");
            }
        }
    }

    public async Task<int> RunOnceAsync(CancellationToken ct)
    {
        await using var handle = await locks.TryAcquireAsync("lock:job:report_snapshot", TimeSpan.FromMinutes(30), ct);
        if (handle is null)
        {
            return 0;
        }

        using var scope = scopes.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ReportSnapshotService>().RunDailyAsync(ct);
    }
}
