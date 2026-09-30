using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Api.Modules.Pricing;
using ATA.Domain.Cancellation;
using ATA.Domain.Common;
using ATA.Domain.Drivers;
using ATA.Domain.Scheduling;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Scheduling;

/// <summary>
/// Admin console of scheduled rides (<c>scheduling.manage</c>): the rules CRUD, the upcoming scheduled trips with their reservation and risk flag, manual assignment / release and
/// the F17.5 KPIs. Writes are audited (<c>scheduled_ride_rule.create|update|delete</c>, <c>scheduled_trip.assign|release</c>).
/// </summary>
public sealed class ScheduledAdminService(
    AtaDbContext db, IClock clock, ICurrentUser currentUser, AuditService audit, ScheduledRideEngine engine, ScheduleRuleProvider rules, ZoneResolver zones)
{
    private const string RuleEntity = "scheduled_ride_rule";
    private const string TripEntity = "scheduled_trip";
    /// <summary><c>atRisk</c>: pickup within this many minutes and no driver has given the first confirmation.</summary>
    public const int AtRiskMinutes = 90;
    /// <summary>The dashboard fetches up to 200 rows at once.</summary>
    public const int MaxPageSize = 200;

    /// <summary>Pickup within <see cref="AtRiskMinutes"/> and no driver has given the first confirmation (no reservation, or one still <c>reserved</c>).</summary>
    public static bool IsAtRisk(Trip trip, ReservationStatus? reservation, DateTime now) =>
        trip.Status == TripStatus.Scheduled && trip.ScheduledAt is { } at && (at - now).TotalMinutes <= AtRiskMinutes && reservation is null or ReservationStatus.Reserved;

    // ----- rules -----

    public async Task<IReadOnlyList<ScheduledRideRuleDto>> ListRulesAsync(CancellationToken ct) =>
        (await db.ScheduledRideRules.AsNoTracking().OrderBy(r => r.CityId != null ? 0 : 1).ThenBy(r => r.RideCategoryId != null ? 0 : 1).ThenBy(r => r.CreatedAt).ToListAsync(ct)).Select(ToDto).ToList();

    public async Task<ScheduledRideRuleDto> CreateRuleAsync(ScheduledRideRuleUpsertRequest request, CancellationToken ct)
    {
        await ValidateAsync(request, null, ct);
        var rule = ScheduledRideRule.Default();
        Apply(rule, request);
        db.ScheduledRideRules.Add(rule);
        audit.Log("scheduled_ride_rule.create", RuleEntity, rule.Id, null, Snapshot(rule));
        await db.SaveChangesAsync(ct);
        return ToDto(rule);
    }

    public async Task<ScheduledRideRuleDto> UpdateRuleAsync(Guid id, ScheduledRideRuleUpsertRequest request, CancellationToken ct)
    {
        var rule = Guard.NotFound(await db.ScheduledRideRules.FirstOrDefaultAsync(r => r.Id == id, ct));
        await ValidateAsync(request, rule, ct);
        var before = Snapshot(rule);
        Apply(rule, request);
        audit.Log("scheduled_ride_rule.update", RuleEntity, rule.Id, before, Snapshot(rule));
        await db.SaveChangesAsync(ct);
        return ToDto(rule);
    }

    public async Task DeleteRuleAsync(Guid id, CancellationToken ct)
    {
        var rule = Guard.NotFound(await db.ScheduledRideRules.FirstOrDefaultAsync(r => r.Id == id, ct));
        audit.Log("scheduled_ride_rule.delete", RuleEntity, rule.Id, Snapshot(rule), null);
        db.ScheduledRideRules.Remove(rule);
        await db.SaveChangesAsync(ct);
    }

    // ----- trips -----

    /// <summary><c>GET /admin/scheduled-trips</c>: open scheduled bookings by <c>scheduledAt</c> with the state of their reservation.</summary>
    public async Task<PagedResult<AdminScheduledTripDto>> TripsAsync(
        DateOnly? from, DateOnly? to, string? reservation, Guid? cityId, Guid? rideCategoryId, Guid? zoneId, bool atRisk, int? page, int? pageSize, CancellationToken ct)
    {
        var filter = string.IsNullOrWhiteSpace(reservation) ? null : reservation.Trim().ToLowerInvariant();
        new Validator()
            .Rule(nameof(reservation), filter is null or "none" or "reserved" or "confirmed" or "assigned", "must be none|reserved|confirmed|assigned")
            .Rule(nameof(to), from is null || to is null || to >= from, "must not be before 'from'").ThrowIfInvalid();
        var paging = new Paging(Math.Max(1, page ?? 1), Math.Clamp(pageSize ?? Paging.DefaultPageSize, 1, MaxPageSize));
        var now = clock.UtcNow;
        var query = db.Trips.AsNoTracking().Where(t => t.BookingType == BookingType.Scheduled && t.ScheduledAt != null && Trip.OpenStatuses.Contains(t.Status));
        if (from is { } f)
        {
            var fromAt = Formats.RiyadhMidnightUtc(f);
            query = query.Where(t => t.ScheduledAt >= fromAt);
        }

        if (to is { } toValue)
        {
            var toAt = Formats.RiyadhMidnightUtc(toValue.AddDays(1));
            query = query.Where(t => t.ScheduledAt < toAt);
        }

        if (rideCategoryId is { } categoryFilter)
        {
            query = query.Where(t => t.RideCategoryId == categoryFilter);
        }

        var trips = await query.OrderBy(t => t.ScheduledAt).ToListAsync(ct);
        if (cityId is not null || zoneId is not null)
        {
            var all = await zones.AllAsync(ct);
            trips = trips.Where(t => zones.Resolve(all, t.PickupLat, t.PickupLng, t.RequestedAt) is { } zone && (cityId is null || zone.CityId == cityId) && (zoneId is null || zone.Id == zoneId)).ToList();
        }

        var tripIds = trips.Select(t => t.Id).ToList();
        var active = await db.ScheduledRideReservations.AsNoTracking()
            .Where(r => tripIds.Contains(r.TripId) && (r.Status == ReservationStatus.Reserved || r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.Assigned))
            .ToDictionaryAsync(r => r.TripId, ct);
        var statusOf = (Trip t) => active.TryGetValue(t.Id, out var r) ? r.Status : (ReservationStatus?)null;
        trips = filter switch
        {
            "none" => trips.Where(t => statusOf(t) is null).ToList(),
            "reserved" => trips.Where(t => statusOf(t) == ReservationStatus.Reserved).ToList(),
            "confirmed" => trips.Where(t => statusOf(t) == ReservationStatus.Confirmed).ToList(),
            "assigned" => trips.Where(t => statusOf(t) == ReservationStatus.Assigned).ToList(),
            _ => trips,
        };
        if (atRisk)
        {
            trips = trips.Where(t => IsAtRisk(t, statusOf(t), now)).ToList();
        }

        var pageRows = trips.Skip(paging.Skip).Take(paging.PageSize).ToList();
        var items = await ToItemsAsync(pageRows, now, ct);
        return paging.Result(items, trips.Count);
    }

    public async Task<AdminScheduledTripDto> AssignAsync(Guid tripId, AssignScheduledTripRequest request, CancellationToken ct)
    {
        new Validator().Require(nameof(request.DriverId), request.DriverId).ThrowIfInvalid();
        var driver = await db.Drivers.FirstOrDefaultAsync(d => d.Id == request.DriverId, ct);
        new Validator().Rule(nameof(request.DriverId), driver is not null, "unknown driver").ThrowIfInvalid();
        if (driver!.ApplicationStatus != ApplicationStatus.Approved)
        {
            throw new DomainException(ErrorCodes.DriverNotApproved, new { status = driver.ApplicationStatus });
        }

        var trip = await db.Trips.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tripId && t.BookingType == BookingType.Scheduled, ct) ?? throw new DomainException(ErrorCodes.NotFound);
        // Idempotent: assigning the driver who already holds the reservation is a no-op.
        if (await db.ScheduledRideReservations.AsNoTracking().AnyAsync(r => r.TripId == tripId && r.DriverId == driver.Id
                && (r.Status == ReservationStatus.Reserved || r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.Assigned), ct))
        {
            return await ItemAsync(tripId, ct);
        }

        var rule = await rules.ResolveForTripAsync(trip, ct);
        var (reservation, _) = await engine.ReserveAsync(tripId, driver, ReservationSource.Admin, rule, enforceLimit: false, ct);
        audit.Log("scheduled_trip.assign", TripEntity, tripId, null, new { driverId = driver.Id, reservationId = reservation.Id, status = reservation.Status });
        await db.SaveChangesAsync(ct);
        return await ItemAsync(tripId, ct);
    }

    public async Task<AdminScheduledTripDto> ReleaseAsync(Guid tripId, AdminReleaseReservationRequest request, CancellationToken ct)
    {
        new Validator().Require(nameof(request.Reason), request.Reason, 500).ThrowIfInvalid();
        var reason = request.Reason!.Trim();
        if (!await db.Trips.AsNoTracking().AnyAsync(t => t.Id == tripId && t.BookingType == BookingType.Scheduled, ct))
        {
            throw new DomainException(ErrorCodes.NotFound);
        }

        // Idempotent: without an active reservation there is nothing to release.
        if (!await db.ScheduledRideReservations.AsNoTracking().AnyAsync(r => r.TripId == tripId
                && (r.Status == ReservationStatus.Reserved || r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.Assigned), ct))
        {
            return await ItemAsync(tripId, ct);
        }

        var reservation = await engine.AdminReleaseAsync(tripId, currentUser.UserId, reason, ct);
        audit.Log("scheduled_trip.release", TripEntity, tripId, new { driverId = reservation.DriverId, reservationId = reservation.Id }, new { reason, status = reservation.Status });
        await db.SaveChangesAsync(ct);
        return await ItemAsync(tripId, ct);
    }

    private async Task<AdminScheduledTripDto> ItemAsync(Guid tripId, CancellationToken ct)
    {
        var trip = Guard.NotFound(await db.Trips.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tripId, ct));
        return (await ToItemsAsync([trip], clock.UtcNow, ct))[0];
    }

    private async Task<List<AdminScheduledTripDto>> ToItemsAsync(IReadOnlyList<Trip> page, DateTime now, CancellationToken ct)
    {
        var tripIds = page.Select(t => t.Id).ToList();
        var active = await db.ScheduledRideReservations.AsNoTracking()
            .Where(r => tripIds.Contains(r.TripId) && (r.Status == ReservationStatus.Reserved || r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.Assigned))
            .ToDictionaryAsync(r => r.TripId, ct);
        var passengerIds = page.Select(t => t.PassengerId).Distinct().ToList();
        var passengerNames = await (from p in db.Passengers.AsNoTracking() join u in db.Users.AsNoTracking() on p.UserId equals u.Id where passengerIds.Contains(p.Id) select new { p.Id, u.FullName })
            .ToDictionaryAsync(x => x.Id, x => x.FullName, ct);
        var driverIds = active.Values.Select(r => r.DriverId).Distinct().ToList();
        var driverNames = await (from d in db.Drivers.AsNoTracking() join u in db.Users.AsNoTracking() on d.UserId equals u.Id where driverIds.Contains(d.Id) select new { d.Id, u.FullName })
            .ToDictionaryAsync(x => x.Id, x => x.FullName, ct);
        var categories = await db.RideCategories.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.NameAr, ct);
        var allZones = await zones.AllAsync(ct);
        return page.Select(t =>
        {
            var reservationRow = active.GetValueOrDefault(t.Id);
            var minutes = (int)Math.Floor((t.ScheduledAt!.Value - now).TotalMinutes);
            var zone = zones.Resolve(allZones, t.PickupLat, t.PickupLng, t.RequestedAt);
            return new AdminScheduledTripDto(
                t.Id, t.TripNumber, t.ScheduledAt.Value, passengerNames.GetValueOrDefault(t.PassengerId), categories.GetValueOrDefault(t.RideCategoryId) ?? string.Empty, t.PickupName, t.DropoffName,
                reservationRow is null ? "none" : Snake(reservationRow.Status), reservationRow is null ? null : driverNames.GetValueOrDefault(reservationRow.DriverId), minutes,
                IsAtRisk(t, reservationRow?.Status, now), t.RideCategoryId, zone?.Id, reservationRow?.DriverId, Snake(t.Status));
        }).ToList();
    }

    // ----- KPIs -----

    /// <summary>
    /// <c>GET /admin/scheduling/stats</c> (doc 11 §F17.5) over bookings made in the range (no range = all time). <c>scheduledCompletionRate</c> = completed ÷ bookings whose pickup time
    /// has come, leaving out passenger cancellations inside the free window; <c>scheduledCancellationRate</c> = cancelled (any party, including <c>no_drivers</c>) ÷ booked;
    /// <c>driverCommitmentRate</c> = completed reservations ÷ reservations except <c>trip_cancelled</c>; <c>driverNoShowRate</c> = driver no-shows ÷ booked; <c>rematched</c> counts trips sent back to matching after a reservation failed.
    /// </summary>
    public async Task<SchedulingStatsDto> StatsAsync(DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        new Validator().Rule("from", from is null || to is null || from <= to, "must be on or before 'to'").ThrowIfInvalid();
        var now = clock.UtcNow;
        var trips = db.Trips.AsNoTracking().Where(t => t.BookingType == BookingType.Scheduled);
        var reservations = db.ScheduledRideReservations.AsNoTracking().AsQueryable();
        if (from is { } f)
        {
            var fromAt = f.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            trips = trips.Where(t => t.RequestedAt >= fromAt);
            reservations = reservations.Where(r => r.ReservedAt >= fromAt);
        }

        if (to is { } t0)
        {
            var toAt = t0.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            trips = trips.Where(t => t.RequestedAt < toAt);
            reservations = reservations.Where(r => r.ReservedAt < toAt);
        }

        var tripRows = await trips.Select(t => new { t.Id, t.Status, t.CancelledBy, t.ScheduledAt }).ToListAsync(ct);
        var tripIds = tripRows.Select(t => t.Id).ToList();
        var events = new List<(Guid TripId, AtFault AtFault, CancellationStage Stage, TripActor Actor)>();
        foreach (var chunk in tripIds.Chunk(500))
        {
            var ids = chunk.ToList();
            events.AddRange((await db.CancellationEvents.AsNoTracking().Where(e => ids.Contains(e.TripId)).Select(e => new { e.TripId, e.AtFault, e.Stage, e.Actor }).ToListAsync(ct))
                .Select(e => (e.TripId, e.AtFault, e.Stage, e.Actor)));
        }

        var rematchedTrips = new HashSet<Guid>();
        foreach (var chunk in tripIds.Chunk(500))
        {
            var ids = chunk.ToList();
            rematchedTrips.UnionWith(await db.TripEvents.AsNoTracking().Where(e => e.Type == TripEventTypes.Rematched && ids.Contains(e.TripId)).Select(e => e.TripId).Distinct().ToListAsync(ct));
        }

        var freeCancelled = events.Where(e => e.Actor == TripActor.Passenger && e.Stage == CancellationStage.Scheduled && e.AtFault == AtFault.None).Select(e => e.TripId).ToHashSet();
        var lateCancelled = events.Count(e => e.Actor == TripActor.Passenger && e.AtFault == AtFault.Passenger);
        var booked = tripRows.Count;
        var completed = tripRows.Count(t => t.Status == TripStatus.Completed);
        var cancelledByPassenger = tripRows.Count(t => t.Status == TripStatus.Cancelled && t.CancelledBy == CancelledBy.Passenger);
        var cancelledAny = tripRows.Count(t => t.Status is TripStatus.Cancelled or TripStatus.NoDrivers);
        var due = tripRows.Count(t => t.ScheduledAt <= now && !freeCancelled.Contains(t.Id));
        var reservationRows = await reservations.Select(r => new { r.Status, r.ReleaseReason, r.ReservedAt, r.TripId }).ToListAsync(ct);
        var reservationTrips = reservationRows.Select(r => r.TripId).Distinct().ToList();
        var scheduledAtByTrip = new Dictionary<Guid, DateTime?>();
        foreach (var chunk in reservationTrips.Chunk(500))
        {
            var ids = chunk.ToList();
            foreach (var row in await db.Trips.AsNoTracking().Where(t => ids.Contains(t.Id)).Select(t => new { t.Id, t.ScheduledAt }).ToListAsync(ct))
            {
                scheduledAtByTrip[row.Id] = row.ScheduledAt;
            }
        }

        var committed = reservationRows.Where(r => r.ReleaseReason != ReservationReleaseReason.TripCancelled).ToList();
        var leads = reservationRows.Where(r => scheduledAtByTrip.GetValueOrDefault(r.TripId) is not null).Select(r => (scheduledAtByTrip[r.TripId]!.Value - r.ReservedAt).TotalHours).ToList();
        return new SchedulingStatsDto(
            booked, completed, cancelledByPassenger, lateCancelled,
            reservationRows.Count(r => r.ReleaseReason == ReservationReleaseReason.DriverReleased),
            reservationRows.Count(r => r.ReleaseReason is ReservationReleaseReason.ConfirmationMissed or ReservationReleaseReason.FinalConfirmationMissed),
            reservationRows.Count(r => r.Status == ReservationStatus.NoShow), rematchedTrips.Count,
            due == 0 ? 0m : Rate(completed, due), booked == 0 ? 0m : Rate(cancelledAny, booked),
            leads.Count == 0 ? null : decimal.Round((decimal)leads.Average(), 2, MidpointRounding.AwayFromZero),
            committed.Count == 0 ? 0m : Rate(committed.Count(r => r.Status == ReservationStatus.Completed), committed.Count),
            booked == 0 ? 0m : Rate(reservationRows.Count(r => r.Status == ReservationStatus.NoShow), booked));
    }

    // ----- validation / mapping -----

    private async Task ValidateAsync(ScheduledRideRuleUpsertRequest r, ScheduledRideRule? existing, CancellationToken ct)
    {
        var lead = r.DriverAssignmentLeadMinutes ?? ScheduledRideRule.Default().DriverAssignmentLeadMinutes;
        var final = r.FinalConfirmationMinutesBefore ?? ScheduledRideRule.Default().FinalConfirmationMinutesBefore;
        var finalTimeout = r.FinalConfirmationTimeoutMinutes ?? ScheduledRideRule.Default().FinalConfirmationTimeoutMinutes;
        var search = r.SearchStartMinutesBefore ?? ScheduledRideRule.Default().SearchStartMinutesBefore;
        var feeType = r.LateCancelFeeType ?? ScheduledRideRule.Default().LateCancelFeeType;
        var v = new Validator()
            .Rule(nameof(r.MaxDaysAhead), r.MaxDaysAhead is null or (>= 1 and <= 90), "must be between 1 and 90")
            .Rule(nameof(r.MinLeadMinutes), r.MinLeadMinutes is null or (>= 0 and <= 1440), "must be between 0 and 1440")
            .Rule(nameof(r.MaxOpenPerPassenger), r.MaxOpenPerPassenger is null or (>= 1 and <= 50), "must be between 1 and 50")
            .Rule(nameof(r.MarketplaceRadiusKm), r.MarketplaceRadiusKm is null or (>= 1 and <= 500), "must be between 1 and 500")
            .Rule(nameof(r.FavoriteExclusiveMinutes), r.FavoriteExclusiveMinutes is null or (>= 0 and <= 1440), "must be between 0 and 1440")
            .Rule(nameof(r.DriverAssignmentLeadMinutes), lead is > 0 and <= 1440, "must be between 1 and 1440")
            .Rule(nameof(r.ConfirmationTimeoutMinutes), r.ConfirmationTimeoutMinutes is null or (>= 1 and <= 240), "must be between 1 and 240")
            .Rule(nameof(r.FinalConfirmationMinutesBefore), final > 0 && final < lead, "must be positive and before driverAssignmentLeadMinutes")
            .Rule(nameof(r.FinalConfirmationTimeoutMinutes), finalTimeout is >= 1 and <= 240, "must be between 1 and 240")
            .Rule(nameof(r.SearchStartMinutesBefore), search > 0 && search <= final, "must be positive and not after the final confirmation request")
            .Rule(nameof(r.FreeCancelMinutesBefore), r.FreeCancelMinutesBefore is null or (>= 0 and <= 10080), "must be between 0 and 10080")
            .Rule(nameof(r.LateCancelFeeType), r.LateCancelFeeType is null || Enum.IsDefined(r.LateCancelFeeType.Value), "must be none|fixed|percent|pricing_rule")
            .Rule(nameof(r.LateCancelFeeAmount), feeType != CancellationFeeType.Fixed || (r.LateCancelFeeAmount ?? ScheduledRideRule.Default().LateCancelFeeAmount) is >= 0, "required for fixed fees")
            .Rule(nameof(r.LateCancelFeeAmount), r.LateCancelFeeAmount is null or >= 0, "must be positive")
            .Rule(nameof(r.LateCancelFeePercent), feeType != CancellationFeeType.Percent || r.LateCancelFeePercent is >= 0 and <= 100, "required (0-100) for percent fees")
            .Rule(nameof(r.LateCancelFeePercent), r.LateCancelFeePercent is null or (>= 0 and <= 100), "must be between 0 and 100")
            .Rule(nameof(r.LateCancelDriverCompensationPercent), r.LateCancelDriverCompensationPercent is null or (>= 0 and <= 100), "must be between 0 and 100")
            .Rule(nameof(r.DriverFreeReleaseMinutesBefore), r.DriverFreeReleaseMinutesBefore is null or (>= 0 and <= 10080), "must be between 0 and 10080")
            .Rule(nameof(r.DriverLateReleasePenaltyPoints), r.DriverLateReleasePenaltyPoints is null or (>= 0 and <= 100), "must be between 0 and 100")
            .Rule(nameof(r.DriverConfirmationMissedPenaltyPoints), r.DriverConfirmationMissedPenaltyPoints is null or (>= 0 and <= 100), "must be between 0 and 100")
            .Rule(nameof(r.DriverNoShowPenaltyPoints), r.DriverNoShowPenaltyPoints is null or (>= 0 and <= 100), "must be between 0 and 100")
            .Rule(nameof(r.DriverNoShowGraceMinutes), r.DriverNoShowGraceMinutes is null or (>= 0 and <= 240), "must be between 0 and 240")
            .Rule(nameof(r.MaxReservationsPerDriver), r.MaxReservationsPerDriver is null or (>= 1 and <= 100), "must be between 1 and 100")
            .Rule(nameof(r.ReservationGapMinutes), r.ReservationGapMinutes is null or (>= 0 and <= 480), "must be between 0 and 480")
            .Rule(nameof(r.RiderReminderOffsets), OffsetsValid(r.RiderReminderOffsets), "must be at most 10 positive minutes")
            .Rule(nameof(r.DriverReminderOffsets), OffsetsValid(r.DriverReminderOffsets), "must be at most 10 positive minutes");
        v.ThrowIfInvalid();
        if (r.CityId is { } cityId)
        {
            v.Rule(nameof(r.CityId), await db.Cities.AnyAsync(c => c.Id == cityId, ct), "unknown city");
        }

        if (r.RideCategoryId is { } categoryId)
        {
            v.Rule(nameof(r.RideCategoryId), await db.RideCategories.AnyAsync(c => c.Id == categoryId, ct), "unknown ride category");
        }

        v.ThrowIfInvalid();
        if (await db.ScheduledRideRules.AnyAsync(x => x.CityId == r.CityId && x.RideCategoryId == r.RideCategoryId && (existing == null || x.Id != existing.Id), ct))
        {
            throw new DomainException(ErrorCodes.Conflict, new { reason = "a rule for this city and category already exists" });
        }
    }

    private static bool OffsetsValid(List<int>? offsets) => offsets is null || (offsets.Count <= 10 && offsets.All(o => o is > 0 and <= 43200));

    private static void Apply(ScheduledRideRule rule, ScheduledRideRuleUpsertRequest r)
    {
        var d = ScheduledRideRule.Default();
        rule.CityId = r.CityId;
        rule.RideCategoryId = r.RideCategoryId;
        rule.MaxDaysAhead = r.MaxDaysAhead ?? d.MaxDaysAhead;
        rule.MinLeadMinutes = r.MinLeadMinutes ?? d.MinLeadMinutes;
        rule.MaxOpenPerPassenger = r.MaxOpenPerPassenger ?? d.MaxOpenPerPassenger;
        rule.LockDemandNormal = r.LockDemandNormal ?? d.LockDemandNormal;
        rule.MarketplaceEnabled = r.MarketplaceEnabled ?? d.MarketplaceEnabled;
        rule.MarketplaceRadiusKm = r.MarketplaceRadiusKm ?? d.MarketplaceRadiusKm;
        rule.FavoriteExclusiveMinutes = r.FavoriteExclusiveMinutes ?? d.FavoriteExclusiveMinutes;
        rule.DriverAssignmentLeadMinutes = r.DriverAssignmentLeadMinutes ?? d.DriverAssignmentLeadMinutes;
        rule.ConfirmationTimeoutMinutes = r.ConfirmationTimeoutMinutes ?? d.ConfirmationTimeoutMinutes;
        rule.FinalConfirmationMinutesBefore = r.FinalConfirmationMinutesBefore ?? d.FinalConfirmationMinutesBefore;
        rule.FinalConfirmationTimeoutMinutes = r.FinalConfirmationTimeoutMinutes ?? d.FinalConfirmationTimeoutMinutes;
        rule.SearchStartMinutesBefore = r.SearchStartMinutesBefore ?? d.SearchStartMinutesBefore;
        rule.RiderReminderOffsets = r.RiderReminderOffsets is null ? d.RiderReminderOffsets : ScheduledRideRule.SerializeOffsets(r.RiderReminderOffsets);
        rule.DriverReminderOffsets = r.DriverReminderOffsets is null ? d.DriverReminderOffsets : ScheduledRideRule.SerializeOffsets(r.DriverReminderOffsets);
        rule.FreeCancelMinutesBefore = r.FreeCancelMinutesBefore ?? d.FreeCancelMinutesBefore;
        rule.LateCancelFeeType = r.LateCancelFeeType ?? d.LateCancelFeeType;
        rule.LateCancelFeeAmount = r.LateCancelFeeType is null && r.LateCancelFeeAmount is null ? d.LateCancelFeeAmount : r.LateCancelFeeAmount;
        rule.LateCancelFeePercent = r.LateCancelFeePercent;
        rule.LateCancelDriverCompensationPercent = r.LateCancelDriverCompensationPercent ?? d.LateCancelDriverCompensationPercent;
        rule.DriverFreeReleaseMinutesBefore = r.DriverFreeReleaseMinutesBefore ?? d.DriverFreeReleaseMinutesBefore;
        rule.DriverLateReleasePenaltyPoints = r.DriverLateReleasePenaltyPoints ?? d.DriverLateReleasePenaltyPoints;
        rule.DriverConfirmationMissedPenaltyPoints = r.DriverConfirmationMissedPenaltyPoints ?? d.DriverConfirmationMissedPenaltyPoints;
        rule.DriverNoShowPenaltyPoints = r.DriverNoShowPenaltyPoints ?? d.DriverNoShowPenaltyPoints;
        rule.DriverNoShowGraceMinutes = r.DriverNoShowGraceMinutes ?? d.DriverNoShowGraceMinutes;
        rule.MaxReservationsPerDriver = r.MaxReservationsPerDriver ?? d.MaxReservationsPerDriver;
        rule.ReservationGapMinutes = r.ReservationGapMinutes ?? d.ReservationGapMinutes;
        rule.IsActive = r.IsActive ?? true;
    }

    private static ScheduledRideRuleDto ToDto(ScheduledRideRule r) => new(
        r.Id, r.CityId, r.RideCategoryId, r.MaxDaysAhead, r.MinLeadMinutes, r.MaxOpenPerPassenger, r.LockDemandNormal, r.MarketplaceEnabled, r.MarketplaceRadiusKm, r.FavoriteExclusiveMinutes,
        r.DriverAssignmentLeadMinutes, r.ConfirmationTimeoutMinutes, r.FinalConfirmationMinutesBefore, r.FinalConfirmationTimeoutMinutes, r.SearchStartMinutesBefore, r.RiderOffsets(), r.DriverOffsets(),
        r.FreeCancelMinutesBefore, r.LateCancelFeeType, r.LateCancelFeeAmount, r.LateCancelFeePercent, r.LateCancelDriverCompensationPercent, r.DriverFreeReleaseMinutesBefore,
        r.DriverLateReleasePenaltyPoints, r.DriverConfirmationMissedPenaltyPoints, r.DriverNoShowPenaltyPoints, r.DriverNoShowGraceMinutes, r.MaxReservationsPerDriver, r.ReservationGapMinutes,
        r.IsActive, r.CreatedAt, r.UpdatedAt);

    private static object Snapshot(ScheduledRideRule r) => ToDto(r);

    private static decimal Rate(int part, int whole) => whole == 0 ? 0m : decimal.Round((decimal)part / whole, 4, MidpointRounding.AwayFromZero);

    private static string Snake<TEnum>(TEnum value) where TEnum : struct, Enum => System.Text.Json.JsonNamingPolicy.SnakeCaseLower.ConvertName(value.ToString());
}
