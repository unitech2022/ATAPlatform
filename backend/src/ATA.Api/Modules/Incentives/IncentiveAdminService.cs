using System.Globalization;
using System.Text.Json;
using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Api.Modules.Promotions;
using ATA.Domain.Common;
using ATA.Domain.Incentives;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Incentives;

/// <summary>Admin console of driver incentives (<c>incentives.manage</c>); writes are audited (<c>incentive.create|update|deactivate</c>, <c>incentive_progress.void</c>).</summary>
public sealed class IncentiveAdminService(AtaDbContext db, IClock clock, ICurrentUser currentUser, AuditService audit, IncentiveService incentives)
{
    private const string EntityType = "incentive";

    public static string StatusOf(DriverIncentive i, DateTime now) =>
        !i.IsActive ? "inactive" : i.StartsAt > now ? "upcoming" : i.EndsAt <= now ? "ended" : "active";

    public async Task<PagedResult<IncentiveDto>> ListAsync(string? status, Guid? cityId, Paging paging, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var query = db.DriverIncentives.AsNoTracking().AsQueryable();
        if (cityId is { } city) query = query.Where(i => i.CityId == city);
        query = status switch
        {
            null or "" or "all" => query,
            "active" => query.Where(i => i.IsActive && i.StartsAt <= now && i.EndsAt > now),
            "upcoming" or "scheduled" => query.Where(i => i.IsActive && i.StartsAt > now),
            "ended" or "expired" or "completed" => query.Where(i => i.IsActive && i.EndsAt <= now),
            "inactive" => query.Where(i => !i.IsActive),
            _ => throw new DomainException(ErrorCodes.ValidationFailed, new Dictionary<string, string> { ["status"] = "must be active|upcoming|ended|inactive" }),
        };
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(i => i.CreatedAt).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        var items = new List<IncentiveDto>(rows.Count);
        foreach (var row in rows)
        {
            items.Add(await ToDtoAsync(row, ct));
        }

        return paging.Result(items, total);
    }

    public async Task<IncentiveDto> GetAsync(Guid id, CancellationToken ct) =>
        await ToDtoAsync(Guard.NotFound(await db.DriverIncentives.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, ct)), ct);

    public async Task<IncentiveDto> CreateAsync(IncentiveUpsertRequest request, CancellationToken ct)
    {
        var (from, to) = await ValidateAsync(request, ct);
        var incentive = new DriverIncentive { NameAr = string.Empty, NameEn = string.Empty, CreatedBy = currentUser.UserId };
        Apply(incentive, request, from, to);
        db.DriverIncentives.Add(incentive);
        await incentives.PublishAsync(incentive, ct);
        audit.Log("incentive.create", EntityType, incentive.Id, null, Snapshot(incentive));
        await db.SaveChangesAsync(ct);
        return await ToDtoAsync(incentive, ct);
    }

    public async Task<IncentiveDto> UpdateAsync(Guid id, IncentiveUpsertRequest request, CancellationToken ct)
    {
        var incentive = Guard.NotFound(await db.DriverIncentives.FirstOrDefaultAsync(i => i.Id == id, ct));
        var (from, to) = await ValidateAsync(request, ct);
        var before = Snapshot(incentive);
        Apply(incentive, request, from, to);
        await incentives.PublishAsync(incentive, ct);
        audit.Log("incentive.update", EntityType, incentive.Id, before, Snapshot(incentive));
        await db.SaveChangesAsync(ct);
        return await ToDtoAsync(incentive, ct);
    }

    public async Task<IncentiveDto> SetActiveAsync(Guid id, bool active, CancellationToken ct)
    {
        var incentive = Guard.NotFound(await db.DriverIncentives.FirstOrDefaultAsync(i => i.Id == id, ct));
        if (incentive.IsActive != active)
        {
            incentive.IsActive = active;
            if (active) await incentives.PublishAsync(incentive, ct);
            audit.Log(active ? "incentive.activate" : "incentive.deactivate", EntityType, incentive.Id, new { isActive = !active }, new { isActive = active });
            await db.SaveChangesAsync(ct);
        }

        return await ToDtoAsync(incentive, ct);
    }

    public async Task<PagedResult<IncentiveProgressDto>> ProgressAsync(Guid id, IncentiveProgressStatus? status, Paging paging, CancellationToken ct)
    {
        Guard.NotFound(await db.DriverIncentives.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, ct));
        var query = from p in db.DriverIncentiveProgress.AsNoTracking()
                    join d in db.Drivers.AsNoTracking() on p.DriverId equals d.Id
                    join u in db.Users.AsNoTracking() on d.UserId equals u.Id
                    where p.IncentiveId == id
                    select new { p, u.FullName };
        if (status is { } s) query = query.Where(x => x.p.Status == s);
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.p.PeriodStart).ThenByDescending(x => x.p.CompletedTrips).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        return paging.Result(rows.Select(x => new IncentiveProgressDto(x.p.Id, x.FullName, x.p.PeriodStart, x.p.CompletedTrips, x.p.Status, x.p.RewardAmount,
            x.p.IncentiveMultiplier, x.p.PaidAt, x.p.DriverId, x.p.PeriodEnd, x.p.AchievedAt, x.p.VoidedReason, x.p.OptedInAt)).ToList(), total);
    }

    /// <summary>Only before payout (<c>in_progress</c> / <c>achieved</c>), otherwise <c>409 conflict</c>.</summary>
    public async Task<IncentiveProgressDto> VoidAsync(Guid progressId, VoidProgressRequest request, CancellationToken ct)
    {
        new Validator().Require(nameof(request.Reason), request?.Reason, 500).ThrowIfInvalid();
        var progress = Guard.NotFound(await db.DriverIncentiveProgress.FirstOrDefaultAsync(p => p.Id == progressId, ct));
        if (!progress.IsOpen)
        {
            throw new DomainException(ErrorCodes.Conflict, new { status = progress.Status });
        }

        var before = new { progress.Status };
        progress.Status = IncentiveProgressStatus.Voided;
        progress.VoidedReason = request!.Reason!.Trim();
        audit.Log("incentive_progress.void", "incentive_progress", progress.Id, before, new { progress.Status, reason = progress.VoidedReason, progress.IncentiveId, progress.DriverId });
        await db.SaveChangesAsync(ct);
        var name = await (from d in db.Drivers.AsNoTracking() join u in db.Users.AsNoTracking() on d.UserId equals u.Id where d.Id == progress.DriverId select u.FullName).FirstOrDefaultAsync(ct);
        return new IncentiveProgressDto(progress.Id, name, progress.PeriodStart, progress.CompletedTrips, progress.Status, progress.RewardAmount, progress.IncentiveMultiplier,
            progress.PaidAt, progress.DriverId, progress.PeriodEnd, progress.AchievedAt, progress.VoidedReason, progress.OptedInAt);
    }

    /// <summary><c>GET /admin/drivers/{id}/incentives</c> (not in doc 10): the driver's progress rows, newest first.</summary>
    public async Task<IReadOnlyList<DriverIncentiveProgressAdminDto>> ForDriverAsync(Guid driverId, Language lang, CancellationToken ct)
    {
        Guard.NotFound(await db.Drivers.AsNoTracking().FirstOrDefaultAsync(d => d.Id == driverId, ct));
        var rows = await (from p in db.DriverIncentiveProgress.AsNoTracking()
                          join i in db.DriverIncentives.AsNoTracking() on p.IncentiveId equals i.Id
                          where p.DriverId == driverId
                          orderby p.PeriodStart descending
                          select new { p, i.NameAr, i.NameEn, i.Type, i.TargetTrips }).Take(100).ToListAsync(ct);
        return rows.Select(x =>
        {
            var name = lang.Pick(x.NameAr, x.NameEn);
            return new DriverIncentiveProgressAdminDto(x.p.Id, x.p.IncentiveId, name, name, x.Type, x.TargetTrips, x.p.CompletedTrips, x.p.PeriodStart, x.p.PeriodEnd,
                x.p.Status, x.p.RewardAmount, x.p.IncentiveMultiplier, x.p.PaidAt);
        }).ToList();
    }

    private async Task<(TimeOnly? From, TimeOnly? To)> ValidateAsync(IncentiveUpsertRequest r, CancellationToken ct)
    {
        TimeOnly? from = null, to = null;
        var fromValid = r.DailyFrom is null || TryTime(r.DailyFrom, out from);
        var toValid = r.DailyTo is null || TryTime(r.DailyTo, out to);
        var v = new Validator()
            .Require(nameof(r.NameAr), r.NameAr, 120)
            .Require(nameof(r.NameEn), r.NameEn, 120)
            .Rule(nameof(r.DescriptionAr), r.DescriptionAr is null || r.DescriptionAr.Length <= 500, "max_length:500")
            .Rule(nameof(r.DescriptionEn), r.DescriptionEn is null || r.DescriptionEn.Length <= 500, "max_length:500")
            .Require(nameof(r.Type), r.Type)
            .Require(nameof(r.CityId), r.CityId)
            .Require(nameof(r.TargetTrips), r.TargetTrips)
            .Rule(nameof(r.TargetTrips), r.TargetTrips is null or > 0, "must be positive")
            .Require(nameof(r.RewardAmount), r.RewardAmount)
            .Rule(nameof(r.RewardAmount), r.RewardAmount is null or > 0, "must be positive")
            .Rule(nameof(r.MinTripFare), r.MinTripFare is null or >= 0, "must be positive")
            .Require(nameof(r.StartsAt), r.StartsAt)
            .Require(nameof(r.EndsAt), r.EndsAt)
            .Rule(nameof(r.EndsAt), r.StartsAt is null || r.EndsAt is null || r.EndsAt > r.StartsAt, "must be after startsAt")
            .Rule(nameof(r.DaysOfWeek), r.DaysOfWeek is null || r.DaysOfWeek.All(d => d is >= 0 and <= 6), "values must be 0-6")
            .Rule(nameof(r.DailyFrom), fromValid, "must be HH:mm")
            .Rule(nameof(r.DailyTo), toValid, "must be HH:mm")
            .Rule(nameof(r.DailyTo), (r.DailyFrom is null) == (r.DailyTo is null), "dailyFrom and dailyTo go together")
            .Rule(nameof(r.MinRating), r.MinRating is null or (>= 0 and <= 5), "must be between 0 and 5")
            .Rule(nameof(r.MaxParticipants), r.MaxParticipants is null or > 0, "must be positive")
            .Rule(nameof(r.BudgetAmount), r.BudgetAmount is null or > 0, "must be positive");
        v.ThrowIfInvalid();
        v.Rule(nameof(r.CityId), await db.Cities.AnyAsync(c => c.Id == r.CityId, ct), "unknown city");
        if (r.ZoneIds is { Count: > 0 } zones)
        {
            v.Rule(nameof(r.ZoneIds), await db.Zones.CountAsync(z => zones.Contains(z.Id), ct) == zones.Distinct().Count(), "unknown zone");
        }

        if (r.RideCategoryIds is { Count: > 0 } categories)
        {
            v.Rule(nameof(r.RideCategoryIds), await db.RideCategories.CountAsync(c => categories.Contains(c.Id), ct) == categories.Distinct().Count(), "unknown ride category");
        }

        v.ThrowIfInvalid();
        return (from, to);
    }

    private static bool TryTime(string value, out TimeOnly? time)
    {
        time = null;
        if (TimeOnly.TryParseExact(value.Trim(), ["HH:mm", "HH:mm:ss"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            time = parsed;
            return true;
        }

        return false;
    }

    private static void Apply(DriverIncentive i, IncentiveUpsertRequest r, TimeOnly? from, TimeOnly? to)
    {
        i.NameAr = r.NameAr!.Trim();
        i.NameEn = r.NameEn!.Trim();
        i.DescriptionAr = string.IsNullOrWhiteSpace(r.DescriptionAr) ? null : r.DescriptionAr.Trim();
        i.DescriptionEn = string.IsNullOrWhiteSpace(r.DescriptionEn) ? null : r.DescriptionEn.Trim();
        i.Type = r.Type!.Value;
        i.CityId = r.CityId!.Value;
        i.ZoneIds = JsonLists.Serialize(r.ZoneIds);
        i.RideCategoryIds = JsonLists.Serialize(r.RideCategoryIds);
        i.TargetTrips = r.TargetTrips!.Value;
        i.RewardAmount = r.RewardAmount!.Value;
        i.MinTripFare = r.MinTripFare;
        i.StartsAt = r.StartsAt!.Value.ToUniversalTime();
        i.EndsAt = r.EndsAt!.Value.ToUniversalTime();
        i.DaysOfWeek = r.DaysOfWeek is { Count: > 0 } days ? JsonSerializer.Serialize(days.Distinct().Order().ToList()) : null;
        i.DailyFrom = from;
        i.DailyTo = to;
        i.MinTier = r.MinTier;
        i.MinRating = r.MinRating;
        i.RequiresOptIn = r.RequiresOptIn ?? false;
        i.MaxParticipants = r.MaxParticipants;
        i.BudgetAmount = r.BudgetAmount;
        i.NotifyOnPublish = r.NotifyOnPublish ?? false;
        i.IsActive = r.IsActive ?? true;
    }

    private static object Snapshot(DriverIncentive i) => new
    {
        i.NameAr, i.NameEn, i.Type, i.CityId, i.ZoneIds, i.RideCategoryIds, i.TargetTrips, i.RewardAmount, i.MinTripFare, i.StartsAt, i.EndsAt, i.DaysOfWeek,
        dailyFrom = IncentiveService.FormatTime(i.DailyFrom), dailyTo = IncentiveService.FormatTime(i.DailyTo), i.MinTier, i.MinRating, i.RequiresOptIn, i.MaxParticipants,
        i.BudgetAmount, i.NotifyOnPublish, i.IsActive,
    };

    private async Task<IncentiveDto> ToDtoAsync(DriverIncentive i, CancellationToken ct)
    {
        var counts = await db.DriverIncentiveProgress.AsNoTracking().Where(p => p.IncentiveId == i.Id)
            .GroupBy(p => p.Status).Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync(ct);
        var participants = await db.DriverIncentiveProgress.AsNoTracking().Where(p => p.IncentiveId == i.Id).Select(p => p.DriverId).Distinct().CountAsync(ct);
        return new IncentiveDto(i.Id, i.NameAr, i.NameEn, i.DescriptionAr, i.DescriptionEn, i.Type, i.CityId, JsonLists.Parse<Guid>(i.ZoneIds), JsonLists.Parse<Guid>(i.RideCategoryIds),
            i.TargetTrips, i.RewardAmount, i.MinTripFare, i.StartsAt, i.EndsAt, DriverIncentive.ParseDays(i.DaysOfWeek), IncentiveService.FormatTime(i.DailyFrom),
            IncentiveService.FormatTime(i.DailyTo), i.MinTier, i.MinRating, i.RequiresOptIn, i.MaxParticipants, i.BudgetAmount, i.SpentAmount, i.NotifyOnPublish, i.IsActive,
            StatusOf(i, clock.UtcNow), i.PublishedAt, i.CreatedBy, i.CreatedAt, i.UpdatedAt, participants,
            counts.Where(c => c.Status is IncentiveProgressStatus.Achieved or IncentiveProgressStatus.Paid).Sum(c => c.Count),
            counts.FirstOrDefault(c => c.Status == IncentiveProgressStatus.Paid)?.Count ?? 0);
    }
}
