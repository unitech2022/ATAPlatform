using ATA.Api.Common;
using ATA.Api.Modules.Trips;
using ATA.Domain.Cancellation;
using ATA.Domain.Common;
using ATA.Domain.Scheduling;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using ATA.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Scheduling;

/// <summary>Passenger side of scheduled rides: the booking rules, the open scheduled trips, the booking limit and the reserved driver's photo.</summary>
public sealed class ScheduledRideService(AtaDbContext db, ICurrentUser currentUser, IClock clock, ScheduleRuleProvider rules, TripReadService reads, IFileStorage storage)
{
    /// <summary><c>GET /passenger/scheduling/rules</c>: the window for the (optional) category and pickup city, from now.</summary>
    public async Task<SchedulingRulesDto> RulesAsync(Guid? rideCategoryId, decimal? lat, decimal? lng, CancellationToken ct)
    {
        var v = new Validator()
            .Rule(nameof(lat), lat is null or (>= -90 and <= 90), "out of range")
            .Rule(nameof(lng), lng is null or (>= -180 and <= 180), "out of range")
            .Rule("lat", (lat is null) == (lng is null), "lat and lng go together");
        v.ThrowIfInvalid();
        if (rideCategoryId is { } categoryId)
        {
            v.Rule(nameof(rideCategoryId), await db.RideCategories.AsNoTracking().AnyAsync(c => c.Id == categoryId && c.IsActive, ct), "unknown or inactive ride category").ThrowIfInvalid();
        }

        var now = clock.UtcNow;
        var cityId = lat is { } la && lng is { } ln ? await rules.CityOfAsync(la, ln, now, ct) : null;
        var rule = await rules.ResolveAsync(cityId, rideCategoryId, ct);
        return new SchedulingRulesDto(rule.MaxDaysAhead, rule.MinLeadMinutes, rule.MinScheduledAt(now), rule.MaxScheduledAt(now), rule.FreeCancelMinutesBefore,
            rule.LateCancelFeeType == CancellationFeeType.Fixed ? rule.LateCancelFeeAmount : null, rule.RiderOffsets());
    }

    /// <summary><c>GET /passenger/trips/scheduled</c>: the passenger's open scheduled bookings ordered by <c>scheduledAt</c>.</summary>
    public async Task<IReadOnlyList<TripDto>> ListAsync(Language lang, CancellationToken ct)
    {
        var passengerId = await PassengerIdAsync(ct);
        var trips = await db.Trips.AsNoTracking().Include(t => t.Stops)
            .Where(t => t.PassengerId == passengerId && t.BookingType == BookingType.Scheduled && Trip.OpenStatuses.Contains(t.Status))
            .OrderBy(t => t.ScheduledAt).ThenBy(t => t.RequestedAt).ToListAsync(ct);
        var result = new List<TripDto>(trips.Count);
        foreach (var trip in trips)
        {
            result.Add(await reads.BuildAsync(trip, TripViewer.Passenger, lang, ct));
        }

        return result;
    }

    /// <summary>Open (non-terminal) scheduled bookings of the passenger; <c>422 scheduled_limit_reached { max }</c> once <c>max_open_per_passenger</c> is reached.</summary>
    public async Task EnsureCanBookAsync(Guid passengerId, ScheduledRideRule rule, CancellationToken ct)
    {
        var open = await db.Trips.AsNoTracking().CountAsync(t => t.PassengerId == passengerId && t.BookingType == BookingType.Scheduled && Trip.OpenStatuses.Contains(t.Status), ct);
        if (open >= rule.MaxOpenPerPassenger)
        {
            throw new DomainException(ErrorCodes.ScheduledLimitReached, new { max = rule.MaxOpenPerPassenger });
        }
    }

    /// <summary>The photo of the driver holding a reservation on the passenger's scheduled trip (<c>404</c> otherwise or without a photo).</summary>
    public async Task<(Stream Content, string ContentType)> DriverPhotoAsync(Guid tripId, CancellationToken ct)
    {
        var passengerId = await PassengerIdAsync(ct);
        var driverId = await (from r in db.ScheduledRideReservations.AsNoTracking() join t in db.Trips.AsNoTracking() on r.TripId equals t.Id
                              where t.Id == tripId && t.PassengerId == passengerId
                                    && (r.Status == ReservationStatus.Reserved || r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.Assigned)
                              select (Guid?)r.DriverId).FirstOrDefaultAsync(ct) ?? throw new DomainException(ErrorCodes.NotFound);
        var fileId = await (from doc in db.DriverDocuments.AsNoTracking() join type in db.DocumentTypes.AsNoTracking() on doc.DocumentTypeId equals type.Id
                            where doc.DriverId == driverId && type.Code == "profile_photo" && doc.Status != Domain.Drivers.DocumentStatus.Rejected select (Guid?)doc.FileId).FirstOrDefaultAsync(ct);
        var file = fileId is { } id ? await db.StoredFiles.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, ct) : null;
        var stream = file is null ? null : await storage.OpenReadAsync(file.StorageKey, ct);
        return stream is null ? throw new DomainException(ErrorCodes.NotFound) : (stream, file!.ContentType);
    }

    private async Task<Guid> PassengerIdAsync(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        return await db.Passengers.AsNoTracking().Where(p => p.UserId == userId).Select(p => (Guid?)p.Id).FirstOrDefaultAsync(ct) ?? throw new DomainException(ErrorCodes.Forbidden);
    }
}
