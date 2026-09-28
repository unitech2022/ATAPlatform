using System.Text.Json;
using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Api.Modules.Notifications;
using ATA.Api.Modules.Payments;
using ATA.Domain.Common;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Trips;

/// <summary>Trip browsing, forced cancellation (audited) and the live map snapshot for the admin console.</summary>
public sealed class AdminTripService(AtaDbContext db, TripReadService reads, TripEventRecorder events, AuditService audit, INotificationDispatcher notifications, CardTripPaymentService cardPayments, ICurrentUser currentUser, IClock clock)
{
    public const string EntityType = "trip";
    private static readonly TimeSpan RecentlyOffline = TimeSpan.FromMinutes(15);

    public async Task<PagedResult<AdminTripListItemDto>> ListAsync(TripStatus? status, DateOnly? from, DateOnly? to, string? search, Paging paging, Language lang, CancellationToken ct)
    {
        var query = from t in db.Trips.AsNoTracking()
                    join p in db.Passengers.AsNoTracking() on t.PassengerId equals p.Id
                    join u in db.Users.AsNoTracking() on p.UserId equals u.Id
                    join c in db.RideCategories.AsNoTracking() on t.RideCategoryId equals c.Id
                    select new { Trip = t, PassengerName = u.FullName, PassengerPhone = u.PhoneNumber, Category = c };

        if (status is not null) query = query.Where(x => x.Trip.Status == status);
        if (from is { } f)
        {
            var fromAt = f.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(x => x.Trip.RequestedAt >= fromAt);
        }

        if (to is { } until)
        {
            var toAt = until.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(x => x.Trip.RequestedAt < toAt);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var phoneTerm = PhoneNumber.TryNormalize(term, out var normalized) ? normalized : term;
            query = query.Where(x => x.Trip.TripNumber.Contains(term) || x.PassengerPhone.Contains(phoneTerm) || (x.PassengerName != null && x.PassengerName.Contains(term)));
        }

        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.Trip.RequestedAt).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        var driverNames = await DriverNamesAsync(rows.Where(r => r.Trip.DriverId != null).Select(r => r.Trip.DriverId!.Value), ct);
        var items = rows.Select(r => new AdminTripListItemDto(
            r.Trip.Id, r.Trip.TripNumber, r.Trip.Status, r.PassengerName, r.PassengerPhone,
            r.Trip.DriverId is { } driverId ? driverNames.GetValueOrDefault(driverId) : null,
            lang.Pick(r.Category.NameAr, r.Category.NameEn), r.Trip.PickupName, r.Trip.DropoffName,
            r.Trip.EstimatedFare, r.Trip.FinalFare, r.Trip.PaymentMethod, r.Trip.RequestedAt)).ToList();
        return paging.Result(items, total);
    }

    public async Task<AdminTripDetailDto> GetAsync(Guid tripId, Language lang, CancellationToken ct)
    {
        var trip = Guard.NotFound(await reads.FindAsync(tripId, ct));
        return await BuildDetailAsync(trip, lang, ct);
    }

    public async Task<AdminTripDetailDto> CancelAsync(Guid tripId, AdminCancelTripRequest request, Language lang, CancellationToken ct)
    {
        new Validator().Require(nameof(request.Reason), request.Reason, 500).ThrowIfInvalid();
        var trip = Guard.NotFound(await reads.FindAsync(tripId, ct));
        var now = clock.UtcNow;
        var reason = request.Reason!.Trim();
        var participants = await reads.ParticipantsAsync(trip, ct);
        var hadDriver = trip.HasDriver;
        var before = new { status = trip.Status, trip.DriverId };
        trip.Cancel(CancelledBy.Admin, reason, now);
        await reads.ReleaseDriverAsync(trip, now, ct);
        events.Add(trip.Id, TripEventTypes.Cancelled, TripActor.Admin, currentUser.UserId, data: new { reason, hadDriver });
        audit.Log("trip.cancel", EntityType, trip.Id, before, new { status = trip.Status, reason });
        await notifications.DispatchAsync(TripNotifications.Cancelled(trip, participants.PassengerUserId), ct);
        if (hadDriver && participants.DriverUserId is { } driverUserId)
        {
            await notifications.DispatchAsync(TripNotifications.Cancelled(trip, driverUserId), ct);
        }

        await db.SaveChangesAsync(ct);
        await cardPayments.ReleaseAsync(trip.Id, ct);
        await reads.PublishAsync(trip, TripViewer.Admin, lang, ct);
        return await BuildDetailAsync(trip, lang, ct);
    }

    public async Task<LiveSnapshotDto> GetLiveAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;
        var recent = now - RecentlyOffline;
        var driverRows = await (from loc in db.DriverLocations.AsNoTracking()
                                join d in db.Drivers.AsNoTracking() on loc.DriverId equals d.Id
                                join u in db.Users.AsNoTracking() on d.UserId equals u.Id
                                where loc.IsOnline || loc.UpdatedAt >= recent
                                select new { loc, u.FullName }).ToListAsync(ct);
        var driverIds = driverRows.Select(r => r.loc.DriverId).ToList();
        var categoryCodes = await (from v in db.Vehicles.AsNoTracking()
                                   join c in db.RideCategories.AsNoTracking() on v.RideCategoryId equals c.Id
                                   where v.IsActive && driverIds.Contains(v.DriverId)
                                   select new { v.DriverId, c.Code }).ToListAsync(ct);
        var codeByDriver = categoryCodes.GroupBy(x => x.DriverId).ToDictionary(g => g.Key, g => g.First().Code);
        var drivers = driverRows.Select(r => new LiveDriverDto(
            r.loc.DriverId, r.FullName, r.loc.Lat, r.loc.Lng, r.loc.IsOnline,
            !r.loc.IsOnline ? "offline" : r.loc.CurrentTripId is null ? "idle" : "on_trip",
            codeByDriver.GetValueOrDefault(r.loc.DriverId), r.loc.CurrentTripId, r.loc.Heading, r.loc.UpdatedAt)).ToList();

        var trips = await (from t in db.Trips.AsNoTracking()
                           join p in db.Passengers.AsNoTracking() on t.PassengerId equals p.Id
                           join u in db.Users.AsNoTracking() on p.UserId equals u.Id
                           where Trip.ActiveStatuses.Contains(t.Status)
                           orderby t.RequestedAt
                           select new { t, u.FullName }).ToListAsync(ct);
        var live = trips.Select(x => new LiveTripDto(
            x.t.Id, x.t.TripNumber, x.t.Status,
            new PlaceDto(x.t.PickupName, x.t.PickupAddress, x.t.PickupLat, x.t.PickupLng),
            new PlaceDto(x.t.DropoffName, x.t.DropoffAddress, x.t.DropoffLat, x.t.DropoffLng),
            x.t.DriverId, x.FullName, x.t.RequestedAt)).ToList();
        return new LiveSnapshotDto(
            drivers,
            live.Where(t => t.Status is not (TripStatus.Requested or TripStatus.Searching)).ToList(),
            live.Where(t => t.Status is TripStatus.Requested or TripStatus.Searching).ToList(),
            now);
    }

    private async Task<AdminTripDetailDto> BuildDetailAsync(Trip trip, Language lang, CancellationToken ct)
    {
        var dto = await reads.BuildAsync(trip, TripViewer.Admin, lang, ct);
        var passenger = await (from p in db.Passengers.AsNoTracking()
                               join u in db.Users.AsNoTracking() on p.UserId equals u.Id
                               where p.Id == trip.PassengerId
                               select new AdminTripPassengerDto(p.Id, u.FullName, u.PhoneNumber, p.RatingAvg)).FirstAsync(ct);

        AdminTripDriverDto? driver = null;
        if (dto.Driver is { } d)
        {
            var phone = await (from dp in db.Drivers.AsNoTracking()
                               join u in db.Users.AsNoTracking() on dp.UserId equals u.Id
                               where dp.Id == d.Id
                               select u.PhoneNumber).FirstAsync(ct);
            driver = new AdminTripDriverDto(d.Id, d.FullName, d.RatingAvg, d.PhotoFileId, d.PhoneMasked, d.Gender, phone);
        }

        var eventRows = await db.TripEvents.AsNoTracking().Where(e => e.TripId == trip.Id).OrderBy(e => e.CreatedAt).ThenBy(e => e.Id).ToListAsync(ct);
        var actorIds = eventRows.Where(e => e.ActorUserId != null).Select(e => e.ActorUserId!.Value).Distinct().ToList();
        var actorNames = await db.Users.AsNoTracking().Where(u => actorIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName, ct);
        var events = eventRows.Select(e => new AdminTripEventDto(
            e.Type, e.Actor, e.ActorUserId is { } actorId ? actorNames.GetValueOrDefault(actorId) : null, e.CreatedAt,
            e.Data is null ? null : JsonSerializer.Deserialize<JsonElement>(e.Data))).ToList();

        var offerRows = await db.TripOffers.AsNoTracking().Where(o => o.TripId == trip.Id).OrderBy(o => o.SentAt).ToListAsync(ct);
        var offerDriverNames = await DriverNamesAsync(offerRows.Select(o => o.DriverId), ct);
        var offers = offerRows.Select(o => new AdminTripOfferDto(
            o.Id, o.DriverId, offerDriverNames.GetValueOrDefault(o.DriverId), o.Status, o.DistanceToPickupM, o.EtaSeconds, o.DriverNetEarnings, o.SentAt, o.RespondedAt, o.ExpiresAt)).ToList();

        var route = await db.DriverLocationHistory.AsNoTracking().Where(h => h.TripId == trip.Id).OrderBy(h => h.RecordedAt)
            .Select(h => new RoutePointDto(h.Lat, h.Lng, h.RecordedAt)).ToListAsync(ct);

        return new AdminTripDetailDto(
            dto.Id, dto.TripNumber, dto.Status, dto.BookingType, dto.ScheduledAt, dto.RideCategory, dto.Pickup, dto.Dropoff, dto.Stops,
            dto.PaymentMethod, dto.PricingMode, dto.OfferedPrice, dto.EstimatedFare, dto.FinalFare, dto.EstimatedDistanceMeters, dto.EstimatedDurationSeconds,
            trip.FinalDistanceM, trip.FinalDurationS, trip.DriverEarnings,
            passenger, driver, dto.Vehicle, dto.WaitingSeconds, dto.CancelledBy, dto.CancellationReason, trip.RiderNote, dto.Timeline,
            events, offers, route);
    }

    private async Task<Dictionary<Guid, string?>> DriverNamesAsync(IEnumerable<Guid> driverIds, CancellationToken ct)
    {
        var ids = driverIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        var rows = await (from d in db.Drivers.AsNoTracking()
                          join u in db.Users.AsNoTracking() on d.UserId equals u.Id
                          where ids.Contains(d.Id)
                          select new { d.Id, u.FullName }).ToListAsync(ct);
        return rows.ToDictionary(x => x.Id, x => (string?)x.FullName);
    }
}
