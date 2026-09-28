using ATA.Api.Common;
using ATA.Api.Modules.Drivers;
using ATA.Api.Modules.Notifications;
using ATA.Api.Modules.Trips.Realtime;
using ATA.Domain.Common;
using ATA.Domain.Drivers;
using ATA.Domain.Notifications;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Trips;

/// <summary>Driver side of the trip lifecycle: location, offers, and the en-route → arrived → PIN → start → complete transitions.</summary>
public sealed class DriverTripService(
    AtaDbContext db,
    ICurrentUser currentUser,
    IClock clock,
    IPricingService pricing,
    TripReadService reads,
    TripEventRecorder events,
    TripPinService pins,
    TripPaymentService payments,
    NotificationService notifications,
    ITripNotifier notifier,
    IOptions<TripOptions> options)
{
    private readonly TripOptions _options = options.Value;

    public async Task UpdateLocationAsync(DriverLocationRequest request, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.Lat), request.Lat)
            .Require(nameof(request.Lng), request.Lng)
            .Rule(nameof(request.Lat), request.Lat is null or (>= -90 and <= 90), "out of range")
            .Rule(nameof(request.Lng), request.Lng is null or (>= -180 and <= 180), "out of range")
            .Rule(nameof(request.Heading), request.Heading is null or (>= 0 and <= 360), "out of range")
            .Rule(nameof(request.Speed), request.Speed is null or >= 0, "must be positive")
            .Rule(nameof(request.Accuracy), request.Accuracy is null or >= 0, "must be positive")
            .ThrowIfInvalid();

        var driver = await LoadDriverAsync(ct);
        var now = clock.UtcNow;
        var location = await db.DriverLocations.FirstOrDefaultAsync(l => l.DriverId == driver.Id, ct);
        if (location is null)
        {
            location = new DriverLocation { DriverId = driver.Id };
            db.DriverLocations.Add(location);
        }

        location.Lat = request.Lat!.Value;
        location.Lng = request.Lng!.Value;
        location.Heading = request.Heading;
        location.Speed = request.Speed;
        location.Accuracy = request.Accuracy;
        location.IsOnline = driver.IsOnline;
        location.CurrentTripId = driver.CurrentTripId;
        location.UpdatedAt = now;

        DriverLocationEvent? broadcast = null;
        Guid? passengerUserId = null;
        if (driver.CurrentTripId is { } tripId)
        {
            db.DriverLocationHistory.Add(new DriverLocationHistory { DriverId = driver.Id, TripId = tripId, Lat = location.Lat, Lng = location.Lng, RecordedAt = now });
            var trip = await db.Trips.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tripId, ct);
            if (trip is not null && !trip.IsTerminal)
            {
                var (targetLat, targetLng) = trip.Status == TripStatus.InTrip ? (trip.DropoffLat, trip.DropoffLng) : (trip.PickupLat, trip.PickupLng);
                var eta = pricing.EtaSeconds(Geo.HaversineMeters(location.Lat, location.Lng, targetLat, targetLng) * FlatPricing.RoadFactor);
                broadcast = new DriverLocationEvent(trip.Id, location.Lat, location.Lng, location.Heading, eta);
                passengerUserId = (await reads.ParticipantsAsync(trip, ct)).PassengerUserId;
            }
        }

        await db.SaveChangesAsync(ct);
        if (broadcast is not null && passengerUserId is { } userId)
        {
            await notifier.DriverLocationAsync(userId, broadcast, ct);
        }
    }

    public async Task<OfferDto?> GetActiveOfferAsync(CancellationToken ct)
    {
        var driver = await LoadDriverAsync(ct);
        var now = clock.UtcNow;
        var offer = await db.TripOffers.AsNoTracking()
            .Where(o => o.DriverId == driver.Id && o.Status == OfferStatus.Sent && o.ExpiresAt > now)
            .OrderByDescending(o => o.SentAt).FirstOrDefaultAsync(ct);
        if (offer is null)
        {
            return null;
        }

        var trip = await reads.FindAsync(offer.TripId, ct);
        return trip is null || trip.Status != TripStatus.Searching ? null : await reads.BuildOfferAsync(offer, trip, ct);
    }

    public async Task<TripDto> AcceptOfferAsync(Guid offerId, Language lang, CancellationToken ct)
    {
        var driver = await LoadDriverAsync(ct);
        var offer = Guard.NotFound(await db.TripOffers.FirstOrDefaultAsync(o => o.Id == offerId && o.DriverId == driver.Id, ct));
        var now = clock.UtcNow;
        offer.Accept(now);

        var trip = Guard.NotFound(await reads.FindAsync(offer.TripId, ct));
        trip.EnsureStatus(TripStatus.Searching);
        if (driver.ApplicationStatus != ApplicationStatus.Approved)
        {
            throw new DomainException(ErrorCodes.DriverNotApproved, new { status = driver.ApplicationStatus });
        }

        if (driver.CurrentTripId is not null)
        {
            throw new DomainException(ErrorCodes.Conflict, new { currentTripId = driver.CurrentTripId });
        }

        var vehicle = await db.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.DriverId == driver.Id && v.IsActive, ct);
        trip.Assign(driver.Id, vehicle?.Id, now);
        driver.CurrentTripId = trip.Id;
        driver.AcceptanceCount++;
        await db.DriverLocations.Where(l => l.DriverId == driver.Id).ExecuteUpdateAsync(s => s.SetProperty(l => l.CurrentTripId, trip.Id), ct);

        events.Add(trip.Id, TripEventTypes.OfferAccepted, TripActor.Driver, driver.UserId, data: new { offerId = offer.Id });
        events.Add(trip.Id, TripEventTypes.DriverAssigned, TripActor.System, data: new { driverId = driver.Id, vehicleId = vehicle?.Id, etaSeconds = offer.EtaSeconds });

        var participants = await reads.ParticipantsAsync(trip, ct);
        var driverName = await db.Users.AsNoTracking().Where(u => u.Id == driver.UserId).Select(u => u.FullName).FirstOrDefaultAsync(ct) ?? "";
        notifications.Add(participants.PassengerUserId, NotificationTypes.TripDriverAssigned,
            ("تم تعيين سائق", "Driver assigned"),
            ($"السائق {driverName} في طريقه إليك (الرحلة {trip.TripNumber}).", $"{driverName} is on the way (trip {trip.TripNumber})."),
            new { tripId = trip.Id, trip.TripNumber, status = trip.Status, driverId = driver.Id });
        await db.SaveChangesAsync(ct);
        return await reads.PublishAsync(trip, TripViewer.Driver, lang, ct);
    }

    public async Task RejectOfferAsync(Guid offerId, RejectOfferRequest? request, CancellationToken ct)
    {
        new Validator().Rule("reasonCode", request?.ReasonCode is null || request.ReasonCode.Length <= 60, "max_length:60").ThrowIfInvalid();
        var driver = await LoadDriverAsync(ct);
        var offer = Guard.NotFound(await db.TripOffers.FirstOrDefaultAsync(o => o.Id == offerId && o.DriverId == driver.Id, ct));
        offer.Reject(clock.UtcNow);
        driver.RejectionCount++;
        events.Add(offer.TripId, TripEventTypes.OfferRejected, TripActor.Driver, driver.UserId,
            data: new { offerId = offer.Id, driverId = driver.Id, reasonCode = request?.ReasonCode?.Trim() });
        await db.SaveChangesAsync(ct);
    }

    public async Task<TripDto?> GetActiveTripAsync(Language lang, CancellationToken ct)
    {
        var driver = await LoadDriverAsync(ct);
        var trip = await db.Trips.AsNoTracking().Include(t => t.Stops)
            .Where(t => t.DriverId == driver.Id && Trip.ActiveStatuses.Contains(t.Status))
            .OrderByDescending(t => t.AssignedAt).FirstOrDefaultAsync(ct);
        return trip is null ? null : await reads.BuildAsync(trip, TripViewer.Driver, lang, ct);
    }

    public async Task<TripDto> EnRouteAsync(Guid tripId, Language lang, CancellationToken ct)
    {
        var (trip, driver) = await LoadOwnAsync(tripId, ct);
        trip.MarkEnRoute();
        var location = await CurrentLocationAsync(driver.Id, ct);
        events.Add(trip.Id, TripEventTypes.DriverEnRoute, TripActor.Driver, driver.UserId, location?.Lat, location?.Lng);
        await db.SaveChangesAsync(ct);
        return await reads.PublishAsync(trip, TripViewer.Driver, lang, ct);
    }

    public async Task<TripDto> ArrivedAsync(Guid tripId, Language lang, CancellationToken ct)
    {
        var (trip, driver) = await LoadOwnAsync(tripId, ct);
        var now = clock.UtcNow;
        var location = await CurrentLocationAsync(driver.Id, ct);
        trip.MarkArrived(now);
        if (location is not null)
        {
            var distance = (int)Math.Round(Geo.HaversineMeters(location.Lat, location.Lng, trip.PickupLat, trip.PickupLng));
            if (distance > _options.ArrivalRadiusMeters)
            {
                events.Add(trip.Id, TripEventTypes.ArrivalDistanceWarning, TripActor.System, null, location.Lat, location.Lng,
                    new { distanceMeters = distance, allowedMeters = _options.ArrivalRadiusMeters });
            }
        }

        events.Add(trip.Id, TripEventTypes.DriverArrived, TripActor.Driver, driver.UserId, location?.Lat, location?.Lng);
        events.Add(trip.Id, TripEventTypes.WaitingStarted, TripActor.System, data: new { freeWaitingMinutes = _options.FreeWaitingMinutes });
        var participants = await reads.ParticipantsAsync(trip, ct);
        notifications.Add(participants.PassengerUserId, NotificationTypes.TripDriverArrived,
            ("وصل السائق", "Your driver has arrived"),
            ($"السائق بانتظارك عند نقطة الالتقاط. شارك رمز الرحلة معه لبدء الرحلة {trip.TripNumber}.", $"Your driver is waiting at the pickup. Share your trip PIN to start trip {trip.TripNumber}."),
            new { tripId = trip.Id, trip.TripNumber, status = trip.Status });
        await db.SaveChangesAsync(ct);
        return await reads.PublishAsync(trip, TripViewer.Driver, lang, ct);
    }

    public async Task<TripDto> VerifyPinAsync(Guid tripId, VerifyPinRequest request, Language lang, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.Pin), request.Pin, Trip.PinLength)
            .Rule(nameof(request.Pin), request.Pin is null || (request.Pin.Length == Trip.PinLength && request.Pin.All(char.IsAsciiDigit)), $"must be {Trip.PinLength} digits")
            .ThrowIfInvalid();

        var (trip, driver) = await LoadOwnAsync(tripId, ct);
        var now = clock.UtcNow;
        try
        {
            trip.VerifyPin(pins.Matches(trip, request.Pin!), _options.PinMaxAttempts, _options.FreeWaitingMinutes * 60, now);
        }
        catch (DomainException ex) when (ex.Code is ErrorCodes.PinInvalid or ErrorCodes.PinLocked)
        {
            events.Add(trip.Id, TripEventTypes.PinFailed, TripActor.Driver, driver.UserId,
                data: new { attempts = trip.PinAttempts, attemptsLeft = trip.PinAttemptsLeft(_options.PinMaxAttempts), locked = ex.Code == ErrorCodes.PinLocked });
            await db.SaveChangesAsync(ct);
            throw;
        }

        events.Add(trip.Id, TripEventTypes.PinVerified, TripActor.Driver, driver.UserId, data: new { waitingSeconds = trip.WaitingSeconds });
        await db.SaveChangesAsync(ct);
        return await reads.PublishAsync(trip, TripViewer.Driver, lang, ct);
    }

    public async Task<TripDto> StartAsync(Guid tripId, Language lang, CancellationToken ct)
    {
        var (trip, driver) = await LoadOwnAsync(tripId, ct);
        trip.Start(clock.UtcNow);
        var location = await CurrentLocationAsync(driver.Id, ct);
        events.Add(trip.Id, TripEventTypes.Started, TripActor.Driver, driver.UserId, location?.Lat, location?.Lng);
        await db.SaveChangesAsync(ct);
        return await reads.PublishAsync(trip, TripViewer.Driver, lang, ct);
    }

    /// <summary>Completes the trip, computes the final fare and settles payment in a single transaction.</summary>
    public async Task<TripDto> CompleteAsync(Guid tripId, CompleteTripRequest? request, Language lang, CancellationToken ct)
    {
        new Validator()
            .Rule("finalLat", request?.FinalLat is null or (>= -90 and <= 90), "out of range")
            .Rule("finalLng", request?.FinalLng is null or (>= -180 and <= 180), "out of range")
            .Rule("finalDistanceMeters", request?.FinalDistanceMeters is null or >= 0, "must be positive")
            .Rule("finalDurationSeconds", request?.FinalDurationSeconds is null or >= 0, "must be positive")
            .ThrowIfInvalid();

        var (trip, driver) = await LoadOwnAsync(tripId, ct);
        trip.EnsureStatus(TripStatus.InTrip);
        var now = clock.UtcNow;
        var category = await db.RideCategories.AsNoTracking().FirstAsync(c => c.Id == trip.RideCategoryId, ct);
        var distance = request?.FinalDistanceMeters ?? trip.EstimatedDistanceM;
        var duration = request?.FinalDurationSeconds ?? trip.EstimatedDurationS;
        var quote = pricing.Quote(category, distance, duration, trip.WaitingSeconds);
        var fare = trip.PricingMode == PricingMode.Offer && trip.OfferedPrice is { } offered ? offered : quote.Fare;
        var driverEarnings = decimal.Round(fare * category.DriverSharePercent / 100m, 2, MidpointRounding.AwayFromZero);
        var participants = await reads.ParticipantsAsync(trip, ct);

        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            trip.Complete(distance, duration, fare, driverEarnings, now);
            await payments.SettleAsync(trip, participants, fare, driverEarnings, ct);
            driver.CurrentTripId = null;
            await reads.ReleaseDriverAsync(trip, now, ct);
            events.Add(trip.Id, TripEventTypes.Completed, TripActor.Driver, driver.UserId, request?.FinalLat, request?.FinalLng,
                new { finalDistanceMeters = distance, finalDurationSeconds = duration, waitingSeconds = trip.WaitingSeconds, finalFare = fare, driverEarnings, paymentMethod = trip.PaymentMethod });
            notifications.Add(participants.PassengerUserId, NotificationTypes.TripCompleted,
                ("انتهت الرحلة", "Trip completed"),
                ($"وصلت بسلامة. أجرة الرحلة {trip.TripNumber}: {fare:0.00} ر.س ({PaymentLabel(trip.PaymentMethod, Language.Ar)}).", $"You have arrived. Trip {trip.TripNumber} fare: SAR {fare:0.00} ({PaymentLabel(trip.PaymentMethod, Language.En)})."),
                new { tripId = trip.Id, trip.TripNumber, status = trip.Status, finalFare = fare, paymentMethod = trip.PaymentMethod });
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });

        return await reads.PublishAsync(trip, TripViewer.Driver, lang, ct);
    }

    public async Task<TripDto> CancelAsync(Guid tripId, CancelTripRequest request, Language lang, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.ReasonCode), request.ReasonCode, 60)
            .Rule(nameof(request.Note), request.Note is null || request.Note.Length <= 500, "max_length:500")
            .ThrowIfInvalid();

        var (trip, driver) = await LoadOwnAsync(tripId, ct);
        var now = clock.UtcNow;
        var participants = await reads.ParticipantsAsync(trip, ct);
        trip.Cancel(CancelledBy.Driver, request.ReasonCode!.Trim(), now);
        driver.CurrentTripId = null;
        await reads.ReleaseDriverAsync(trip, now, ct);
        events.Add(trip.Id, TripEventTypes.Cancelled, TripActor.Driver, driver.UserId,
            data: new { reasonCode = trip.CancellationReason, note = request.Note?.Trim() });
        notifications.Add(participants.PassengerUserId, NotificationTypes.TripCancelled,
            ("تم إلغاء الرحلة", "Trip cancelled"),
            ($"ألغى السائق الرحلة {trip.TripNumber}. يمكنك طلب رحلة جديدة.", $"The driver cancelled trip {trip.TripNumber}. You can request a new trip."),
            new { tripId = trip.Id, trip.TripNumber, cancelledBy = trip.CancelledBy });
        await db.SaveChangesAsync(ct);
        return await reads.PublishAsync(trip, TripViewer.Driver, lang, ct);
    }

    public async Task<PagedResult<DriverTripDto>> ListAsync(string? status, Paging paging, CancellationToken ct)
    {
        var statuses = TripReadService.StatusesFor(status);
        var driver = await LoadDriverAsync(ct);
        var query = db.Trips.AsNoTracking().Where(t => t.DriverId == driver.Id && statuses.Contains(t.Status));
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(t => t.RequestedAt).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        var items = rows.Select(t => new DriverTripDto(
            t.Id, t.PickupName, t.DropoffName, t.CompletedAt, System.Text.Json.JsonNamingPolicy.SnakeCaseLower.ConvertName(t.Status.ToString()),
            t.FinalFare ?? t.EstimatedFare, t.DriverEarnings ?? 0m)).ToList();
        return paging.Result(items, total);
    }

    private static string PaymentLabel(PaymentMethodKind method, Language lang) => method switch
    {
        PaymentMethodKind.Wallet => lang.Pick("المحفظة", "wallet"),
        PaymentMethodKind.Card => lang.Pick("البطاقة", "card"),
        _ => lang.Pick("نقداً", "cash"),
    };

    private async Task<DriverLocation?> CurrentLocationAsync(Guid driverId, CancellationToken ct) =>
        await db.DriverLocations.AsNoTracking().FirstOrDefaultAsync(l => l.DriverId == driverId, ct);

    private async Task<DriverProfile> LoadDriverAsync(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        return await db.Drivers.FirstOrDefaultAsync(d => d.UserId == userId, ct) ?? throw new DomainException(ErrorCodes.Forbidden);
    }

    private async Task<(Trip Trip, DriverProfile Driver)> LoadOwnAsync(Guid tripId, CancellationToken ct)
    {
        var driver = await LoadDriverAsync(ct);
        var trip = await reads.FindAsync(tripId, ct);
        if (trip is null || trip.DriverId != driver.Id)
        {
            throw new DomainException(ErrorCodes.NotFound);
        }

        return (trip, driver);
    }
}
