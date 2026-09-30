using ATA.Domain.Common;
using ATA.Domain.Scheduling;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Scheduling;

/// <summary>
/// Plans and cancels the rows of <c>scheduled_ride_reminders</c> (doc 11 §F17.3). Reminders are only created for offsets that are still in the future; the driver's confirmation
/// requests (first at <c>T − driver_assignment_lead_minutes</c>, final at <c>T − final_confirmation_minutes_before</c>) are rows too. Sending lives in <see cref="ScheduledRideEngine"/>.
/// </summary>
public sealed class ScheduledReminderService(AtaDbContext db, IClock clock)
{
    /// <summary>Rider reminders at <c>T − offset</c> for every configured offset (booking time = now); the caller saves.</summary>
    public void PlanRider(Trip trip, ScheduledRideRule rule, Guid passengerUserId)
    {
        var now = clock.UtcNow;
        foreach (var offset in rule.RiderOffsets())
        {
            AddIfFuture(trip.Id, null, passengerUserId, ReminderRecipientRole.Passenger, ReminderKind.Reminder, offset, trip.ScheduledAt!.Value.AddMinutes(-offset), now);
        }
    }

    /// <summary>Driver reminders plus the two confirmation requests of a reservation; the caller saves.</summary>
    public void PlanDriver(Trip trip, ScheduledRideReservation reservation, ScheduledRideRule rule, Guid driverUserId)
    {
        var now = clock.UtcNow;
        var scheduledAt = trip.ScheduledAt!.Value;
        foreach (var offset in rule.DriverOffsets())
        {
            AddIfFuture(trip.Id, reservation.Id, driverUserId, ReminderRecipientRole.Driver, ReminderKind.Reminder, offset, scheduledAt.AddMinutes(-offset), now);
        }

        AddIfFuture(trip.Id, reservation.Id, driverUserId, ReminderRecipientRole.Driver, ReminderKind.ConfirmRequest, rule.DriverAssignmentLeadMinutes, rule.FirstConfirmationAt(scheduledAt), now);
        AddIfFuture(trip.Id, reservation.Id, driverUserId, ReminderRecipientRole.Driver, ReminderKind.FinalConfirmRequest, rule.FinalConfirmationMinutesBefore, rule.FinalConfirmationAt(scheduledAt), now);
    }

    /// <summary>Pending rows of the reservation (its driver's reminders and confirmation requests) become <c>cancelled</c>; the caller saves.</summary>
    public async Task CancelForReservationAsync(Guid reservationId, CancellationToken ct) =>
        await CancelAsync(r => r.ReservationId == reservationId, ct);

    /// <summary>Every pending row of the trip (rider and driver) becomes <c>cancelled</c>; the caller saves.</summary>
    public async Task CancelForTripAsync(Guid tripId, CancellationToken ct) => await CancelAsync(r => r.TripId == tripId, ct);

    /// <summary>Marks the pending confirmation rows of the reservation as sent (the worker already asked the driver); the caller saves.</summary>
    public async Task MarkConfirmationSentAsync(Guid reservationId, ReminderKind kind, DateTime now, CancellationToken ct)
    {
        var rows = await db.ScheduledRideReminders.Where(r => r.ReservationId == reservationId && r.Kind == kind && r.Status == ReminderStatus.Pending).ToListAsync(ct);
        foreach (var row in rows)
        {
            row.Status = ReminderStatus.Sent;
            row.SentAt = now;
        }
    }

    private async Task CancelAsync(System.Linq.Expressions.Expression<Func<ScheduledRideReminder, bool>> filter, CancellationToken ct)
    {
        var rows = await db.ScheduledRideReminders.Where(filter).Where(r => r.Status == ReminderStatus.Pending).ToListAsync(ct);
        foreach (var row in rows)
        {
            row.Status = ReminderStatus.Cancelled;
        }

        foreach (var row in db.ScheduledRideReminders.Local.Where(r => r.Status == ReminderStatus.Pending && filter.Compile()(r)))
        {
            row.Status = ReminderStatus.Cancelled;
        }
    }

    private void AddIfFuture(Guid tripId, Guid? reservationId, Guid userId, ReminderRecipientRole role, ReminderKind kind, int offset, DateTime sendAt, DateTime now)
    {
        if (sendAt <= now)
        {
            return;
        }

        db.ScheduledRideReminders.Add(new ScheduledRideReminder
        {
            TripId = tripId, ReservationId = reservationId, RecipientUserId = userId, RecipientRole = role, Kind = kind, OffsetMinutes = offset, SendAt = sendAt,
        });
    }
}
