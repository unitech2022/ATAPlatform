using ATA.Api.Common;
using ATA.Api.Modules.Notifications;
using ATA.Api.Modules.Payments;
using ATA.Api.Modules.Pricing;
using ATA.Api.Modules.Trips;
using ATA.Domain.Common;
using ATA.Domain.Drivers;
using ATA.Domain.Notifications;
using ATA.Domain.Payments;
using ATA.Domain.Scheduling;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Scheduling;

/// <summary>
/// The scheduled-ride state machine of doc 11 §F17.3: reservations (reserve, first and final confirmation, release), the re-match after a driver cancellation / no-show, the timeline
/// passes run by <c>ScheduledRideWorker</c> (confirmation requests and timeouts, the start of the normal search, driver no-show, favourite window) and the due reminders
/// sent by <c>ScheduledReminderJob</c>. Penalty points live on the reservation row and reach the driver's reliability profile through <c>ReliabilityService</c>.
/// </summary>
public sealed class ScheduledRideEngine(
    AtaDbContext db,
    IClock clock,
    ScheduleRuleProvider rules,
    ScheduledReminderService reminders,
    INotificationDispatcher notifications,
    TripEventRecorder events,
    TripReadService reads,
    Cancellation.ReliabilityService reliability,
    CardTripPaymentService cardPayments,
    Safety.TripShareService shares,
    Favorites.FavoriteDiscountService favoriteDiscounts,
    IPricingService pricing,
    ILogger<ScheduledRideEngine> logger)
{
    // ----- booking -----

    /// <summary>
    /// A new scheduled booking (the caller saves): rider reminders for the offsets that are still ahead, the <c>scheduled_booked</c> event, <c>scheduled.booked</c> for the
    /// passenger and <c>scheduled.favorite_request</c> for the requested favourite driver.
    /// </summary>
    public async Task OnBookedAsync(Trip trip, ScheduledRideRule rule, Guid passengerUserId, string? passengerFullName, CancellationToken ct)
    {
        var scheduledAt = trip.ScheduledAt!.Value;
        reminders.PlanRider(trip, rule, passengerUserId);
        events.Add(trip.Id, TripEventTypes.ScheduledBooked, TripActor.System, data: new
        {
            scheduledAt, searchStartsAt = rule.SearchStartsAt(scheduledAt), freeCancelUntil = rule.FreeCancelUntil(scheduledAt), maxOpenPerPassenger = rule.MaxOpenPerPassenger,
        });
        await notifications.DispatchAsync(ScheduledNotifications.Booked(trip, passengerUserId), ct);
        if (trip.FavoriteDriverId is { } favoriteId && trip.FavoriteStatus == FavoriteStatus.Requested
            && await db.Drivers.AsNoTracking().Where(d => d.Id == favoriteId).Select(d => (Guid?)d.UserId).FirstOrDefaultAsync(ct) is { } favoriteUserId)
        {
            await notifications.DispatchAsync(ScheduledNotifications.FavoriteRequest(trip, favoriteUserId, Ratings.RatingService.FirstName(passengerFullName)), ct);
        }
    }

    // ----- reservations -----

    public Task<ScheduledRideReservation?> ActiveReservationAsync(Guid tripId, CancellationToken ct) =>
        db.ScheduledRideReservations.FirstOrDefaultAsync(r => r.TripId == tripId && (r.Status == ReservationStatus.Reserved || r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.Assigned), ct);

    /// <summary>
    /// Reserves a scheduled trip for a driver: at most <c>max_reservations_per_driver</c> active ones (<paramref name="enforceLimit"/>, not for admins), no overlap of
    /// <c>[T, T + duration + reservation_gap_minutes]</c> with the driver's other reservations, and an atomic claim of <c>trips.reserved_driver_id</c> so only one of two
    /// concurrent drivers wins (<c>409 reservation_taken</c>). Reserving after <c>T − driver_assignment_lead_minutes</c> is confirmed at once; inside the final window the final
    /// confirmation is requested immediately.
    /// </summary>
    public async Task<(ScheduledRideReservation Reservation, Trip Trip)> ReserveAsync(
        Guid tripId, DriverProfile driver, ReservationSource source, ScheduledRideRule rule, bool enforceLimit, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var snapshot = await db.Trips.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tripId && t.BookingType == BookingType.Scheduled, ct) ?? throw new DomainException(ErrorCodes.NotFound);
        if (snapshot.Status != TripStatus.Scheduled)
        {
            throw new DomainException(ErrorCodes.Conflict, new { status = snapshot.Status });
        }

        if (snapshot.ReservedDriverId is not null)
        {
            throw new DomainException(ErrorCodes.ReservationTaken);
        }

        var scheduledAt = snapshot.ScheduledAt!.Value;
        if (now >= rule.SearchStartsAt(scheduledAt))
        {
            throw new DomainException(ErrorCodes.Conflict, new { reason = "reservation_window_closed" });
        }

        if (enforceLimit)
        {
            var active = await db.ScheduledRideReservations.CountAsync(r => r.DriverId == driver.Id
                && (r.Status == ReservationStatus.Reserved || r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.Assigned), ct);
            if (active >= rule.MaxReservationsPerDriver)
            {
                throw new DomainException(ErrorCodes.ReservationLimitReached, new { max = rule.MaxReservationsPerDriver });
            }
        }

        await EnsureNoOverlapAsync(driver.Id, snapshot, rule, ct);

        ScheduledRideReservation reservation = null!;
        Trip trip = null!;
        var driverName = await db.Users.AsNoTracking().Where(u => u.Id == driver.UserId).Select(u => u.FullName).FirstOrDefaultAsync(ct);
        await db.InTransactionAsync(async () =>
        {
            var claimed = await db.Trips.Where(t => t.Id == tripId && t.Status == TripStatus.Scheduled && t.ReservedDriverId == null)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.ReservedDriverId, driver.Id), ct);
            if (claimed == 0)
            {
                throw new DomainException(ErrorCodes.ReservationTaken);
            }

            trip = await reads.FindAsync(tripId, ct) ?? throw new DomainException(ErrorCodes.NotFound);
            var effectiveSource = trip.FavoriteDriverId == driver.Id && source != ReservationSource.Admin ? ReservationSource.Favorite : source;
            reservation = new ScheduledRideReservation { TripId = tripId, DriverId = driver.Id, Source = effectiveSource, Status = ReservationStatus.Reserved, ReservedAt = now };
            if (now >= rule.FirstConfirmationAt(scheduledAt))
            {
                reservation.Status = ReservationStatus.Confirmed;
                reservation.ConfirmedAt = now;
                if (now >= rule.FinalConfirmationAt(scheduledAt))
                {
                    reservation.FinalConfirmRequestedAt = now;
                    await notifications.DispatchAsync(ScheduledNotifications.ConfirmRequest(trip, driver.UserId, rule.FinalConfirmationTimeoutMinutes, final: true), ct);
                }
            }

            db.ScheduledRideReservations.Add(reservation);
            reminders.PlanDriver(trip, reservation, rule, driver.UserId);
            events.Add(trip.Id, TripEventTypes.DriverReserved, source == ReservationSource.Admin ? TripActor.Admin : TripActor.Driver, driver.UserId,
                data: new { reservationId = reservation.Id, driverId = driver.Id, source = reservation.Source, status = reservation.Status });
            var passengerUserId = await PassengerUserIdAsync(trip, ct);
            await notifications.DispatchAsync(ScheduledNotifications.DriverReserved(trip, passengerUserId, driverName), ct);
            await db.SaveChangesAsync(ct);
        }, ct);

        await reads.PublishAsync(trip, TripViewer.Admin, Language.Ar, ct);
        return (reservation, trip);
    }

    /// <summary>
    /// Executes the confirmation that is due: the first one (<c>reserved → confirmed</c>) or the final one (<c>confirmed → assigned</c>, which assigns the driver to the trip
    /// and continues as an ordinary F8 trip). <c>409 reservation_not_confirmable { reason }</c> with <c>not_due</c>, <c>expired</c>, and for the final one <c>offline</c> / <c>on_trip</c>.
    /// </summary>
    public async Task<(ScheduledRideReservation Reservation, Trip? AssignedTrip)> ConfirmAsync(Guid tripId, DriverProfile driver, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var reservation = await db.ScheduledRideReservations.FirstOrDefaultAsync(r => r.TripId == tripId && r.DriverId == driver.Id
            && (r.Status == ReservationStatus.Reserved || r.Status == ReservationStatus.Confirmed), ct) ?? throw new DomainException(ErrorCodes.NotFound);
        var trip = await reads.FindAsync(tripId, ct) ?? throw new DomainException(ErrorCodes.NotFound);
        if (trip.Status != TripStatus.Scheduled)
        {
            throw new DomainException(ErrorCodes.ReservationNotConfirmable, new { reason = "not_due" });
        }

        var rule = await rules.ResolveForTripAsync(trip, ct);
        var scheduledAt = trip.ScheduledAt!.Value;
        if (reservation.Status == ReservationStatus.Reserved)
        {
            if (reservation.ConfirmDeadline(rule) is not { } deadline)
            {
                throw new DomainException(ErrorCodes.ReservationNotConfirmable, new { reason = "not_due" });
            }

            if (now > deadline)
            {
                throw new DomainException(ErrorCodes.ReservationNotConfirmable, new { reason = "expired" });
            }

            reservation.Status = ReservationStatus.Confirmed;
            reservation.ConfirmedAt = now;
            events.Add(trip.Id, TripEventTypes.ReservationConfirmed, TripActor.Driver, driver.UserId, data: new { reservationId = reservation.Id, driverId = driver.Id });
            if (now >= rule.FinalConfirmationAt(scheduledAt) && reservation.FinalConfirmRequestedAt is null)
            {
                await RequestFinalConfirmationAsync(reservation, trip, rule, driver.UserId, now, ct);
            }

            await db.SaveChangesAsync(ct);
            return (reservation, null);
        }

        if (reservation.FinalConfirmDeadline(rule) is not { } finalDeadline)
        {
            throw new DomainException(ErrorCodes.ReservationNotConfirmable, new { reason = "not_due" });
        }

        if (now > finalDeadline)
        {
            throw new DomainException(ErrorCodes.ReservationNotConfirmable, new { reason = "expired" });
        }

        if (!driver.IsOnline)
        {
            throw new DomainException(ErrorCodes.ReservationNotConfirmable, new { reason = "offline" });
        }

        if (driver.CurrentTripId is not null)
        {
            throw new DomainException(ErrorCodes.ReservationNotConfirmable, new { reason = "on_trip" });
        }

        await AssignAsync(trip, reservation, driver, now, ct);
        return (reservation, trip);
    }

    /// <summary>
    /// Releases a reservation that has not been assigned: free before <c>T − driver_free_release_minutes_before</c>, later it is a late release with
    /// <c>driver_late_release_penalty_points</c>. The trip goes back to the marketplace and the passenger is told.
    /// </summary>
    public async Task<ScheduledRideReservation> DriverReleaseAsync(Guid tripId, DriverProfile driver, string? note, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var reservation = await db.ScheduledRideReservations.FirstOrDefaultAsync(r => r.TripId == tripId && r.DriverId == driver.Id
            && (r.Status == ReservationStatus.Reserved || r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.Assigned), ct) ?? throw new DomainException(ErrorCodes.NotFound);
        if (reservation.Status == ReservationStatus.Assigned)
        {
            throw new DomainException(ErrorCodes.Conflict, new { status = reservation.Status, useTripCancel = true });
        }

        var trip = await reads.FindAsync(tripId, ct) ?? throw new DomainException(ErrorCodes.NotFound);
        var rule = await rules.ResolveForTripAsync(trip, ct);
        var late = now >= rule.FreeReleaseUntil(trip.ScheduledAt!.Value);
        var points = late ? rule.DriverLateReleasePenaltyPoints : 0;
        MarkReleased(trip, reservation, ReservationReleaseReason.DriverReleased, points, late, TripActor.Driver, driver.UserId, now, note);
        await reminders.CancelForReservationAsync(reservation.Id, ct);
        var passengerUserId = await PassengerUserIdAsync(trip, ct);
        await notifications.DispatchAsync(ScheduledNotifications.ReservationReleased(trip, passengerUserId, ReservationReleaseReason.DriverReleased), ct);
        await db.SaveChangesAsync(ct);
        await RefreshReliabilityAsync(driver.UserId, points > 0 || late, ct);
        await reads.PublishAsync(trip, TripViewer.Admin, Language.Ar, ct);
        return reservation;
    }

    /// <summary>Admin release (no points): a pre-assignment reservation goes back to the marketplace, an assigned one re-matches the trip. Saves.</summary>
    public async Task<ScheduledRideReservation> AdminReleaseAsync(Guid tripId, Guid adminUserId, string? reason, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var reservation = await ActiveReservationAsync(tripId, ct) ?? throw new DomainException(ErrorCodes.NotFound);
        var trip = await reads.FindAsync(tripId, ct) ?? throw new DomainException(ErrorCodes.NotFound);
        var driverUserId = await DriverUserIdAsync(reservation.DriverId, ct);
        var passengerUserId = await PassengerUserIdAsync(trip, ct);
        if (reservation.Status == ReservationStatus.Assigned)
        {
            await RematchAsync(trip, reservation, ReservationReleaseReason.Admin, 0, false, TripActor.Admin, adminUserId, now, reason, ct);
            await notifications.DispatchAsync(ScheduledNotifications.ReservationReleased(trip, driverUserId, ReservationReleaseReason.Admin), ct);
        }
        else
        {
            MarkReleased(trip, reservation, ReservationReleaseReason.Admin, 0, false, TripActor.Admin, adminUserId, now, reason);
            await reminders.CancelForReservationAsync(reservation.Id, ct);
            await notifications.DispatchAsync(ScheduledNotifications.ReservationReleased(trip, driverUserId, ReservationReleaseReason.Admin), ct);
            await notifications.DispatchAsync(ScheduledNotifications.ReservationReleased(trip, passengerUserId, ReservationReleaseReason.Admin), ct);
        }

        await db.SaveChangesAsync(ct);
        await reads.PublishAsync(trip, TripViewer.Admin, Language.Ar, ct);
        return reservation;
    }

    /// <summary>
    /// A driver who cancels an assigned scheduled trip does not cancel the rider's trip: it goes back to matching (<c>Trip.Reassign()</c>) and the driver gets the penalty
    /// points of the F14 rule of the stage (<paramref name="points"/>, 0 for an excusable reason awaiting review). Saves.
    /// </summary>
    public async Task RematchOnDriverCancelAsync(Trip trip, Guid driverUserId, int points, string reasonCode, string? note, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var reservation = await ActiveReservationAsync(trip.Id, ct);
        await RematchAsync(trip, reservation, ReservationReleaseReason.DriverReleased, points, true, TripActor.Driver, driverUserId, now, note ?? reasonCode, ct);
        await db.SaveChangesAsync(ct);
        await RefreshReliabilityAsync(driverUserId, true, ct);
        await reads.PublishAsync(trip, TripViewer.Admin, Language.Ar, ct);
    }

    /// <summary>The trip ended (cancelled / no drivers → the reservation is <c>cancelled</c> without points; completed → <c>completed</c>) and its pending reminders are cancelled; the caller saves.</summary>
    public async Task OnTripEndedAsync(Trip trip, bool completed, DateTime now, CancellationToken ct)
    {
        if (!trip.IsScheduledBooking)
        {
            return;
        }

        await reminders.CancelForTripAsync(trip.Id, ct);
        var reservation = db.ScheduledRideReservations.Local.FirstOrDefault(r => r.TripId == trip.Id && r.IsActive) ?? await ActiveReservationAsync(trip.Id, ct);
        if (reservation is null)
        {
            return;
        }

        if (completed)
        {
            reservation.Status = ReservationStatus.Completed;
            return;
        }

        reservation.Status = ReservationStatus.Cancelled;
        reservation.ReleaseReason = ReservationReleaseReason.TripCancelled;
        reservation.ReleasedAt = now;
        trip.ReservedDriverId = null;
    }

    // ----- final confirmation → assignment -----

    private async Task AssignAsync(Trip trip, ScheduledRideReservation reservation, DriverProfile driver, DateTime now, CancellationToken ct)
    {
        var passengerUserId = await PassengerUserIdAsync(trip, ct);
        // Card trips are authorized now (gateway calls never run inside the unit of work); a decline falls back to cash.
        await EnsureCardAuthorizedAsync(trip, passengerUserId, ct);

        var vehicle = await db.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.DriverId == driver.Id && v.IsActive, ct);
        var location = await db.DriverLocations.AsNoTracking().FirstOrDefaultAsync(l => l.DriverId == driver.Id, ct);
        var etaSeconds = location is null ? 60 : pricing.EtaSeconds(Geo.HaversineMeters(location.Lat, location.Lng, trip.PickupLat, trip.PickupLng) * FlatPricing.RoadFactor);
        var driverName = await db.Users.AsNoTracking().Where(u => u.Id == driver.UserId).Select(u => u.FullName).FirstOrDefaultAsync(ct);
        var favoriteFallback = false;
        await db.InTransactionAsync(async () =>
        {
            trip.Assign(driver.Id, vehicle?.Id, now);
            driver.CurrentTripId = trip.Id;
            driver.AcceptanceCount++;
            reservation.Status = ReservationStatus.Assigned;
            reservation.AssignedAt = now;
            await db.DriverLocations.Where(l => l.DriverId == driver.Id).ExecuteUpdateAsync(s => s.SetProperty(l => l.CurrentTripId, trip.Id), ct);
            events.Add(trip.Id, TripEventTypes.DriverAssigned, TripActor.System, data: new { driverId = driver.Id, vehicleId = vehicle?.Id, etaSeconds, scheduled = true, reservationId = reservation.Id });
            // F16: the requested favourite driver took the scheduled trip → the discount rule is pinned (the discount applies at completion).
            if (trip.FavoriteDriverId == driver.Id && trip.FavoriteStatus is FavoriteStatus.Requested or FavoriteStatus.Unavailable)
            {
                var rule = await favoriteDiscounts.AcceptAsync(trip, ct);
                events.Add(trip.Id, TripEventTypes.FavoriteAccepted, TripActor.System, data: new { driverId = driver.Id, discountRuleId = rule?.Id, estimatedFare = trip.EstimatedFare });
            }
            else if (trip.FavoriteDriverId is not null && trip.FavoriteDriverId != driver.Id && trip.FavoriteStatus is FavoriteStatus.Requested)
            {
                trip.FavoriteStatus = FavoriteStatus.Unavailable;
                favoriteFallback = true;
            }

            await notifications.DispatchAsync(TripNotifications.DriverAssigned(trip, passengerUserId, driverName, vehicle, etaSeconds), ct);
            if (favoriteFallback)
            {
                await notifications.DispatchAsync(TripNotifications.FavoriteFallback(trip, passengerUserId, driverName), ct);
            }

            await shares.AutoShareOnAssignAsync(trip, passengerUserId, ct);
            await db.SaveChangesAsync(ct);
        }, ct);
        await RefreshReliabilityAsync(driver.UserId, true, ct);
        await reads.PublishAsync(trip, TripViewer.Driver, Language.Ar, ct);
    }

    // ----- worker pass -----

    /// <summary>
    /// One pass of <c>ScheduledRideWorker</c> (every 30 s): first / final confirmation requests and their timeouts, the start of the normal search, the driver no-show and the
    /// end of the favourite window. Returns the number of state changes.
    /// </summary>
    public async Task<int> RunWorkerPassAsync(CancellationToken ct)
    {
        var count = 0;
        count += await RequestFirstConfirmationsAsync(ct);
        count += await ExpireFirstConfirmationsAsync(ct);
        count += await RequestFinalConfirmationsAsync(ct);
        count += await ExpireFinalConfirmationsAsync(ct);
        count += await StartSearchesAsync(ct);
        count += await DetectDriverNoShowsAsync(ct);
        count += await MarkFavoriteWindowsAsync(ct);
        return count;
    }

    private async Task<int> RequestFirstConfirmationsAsync(CancellationToken ct)
    {
        var ids = await db.ScheduledRideReservations.AsNoTracking().Where(r => r.Status == ReservationStatus.Reserved && r.ConfirmRequestedAt == null).Select(r => r.Id).ToListAsync(ct);
        return await EachAsync(ids, "first confirmation request", async id =>
        {
            var (reservation, trip, rule) = await LoadAsync(id, ct);
            var now = clock.UtcNow;
            if (trip is null || trip.Status != TripStatus.Scheduled || now < rule.FirstConfirmationAt(trip.ScheduledAt!.Value))
            {
                return false;
            }

            reservation.ConfirmRequestedAt = now;
            var driverUserId = await DriverUserIdAsync(reservation.DriverId, ct);
            await notifications.DispatchAsync(ScheduledNotifications.ConfirmRequest(trip, driverUserId, rule.ConfirmationTimeoutMinutes, final: false), ct);
            await reminders.MarkConfirmationSentAsync(reservation.Id, ReminderKind.ConfirmRequest, now, ct);
            await db.SaveChangesAsync(ct);
            return true;
        }, ct);
    }

    private async Task<int> ExpireFirstConfirmationsAsync(CancellationToken ct)
    {
        var ids = await db.ScheduledRideReservations.AsNoTracking().Where(r => r.Status == ReservationStatus.Reserved && r.ConfirmRequestedAt != null).Select(r => r.Id).ToListAsync(ct);
        return await EachAsync(ids, "first confirmation timeout", async id =>
        {
            var (reservation, trip, rule) = await LoadAsync(id, ct);
            var now = clock.UtcNow;
            if (trip is null || reservation.ConfirmDeadline(rule) is not { } deadline || deadline > now)
            {
                return false;
            }

            var driverUserId = await DriverUserIdAsync(reservation.DriverId, ct);
            MarkReleased(trip, reservation, ReservationReleaseReason.ConfirmationMissed, rule.DriverConfirmationMissedPenaltyPoints, false, TripActor.System, null, now, null);
            await reminders.CancelForReservationAsync(reservation.Id, ct);
            var passengerUserId = await PassengerUserIdAsync(trip, ct);
            await notifications.DispatchAsync(ScheduledNotifications.ReservationReleased(trip, driverUserId, ReservationReleaseReason.ConfirmationMissed), ct);
            await notifications.DispatchAsync(ScheduledNotifications.ReservationReleased(trip, passengerUserId, ReservationReleaseReason.ConfirmationMissed), ct);
            await db.SaveChangesAsync(ct);
            await RefreshReliabilityAsync(driverUserId, true, ct);
            await reads.PublishAsync(trip, TripViewer.Admin, Language.Ar, ct);
            return true;
        }, ct);
    }

    private async Task<int> RequestFinalConfirmationsAsync(CancellationToken ct)
    {
        var ids = await db.ScheduledRideReservations.AsNoTracking().Where(r => r.Status == ReservationStatus.Confirmed && r.FinalConfirmRequestedAt == null).Select(r => r.Id).ToListAsync(ct);
        return await EachAsync(ids, "final confirmation request", async id =>
        {
            var (reservation, trip, rule) = await LoadAsync(id, ct);
            var now = clock.UtcNow;
            if (trip is null || trip.Status != TripStatus.Scheduled || now < rule.FinalConfirmationAt(trip.ScheduledAt!.Value))
            {
                return false;
            }

            await RequestFinalConfirmationAsync(reservation, trip, rule, await DriverUserIdAsync(reservation.DriverId, ct), now, ct);
            await db.SaveChangesAsync(ct);
            return true;
        }, ct);
    }

    private async Task<int> ExpireFinalConfirmationsAsync(CancellationToken ct)
    {
        var ids = await db.ScheduledRideReservations.AsNoTracking().Where(r => r.Status == ReservationStatus.Confirmed && r.FinalConfirmRequestedAt != null).Select(r => r.Id).ToListAsync(ct);
        return await EachAsync(ids, "final confirmation timeout", async id =>
        {
            var (reservation, trip, rule) = await LoadAsync(id, ct);
            var now = clock.UtcNow;
            if (trip is null || trip.Status != TripStatus.Scheduled || reservation.FinalConfirmDeadline(rule) is not { } deadline || deadline > now)
            {
                return false;
            }

            var driverUserId = await DriverUserIdAsync(reservation.DriverId, ct);
            var passengerUserId = await PassengerUserIdAsync(trip, ct);
            MarkReleased(trip, reservation, ReservationReleaseReason.FinalConfirmationMissed, rule.DriverConfirmationMissedPenaltyPoints, false, TripActor.System, null, now, null);
            await reminders.CancelForReservationAsync(reservation.Id, ct);
            await StartSearchAsync(trip, passengerUserId, "final_confirmation_missed", ct);
            events.Add(trip.Id, TripEventTypes.Rematched, TripActor.System, data: new { reason = "final_confirmation_missed", driverId = reservation.DriverId });
            await notifications.DispatchAsync(ScheduledNotifications.ReservationReleased(trip, driverUserId, ReservationReleaseReason.FinalConfirmationMissed), ct);
            await notifications.DispatchAsync(ScheduledNotifications.Rematched(trip, passengerUserId), ct);
            await db.SaveChangesAsync(ct);
            await RefreshReliabilityAsync(driverUserId, true, ct);
            await reads.PublishAsync(trip, TripViewer.Admin, Language.Ar, ct);
            return true;
        }, ct);
    }

    /// <summary>
    /// <c>T − search_start_minutes_before</c> without a driver in the confirmation process: <c>scheduled → searching</c> (the matcher takes over; card authorization happens now).
    /// A reservation that is still pending confirmation keeps the trip until its deadline; at <c>T</c> it is released.
    /// </summary>
    private async Task<int> StartSearchesAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;
        var ruleRows = await db.ScheduledRideRules.AsNoTracking().Where(r => r.IsActive).ToListAsync(ct);
        var maxLead = Math.Max(ScheduledRideRule.Default().SearchStartMinutesBefore, ruleRows.Count == 0 ? 0 : ruleRows.Max(r => r.SearchStartMinutesBefore));
        var horizon = now.AddMinutes(maxLead);
        var ids = await db.Trips.AsNoTracking().Where(t => t.Status == TripStatus.Scheduled && t.BookingType == BookingType.Scheduled && t.ScheduledAt != null && t.ScheduledAt <= horizon)
            .OrderBy(t => t.ScheduledAt).Select(t => t.Id).ToListAsync(ct);
        return await EachAsync(ids, "search start", async id =>
        {
            var trip = await reads.FindAsync(id, ct);
            if (trip is null || trip.Status != TripStatus.Scheduled)
            {
                return false;
            }

            var rule = await rules.ResolveForTripAsync(trip, ct);
            var scheduledAt = trip.ScheduledAt!.Value;
            if (now < rule.SearchStartsAt(scheduledAt))
            {
                return false;
            }

            var reservation = await ActiveReservationAsync(id, ct);
            var passengerUserId = await PassengerUserIdAsync(trip, ct);
            Guid? releasedDriverUserId = null;
            if (reservation is not null)
            {
                if (now < scheduledAt)
                {
                    return false;
                }

                releasedDriverUserId = await DriverUserIdAsync(reservation.DriverId, ct);
                MarkReleased(trip, reservation, ReservationReleaseReason.FinalConfirmationMissed, rule.DriverConfirmationMissedPenaltyPoints, false, TripActor.System, null, now, "pickup_time_reached");
                await reminders.CancelForReservationAsync(reservation.Id, ct);
                await notifications.DispatchAsync(ScheduledNotifications.ReservationReleased(trip, releasedDriverUserId.Value, ReservationReleaseReason.FinalConfirmationMissed), ct);
            }

            await StartSearchAsync(trip, passengerUserId, reservation is null ? "scheduled_window" : "pickup_time_reached", ct);
            await db.SaveChangesAsync(ct);
            if (releasedDriverUserId is { } driverUserId)
            {
                await RefreshReliabilityAsync(driverUserId, true, ct);
            }

            await reads.PublishAsync(trip, TripViewer.Admin, Language.Ar, ct);
            return true;
        }, ct);
    }

    /// <summary>
    /// An assigned scheduled trip whose driver did not arrive (<c>arrived_at</c> empty) by <c>T + driver_no_show_grace_minutes</c>: the reservation becomes <c>no_show</c> with
    /// <c>driver_no_show_penalty_points</c> (no <c>cancellation_events</c> row) and the trip is re-matched.
    /// </summary>
    private async Task<int> DetectDriverNoShowsAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;
        var ids = await db.Trips.AsNoTracking().Where(t => t.BookingType == BookingType.Scheduled && (t.Status == TripStatus.DriverAssigned || t.Status == TripStatus.DriverEnRoute)
            && t.ArrivedAt == null && t.ScheduledAt != null && t.ScheduledAt <= now).Select(t => t.Id).ToListAsync(ct);
        return await EachAsync(ids, "driver no-show", async id =>
        {
            var trip = await reads.FindAsync(id, ct);
            if (trip is null || trip.DriverId is null || trip.Status is not (TripStatus.DriverAssigned or TripStatus.DriverEnRoute) || trip.ArrivedAt is not null)
            {
                return false;
            }

            var rule = await rules.ResolveForTripAsync(trip, ct);
            if (now < trip.ScheduledAt!.Value.AddMinutes(rule.DriverNoShowGraceMinutes))
            {
                return false;
            }

            var driverUserId = await DriverUserIdAsync(trip.DriverId.Value, ct);
            var reservation = await ActiveReservationAsync(id, ct);
            await RematchAsync(trip, reservation, ReservationReleaseReason.NoShow, rule.DriverNoShowPenaltyPoints, true, TripActor.System, null, now, null, ct);
            await notifications.DispatchAsync(ScheduledNotifications.ReservationReleased(trip, driverUserId, ReservationReleaseReason.NoShow), ct);
            await db.SaveChangesAsync(ct);
            await RefreshReliabilityAsync(driverUserId, true, ct);
            await reads.PublishAsync(trip, TripViewer.Admin, Language.Ar, ct);
            return true;
        }, ct);
    }

    /// <summary>The exclusive window of a requested favourite driver ended without a reservation: the marketplace is open to everybody (recorded once as a trip event).</summary>
    private async Task<int> MarkFavoriteWindowsAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;
        var ids = await db.Trips.AsNoTracking().Where(t => t.Status == TripStatus.Scheduled && t.FavoriteDriverId != null && t.ReservedDriverId == null).Select(t => t.Id).ToListAsync(ct);
        return await EachAsync(ids, "favourite window", async id =>
        {
            var trip = await reads.FindAsync(id, ct);
            if (trip is null || await db.TripEvents.AnyAsync(e => e.TripId == id && e.Type == TripEventTypes.FavoriteWindowExpired, ct))
            {
                return false;
            }

            var rule = await rules.ResolveForTripAsync(trip, ct);
            if (now < ExclusiveUntil(trip, rule))
            {
                return false;
            }

            events.Add(trip.Id, TripEventTypes.FavoriteWindowExpired, TripActor.System, data: new { driverId = trip.FavoriteDriverId });
            await db.SaveChangesAsync(ct);
            return true;
        }, ct);
    }

    /// <summary>End of the favourite driver's exclusive marketplace window: <c>booking + favorite_exclusive_minutes</c>, never later than the start of the normal search.</summary>
    public static DateTime ExclusiveUntil(Trip trip, ScheduledRideRule rule)
    {
        var end = trip.RequestedAt.AddMinutes(rule.FavoriteExclusiveMinutes);
        var searchStart = rule.SearchStartsAt(trip.ScheduledAt!.Value);
        return end < searchStart ? end : searchStart;
    }

    // ----- reminders -----

    /// <summary>
    /// <c>ScheduledReminderJob</c>: sends the pending rows whose <c>send_at</c> passed via <c>scheduled.reminder</c> (rows late by more than 10 minutes are <c>skipped</c>, rows of
    /// a trip / reservation that ended are <c>cancelled</c>); confirmation rows the worker already handled are marked sent. Returns the number of rows processed.
    /// </summary>
    public async Task<int> SendDueRemindersAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;
        var ids = await db.ScheduledRideReminders.AsNoTracking().Where(r => r.Status == ReminderStatus.Pending && r.SendAt <= now).OrderBy(r => r.SendAt).Select(r => r.Id).Take(500).ToListAsync(ct);
        return await EachAsync(ids, "reminder", async id =>
        {
            var row = await db.ScheduledRideReminders.FirstAsync(r => r.Id == id, ct);
            var trip = await reads.FindAsync(row.TripId, ct);
            var reservation = row.ReservationId is { } reservationId ? await db.ScheduledRideReservations.FirstOrDefaultAsync(r => r.Id == reservationId, ct) : null;
            if (!IsStillRelevant(row, trip, reservation))
            {
                row.Status = ReminderStatus.Cancelled;
                await db.SaveChangesAsync(ct);
                return true;
            }

            if ((now - row.SendAt).TotalMinutes > ScheduledRideRule.ReminderLateSkipMinutes)
            {
                row.Status = ReminderStatus.Skipped;
                await db.SaveChangesAsync(ct);
                return true;
            }

            switch (row.Kind)
            {
                case ReminderKind.Reminder:
                    await notifications.DispatchAsync(ScheduledNotifications.Reminder(trip!, row.RecipientUserId, row.OffsetMinutes), ct);
                    break;
                case ReminderKind.ConfirmRequest when reservation is { ConfirmRequestedAt: null }:
                {
                    var rule = await rules.ResolveForTripAsync(trip!, ct);
                    reservation.ConfirmRequestedAt = now;
                    await notifications.DispatchAsync(ScheduledNotifications.ConfirmRequest(trip!, row.RecipientUserId, rule.ConfirmationTimeoutMinutes, final: false), ct);
                    break;
                }

                case ReminderKind.FinalConfirmRequest when reservation is { FinalConfirmRequestedAt: null }:
                {
                    var rule = await rules.ResolveForTripAsync(trip!, ct);
                    await RequestFinalConfirmationAsync(reservation, trip!, rule, row.RecipientUserId, now, ct);
                    break;
                }
            }

            row.Status = ReminderStatus.Sent;
            row.SentAt = now;
            await db.SaveChangesAsync(ct);
            return true;
        }, ct);
    }

    private static bool IsStillRelevant(ScheduledRideReminder row, Trip? trip, ScheduledRideReservation? reservation)
    {
        if (trip is null || trip.IsTerminal || trip.Status == TripStatus.InTrip)
        {
            return false;
        }

        return row.RecipientRole switch
        {
            ReminderRecipientRole.Passenger => true,
            _ => reservation is not null && reservation.IsActive && (row.Kind switch
            {
                ReminderKind.ConfirmRequest => reservation.Status == ReservationStatus.Reserved,
                ReminderKind.FinalConfirmRequest => reservation.Status == ReservationStatus.Confirmed,
                _ => true,
            }),
        };
    }

    // ----- shared transitions -----

    private async Task RequestFinalConfirmationAsync(ScheduledRideReservation reservation, Trip trip, ScheduledRideRule rule, Guid driverUserId, DateTime now, CancellationToken ct)
    {
        reservation.FinalConfirmRequestedAt = now;
        await notifications.DispatchAsync(ScheduledNotifications.ConfirmRequest(trip, driverUserId, rule.FinalConfirmationTimeoutMinutes, final: true), ct);
        await reminders.MarkConfirmationSentAsync(reservation.Id, ReminderKind.FinalConfirmRequest, now, ct);
    }

    /// <summary>The reservation ends without an assignment (<c>released</c> / <c>no_show</c>): status, reason, points, the trip's reserved driver and the favourite request.</summary>
    private void MarkReleased(Trip trip, ScheduledRideReservation reservation, ReservationReleaseReason reason, int points, bool late, TripActor actor, Guid? actorUserId, DateTime now, string? note)
    {
        reservation.Status = reason == ReservationReleaseReason.NoShow ? ReservationStatus.NoShow : ReservationStatus.Released;
        reservation.ReleaseReason = reason;
        reservation.ReleasedAt = now;
        reservation.PenaltyPoints = points;
        reservation.IsLateRelease = late;
        trip.ReservedDriverId = null;
        events.Add(trip.Id, TripEventTypes.ReservationReleased, actor, actorUserId,
            data: new { reservationId = reservation.Id, driverId = reservation.DriverId, reason, penaltyPoints = points, isLateRelease = late, note });
        if (reservation.DriverId == trip.FavoriteDriverId && trip.FavoriteStatus == FavoriteStatus.Requested)
        {
            trip.FavoriteStatus = FavoriteStatus.Unavailable;
            events.Add(trip.Id, TripEventTypes.FavoriteUnavailable, TripActor.System, data: new { driverId = trip.FavoriteDriverId, reason = "reservation_released" });
        }
    }

    /// <summary>
    /// <c>driver_assigned / driver_en_route → searching</c> for a scheduled trip (doc 11 §F17.1): the driver is released, the reservation ends with
    /// <paramref name="reason"/> (a reservation row is created for drivers who took the trip through normal matching so their fault is recorded), <c>Trip.Reassign()</c>
    /// clears the assignment and the rider is told a new driver is being found. The caller saves.
    /// </summary>
    private async Task RematchAsync(Trip trip, ScheduledRideReservation? reservation, ReservationReleaseReason reason, int points, bool late, TripActor actor, Guid? actorUserId,
        DateTime now, string? note, CancellationToken ct)
    {
        var driverId = trip.DriverId ?? throw new DomainException(ErrorCodes.Conflict, new { status = trip.Status });
        var passengerUserId = await PassengerUserIdAsync(trip, ct);
        var wasFavorite = trip.FavoriteDriverId == driverId && trip.FavoriteStatus == FavoriteStatus.Accepted;
        await reads.ReleaseDriverAsync(trip, now, ct);
        if (reservation is null)
        {
            reservation = new ScheduledRideReservation
            {
                TripId = trip.Id, DriverId = driverId, Source = ReservationSource.Marketplace, Status = ReservationStatus.Assigned, ReservedAt = trip.AssignedAt ?? now, AssignedAt = trip.AssignedAt,
            };
            db.ScheduledRideReservations.Add(reservation);
        }

        MarkReleased(trip, reservation, reason, points, late, actor, actorUserId, now, note);
        await reminders.CancelForReservationAsync(reservation.Id, ct);
        events.Add(trip.Id, TripEventTypes.DriverUnassigned, actor, actorUserId, data: new { driverId, reason });
        if (reason == ReservationReleaseReason.NoShow)
        {
            events.Add(trip.Id, TripEventTypes.DriverNoShow, TripActor.System, data: new { driverId, penaltyPoints = points, scheduledAt = trip.ScheduledAt });
        }

        trip.Reassign();
        if (wasFavorite)
        {
            trip.FavoriteStatus = FavoriteStatus.Unavailable;
            await favoriteDiscounts.RevertAsync(trip, ct);
        }

        events.Add(trip.Id, TripEventTypes.Rematched, TripActor.System, data: new { reason, driverId });
        events.Add(trip.Id, TripEventTypes.SearchStarted, TripActor.System, data: new { reason = "rematch" });
        await notifications.DispatchAsync(ScheduledNotifications.Rematched(trip, passengerUserId), ct);
    }

    private async Task StartSearchAsync(Trip trip, Guid passengerUserId, string reason, CancellationToken ct)
    {
        await EnsureCardAuthorizedAsync(trip, passengerUserId, ct);
        trip.StartScheduledSearch();
        events.Add(trip.Id, TripEventTypes.SearchStarted, TripActor.System, data: new { reason });
    }

    /// <summary>
    /// Card trips are authorized when the search starts or the final confirmation assigns the driver (not at booking). A decline, an unavailable gateway or a 3-D Secure request
    /// (nobody is there to answer it) falls back to cash with <c>payment_fallback_cash (authorization_failed)</c> and <c>payment.failed</c>.
    /// </summary>
    private async Task EnsureCardAuthorizedAsync(Trip trip, Guid passengerUserId, CancellationToken ct)
    {
        if (trip.PaymentMethod != PaymentMethodKind.Card
            || await db.Payments.AnyAsync(p => p.TripId == trip.Id && p.Purpose == PaymentPurpose.Trip
                && (p.Status == PaymentStatus.Authorized || p.Status == PaymentStatus.Initiated || p.Status == PaymentStatus.Captured), ct))
        {
            return;
        }

        Payment? payment = null;
        string? failure = null;
        try
        {
            payment = await cardPayments.AuthorizeForTripAsync(trip, passengerUserId, trip.PaymentMethodId, null, ct);
        }
        catch (DomainException ex) when (ex.Code is ErrorCodes.PaymentFailed or ErrorCodes.PaymentProviderUnavailable or ErrorCodes.PaymentMethodExpired or ErrorCodes.NotFound or ErrorCodes.ValidationFailed)
        {
            failure = ex.Code;
        }

        if (payment is { Status: PaymentStatus.Initiated })
        {
            payment.MarkFailed("authentication_required", "The card needs an interactive verification that cannot be completed for a scheduled trip", clock.UtcNow);
            failure = "authentication_required";
        }

        if (failure is not null)
        {
            var requested = trip.PaymentMethod;
            trip.PaymentMethod = PaymentMethodKind.Cash;
            trip.PaymentMethodId = null;
            events.Add(trip.Id, TripEventTypes.PaymentFallbackCash, TripActor.System, data: new { requested, reason = "authorization_failed", failureCode = failure, amount = trip.EstimatedFare });
            await notifications.DispatchAsync(new NotificationRequest(NotificationTypes.PaymentFailed, passengerUserId,
                NotificationPlaceholders.Of().Money("amount", trip.EstimatedFare).Localized("reason", "سيتم الدفع نقداً للكابتن", "please pay the driver in cash"),
                "trip", trip.Id, new Dictionary<string, object?> { ["tripNumber"] = trip.TripNumber, ["paymentId"] = payment?.Id }), ct);
        }
        else if (payment is { Status: PaymentStatus.Authorized })
        {
            events.Add(trip.Id, TripEventTypes.PaymentAuthorized, TripActor.System, data: new { paymentId = payment.Id, amount = payment.AuthorizedAmount });
        }
    }

    private async Task EnsureNoOverlapAsync(Guid driverId, Trip trip, ScheduledRideRule rule, CancellationToken ct)
    {
        var start = trip.ScheduledAt!.Value;
        var end = start.AddSeconds(trip.EstimatedDurationS).AddMinutes(rule.ReservationGapMinutes);
        var others = await (from r in db.ScheduledRideReservations.AsNoTracking()
                            join t in db.Trips.AsNoTracking() on r.TripId equals t.Id
                            where r.DriverId == driverId && t.Id != trip.Id
                                  && (r.Status == ReservationStatus.Reserved || r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.Assigned)
                            select new { t.Id, t.ScheduledAt, t.EstimatedDurationS }).ToListAsync(ct);
        foreach (var other in others.Where(o => o.ScheduledAt is not null))
        {
            var otherStart = other.ScheduledAt!.Value;
            var otherEnd = otherStart.AddSeconds(other.EstimatedDurationS).AddMinutes(rule.ReservationGapMinutes);
            if (start < otherEnd && otherStart < end)
            {
                throw new DomainException(ErrorCodes.ReservationConflict, new { conflictingTripId = other.Id });
            }
        }
    }

    private async Task<(ScheduledRideReservation Reservation, Trip? Trip, ScheduledRideRule Rule)> LoadAsync(Guid reservationId, CancellationToken ct)
    {
        var reservation = await db.ScheduledRideReservations.FirstAsync(r => r.Id == reservationId, ct);
        var trip = await reads.FindAsync(reservation.TripId, ct);
        var rule = trip is null ? ScheduledRideRule.Default() : await rules.ResolveForTripAsync(trip, ct);
        return (reservation, trip, rule);
    }

    private async Task<int> EachAsync(IReadOnlyList<Guid> ids, string step, Func<Guid, Task<bool>> action, CancellationToken ct)
    {
        var changed = 0;
        foreach (var id in ids)
        {
            try
            {
                if (await action(id))
                {
                    changed++;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Scheduled ride step '{Step}' failed for {Id}", step, id);
                db.ChangeTracker.Clear();
            }
        }

        return changed;
    }

    private async Task<Guid> PassengerUserIdAsync(Trip trip, CancellationToken ct) =>
        await db.Passengers.AsNoTracking().Where(p => p.Id == trip.PassengerId).Select(p => p.UserId).FirstAsync(ct);

    private async Task<Guid> DriverUserIdAsync(Guid driverId, CancellationToken ct) =>
        await db.Drivers.AsNoTracking().Where(d => d.Id == driverId).Select(d => d.UserId).FirstAsync(ct);

    /// <summary>Recomputes the driver's reliability profile after a fault (a concurrent refresh that created the profile first is harmless).</summary>
    private async Task RefreshReliabilityAsync(Guid driverUserId, bool atFault, CancellationToken ct)
    {
        if (!atFault)
        {
            return;
        }

        try
        {
            await reliability.RefreshAsync(driverUserId, Role.Driver, ct);
        }
        catch (DbUpdateException)
        {
            // The next event or the nightly job recomputes it.
        }
    }
}
