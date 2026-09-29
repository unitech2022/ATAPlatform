using ATA.Api.Common;
using ATA.Api.Modules.Trips.Realtime;
using ATA.Domain.Common;
using ATA.Api.Modules.Payments;
using ATA.Domain.Matching;
using ATA.Domain.Payments;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Trips;

/// <summary>The viewer's rating state of a trip (F15).</summary>
public sealed record TripRatingState(Ratings.MyRatingDto? MyRating, bool CanRate, DateTime? RateUntil)
{
    public static readonly TripRatingState None = new(null, false, null);
}

public static class TripDtoRatings
{
    public static TripDto WithRating(this TripDto dto, TripRatingState state) => dto with { MyRating = state.MyRating, CanRate = state.CanRate, RateUntil = state.RateUntil };
}

/// <summary>Participants of a trip resolved to user ids (for notifications and real-time fan-out).</summary>
public sealed record TripParticipants(Guid PassengerUserId, Guid? DriverUserId);

/// <summary>Builds the <see cref="TripDto"/>/<see cref="OfferDto"/> read models and publishes <c>TripUpdated</c> to both parties.</summary>
public sealed class TripReadService(AtaDbContext db, TripPinService pins, ITripNotifier notifier, IClock clock, Microsoft.Extensions.Options.IOptions<Ratings.RatingsOptions> ratingOptions)
{
    public async Task<Trip?> FindAsync(Guid tripId, CancellationToken ct) =>
        await db.Trips.Include(t => t.Stops).FirstOrDefaultAsync(t => t.Id == tripId, ct);

    public async Task<TripParticipants> ParticipantsAsync(Trip trip, CancellationToken ct)
    {
        var passengerUserId = await db.Passengers.AsNoTracking().Where(p => p.Id == trip.PassengerId).Select(p => p.UserId).FirstAsync(ct);
        Guid? driverUserId = trip.DriverId is null
            ? null
            : await db.Drivers.AsNoTracking().Where(d => d.Id == trip.DriverId).Select(d => (Guid?)d.UserId).FirstOrDefaultAsync(ct);
        return new TripParticipants(passengerUserId, driverUserId);
    }

    /// <summary>The PIN is only revealed to the passenger while a driver is assigned and the trip is still running.</summary>
    public string? PinFor(Trip trip, TripViewer viewer) =>
        viewer == TripViewer.Passenger && trip.HasDriver && !trip.IsTerminal ? pins.Reveal(trip) : null;

    public async Task<TripDto> BuildAsync(Trip trip, TripViewer viewer, Language lang, CancellationToken ct)
    {
        var category = await db.RideCategories.AsNoTracking().FirstAsync(c => c.Id == trip.RideCategoryId, ct);
        var stops = trip.Stops.Count > 0 ? trip.Stops.ToList() : await db.TripStops.AsNoTracking().Where(s => s.TripId == trip.Id).ToListAsync(ct);
        var events = await db.TripEvents.AsNoTracking().Where(e => e.TripId == trip.Id).OrderBy(e => e.CreatedAt).ThenBy(e => e.Id)
            .Select(e => new TripEventDto(e.Type, e.Actor, e.CreatedAt)).ToListAsync(ct);

        TripDriverDto? driver = null;
        TripVehicleDto? vehicle = null;
        if (trip.DriverId is { } driverId && trip.HasDriver)
        {
            var row = await (from d in db.Drivers.AsNoTracking()
                             join u in db.Users.AsNoTracking() on d.UserId equals u.Id
                             where d.Id == driverId
                             select new { d.Id, u.FullName, d.RatingAvg, u.PhoneNumber, d.Gender }).FirstAsync(ct);
            var photoFileId = await (from doc in db.DriverDocuments.AsNoTracking()
                                     join type in db.DocumentTypes.AsNoTracking() on doc.DocumentTypeId equals type.Id
                                     where doc.DriverId == driverId && type.Code == "profile_photo" && doc.Status != Domain.Drivers.DocumentStatus.Rejected
                                     select (Guid?)doc.FileId).FirstOrDefaultAsync(ct);
            driver = new TripDriverDto(row.Id, row.FullName, row.RatingAvg, photoFileId, PhoneMasking.Mask(row.PhoneNumber), row.Gender);

            if (trip.VehicleId is { } vehicleId)
            {
                vehicle = await db.Vehicles.AsNoTracking().Where(v => v.Id == vehicleId)
                    .Select(v => new TripVehicleDto(v.Make, v.Model, v.Color, v.PlateNumber)).FirstOrDefaultAsync(ct);
            }
        }

        return new TripDto(
            trip.Id,
            trip.TripNumber,
            trip.Status,
            trip.BookingType,
            trip.ScheduledAt,
            new TripRideCategoryDto(category.Id, category.Code, lang.Pick(category.NameAr, category.NameEn)),
            new PlaceDto(trip.PickupName, trip.PickupAddress, trip.PickupLat, trip.PickupLng),
            new PlaceDto(trip.DropoffName, trip.DropoffAddress, trip.DropoffLat, trip.DropoffLng),
            stops.OrderBy(s => s.Sequence).Select(s => new PlaceDto(s.Name, s.Address, s.Lat, s.Lng)).ToList(),
            trip.PaymentMethod,
            trip.PricingMode,
            trip.OfferedPrice,
            trip.EstimatedFare,
            trip.FinalFare,
            trip.EstimatedDistanceM,
            trip.EstimatedDurationS,
            driver,
            vehicle,
            PinFor(trip, viewer),
            trip.WaitingSeconds,
            trip.CancelledBy,
            trip.CancellationReason,
            new TripTimelineDto(trip.RequestedAt, trip.AssignedAt, trip.ArrivedAt, trip.StartedAt, trip.CompletedAt, trip.CancelledAt),
            events,
            await PaymentForAsync(trip.Id, ct),
            // Only the driver sees the cash to collect once the trip is completed (including a card that fell back to cash).
            viewer == TripViewer.Driver && trip.Status == TripStatus.Completed && trip.PaymentMethod == PaymentMethodKind.Cash ? trip.FinalFare : null,
            trip.DiscountTotal,
            await CancellationForAsync(trip, viewer, lang, ct),
            await PromotionForAsync(trip.Id, ct))
            .WithRating(await RatingForAsync(trip, viewer, ct));
    }

    /// <summary><c>Trip.promotion</c> (F15): the code reserved / applied / released for the trip.</summary>
    public async Task<Promotions.TripPromotionDto?> PromotionForAsync(Guid tripId, CancellationToken ct) =>
        await (from r in db.PromotionRedemptions.AsNoTracking()
               join p in db.Promotions.AsNoTracking() on r.PromotionId equals p.Id
               where r.TripId == tripId
               select new Promotions.TripPromotionDto(p.Code, r.Status, r.DiscountAmount, p.Id, r.ReservedAmount)).FirstOrDefaultAsync(ct);

    /// <summary>Rating state of many trips at once for the trip history lists (F15).</summary>
    public async Task<IReadOnlyDictionary<Guid, TripRatingState>> RatingStatesAsync(IReadOnlyCollection<Trip> trips, TripViewer viewer, CancellationToken ct)
    {
        var role = Ratings.RatingService.RoleOf(viewer);
        var ids = trips.Where(t => t.Status == TripStatus.Completed).Select(t => t.Id).ToList();
        var mine = ids.Count == 0
            ? []
            : await db.Ratings.AsNoTracking().Where(r => ids.Contains(r.TripId) && r.RaterRole == role).ToDictionaryAsync(r => r.TripId, r => new { r.Stars, r.Tags }, ct);
        var now = clock.UtcNow;
        return trips.ToDictionary(t => t.Id, t =>
        {
            if (viewer == TripViewer.Admin || t.Status != TripStatus.Completed || t.CompletedAt is not { } completedAt)
            {
                return TripRatingState.None;
            }

            var rateUntil = completedAt.AddHours(ratingOptions.Value.WindowHours);
            var rating = mine.GetValueOrDefault(t.Id);
            return new TripRatingState(rating is null ? null : new Ratings.MyRatingDto(rating.Stars, Ratings.RatingService.ParseTags(rating.Tags)),
                rating is null && t.DriverId is not null && now <= rateUntil, rateUntil);
        });
    }

    /// <summary><c>Trip.myRating</c>, <c>canRate</c>, <c>rateUntil</c> for the passenger / driver viewer (F15).</summary>
    public async Task<TripRatingState> RatingForAsync(Trip trip, TripViewer viewer, CancellationToken ct)
    {
        if (viewer == TripViewer.Admin || trip.Status != TripStatus.Completed || trip.CompletedAt is not { } completedAt)
        {
            return TripRatingState.None;
        }

        var role = Ratings.RatingService.RoleOf(viewer);
        var mine = await db.Ratings.AsNoTracking().Where(r => r.TripId == trip.Id && r.RaterRole == role).Select(r => new { r.Stars, r.Tags }).FirstOrDefaultAsync(ct);
        var rateUntil = completedAt.AddHours(ratingOptions.Value.WindowHours);
        return new TripRatingState(
            mine is null ? null : new Ratings.MyRatingDto(mine.Stars, Ratings.RatingService.ParseTags(mine.Tags)),
            mine is null && trip.DriverId is not null && clock.UtcNow <= rateUntil,
            rateUntil);
    }

    /// <summary>
    /// <c>Trip.cancellation</c> (F14): the passenger sees the fee, the driver the compensation instead, admins everything; <c>null</c> unless the trip
    /// has a <c>cancellation_events</c> row.
    /// </summary>
    public async Task<ATA.Api.Modules.Cancellation.TripCancellationDto?> CancellationForAsync(Trip trip, TripViewer viewer, Language lang, CancellationToken ct)
    {
        if (trip.Status is not (TripStatus.Cancelled or TripStatus.NoDrivers))
        {
            return null;
        }

        var row = await (from ce in db.CancellationEvents.AsNoTracking()
                         join r in db.CancellationReasons.AsNoTracking() on ce.ReasonId equals r.Id into reasons
                         from r in reasons.DefaultIfEmpty()
                         where ce.TripId == trip.Id
                         select new { e = ce, NameAr = r == null ? null : r.NameAr, NameEn = r == null ? null : r.NameEn }).FirstOrDefaultAsync(ct);
        if (row is null)
        {
            return null;
        }

        var e = row.e;
        var admin = viewer == TripViewer.Admin;
        var passenger = viewer == TripViewer.Passenger || admin;
        var driver = viewer == TripViewer.Driver || admin;
        return new ATA.Api.Modules.Cancellation.TripCancellationDto(
            admin ? e.Id : null, admin ? e.Actor : null, e.Stage, e.ReasonCode, lang.PickOptional(row.NameAr, row.NameEn), admin ? e.Note : null, e.AtFault,
            passenger ? e.FeeAmount : null, passenger ? e.FeeCharged : null, e.FeeStatus, driver ? e.CompensationAmount : null,
            admin || (viewer == TripViewer.Passenger && e.AtFault == ATA.Domain.Cancellation.AtFault.Passenger) || (viewer == TripViewer.Driver && e.AtFault == ATA.Domain.Cancellation.AtFault.Driver) ? e.PenaltyPoints : null,
            e.ExcuseStatus, admin ? e.ReviewNote : null);
    }

    /// <summary><c>trip.payment</c>: the latest gateway payment of the trip with its card (card trips only).</summary>
    public async Task<TripPaymentDto?> PaymentForAsync(Guid tripId, CancellationToken ct)
    {
        var payment = await db.Payments.AsNoTracking().Where(p => p.TripId == tripId && p.Purpose == PaymentPurpose.Trip)
            .OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id).FirstOrDefaultAsync(ct);
        if (payment is null)
        {
            return null;
        }

        var card = payment.PaymentMethodId is { } id
            ? await db.PaymentMethods.AsNoTracking().Where(m => m.Id == id).Select(m => new { m.Brand, m.Last4 }).FirstOrDefaultAsync(ct)
            : null;
        return new TripPaymentDto(payment.Id, payment.Status, payment.Method, card?.Brand, card?.Last4, payment.AuthorizedAmount, payment.CapturedAmount, PaymentService.ActionOf(payment));
    }

    /// <summary>Builds the trip once, pushes <c>TripUpdated</c> to the passenger (with PIN), the driver and admins, and returns the caller's view.</summary>
    public async Task<TripDto> PublishAsync(Trip trip, TripViewer responder, Language lang, CancellationToken ct)
    {
        var participants = await ParticipantsAsync(trip, ct);
        var dto = await BuildAsync(trip, TripViewer.Driver, lang, ct);
        var passengerDto = (dto with
        {
            Pin = PinFor(trip, TripViewer.Passenger), CollectCashAmount = null,
            Cancellation = dto.Cancellation is null ? null : await CancellationForAsync(trip, TripViewer.Passenger, lang, ct),
        }).WithRating(await RatingForAsync(trip, TripViewer.Passenger, ct));
        await notifier.TripUpdatedAsync(participants.PassengerUserId, passengerDto, ct);
        if (participants.DriverUserId is { } driverUserId)
        {
            await notifier.TripUpdatedAsync(driverUserId, dto, ct);
        }

        var adminDto = (passengerDto with { Pin = null, Cancellation = dto.Cancellation is null ? null : await CancellationForAsync(trip, TripViewer.Admin, lang, ct) })
            .WithRating(TripRatingState.None);
        await notifier.TripUpdatedForAdminsAsync(adminDto, ct);
        return responder switch
        {
            TripViewer.Passenger => passengerDto,
            TripViewer.Admin => adminDto,
            _ => dto,
        };
    }

    public async Task<OfferDto> BuildOfferAsync(TripOffer offer, Trip trip, CancellationToken ct)
    {
        var passenger = await (from p in db.Passengers.AsNoTracking()
                               join u in db.Users.AsNoTracking() on p.UserId equals u.Id
                               where p.Id == trip.PassengerId
                               select new { u.FullName, p.RatingAvg }).FirstAsync(ct);
        var stops = trip.Stops.Count > 0 ? trip.Stops.ToList() : await db.TripStops.AsNoTracking().Where(s => s.TripId == trip.Id).ToListAsync(ct);
        var firstName = passenger.FullName?.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        var round = await db.MatchingAttempts.AsNoTracking().Where(a => a.TripId == trip.Id).OrderByDescending(a => a.Round).Select(a => (int?)a.Round).FirstOrDefaultAsync(ct) ?? 1;
        return new OfferDto(
            offer.Id,
            trip.Id,
            new PlaceDto(trip.PickupName, trip.PickupAddress, trip.PickupLat, trip.PickupLng),
            new PlaceDto(trip.DropoffName, trip.DropoffAddress, trip.DropoffLat, trip.DropoffLng),
            stops.OrderBy(s => s.Sequence).Select(s => new PlaceDto(s.Name, s.Address, s.Lat, s.Lng)).ToList(),
            offer.DistanceToPickupM,
            offer.EtaSeconds,
            trip.EstimatedDistanceM,
            trip.EstimatedFare,
            offer.DriverNetEarnings,
            offer.ExpiresAt,
            new OfferPassengerDto(firstName, passenger.RatingAvg),
            round,
            trip.PricingMode == PricingMode.Offer,
            trip.PaymentMethod);
    }

    /// <summary>
    /// Clears the driver's current trip, expires any open offers and closes the open matching round for the trip
    /// (used on cancel/complete/no_drivers).
    /// </summary>
    public async Task ReleaseDriverAsync(Trip trip, DateTime now, CancellationToken ct)
    {
        var outcome = trip.Status switch
        {
            TripStatus.Cancelled => MatchingOutcome.Cancelled,
            TripStatus.NoDrivers => MatchingOutcome.Timeout,
            _ => MatchingOutcome.Assigned,
        };
        var openAttempts = await db.MatchingAttempts.Include(a => a.Candidates).Where(a => a.TripId == trip.Id && a.FinishedAt == null).ToListAsync(ct);
        foreach (var attempt in openAttempts)
        {
            foreach (var candidate in attempt.Candidates.Where(c => c.Offered && c.Response == null))
            {
                candidate.Response = CandidateResponse.Expired;
            }

            attempt.Finish(outcome, now);
        }

        if (trip.DriverId is { } driverId)
        {
            await db.Drivers.Where(d => d.Id == driverId && d.CurrentTripId == trip.Id).ExecuteUpdateAsync(s => s.SetProperty(d => d.CurrentTripId, (Guid?)null), ct);
            await db.DriverLocations.Where(l => l.DriverId == driverId && l.CurrentTripId == trip.Id).ExecuteUpdateAsync(s => s.SetProperty(l => l.CurrentTripId, (Guid?)null), ct);
        }

        var open = await db.TripOffers.Where(o => o.TripId == trip.Id && o.Status == OfferStatus.Sent).ToListAsync(ct);
        foreach (var offer in open)
        {
            offer.Expire(now);
        }
    }

    public static TripStatus[] StatusesFor(string? filter) => filter switch
    {
        null or "all" => Enum.GetValues<TripStatus>(),
        "active" => Trip.ActiveStatuses,
        "completed" => [TripStatus.Completed],
        "cancelled" => [TripStatus.Cancelled, TripStatus.NoDrivers],
        _ => throw new DomainException(ErrorCodes.ValidationFailed, new Dictionary<string, string> { ["status"] = "must be all|active|completed|cancelled" }),
    };
}
