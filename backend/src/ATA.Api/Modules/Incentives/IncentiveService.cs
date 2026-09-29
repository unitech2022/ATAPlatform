using System.Text.Json;
using ATA.Api.Common;
using ATA.Api.Modules.Cancellation;
using ATA.Api.Modules.Notifications;
using ATA.Api.Modules.Payments;
using ATA.Api.Modules.Pricing;
using ATA.Api.Modules.Promotions;
using ATA.Domain.Common;
using ATA.Domain.Drivers;
using ATA.Domain.Incentives;
using ATA.Domain.Notifications;
using ATA.Domain.Payments;
using ATA.Domain.Trips;
using ATA.Domain.Wallet;
using ATA.Infrastructure.Locking;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Incentives;

/// <summary>
/// Driver incentives / quests (doc 10 §F15.9): progress counted at trip completion (city, period and daily window, pickup zone, category, minimum fare,
/// tier/rating, opt-in, participant cap), removal of fully refunded trips, payout after <c>Incentives:PayoutDelayHours</c> through the ledger
/// (<c>incentives → driver_wallet</c>, key <c>incentive:{progressId}</c>) reduced by the F14 <c>IncentiveMultiplier</c>, period expiry and publication.
/// </summary>
public sealed class IncentiveService(
    AtaDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    LedgerService ledger,
    INotificationDispatcher notifications,
    IReliabilityService reliability,
    ZoneResolver zones,
    IOptions<IncentivesOptions> options)
{
    public const int OffsetMinutes = Formats.RiyadhOffsetMinutes;
    private readonly IncentivesOptions _options = options.Value;

    // ----- progress -----

    /// <summary>Counts a completed trip towards every matching running incentive (idempotent per progress and trip). Saves.</summary>
    public async Task<int> RecordTripAsync(Guid tripId, CancellationToken ct)
    {
        var trip = await db.Trips.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tripId, ct);
        if (trip is not { Status: TripStatus.Completed, CompletedAt: { } at, DriverId: { } driverId })
        {
            return 0;
        }

        var driver = await db.Drivers.AsNoTracking().FirstOrDefaultAsync(d => d.Id == driverId, ct);
        if (driver?.CityId is not { } cityId)
        {
            return 0;
        }

        var incentives = await db.DriverIncentives.AsNoTracking()
            .Where(i => i.IsActive && i.CityId == cityId && i.StartsAt <= at && i.EndsAt > at).ToListAsync(ct);
        var counted = 0;
        foreach (var incentive in incentives)
        {
            if (!await TripMatchesAsync(incentive, trip, at, ct) || !IsEligible(incentive, driver) || incentive.PeriodAt(at, OffsetMinutes) is not { } period)
            {
                continue;
            }

            var progress = await db.DriverIncentiveProgress.FirstOrDefaultAsync(p => p.IncentiveId == incentive.Id && p.DriverId == driver.Id && p.PeriodStart == period.Start, ct);
            if (progress is null)
            {
                var optedInAt = await OptedInAtAsync(incentive.Id, driver.Id, ct);
                if ((incentive.RequiresOptIn && optedInAt is null) || await IsFullAsync(incentive, period.Start, ct))
                {
                    continue;
                }

                progress = new DriverIncentiveProgress
                {
                    IncentiveId = incentive.Id, DriverId = driver.Id, PeriodStart = period.Start, PeriodEnd = period.End, OptedInAt = optedInAt,
                };
                db.DriverIncentiveProgress.Add(progress);
            }
            else if (!progress.IsOpen || await db.DriverIncentiveTrips.AnyAsync(x => x.ProgressId == progress.Id && x.TripId == trip.Id, ct))
            {
                continue;
            }

            db.DriverIncentiveTrips.Add(new DriverIncentiveTrip { ProgressId = progress.Id, TripId = trip.Id, CountedAt = clock.UtcNow });
            progress.CompletedTrips++;
            if (progress.Status == IncentiveProgressStatus.InProgress && progress.CompletedTrips >= incentive.TargetTrips)
            {
                progress.Status = IncentiveProgressStatus.Achieved;
                progress.AchievedAt = clock.UtcNow;
            }

            counted++;
        }

        await db.SaveChangesAsync(ct);
        return counted;
    }

    private async Task<bool> TripMatchesAsync(DriverIncentive incentive, Trip trip, DateTime at, CancellationToken ct)
    {
        if (!incentive.IsInWindow(at, OffsetMinutes))
        {
            return false;
        }

        if (JsonLists.Parse<Guid>(incentive.RideCategoryIds) is { Count: > 0 } categories && !categories.Contains(trip.RideCategoryId))
        {
            return false;
        }

        if (incentive.MinTripFare is { } minFare && (trip.FinalFare ?? 0m) < minFare)
        {
            return false;
        }

        if (JsonLists.Parse<Guid>(incentive.ZoneIds) is { Count: > 0 } zoneIds)
        {
            var inside = false;
            foreach (var zoneId in zoneIds)
            {
                if (await zones.FindAsync(zoneId, ct) is { } zone && zone.Ring.Contains((double)trip.PickupLat, (double)trip.PickupLng))
                {
                    inside = true;
                    break;
                }
            }

            return inside;
        }

        return true;
    }

    public static bool IsEligible(DriverIncentive incentive, DriverProfile driver) =>
        (incentive.MinTier is not { } minTier || driver.Tier >= minTier) && (incentive.MinRating is not { } minRating || driver.RatingAvg >= minRating);

    private async Task<DateTime?> OptedInAtAsync(Guid incentiveId, Guid driverId, CancellationToken ct) =>
        await db.DriverIncentiveProgress.AsNoTracking().Where(p => p.IncentiveId == incentiveId && p.DriverId == driverId && p.OptedInAt != null)
            .OrderBy(p => p.OptedInAt).Select(p => p.OptedInAt).FirstOrDefaultAsync(ct);

    /// <summary><c>max_participants</c> reached for the period (drivers with a progress row that is not voided).</summary>
    private async Task<bool> IsFullAsync(DriverIncentive incentive, DateTime periodStart, CancellationToken ct) =>
        incentive.MaxParticipants is { } max
        && await db.DriverIncentiveProgress.AsNoTracking()
            .Where(p => p.IncentiveId == incentive.Id && p.PeriodStart == periodStart && p.Status != IncentiveProgressStatus.Voided)
            .Select(p => p.DriverId).Distinct().CountAsync(ct) >= max;

    /// <summary>A fully refunded trip (F11) is removed from progress that has not been paid yet. Saves.</summary>
    public async Task<int> RemoveTripAsync(Guid tripId, CancellationToken ct)
    {
        var rows = await (from x in db.DriverIncentiveTrips
                          join p in db.DriverIncentiveProgress on x.ProgressId equals p.Id
                          where x.TripId == tripId && (p.Status == IncentiveProgressStatus.InProgress || p.Status == IncentiveProgressStatus.Achieved)
                          select new { Row = x, Progress = p }).ToListAsync(ct);
        foreach (var row in rows)
        {
            var target = await db.DriverIncentives.AsNoTracking().Where(i => i.Id == row.Progress.IncentiveId).Select(i => i.TargetTrips).FirstAsync(ct);
            db.DriverIncentiveTrips.Remove(row.Row);
            row.Progress.CompletedTrips = Math.Max(0, row.Progress.CompletedTrips - 1);
            if (row.Progress.Status == IncentiveProgressStatus.Achieved && row.Progress.CompletedTrips < target)
            {
                row.Progress.Status = IncentiveProgressStatus.InProgress;
                row.Progress.AchievedAt = null;
            }
        }

        await db.SaveChangesAsync(ct);
        return rows.Count;
    }

    /// <summary>Called after a refund succeeded: once the trip's succeeded refunds cover the whole fare, the trip leaves unpaid progress.</summary>
    public async Task OnTripRefundedAsync(Guid tripId, CancellationToken ct)
    {
        var fare = await db.Trips.AsNoTracking().Where(t => t.Id == tripId && t.Status == TripStatus.Completed).Select(t => t.FinalFare).FirstOrDefaultAsync(ct);
        if (fare is not { } finalFare)
        {
            return;
        }

        var refunded = await db.Refunds.AsNoTracking().Where(r => r.TripId == tripId && r.Status == RefundStatus.Succeeded).SumAsync(r => r.Amount, ct);
        if (refunded >= finalFare)
        {
            await RemoveTripAsync(tripId, ct);
        }
    }

    // ----- jobs -----

    /// <summary>
    /// <c>IncentivePayoutJob</c>: achieved periods ended at least <c>PayoutDelayHours</c> ago are paid <c>round(reward × IncentiveMultiplier, 2)</c> (F14),
    /// or voided with <c>budget_exhausted</c> when the budget would be exceeded.
    /// </summary>
    public async Task<int> PayoutDueAsync(CancellationToken ct)
    {
        var cutoff = clock.UtcNow.AddHours(-_options.PayoutDelayHours);
        var due = await db.DriverIncentiveProgress.AsNoTracking()
            .Where(p => p.Status == IncentiveProgressStatus.Achieved && p.PeriodEnd <= cutoff).OrderBy(p => p.PeriodEnd).Select(p => p.Id).Take(500).ToListAsync(ct);
        var paid = 0;
        foreach (var id in due)
        {
            if (await PayAsync(id, ct)) paid++;
            db.ChangeTracker.Clear();
        }

        return paid;
    }

    private async Task<bool> PayAsync(Guid progressId, CancellationToken ct)
    {
        var result = false;
        await db.InTransactionAsync(async () =>
        {
            var progress = await db.DriverIncentiveProgress.FirstOrDefaultAsync(p => p.Id == progressId && p.Status == IncentiveProgressStatus.Achieved, ct);
            if (progress is null) return;
            var incentive = await db.DriverIncentives.FirstAsync(i => i.Id == progress.IncentiveId, ct);
            var driverUserId = await db.Drivers.AsNoTracking().Where(d => d.Id == progress.DriverId).Select(d => d.UserId).FirstAsync(ct);
            var multiplier = reliability.IncentiveMultiplier(await reliability.GetAsync(driverUserId, Role.Driver, ct));
            var reward = PricingMath.Round2(incentive.RewardAmount * multiplier);
            var now = clock.UtcNow;
            progress.IncentiveMultiplier = multiplier;
            if (incentive.BudgetAmount is { } budget && incentive.SpentAmount + reward > budget)
            {
                progress.Status = IncentiveProgressStatus.Voided;
                progress.VoidedReason = "budget_exhausted";
                await db.SaveChangesAsync(ct);
                return;
            }

            if (reward > 0)
            {
                var wallet = await ledger.GetOrCreateWalletAsync(driverUserId, WalletKind.Driver, ct);
                var transaction = await ledger.PostAsync(wallet, TransactionType.Incentive, TransactionDirection.Credit, reward, LedgerAccounts.Incentives,
                    $"Incentive {incentive.NameEn}", $"incentive:{progress.Id}", "incentive_progress", progress.Id, ct);
                progress.WalletTransactionId = transaction?.Id;
            }

            progress.Status = IncentiveProgressStatus.Paid;
            progress.RewardAmount = reward;
            progress.PaidAt = now;
            incentive.SpentAmount += reward;
            await notifications.DispatchAsync(new NotificationRequest(NotificationTypes.IncentiveAchieved, driverUserId,
                NotificationPlaceholders.Of().Localized("incentiveName", incentive.NameAr, incentive.NameEn).Money("reward", reward), "incentive", incentive.Id,
                new Dictionary<string, object?> { ["progressId"] = progress.Id, ["incentiveId"] = incentive.Id, ["multiplier"] = multiplier }), ct);
            await db.SaveChangesAsync(ct);
            result = true;
        }, ct);
        return result;
    }

    /// <summary>
    /// <c>IncentivePeriodJob</c>: periods that ended without reaching the target → <c>expired</c>; opted-in drivers of running recurring incentives get the
    /// current period opened.
    /// </summary>
    public async Task<int> RunPeriodsAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;
        var expired = await db.DriverIncentiveProgress.Where(p => p.Status == IncentiveProgressStatus.InProgress && p.PeriodEnd <= now).ToListAsync(ct);
        foreach (var progress in expired)
        {
            progress.Status = IncentiveProgressStatus.Expired;
        }

        var opened = 0;
        var recurring = await db.DriverIncentives.AsNoTracking()
            .Where(i => i.IsActive && i.RequiresOptIn && (i.Type == IncentiveType.Daily || i.Type == IncentiveType.Weekly) && i.StartsAt <= now && i.EndsAt > now)
            .ToListAsync(ct);
        foreach (var incentive in recurring)
        {
            if (incentive.PeriodAt(now, OffsetMinutes) is not { } period) continue;
            var subscribers = await db.DriverIncentiveProgress.AsNoTracking().Where(p => p.IncentiveId == incentive.Id && p.OptedInAt != null)
                .GroupBy(p => p.DriverId).Select(g => new { DriverId = g.Key, OptedInAt = g.Min(p => p.OptedInAt) }).ToListAsync(ct);
            foreach (var subscriber in subscribers)
            {
                if (await db.DriverIncentiveProgress.AnyAsync(p => p.IncentiveId == incentive.Id && p.DriverId == subscriber.DriverId && p.PeriodStart == period.Start, ct)
                    || db.DriverIncentiveProgress.Local.Any(p => p.IncentiveId == incentive.Id && p.DriverId == subscriber.DriverId && p.PeriodStart == period.Start))
                {
                    continue;
                }

                db.DriverIncentiveProgress.Add(new DriverIncentiveProgress
                {
                    IncentiveId = incentive.Id, DriverId = subscriber.DriverId, PeriodStart = period.Start, PeriodEnd = period.End, OptedInAt = subscriber.OptedInAt,
                });
                opened++;
            }
        }

        await db.SaveChangesAsync(ct);
        return expired.Count + opened;
    }

    /// <summary><c>incentive.new</c> to the eligible approved drivers of the city, once, for an active incentive with <c>notify_on_publish</c>. The caller saves.</summary>
    public async Task PublishAsync(DriverIncentive incentive, CancellationToken ct)
    {
        if (!incentive.IsActive || !incentive.NotifyOnPublish || incentive.PublishedAt is not null || incentive.EndsAt <= clock.UtcNow)
        {
            return;
        }

        var drivers = await db.Drivers.AsNoTracking().Where(d => d.ApplicationStatus == ApplicationStatus.Approved && d.CityId == incentive.CityId).ToListAsync(ct);
        foreach (var driver in drivers.Where(d => IsEligible(incentive, d)))
        {
            await notifications.DispatchAsync(new NotificationRequest(NotificationTypes.IncentiveNew, driver.UserId,
                NotificationPlaceholders.Of().Localized("incentiveName", incentive.NameAr, incentive.NameEn).Money("reward", incentive.RewardAmount), "incentive", incentive.Id,
                new Dictionary<string, object?> { ["incentiveId"] = incentive.Id }), ct);
        }

        incentive.PublishedAt = clock.UtcNow;
    }

    // ----- driver endpoints -----

    public async Task<IReadOnlyList<DriverIncentiveDto>> ListForDriverAsync(string? status, Language lang, CancellationToken ct)
    {
        var driver = await LoadDriverAsync(ct);
        var now = clock.UtcNow;
        if (driver.CityId is not { } cityId)
        {
            return [];
        }

        var query = db.DriverIncentives.AsNoTracking().Where(i => i.CityId == cityId);
        List<DriverIncentive> rows;
        switch (status ?? "active")
        {
            case "active":
                rows = await query.Where(i => i.IsActive && i.StartsAt <= now && i.EndsAt > now).OrderBy(i => i.EndsAt).ToListAsync(ct);
                break;
            case "upcoming":
                rows = await query.Where(i => i.IsActive && i.StartsAt > now).OrderBy(i => i.StartsAt).ToListAsync(ct);
                break;
            case "completed":
                var mine = db.DriverIncentiveProgress.AsNoTracking().Where(p => p.DriverId == driver.Id).Select(p => p.IncentiveId);
                rows = await query.Where(i => (i.EndsAt <= now || !i.IsActive) && mine.Contains(i.Id)).OrderByDescending(i => i.EndsAt).Take(50).ToListAsync(ct);
                break;
            default:
                throw new DomainException(ErrorCodes.ValidationFailed, new Dictionary<string, string> { ["status"] = "must be active|upcoming|completed" });
        }

        var result = new List<DriverIncentiveDto>(rows.Count);
        foreach (var incentive in rows.Where(i => status == "completed" || IsEligible(i, driver)))
        {
            var d = await BuildAsync(incentive, driver, now, lang, false, ct);
            result.Add(new DriverIncentiveDto(d.Id, d.Name, d.Description, d.Type, d.TargetTrips, d.RewardAmount, d.PeriodStart, d.PeriodEnd, d.Window, d.Zones,
                d.RideCategoryCodes, d.RequiresOptIn, d.OptedIn, d.Progress, d.RewardMultiplier, d.EffectiveRewardAmount));
        }

        return result;
    }

    public async Task<DriverIncentiveDetailDto> GetForDriverAsync(Guid id, Language lang, CancellationToken ct)
    {
        var driver = await LoadDriverAsync(ct);
        var incentive = await db.DriverIncentives.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id && i.CityId == driver.CityId, ct) ?? throw new DomainException(ErrorCodes.NotFound);
        return await BuildAsync(incentive, driver, clock.UtcNow, lang, true, ct);
    }

    /// <summary><c>POST /driver/incentives/{id}/opt-in</c>: <c>409 incentive_opt_in_closed</c> when not running, not eligible or full.</summary>
    public async Task<DriverIncentiveDetailDto> OptInAsync(Guid id, Language lang, CancellationToken ct)
    {
        var driver = await LoadDriverAsync(ct);
        var incentive = await db.DriverIncentives.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id && i.CityId == driver.CityId, ct) ?? throw new DomainException(ErrorCodes.NotFound);
        var now = clock.UtcNow;
        if (!incentive.IsRunningAt(now) || !IsEligible(incentive, driver) || incentive.PeriodAt(now, OffsetMinutes) is not { } period)
        {
            throw new DomainException(ErrorCodes.IncentiveOptInClosed);
        }

        var progress = await db.DriverIncentiveProgress.FirstOrDefaultAsync(p => p.IncentiveId == id && p.DriverId == driver.Id && p.PeriodStart == period.Start, ct);
        if (progress is null)
        {
            if (await IsFullAsync(incentive, period.Start, ct))
            {
                throw new DomainException(ErrorCodes.IncentiveOptInClosed, new { reason = "full" });
            }

            db.DriverIncentiveProgress.Add(new DriverIncentiveProgress
            {
                IncentiveId = id, DriverId = driver.Id, PeriodStart = period.Start, PeriodEnd = period.End, OptedInAt = now,
            });
        }
        else
        {
            progress.OptedInAt ??= now;
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // A concurrent opt-in created the row first.
        }

        return await BuildAsync(incentive, driver, now, lang, true, ct);
    }

    private async Task<DriverIncentiveDetailDto> BuildAsync(DriverIncentive i, DriverProfile driver, DateTime now, Language lang, bool withPolygons, CancellationToken ct)
    {
        var latest = await db.DriverIncentiveProgress.AsNoTracking().Where(p => p.IncentiveId == i.Id && p.DriverId == driver.Id)
            .OrderByDescending(p => p.PeriodStart).FirstOrDefaultAsync(ct);
        (DateTime Start, DateTime End) period = i.StartsAt > now
            ? i.PeriodAt(i.StartsAt, OffsetMinutes) ?? (i.StartsAt, i.EndsAt)
            : i.EndsAt <= now || !i.IsActive
                ? latest is null ? i.PeriodAt(i.EndsAt.AddTicks(-1), OffsetMinutes) ?? (i.StartsAt, i.EndsAt) : (latest.PeriodStart, latest.PeriodEnd)
                : i.PeriodAt(now, OffsetMinutes) ?? (i.StartsAt, i.EndsAt);
        var progress = latest is not null && latest.PeriodStart == period.Start ? latest
            : await db.DriverIncentiveProgress.AsNoTracking().FirstOrDefaultAsync(p => p.IncentiveId == i.Id && p.DriverId == driver.Id && p.PeriodStart == period.Start, ct);
        var optedIn = await db.DriverIncentiveProgress.AsNoTracking().AnyAsync(p => p.IncentiveId == i.Id && p.DriverId == driver.Id && p.OptedInAt != null, ct);

        var days = DriverIncentive.ParseDays(i.DaysOfWeek);
        var window = days is null && i.DailyFrom is null && i.DailyTo is null ? null : new IncentiveWindowDto(days, FormatTime(i.DailyFrom), FormatTime(i.DailyTo));
        var zoneIds = JsonLists.Parse<Guid>(i.ZoneIds);
        List<IncentiveZoneDto>? zoneRefs = null;
        List<IncentiveZonePolygonDto>? polygons = null;
        if (zoneIds is { Count: > 0 })
        {
            var zoneRows = await db.Zones.AsNoTracking().Where(z => zoneIds.Contains(z.Id)).ToListAsync(ct);
            zoneRefs = zoneRows.Select(z => new IncentiveZoneDto(z.Id, lang.Pick(z.NameAr, z.NameEn))).ToList();
            if (withPolygons)
            {
                polygons = zoneRows.Select(z => new IncentiveZonePolygonDto(z.Id, lang.Pick(z.NameAr, z.NameEn), JsonSerializer.Deserialize<JsonElement>(z.Polygon))).ToList();
            }
        }

        // F14: the reward the driver would get now (reliability multiplier; the paid amount is in progress.rewardAmount).
        var multiplier = reliability.IncentiveMultiplier(await reliability.GetAsync(driver.UserId, Role.Driver, ct));
        var categoryIds = JsonLists.Parse<Guid>(i.RideCategoryIds);
        var codes = categoryIds is { Count: > 0 }
            ? await db.RideCategories.AsNoTracking().Where(c => categoryIds.Contains(c.Id)).OrderBy(c => c.SortOrder).Select(c => c.Code).ToListAsync(ct)
            : null;
        return new DriverIncentiveDetailDto(i.Id, lang.Pick(i.NameAr, i.NameEn), lang.PickOptional(i.DescriptionAr, i.DescriptionEn), i.Type, i.TargetTrips, i.RewardAmount,
            period.Start, period.End, window, zoneRefs, codes, i.RequiresOptIn, optedIn,
            progress is null ? null : new DriverIncentiveProgressSummaryDto(progress.CompletedTrips, progress.Status, progress.RewardAmount, progress.PaidAt),
            withPolygons ? polygons : null, multiplier, PricingMath.Round2(i.RewardAmount * multiplier));
    }

    public static string? FormatTime(TimeOnly? time) => time?.ToString("HH:mm", System.Globalization.CultureInfo.InvariantCulture);

    private async Task<DriverProfile> LoadDriverAsync(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        return await db.Drivers.AsNoTracking().FirstOrDefaultAsync(d => d.UserId == userId, ct) ?? throw new DomainException(ErrorCodes.Forbidden);
    }
}

/// <summary>
/// Runs <c>IncentivePeriodJob</c> (every 15 minutes), <c>IncentivePayoutJob</c> (hourly) and <c>DriverTierRecalcJob</c> (weekly, <c>Tiers:RecalcDayOfWeek</c> at
/// <c>Tiers:RecalcHourLocal</c> Riyadh). Disabled with <c>Incentives:JobsEnabled=false</c>.
/// </summary>
public sealed class IncentivesBackgroundService(
    IServiceScopeFactory scopes, IDistributedLock locks, IOptions<IncentivesOptions> options, IOptions<TiersOptions> tiers, IClock clock, ILogger<IncentivesBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.JobsEnabled)
        {
            return;
        }

        var nextPeriods = DateTime.UtcNow;
        var nextPayout = DateTime.UtcNow;
        DateOnly? lastRecalc = null;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                if (DateTime.UtcNow >= nextPeriods)
                {
                    nextPeriods = DateTime.UtcNow.AddMinutes(15);
                    await RunOnceAsync<IncentiveService>("incentive_periods", (s, ct) => s.RunPeriodsAsync(ct), stoppingToken);
                }

                if (DateTime.UtcNow >= nextPayout)
                {
                    nextPayout = DateTime.UtcNow.AddHours(1);
                    await RunOnceAsync<IncentiveService>("incentive_payout", (s, ct) => s.PayoutDueAsync(ct), stoppingToken);
                }

                var local = Formats.ToRiyadh(clock.UtcNow);
                var today = DateOnly.FromDateTime(local);
                if ((int)local.DayOfWeek == tiers.Value.RecalcDayOfWeek && local.Hour >= tiers.Value.RecalcHourLocal && lastRecalc != today)
                {
                    lastRecalc = today;
                    await RunOnceAsync<TierService>("driver_tier_recalc", async (s, ct) => (await s.RecalculateAllAsync(ct)).Changed, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Incentive job failed");
            }
        }
    }

    public async Task<int> RunOnceAsync<TService>(string name, Func<TService, CancellationToken, Task<int>> run, CancellationToken ct) where TService : notnull
    {
        await using var handle = await locks.TryAcquireAsync($"lock:job:{name}", TimeSpan.FromMinutes(30), ct);
        if (handle is null) return 0;
        using var scope = scopes.CreateScope();
        return await run(scope.ServiceProvider.GetRequiredService<TService>(), ct);
    }
}
