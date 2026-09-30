using ATA.Api.Modules.Ratings;
using ATA.Api.Modules.Trips;
using ATA.Domain.Scheduling;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Scheduling;

/// <summary>Builds <c>Trip.scheduling</c> (doc 11 §F17.4) for <see cref="TripReadService"/>; <c>null</c> for trips that are not scheduled bookings.</summary>
public sealed class SchedulingViewBuilder(AtaDbContext db, ScheduleRuleProvider rules)
{
    public static string DriverPhotoUrl(Guid tripId) => $"/api/v1/passenger/scheduled/{tripId}/driver-photo";

    public async Task<TripSchedulingDto?> BuildAsync(Trip trip, CancellationToken ct)
    {
        if (!trip.IsScheduledBooking || trip.ScheduledAt is not { } scheduledAt)
        {
            return null;
        }

        var rule = await rules.ResolveForTripAsync(trip, ct);
        var reservation = trip.IsTerminal
            ? null
            : await db.ScheduledRideReservations.AsNoTracking().FirstOrDefaultAsync(r => r.TripId == trip.Id
                && (r.Status == ReservationStatus.Reserved || r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.Assigned), ct);
        return new TripSchedulingDto(rule.FreeCancelUntil(scheduledAt), rule.SearchStartsAt(scheduledAt), reservation is null ? null : await ReservationAsync(reservation, trip, ct));
    }

    /// <summary><c>scheduling</c> of the admin trip detail: the trip's window plus every reservation (with the driver's name) and reminder.</summary>
    public async Task<AdminTripSchedulingDto?> BuildAdminAsync(Trip trip, CancellationToken ct)
    {
        if (!trip.IsScheduledBooking || trip.ScheduledAt is not { } scheduledAt)
        {
            return null;
        }

        var rule = await rules.ResolveForTripAsync(trip, ct);
        var rows = await db.ScheduledRideReservations.AsNoTracking().Where(r => r.TripId == trip.Id).OrderBy(r => r.ReservedAt).ThenBy(r => r.Id).ToListAsync(ct);
        var driverIds = rows.Select(r => r.DriverId).Distinct().ToList();
        var names = await (from d in db.Drivers.AsNoTracking() join u in db.Users.AsNoTracking() on d.UserId equals u.Id where driverIds.Contains(d.Id) select new { d.Id, u.FullName })
            .ToDictionaryAsync(x => x.Id, x => x.FullName, ct);
        var reservations = rows.Select(r => new AdminReservationDto(
            r.Id, r.DriverId, names.GetValueOrDefault(r.DriverId), r.Source, r.Status, r.ReservedAt, r.ConfirmRequestedAt, r.ConfirmedAt, r.FinalConfirmRequestedAt, r.AssignedAt, r.ReleasedAt,
            r.ReleaseReason, r.IsLateRelease, r.PenaltyPoints)).ToList();
        var reminders = (await db.ScheduledRideReminders.AsNoTracking().Where(r => r.TripId == trip.Id).OrderBy(r => r.SendAt).ThenBy(r => r.Id).ToListAsync(ct))
            .Select(r => new AdminReminderDto(r.Id, r.RecipientRole, r.Kind, r.OffsetMinutes, r.SendAt, r.SentAt, r.Status)).ToList();
        var active = trip.IsTerminal ? null : rows.FirstOrDefault(r => r.IsActive);
        var brief = active is null ? null : new AdminReservationBriefDto(active.Status, active.DriverId, names.GetValueOrDefault(active.DriverId), RatingService.FirstName(names.GetValueOrDefault(active.DriverId)), active.ReservedAt);
        return new AdminTripSchedulingDto(rule.FreeCancelUntil(scheduledAt), rule.SearchStartsAt(scheduledAt), brief, reservations, reminders);
    }

    private async Task<TripReservationDto> ReservationAsync(ScheduledRideReservation reservation, Trip trip, CancellationToken ct)
    {
        var driver = await (from d in db.Drivers.AsNoTracking() join u in db.Users.AsNoTracking() on d.UserId equals u.Id where d.Id == reservation.DriverId select new { d.Id, u.FullName, d.RatingAvg })
            .FirstAsync(ct);
        var vehicle = await db.Vehicles.AsNoTracking().Where(v => v.DriverId == driver.Id && v.IsActive).Select(v => new TripVehicleDto(v.Make, v.Model, v.Color, v.PlateNumber)).FirstOrDefaultAsync(ct);
        var hasPhoto = await (from doc in db.DriverDocuments.AsNoTracking() join type in db.DocumentTypes.AsNoTracking() on doc.DocumentTypeId equals type.Id
                              where doc.DriverId == driver.Id && type.Code == "profile_photo" && doc.Status != Domain.Drivers.DocumentStatus.Rejected select doc.Id).AnyAsync(ct);
        return new TripReservationDto(reservation.Status, RatingService.FirstName(driver.FullName), hasPhoto ? DriverPhotoUrl(trip.Id) : null, driver.RatingAvg, vehicle, reservation.ReservedAt);
    }
}
