using System.Globalization;
using System.Text.Json;
using ATA.Api.Common;
using ATA.Api.Modules.Notifications;
using ATA.Api.Modules.Trips;
using ATA.Domain.Common;
using ATA.Domain.Notifications;
using ATA.Domain.Ratings;
using ATA.Domain.Trips;
using ATA.Infrastructure.Locking;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Ratings;

/// <summary>
/// Two-way trip ratings (doc 10 §F15.2): one rating per party within <c>Ratings:WindowHours</c> of completion, tags of the rated role, the rolling weighted
/// average updated in the same transaction, flags for ops (low rating, low driver average, abusive comment) and the privacy-preserving summary.
/// </summary>
public sealed class RatingService(
    AtaDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    INotificationDispatcher notifications,
    IOptions<RatingsOptions> options)
{
    private const int RecentComments = 20;
    private const int TopTagsCount = 5;
    private readonly RatingsOptions _options = options.Value;

    public static RatingRole RoleOf(TripViewer viewer) => viewer == TripViewer.Driver ? RatingRole.Driver : RatingRole.Passenger;

    public async Task<IReadOnlyList<RatingTagDto>> TagsAsync(RatingRole? target, Language lang, CancellationToken ct)
    {
        var query = db.RatingTags.AsNoTracking().Where(t => t.IsActive);
        if (target is { } role) query = query.Where(t => t.TargetRole == role);
        var rows = await query.OrderBy(t => t.TargetRole).ThenBy(t => t.SortOrder).ToListAsync(ct);
        return rows.Select(t => new RatingTagDto(t.Code, lang.Pick(t.NameAr, t.NameEn))).ToList();
    }

    /// <summary>
    /// <c>POST /passenger|driver/trips/{id}/rating</c>: not a party → 403, not completed → 409, after the window → <c>422 rating_window_closed</c>,
    /// twice → <c>409 rating_exists</c>; unknown tag → <c>422 { tags: "invalid" }</c>.
    /// </summary>
    public async Task<RatingDto> SubmitAsync(Guid tripId, RatingRole raterRole, SubmitRatingRequest request, CancellationToken ct)
    {
        var tags = (request.Tags ?? []).Select(t => t?.Trim() ?? string.Empty).Distinct(StringComparer.Ordinal).ToList();
        var comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();
        new Validator()
            .Require(nameof(request.Stars), request.Stars)
            .Rule(nameof(request.Stars), request.Stars is null or (>= 1 and <= 5), "must be between 1 and 5")
            .Rule(nameof(request.Comment), comment is null || comment.Length <= 500, "max_length:500")
            .Rule(nameof(request.Tags), tags.Count <= 10, "at most 10")
            .ThrowIfInvalid();

        var userId = currentUser.UserId;
        var trip = await db.Trips.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tripId, ct) ?? throw new DomainException(ErrorCodes.NotFound);
        var (passengerUserId, driverUserId) = await PartiesAsync(trip, ct);
        var isParty = raterRole == RatingRole.Passenger ? passengerUserId == userId : driverUserId == userId;
        if (!isParty)
        {
            throw new DomainException(ErrorCodes.Forbidden);
        }

        if (trip.Status != TripStatus.Completed || trip.CompletedAt is not { } completedAt || driverUserId is not { } driverUser)
        {
            throw new DomainException(ErrorCodes.Conflict, new { status = trip.Status });
        }

        var now = clock.UtcNow;
        var rateUntil = completedAt.AddHours(_options.WindowHours);
        if (now > rateUntil)
        {
            throw new DomainException(ErrorCodes.RatingWindowClosed, new { rateUntil });
        }

        if (await db.Ratings.AnyAsync(r => r.TripId == tripId && r.RaterRole == raterRole, ct))
        {
            throw new DomainException(ErrorCodes.RatingExists);
        }

        var rateeRole = raterRole == RatingRole.Passenger ? RatingRole.Driver : RatingRole.Passenger;
        if (tags.Count > 0)
        {
            var known = await db.RatingTags.AsNoTracking().Where(t => t.TargetRole == rateeRole && t.IsActive && tags.Contains(t.Code)).CountAsync(ct);
            new Validator().Rule(nameof(request.Tags), known == tags.Count, "invalid").ThrowIfInvalid();
        }

        var abusive = comment is not null && _options.AbusiveWords.Any(w => !string.IsNullOrWhiteSpace(w) && comment.Contains(w.Trim(), StringComparison.OrdinalIgnoreCase));
        var rating = new Rating
        {
            TripId = tripId,
            RaterUserId = userId,
            RaterRole = raterRole,
            RateeUserId = rateeRole == RatingRole.Driver ? driverUser : passengerUserId,
            RateeRole = rateeRole,
            Stars = (byte)request.Stars!.Value,
            Tags = JsonSerializer.Serialize(tags),
            Comment = comment,
            CommentHidden = abusive,
        };

        await db.InTransactionAsync(async () =>
        {
            db.Ratings.Add(rating);
            if (rating.Stars <= _options.LowRatingThreshold)
            {
                db.RatingFlags.Add(new RatingFlag { UserId = rating.RateeUserId, Role = rateeRole, Type = RatingFlagType.LowRating, RatingId = rating.Id, Value = rating.Stars });
            }

            if (abusive)
            {
                db.RatingFlags.Add(new RatingFlag { UserId = rating.RaterUserId, Role = raterRole, Type = RatingFlagType.AbusiveComment, RatingId = rating.Id });
            }

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                throw new DomainException(ErrorCodes.RatingExists);
            }

            await RecomputeAsync(rating.RateeUserId, rateeRole, ct);
            await db.SaveChangesAsync(ct);
        }, ct);

        return ToDto(rating);
    }

    /// <summary>
    /// Recomputes <c>rating_avg</c> (weighted, last <c>WindowSize</c> visible ratings, newest first) and <c>rating_count</c> (all visible ratings) of the
    /// ratee's profile, and raises a <c>low_average</c> flag for a driver under <c>DriverMinAverage</c> with enough ratings (one open flag per driver).
    /// The caller saves.
    /// </summary>
    public async Task RecomputeAsync(Guid rateeUserId, RatingRole role, CancellationToken ct)
    {
        var visible = db.Ratings.AsNoTracking().Where(r => r.RateeUserId == rateeUserId && r.RateeRole == role && r.Status == RatingStatus.Visible);
        var count = await visible.CountAsync(ct);
        var stars = await visible.OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id).Take(Math.Max(1, _options.WindowSize)).Select(r => r.Stars).ToListAsync(ct);
        var average = RatingMath.WeightedAverage(stars, _options.MinWeight);
        if (role == RatingRole.Driver)
        {
            var driver = await db.Drivers.FirstOrDefaultAsync(d => d.UserId == rateeUserId, ct);
            if (driver is null) return;
            driver.RatingAvg = average;
            driver.RatingCount = count;
            await FlagLowAverageAsync(rateeUserId, average, count, ct);
        }
        else
        {
            var passenger = await db.Passengers.FirstOrDefaultAsync(p => p.UserId == rateeUserId, ct);
            if (passenger is null) return;
            passenger.RatingAvg = average;
            passenger.RatingCount = count;
        }
    }

    private async Task<bool> FlagLowAverageAsync(Guid driverUserId, decimal average, int count, CancellationToken ct)
    {
        if (count < _options.MinCountForAverageFlag || average >= _options.DriverMinAverage)
        {
            return false;
        }

        var open = db.RatingFlags.Local.Any(f => f.UserId == driverUserId && f.Type == RatingFlagType.LowAverage && f.Status == RatingFlagStatus.Open)
                   || await db.RatingFlags.AnyAsync(f => f.UserId == driverUserId && f.Type == RatingFlagType.LowAverage && f.Status == RatingFlagStatus.Open, ct);
        if (open)
        {
            return false;
        }

        db.RatingFlags.Add(new RatingFlag { UserId = driverUserId, Role = RatingRole.Driver, Type = RatingFlagType.LowAverage, Value = average });
        return true;
    }

    /// <summary>Completed trips of the caller in <paramref name="role"/> that are still in the window and not rated by them.</summary>
    public async Task<IReadOnlyList<PendingRatingDto>> PendingAsync(RatingRole role, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var now = clock.UtcNow;
        var since = now.AddHours(-_options.WindowHours);
        IQueryable<Trip> trips;
        if (role == RatingRole.Passenger)
        {
            var passengerId = await db.Passengers.AsNoTracking().Where(p => p.UserId == userId).Select(p => (Guid?)p.Id).FirstOrDefaultAsync(ct) ?? throw new DomainException(ErrorCodes.Forbidden);
            trips = db.Trips.AsNoTracking().Where(t => t.PassengerId == passengerId);
        }
        else
        {
            var driverId = await db.Drivers.AsNoTracking().Where(d => d.UserId == userId).Select(d => (Guid?)d.Id).FirstOrDefaultAsync(ct) ?? throw new DomainException(ErrorCodes.Forbidden);
            trips = db.Trips.AsNoTracking().Where(t => t.DriverId == driverId);
        }

        var rows = await trips
            .Where(t => t.Status == TripStatus.Completed && t.CompletedAt >= since && !db.Ratings.Any(r => r.TripId == t.Id && r.RaterRole == role))
            .OrderByDescending(t => t.CompletedAt)
            .Select(t => new { t.Id, t.TripNumber, t.CompletedAt, t.PassengerId, t.DriverId })
            .ToListAsync(ct);
        var result = new List<PendingRatingDto>(rows.Count);
        foreach (var row in rows)
        {
            var counterpart = role == RatingRole.Passenger
                ? await (from d in db.Drivers.AsNoTracking() join u in db.Users.AsNoTracking() on d.UserId equals u.Id where d.Id == row.DriverId select u.FullName).FirstOrDefaultAsync(ct)
                : await (from p in db.Passengers.AsNoTracking() join u in db.Users.AsNoTracking() on p.UserId equals u.Id where p.Id == row.PassengerId select u.FullName).FirstOrDefaultAsync(ct);
            result.Add(new PendingRatingDto(row.Id, row.TripNumber, FirstName(counterpart), row.CompletedAt!.Value, row.CompletedAt.Value.AddHours(_options.WindowHours)));
        }

        return result;
    }

    /// <summary>
    /// What the rated user may see: average, distribution, most frequent tags and the last comments — without names, trip numbers or exact dates (ISO week only).
    /// </summary>
    public async Task<RatingSummaryDto> SummaryAsync(RatingRole role, Language lang, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        decimal avg;
        int count;
        if (role == RatingRole.Driver)
        {
            var driver = await db.Drivers.AsNoTracking().FirstOrDefaultAsync(d => d.UserId == userId, ct) ?? throw new DomainException(ErrorCodes.Forbidden);
            (avg, count) = (driver.RatingAvg, driver.RatingCount);
        }
        else
        {
            var passenger = await db.Passengers.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, ct) ?? throw new DomainException(ErrorCodes.Forbidden);
            (avg, count) = (passenger.RatingAvg, passenger.RatingCount);
        }

        var visible = db.Ratings.AsNoTracking().Where(r => r.RateeUserId == userId && r.RateeRole == role && r.Status == RatingStatus.Visible);
        var byStars = await visible.GroupBy(r => r.Stars).Select(g => new { Stars = g.Key, Count = g.Count() }).ToListAsync(ct);
        var distribution = Enumerable.Range(1, 5).ToDictionary(s => s.ToString(CultureInfo.InvariantCulture), s => byStars.FirstOrDefault(b => b.Stars == s)?.Count ?? 0);

        var recent = await visible.OrderByDescending(r => r.CreatedAt).Take(Math.Max(1, _options.WindowSize)).Select(r => new { r.Stars, r.Tags }).ToListAsync(ct);
        var tagNames = await db.RatingTags.AsNoTracking().Where(t => t.TargetRole == role).ToDictionaryAsync(t => t.Code, t => lang.Pick(t.NameAr, t.NameEn), ct);
        var topTags = recent
            .SelectMany(r => ParseTags(r.Tags).Select(code => (code, positive: r.Stars >= 4)))
            .GroupBy(x => x)
            .Select(g => new RatingTagCountDto(g.Key.code, tagNames.GetValueOrDefault(g.Key.code, g.Key.code), g.Count(), g.Key.positive))
            .OrderByDescending(t => t.Count).ThenBy(t => t.Code, StringComparer.Ordinal)
            .Take(TopTagsCount).ToList();

        var comments = await visible.Where(r => r.Comment != null && !r.CommentHidden).OrderByDescending(r => r.CreatedAt).Take(RecentComments)
            .Select(r => new { r.Stars, r.Comment, r.CreatedAt }).ToListAsync(ct);
        return new RatingSummaryDto(avg, count, distribution, topTags,
            comments.Select(c => new RatingCommentDto(c.Stars, c.Comment!, IsoWeek(c.CreatedAt))).ToList());
    }

    /// <summary><c>rating.reminder</c> once per completed trip, <c>ReminderAfterMinutes</c> after completion, to each party that has not rated yet.</summary>
    public async Task<int> SendRemindersAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;
        var dueBefore = now.AddMinutes(-_options.ReminderAfterMinutes);
        var windowStart = now.AddHours(-_options.WindowHours);
        var trips = await db.Trips.Where(t => t.Status == TripStatus.Completed && t.RatingRemindedAt == null && t.CompletedAt <= dueBefore && t.CompletedAt >= windowStart)
            .OrderBy(t => t.CompletedAt).Take(200).ToListAsync(ct);
        var sent = 0;
        foreach (var trip in trips)
        {
            var rated = await db.Ratings.AsNoTracking().Where(r => r.TripId == trip.Id).Select(r => r.RaterRole).ToListAsync(ct);
            var (passengerUserId, driverUserId) = await PartiesAsync(trip, ct);
            var passengerName = await db.Users.AsNoTracking().Where(u => u.Id == passengerUserId).Select(u => u.FullName).FirstOrDefaultAsync(ct);
            var driverName = driverUserId is { } du ? await db.Users.AsNoTracking().Where(u => u.Id == du).Select(u => u.FullName).FirstOrDefaultAsync(ct) : null;
            var placeholders = NotificationPlaceholders.Of(("driverName", FirstName(driverName) ?? string.Empty), ("passengerName", FirstName(passengerName) ?? string.Empty));
            var data = new Dictionary<string, object?> { ["tripId"] = trip.Id, ["tripNumber"] = trip.TripNumber };
            if (!rated.Contains(RatingRole.Passenger))
            {
                await notifications.DispatchAsync(new NotificationRequest(NotificationTypes.RatingReminder, passengerUserId, placeholders, "trip", trip.Id, data), ct);
                sent++;
            }

            if (!rated.Contains(RatingRole.Driver) && driverUserId is { } driverUser)
            {
                await notifications.DispatchAsync(new NotificationRequest(NotificationTypes.RatingReminder, driverUser, placeholders, "trip", trip.Id, data), ct);
                sent++;
            }

            trip.RatingRemindedAt = now;
        }

        await db.SaveChangesAsync(ct);
        return sent;
    }

    /// <summary><c>LowAverageFlagJob</c>: drivers under <c>DriverMinAverage</c> with at least <c>MinCountForAverageFlag</c> ratings and no open flag.</summary>
    public async Task<int> FlagLowAveragesAsync(CancellationToken ct)
    {
        var min = _options.DriverMinAverage;
        var drivers = await db.Drivers.AsNoTracking().Where(d => d.RatingCount >= _options.MinCountForAverageFlag && d.RatingAvg < min)
            .Select(d => new { d.UserId, d.RatingAvg, d.RatingCount }).ToListAsync(ct);
        var created = 0;
        foreach (var d in drivers)
        {
            if (await FlagLowAverageAsync(d.UserId, d.RatingAvg, d.RatingCount, ct)) created++;
        }

        await db.SaveChangesAsync(ct);
        return created;
    }

    public static RatingDto ToDto(Rating r) => new(r.Id, r.TripId, r.Stars, ParseTags(r.Tags), r.Comment, r.CreatedAt);

    public static IReadOnlyList<string> ParseTags(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string IsoWeek(DateTime utc)
    {
        var local = Formats.ToRiyadh(utc);
        return $"{ISOWeek.GetYear(local)}-W{ISOWeek.GetWeekOfYear(local):00}";
    }

    public static string? FirstName(string? fullName) => fullName?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

    private async Task<(Guid PassengerUserId, Guid? DriverUserId)> PartiesAsync(Trip trip, CancellationToken ct)
    {
        var passengerUserId = await db.Passengers.AsNoTracking().Where(p => p.Id == trip.PassengerId).Select(p => p.UserId).FirstAsync(ct);
        Guid? driverUserId = trip.DriverId is { } driverId
            ? await db.Drivers.AsNoTracking().Where(d => d.Id == driverId).Select(d => (Guid?)d.UserId).FirstOrDefaultAsync(ct)
            : null;
        return (passengerUserId, driverUserId);
    }
}

/// <summary>Runs <c>RatingReminderJob</c> (every 5 minutes) and <c>LowAverageFlagJob</c> (daily at <c>Ratings:LowAverageHourLocal</c> Riyadh).</summary>
public sealed class RatingsBackgroundService(IServiceScopeFactory scopes, IDistributedLock locks, IOptions<RatingsOptions> options, IClock clock, ILogger<RatingsBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.JobsEnabled)
        {
            return;
        }

        DateOnly? lastFlagRun = null;
        var nextReminder = DateTime.UtcNow;
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                if (DateTime.UtcNow >= nextReminder)
                {
                    nextReminder = DateTime.UtcNow.AddMinutes(5);
                    await RunOnceAsync("rating_reminder", (s, ct) => s.SendRemindersAsync(ct), stoppingToken);
                }

                var local = Formats.ToRiyadh(clock.UtcNow);
                var today = DateOnly.FromDateTime(local);
                if (local.Hour >= options.Value.LowAverageHourLocal && lastFlagRun != today)
                {
                    lastFlagRun = today;
                    await RunOnceAsync("low_average_flag", (s, ct) => s.FlagLowAveragesAsync(ct), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ratings job failed");
            }
        }
    }

    public async Task<int> RunOnceAsync(string name, Func<RatingService, CancellationToken, Task<int>> run, CancellationToken ct)
    {
        await using var handle = await locks.TryAcquireAsync($"lock:job:{name}", TimeSpan.FromMinutes(30), ct);
        if (handle is null) return 0;
        using var scope = scopes.CreateScope();
        return await run(scope.ServiceProvider.GetRequiredService<RatingService>(), ct);
    }
}
