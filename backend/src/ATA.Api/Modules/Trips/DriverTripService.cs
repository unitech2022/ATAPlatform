using ATA.Api.Common;
using ATA.Api.Modules.Drivers;
using ATA.Api.Modules.Notifications;
using ATA.Api.Modules.Payments;
using ATA.Api.Modules.Pricing;
using ATA.Api.Modules.Trips.Matching;
using ATA.Api.Modules.Trips.Realtime;
using ATA.Domain.Common;
using ATA.Domain.Drivers;
using ATA.Domain.Matching;
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
    CardTripPaymentService cardPayments,
    PaymentService paymentService,
    MatchingRecorder matching,
    INotificationDispatcher notifications,
    ITripNotifier notifier,
    IOptions<TripOptions> options,
    Cancellation.CancellationEngine cancellations,
    Cancellation.ReliabilityService reliability,
    Safety.TripShareService shares)
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

        await matching.RecordResponseAsync(trip.Id, driver.Id, CandidateResponse.Accepted, ct);
        await matching.CloseOpenAttemptAsync(trip.Id, MatchingOutcome.Assigned, now, ct);
        events.Add(trip.Id, TripEventTypes.OfferAccepted, TripActor.Driver, driver.UserId, data: new { offerId = offer.Id });
        events.Add(trip.Id, TripEventTypes.DriverAssigned, TripActor.System, data: new { driverId = driver.Id, vehicleId = vehicle?.Id, etaSeconds = offer.EtaSeconds });

        var participants = await reads.ParticipantsAsync(trip, ct);
        var driverName = await db.Users.AsNoTracking().Where(u => u.Id == driver.UserId).Select(u => u.FullName).FirstOrDefaultAsync(ct);
        await notifications.DispatchAsync(TripNotifications.DriverAssigned(trip, participants.PassengerUserId, driverName, vehicle, offer.EtaSeconds), ct);
        // F12: automatic sharing with the passenger's trusted contacts (auto_share).
        await shares.AutoShareOnAssignAsync(trip, participants.PassengerUserId, ct);
        await db.SaveChangesAsync(ct);
        await RefreshReliabilityAsync(driver.UserId, Role.Driver, ct);
        return await reads.PublishAsync(trip, TripViewer.Driver, lang, ct);
    }

    public async Task RejectOfferAsync(Guid offerId, RejectOfferRequest? request, CancellationToken ct)
    {
        new Validator().Rule("reasonCode", request?.ReasonCode is null || request.ReasonCode.Length <= 60, "max_length:60").ThrowIfInvalid();
        var driver = await LoadDriverAsync(ct);
        var offer = Guard.NotFound(await db.TripOffers.FirstOrDefaultAsync(o => o.Id == offerId && o.DriverId == driver.Id, ct));
        offer.Reject(clock.UtcNow);
        driver.RejectionCount++;
        await matching.RecordResponseAsync(offer.TripId, driver.Id, CandidateResponse.Rejected, ct);
        events.Add(offer.TripId, TripEventTypes.OfferRejected, TripActor.Driver, driver.UserId,
            data: new { offerId = offer.Id, driverId = driver.Id, reasonCode = request?.ReasonCode?.Trim() });
        await db.SaveChangesAsync(ct);
        await RefreshReliabilityAsync(driver.UserId, Role.Driver, ct);
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
        var arrivedDriver = await db.Users.AsNoTracking().Where(u => u.Id == driver.UserId).Select(u => u.FullName).FirstOrDefaultAsync(ct);
        var plate = trip.VehicleId is { } vehicleId ? await db.Vehicles.AsNoTracking().Where(v => v.Id == vehicleId).Select(v => v.PlateNumber).FirstOrDefaultAsync(ct) : null;
        await notifications.DispatchAsync(TripNotifications.DriverArrived(trip, participants.PassengerUserId, arrivedDriver, plate, _options.FreeWaitingMinutes), ct);
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
        var category = await db.RideCategories.AsNoTracking().FirstAsync(c => c.Id == trip.RideCategoryId, ct);
        var freeWaitingMinutes = await pricing.FreeWaitingMinutesAsync(category, new GeoPoint(trip.PickupLat, trip.PickupLng), trip.RequestedAt, ct);
        try
        {
            trip.VerifyPin(pins.Matches(trip, request.Pin!), _options.PinMaxAttempts, freeWaitingMinutes * 60, now);
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
        await notifications.DispatchAsync(TripNotifications.Started(trip, (await reads.ParticipantsAsync(trip, ct)).PassengerUserId), ct);
        await db.SaveChangesAsync(ct);
        return await reads.PublishAsync(trip, TripViewer.Driver, lang, ct);
    }

    /// <summary>
    /// Completes the trip, computes the final fare and settles payment in a single transaction. The final fare re-runs the pricing
    /// engine with the actual distance/duration/waiting at the trip's request time and the demand multiplier locked in the quote; in
    /// <c>offer</c> mode the offered price is the fare. The driver's net is the quoted share of the (subtotal × multipliers) core.
    /// </summary>
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
        var quote = await db.FareQuotes.AsNoTracking().FirstOrDefaultAsync(q => q.UsedTripId == trip.Id, ct);
        var pickupAt = trip.ScheduledAt ?? trip.RequestedAt;
        var calculation = await pricing.CalculateAsync(new FareRequest(category, new GeoPoint(trip.PickupLat, trip.PickupLng), new GeoPoint(trip.DropoffLat, trip.DropoffLng),
            distance, duration, pickupAt, trip.WaitingSeconds, quote is null ? null : QuoteService.LockedDemandOf(quote)), ct);
        decimal fare, driverEarnings;
        if (trip.PricingMode == PricingMode.Offer && trip.OfferedPrice is { } offered)
        {
            fare = offered;
            driverEarnings = PricingMath.Round2(offered * (quote?.DriverSharePercent ?? calculation.DriverSharePercent) / 100m);
        }
        else
        {
            fare = calculation.Total;
            driverEarnings = calculation.DriverNetEarnings;
        }

        var participants = await reads.ParticipantsAsync(trip, ct);
        var breakdown = await ReceiptService.StoredBreakdownAsync(db, calculation, category, trip.DiscountTotal, ct);

        // Card trips are charged before the transaction (gateway calls never run inside one).
        var card = trip.PaymentMethod == PaymentMethodKind.Card ? await cardPayments.CaptureForCompletionAsync(trip, fare, ct) : null;

        await db.InTransactionAsync(async () =>
        {
            trip.Complete(distance, duration, fare, driverEarnings, now);
            trip.FareBreakdown = System.Text.Json.JsonSerializer.Serialize(breakdown, JsonDefaults.Options);
            await payments.SettleAsync(trip, participants, fare, driverEarnings, card, ct);
            driver.CurrentTripId = null;
            await reads.ReleaseDriverAsync(trip, now, ct);
            events.Add(trip.Id, TripEventTypes.Completed, TripActor.Driver, driver.UserId, request?.FinalLat, request?.FinalLng,
                new { finalDistanceMeters = distance, finalDurationSeconds = duration, waitingSeconds = trip.WaitingSeconds, finalFare = fare, driverEarnings, paymentMethod = trip.PaymentMethod, breakdown = QuoteService.ToDto(calculation.Breakdown), pricingSource = calculation.Source });
            await notifications.DispatchAsync(TripNotifications.Completed(trip, participants.PassengerUserId, fare), ct);
            await shares.ExpireForTripAsync(trip.Id, now, ct);
            await db.SaveChangesAsync(ct);
        }, ct);

        await paymentService.PublishPendingAsync(ct);
        await RefreshReliabilityAsync(participants.PassengerUserId, Role.Passenger, ct);
        await RefreshReliabilityAsync(driver.UserId, Role.Driver, ct);
        return await reads.PublishAsync(trip, TripViewer.Driver, lang, ct);
    }

    /// <summary>F14: goes through the cancellation engine (reason catalogue, penalty points, <c>expectedPenaltyPoints</c> guard).</summary>
    public async Task<TripDto> CancelAsync(Guid tripId, CancelTripRequest request, Language lang, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.ReasonCode), request.ReasonCode, 60)
            .Rule(nameof(request.Note), request.Note is null || request.Note.Length <= 500, "max_length:500")
            .Rule(nameof(request.ExpectedPenaltyPoints), request.ExpectedPenaltyPoints is null or >= 0, "must be positive")
            .ThrowIfInvalid();

        var (trip, driver) = await LoadOwnAsync(tripId, ct);
        await cancellations.CancelAsync(trip, new Cancellation.CancelCommand(TripActor.Driver, driver.UserId, request.ReasonCode!.Trim(), request.Note,
            ExpectedPenaltyPoints: request.ExpectedPenaltyPoints), ct);
        return await reads.PublishAsync(trip, TripViewer.Driver, lang, ct);
    }

    /// <summary>Incremental reliability refresh (F14); a failure never breaks the trip flow.</summary>
    private async Task RefreshReliabilityAsync(Guid userId, Role role, CancellationToken ct)
    {
        try
        {
            await reliability.RefreshAsync(userId, role, ct);
        }
        catch (DbUpdateException)
        {
            // A concurrent refresh created the profile first; the next event or the nightly job recomputes it.
        }
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
