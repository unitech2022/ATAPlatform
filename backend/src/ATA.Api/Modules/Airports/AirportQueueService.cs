using System.Collections.Concurrent;
using ATA.Api.Common;
using ATA.Api.Modules.Trips;
using ATA.Api.Modules.Trips.Matching;
using ATA.Api.Modules.Trips.Realtime;
using ATA.Domain.Airports;
using ATA.Domain.Common;
using ATA.Domain.Drivers;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Airports;

/// <summary>What was last pushed to each queued driver, so <c>AirportQueueUpdated</c> is only sent when the position, size or estimate changed.</summary>
public sealed class AirportQueueBroadcastState
{
    private readonly ConcurrentDictionary<Guid, AirportQueueUpdatedEvent> _last = new();

    public bool Changed(Guid entryId, AirportQueueUpdatedEvent current) => !_last.TryGetValue(entryId, out var previous) || previous != current;

    public void Remember(Guid entryId, AirportQueueUpdatedEvent current) => _last[entryId] = current;

    public void Forget(Guid entryId) => _last.TryRemove(entryId, out _);
}

/// <summary>A queued driver who can take a trip now: the entry (FIFO) and the F9 candidate for the offer.</summary>
public sealed record QueueOffer(AirportQueueEntry Entry, DriverCandidate Candidate);

/// <summary>
/// The airport driver queue (doc 11 §F17.7): automatic entry from the location update inside a <c>driver_waiting_area</c>, manual join / leave, FIFO position and
/// estimate, the matching hooks (offers in <c>entered_at</c> order, reject → back / removed, dispatch) and the job that drops stale entries.
/// </summary>
public sealed class AirportQueueService(
    AtaDbContext db,
    AirportCatalog catalog,
    ICurrentUser currentUser,
    IClock clock,
    IMatcher matcher,
    ITripNotifier notifier,
    AirportQueueBroadcastState broadcasts,
    IOptions<AirportOptions> options)
{
    private static readonly TimeSpan EstimateWindow = TimeSpan.FromHours(2);
    private readonly AirportOptions _options = options.Value;

    // ----- driver API -----

    public async Task<AirportQueueStatusDto> StatusAsync(Language lang, CancellationToken ct)
    {
        var driver = await LoadDriverAsync(ct);
        return await StatusOfAsync(driver, lang, ct);
    }

    public async Task<AirportQueueStatusDto> JoinAsync(AirportQueueJoinRequest request, Language lang, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.Lat), request.Lat).Require(nameof(request.Lng), request.Lng)
            .Rule(nameof(request.Lat), request.Lat is null or (>= -90 and <= 90), "out of range")
            .Rule(nameof(request.Lng), request.Lng is null or (>= -180 and <= 180), "out of range")
            .ThrowIfInvalid();
        var driver = await LoadDriverAsync(ct);
        if (driver.ApplicationStatus != ApplicationStatus.Approved)
        {
            throw new DomainException(ErrorCodes.DriverNotApproved, new { status = driver.ApplicationStatus });
        }

        var found = await catalog.WaitingAreaAtAsync(request.Lat!.Value, request.Lng!.Value, ct)
                    ?? throw new DomainException(ErrorCodes.NotInAirportWaitingArea);
        if (!driver.IsOnline || driver.CurrentTripId is not null)
        {
            throw new DomainException(ErrorCodes.Conflict, new { isOnline = driver.IsOnline, currentTripId = driver.CurrentTripId });
        }

        var entry = await ActiveEntryAsync(driver.Id, ct);
        if (entry is null)
        {
            entry = await EnterAsync(driver, found.Airport, ct);
            await db.SaveChangesAsync(ct);
            await PublishAsync(entry, ct);
        }
        else if (entry.AirportId == found.Airport.Id)
        {
            entry.LastSeenAt = clock.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        return await StatusOfAsync(driver, lang, ct);
    }

    public async Task LeaveAsync(CancellationToken ct)
    {
        var driver = await LoadDriverAsync(ct);
        var entry = await ActiveEntryAsync(driver.Id, ct);
        if (entry is not null)
        {
            entry.Status = AirportQueueStatus.Left;
            entry.LeftAt = clock.UtcNow;
            entry.LeftReason = null;
            entry.OfferedTripId = null;
            broadcasts.Forget(entry.Id);
            await db.SaveChangesAsync(ct);
        }
    }

    // ----- location hook -----

    /// <summary>
    /// Called by the driver location update (before its <c>SaveChanges</c>): refreshes <c>last_seen_at</c> of an active entry while the driver is online inside the waiting
    /// area, or enters a free online driver into the queue. Returns the entry that was just created (published by the caller after saving).
    /// </summary>
    public async Task<AirportQueueEntry?> TrackLocationAsync(DriverProfile driver, decimal lat, decimal lng, CancellationToken ct)
    {
        if (!driver.IsOnline)
        {
            return null;
        }

        var found = await catalog.WaitingAreaAtAsync(lat, lng, ct);
        if (found is null)
        {
            return null;
        }

        var entry = await ActiveEntryAsync(driver.Id, ct);
        if (entry is not null)
        {
            if (entry.AirportId == found.Value.Airport.Id)
            {
                entry.LastSeenAt = clock.UtcNow;
            }

            return null;
        }

        if (driver.CurrentTripId is not null || driver.ApplicationStatus != ApplicationStatus.Approved)
        {
            return null;
        }

        return await EnterAsync(driver, found.Value.Airport, ct);
    }

    public async Task PublishAsync(AirportQueueEntry entry, CancellationToken ct)
    {
        var (position, total, estimate) = await PositionOfAsync(entry, ct);
        var current = new AirportQueueUpdatedEvent(position, total, estimate);
        var userId = await db.Drivers.AsNoTracking().Where(d => d.Id == entry.DriverId).Select(d => d.UserId).FirstAsync(ct);
        broadcasts.Remember(entry.Id, current);
        await notifier.AirportQueueUpdatedAsync(userId, current, ct);
    }

    // ----- matching hooks -----

    /// <summary>
    /// The first queued driver (FIFO by <c>entered_at</c>) who is eligible for the trip (F9 eligibility for its category, within <paramref name="radiusMeters"/> of the pickup)
    /// and was not already offered it; no score is applied.
    /// </summary>
    public async Task<QueueOffer?> NextForTripAsync(Trip trip, int radiusMeters, CancellationToken ct)
    {
        if (trip.AirportId is not { } airportId)
        {
            return null;
        }

        var entries = await db.AirportQueueEntries.Where(e => e.AirportId == airportId && e.Status == AirportQueueStatus.Waiting)
            .OrderBy(e => e.EnteredAt).ThenBy(e => e.Id).ToListAsync(ct);
        foreach (var entry in entries)
        {
            var criteria = new MatchCriteria(trip.PickupLat, trip.PickupLng, trip.RideCategoryId, trip.PreferFemaleDriver, [], radiusMeters, trip.PassengerId, trip.Id, [entry.DriverId]);
            var candidate = (await matcher.FindCandidatesAsync(criteria, ct)).FirstOrDefault();
            if (candidate is not null)
            {
                return new QueueOffer(entry, candidate);
            }
        }

        return null;
    }

    public void MarkOffered(AirportQueueEntry entry, Guid tripId)
    {
        entry.Status = AirportQueueStatus.Offered;
        entry.OfferedTripId = tripId;
    }

    /// <summary>The queued driver rejected or let the offer expire: back of the queue (<c>entered_at = now</c>) or out of it, per <c>Airport:RejectAction</c>.</summary>
    public async Task OfferEndedAsync(Guid tripId, Guid driverId, CancellationToken ct)
    {
        var entry = await db.AirportQueueEntries.FirstOrDefaultAsync(e => e.DriverId == driverId && e.OfferedTripId == tripId && e.Status == AirportQueueStatus.Offered, ct);
        if (entry is null)
        {
            return;
        }

        var now = clock.UtcNow;
        entry.OfferedTripId = null;
        if (_options.RemovesOnReject)
        {
            entry.Status = AirportQueueStatus.Removed;
            entry.LeftAt = now;
            entry.LeftReason = AirportQueueLeftReason.RejectedOffer;
            broadcasts.Forget(entry.Id);
        }
        else
        {
            entry.Status = AirportQueueStatus.Waiting;
            entry.EnteredAt = now;
        }
    }

    /// <summary>The queued driver accepted the trip: <c>dispatched</c> (leaves the queue).</summary>
    public async Task DispatchedAsync(Guid driverId, Guid tripId, CancellationToken ct)
    {
        var entry = await db.AirportQueueEntries.FirstOrDefaultAsync(e => e.DriverId == driverId && e.OfferedTripId == tripId && e.Status == AirportQueueStatus.Offered, ct);
        if (entry is null)
        {
            return;
        }

        entry.Status = AirportQueueStatus.Dispatched;
        entry.LeftAt = clock.UtcNow;
        entry.LeftReason = AirportQueueLeftReason.TripAssigned;
        broadcasts.Forget(entry.Id);
    }

    // ----- job -----

    /// <summary>
    /// <c>AirportQueueJob</c>: entries not seen for <c>Airport:QueueExitGraceSeconds</c> leave (<c>offline</c> when the driver is offline, else <c>exited_area</c>), waiting
    /// drivers who took a trip leave (<c>trip_assigned</c>), and the drivers whose position changed get <c>AirportQueueUpdated</c>. Returns the number of changes.
    /// </summary>
    public async Task<int> RunJobAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;
        var cutoff = now.AddSeconds(-_options.QueueExitGraceSeconds);
        var active = await db.AirportQueueEntries.Where(e => e.Status == AirportQueueStatus.Waiting || e.Status == AirportQueueStatus.Offered).ToListAsync(ct);
        var activeDriverIds = active.Select(e => e.DriverId).Distinct().ToList();
        var drivers = await db.Drivers.AsNoTracking().Where(d => activeDriverIds.Contains(d.Id))
            .Select(d => new { d.Id, d.UserId, d.IsOnline, d.CurrentTripId }).ToDictionaryAsync(d => d.Id, ct);
        var changes = 0;
        foreach (var entry in active)
        {
            var driver = drivers.GetValueOrDefault(entry.DriverId);
            if (entry.LastSeenAt < cutoff)
            {
                Leave(entry, driver is { IsOnline: false } ? AirportQueueLeftReason.Offline : AirportQueueLeftReason.ExitedArea, now);
                changes++;
            }
            else if (entry.Status == AirportQueueStatus.Waiting && driver?.CurrentTripId is not null)
            {
                Leave(entry, AirportQueueLeftReason.TripAssigned, now);
                changes++;
            }
        }

        await db.SaveChangesAsync(ct);

        foreach (var group in active.Where(e => e.IsActive).GroupBy(e => (e.AirportId, e.RideCategoryId)))
        {
            var recent = await RecentDispatchesAsync(group.Key.AirportId, now, ct);
            var ordered = group.Where(e => e.Status == AirportQueueStatus.Waiting).OrderBy(e => e.EnteredAt).ThenBy(e => e.Id).ToList();
            var total = group.Count();
            for (var i = 0; i < ordered.Count; i++)
            {
                var current = new AirportQueueUpdatedEvent(i + 1, total, EstimateOf(i + 1, recent));
                if (!broadcasts.Changed(ordered[i].Id, current))
                {
                    continue;
                }

                broadcasts.Remember(ordered[i].Id, current);
                if (drivers.GetValueOrDefault(ordered[i].DriverId) is { } target)
                {
                    await notifier.AirportQueueUpdatedAsync(target.UserId, current, ct);
                    changes++;
                }
            }
        }

        return changes;
    }

    // ----- admin -----

    public async Task<IReadOnlyList<AdminAirportQueueEntryDto>> AdminListAsync(Guid airportId, CancellationToken ct)
    {
        var entries = await db.AirportQueueEntries.AsNoTracking().Where(e => e.AirportId == airportId && (e.Status == AirportQueueStatus.Waiting || e.Status == AirportQueueStatus.Offered))
            .OrderBy(e => e.EnteredAt).ThenBy(e => e.Id).ToListAsync(ct);
        var driverIds = entries.Select(e => e.DriverId).Distinct().ToList();
        var names = await (from d in db.Drivers.AsNoTracking() join u in db.Users.AsNoTracking() on d.UserId equals u.Id where driverIds.Contains(d.Id) select new { d.Id, u.FullName })
            .ToDictionaryAsync(x => x.Id, x => x.FullName, ct);
        var categoryIds = entries.Select(e => e.RideCategoryId).Distinct().ToList();
        var codes = await db.RideCategories.AsNoTracking().Where(c => categoryIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.Code, ct);
        var position = new Dictionary<Guid, int>();
        foreach (var group in entries.Where(e => e.Status == AirportQueueStatus.Waiting).GroupBy(e => e.RideCategoryId))
        {
            var n = 0;
            foreach (var entry in group)
            {
                position[entry.Id] = ++n;
            }
        }

        return entries.Select(e => new AdminAirportQueueEntryDto(
            e.Id, position.GetValueOrDefault(e.Id), e.DriverId, names.GetValueOrDefault(e.DriverId), codes.GetValueOrDefault(e.RideCategoryId), e.EnteredAt, e.LastSeenAt, e.Status)).ToList();
    }

    // ----- helpers -----

    private async Task<AirportQueueStatusDto> StatusOfAsync(DriverProfile driver, Language lang, CancellationToken ct)
    {
        var entry = await ActiveEntryAsync(driver.Id, ct);
        if (entry is not null)
        {
            var airport = await catalog.FindAsync(entry.AirportId, ct);
            var (position, total, estimate) = await PositionOfAsync(entry, ct);
            var reference = airport is null ? await AirportRefAsync(entry.AirportId, lang, ct) : new AirportRefDto(airport.Id, airport.Code, airport.Name(lang));
            return new AirportQueueStatusDto(true, reference, position, total, entry.EnteredAt, estimate);
        }

        var location = await db.DriverLocations.AsNoTracking().FirstOrDefaultAsync(l => l.DriverId == driver.Id, ct);
        var eligible = location is null ? null : (await catalog.AllAsync(ct)).FirstOrDefault(a => a.QueueEnabled && a.ContainsPoint(location.Lat, location.Lng));
        return new AirportQueueStatusDto(false, EligibleAirport: eligible is null ? null : new AirportRefDto(eligible.Id, eligible.Code, eligible.Name(lang)));
    }

    private async Task<AirportRefDto?> AirportRefAsync(Guid airportId, Language lang, CancellationToken ct)
    {
        var row = await db.Airports.AsNoTracking().Where(a => a.Id == airportId).Select(a => new { a.Id, a.Code, a.NameAr, a.NameEn }).FirstOrDefaultAsync(ct);
        return row is null ? null : new AirportRefDto(row.Id, row.Code, lang.Pick(row.NameAr, row.NameEn));
    }

    private async Task<AirportQueueEntry> EnterAsync(DriverProfile driver, AirportSnapshot airport, CancellationToken ct)
    {
        var categoryId = await db.Vehicles.AsNoTracking().Where(v => v.DriverId == driver.Id && v.IsActive).Select(v => (Guid?)v.RideCategoryId).FirstOrDefaultAsync(ct)
                         ?? throw new DomainException(ErrorCodes.Conflict, new { reason = "no_active_vehicle" });
        var now = clock.UtcNow;
        var entry = new AirportQueueEntry { AirportId = airport.Id, DriverId = driver.Id, RideCategoryId = categoryId, Status = AirportQueueStatus.Waiting, EnteredAt = now, LastSeenAt = now };
        db.AirportQueueEntries.Add(entry);
        return entry;
    }

    private void Leave(AirportQueueEntry entry, AirportQueueLeftReason reason, DateTime now)
    {
        entry.Status = AirportQueueStatus.Left;
        entry.LeftAt = now;
        entry.LeftReason = reason;
        entry.OfferedTripId = null;
        broadcasts.Forget(entry.Id);
    }

    private async Task<AirportQueueEntry?> ActiveEntryAsync(Guid driverId, CancellationToken ct) =>
        db.AirportQueueEntries.Local.FirstOrDefault(e => e.DriverId == driverId && e.IsActive)
        ?? await db.AirportQueueEntries.FirstOrDefaultAsync(e => e.DriverId == driverId && (e.Status == AirportQueueStatus.Waiting || e.Status == AirportQueueStatus.Offered), ct);

    /// <summary>Position = waiting entries ahead in the same airport and category + 1; total = active entries of the group.</summary>
    private async Task<(int Position, int Total, int? EstimatedWaitMinutes)> PositionOfAsync(AirportQueueEntry entry, CancellationToken ct)
    {
        var group = await db.AirportQueueEntries.AsNoTracking()
            .Where(e => e.AirportId == entry.AirportId && e.RideCategoryId == entry.RideCategoryId && (e.Status == AirportQueueStatus.Waiting || e.Status == AirportQueueStatus.Offered))
            .OrderBy(e => e.EnteredAt).ThenBy(e => e.Id).Select(e => new { e.Id, e.Status, e.EnteredAt }).ToListAsync(ct);
        var ahead = group.TakeWhile(e => e.Id != entry.Id).Count(e => e.Status == AirportQueueStatus.Waiting);
        var position = ahead + 1;
        var recent = await RecentDispatchesAsync(entry.AirportId, clock.UtcNow, ct);
        return (position, Math.Max(group.Count, position), EstimateOf(position, recent));
    }

    private async Task<IReadOnlyList<DateTime>> RecentDispatchesAsync(Guid airportId, DateTime now, CancellationToken ct)
    {
        var since = now - EstimateWindow;
        return await db.AirportQueueEntries.AsNoTracking()
            .Where(e => e.AirportId == airportId && e.Status == AirportQueueStatus.Dispatched && e.LeftAt != null && e.LeftAt >= since)
            .OrderBy(e => e.LeftAt).Select(e => e.LeftAt!.Value).ToListAsync(ct);
    }

    /// <summary><c>position × average gap between dispatches of the last two hours</c>; <c>null</c> without at least two dispatches.</summary>
    public static int? EstimateOf(int position, IReadOnlyList<DateTime> dispatches)
    {
        if (dispatches.Count < 2)
        {
            return null;
        }

        var averageMinutes = (dispatches[^1] - dispatches[0]).TotalMinutes / (dispatches.Count - 1);
        return (int)Math.Ceiling(position * averageMinutes);
    }

    private async Task<DriverProfile> LoadDriverAsync(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        return await db.Drivers.FirstOrDefaultAsync(d => d.UserId == userId, ct) ?? throw new DomainException(ErrorCodes.Forbidden);
    }
}
