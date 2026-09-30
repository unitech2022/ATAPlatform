using ATA.Api.Modules.Notifications;
using ATA.Domain.Notifications;
using ATA.Domain.Scheduling;
using ATA.Domain.Trips;

namespace ATA.Api.Modules.Scheduling;

/// <summary>Builds the F17 notifications (catalogue codes <c>scheduled.*</c>) sent through <see cref="INotificationDispatcher"/>.</summary>
public static class ScheduledNotifications
{
    public const string EntityType = "trip";

    public static NotificationRequest Booked(Trip trip, Guid passengerUserId) =>
        new(NotificationTypes.ScheduledBooked, passengerUserId,
            NotificationPlaceholders.Of(("scheduledAt", trip.ScheduledAt), ("pickupName", trip.PickupName)), EntityType, trip.Id, Extra(trip));

    public static NotificationRequest Reminder(Trip trip, Guid recipientUserId, int minutesBefore) =>
        new(NotificationTypes.ScheduledReminder, recipientUserId,
            NotificationPlaceholders.Of(("scheduledAt", trip.ScheduledAt), ("minutesBefore", minutesBefore), ("pickupName", trip.PickupName)), EntityType, trip.Id,
            Extra(trip, ("minutesBefore", minutesBefore)));

    public static NotificationRequest DriverReserved(Trip trip, Guid passengerUserId, string? driverName) =>
        new(NotificationTypes.ScheduledDriverReserved, passengerUserId,
            NotificationPlaceholders.Of(("driverName", driverName ?? string.Empty), ("scheduledAt", trip.ScheduledAt)), EntityType, trip.Id, Extra(trip));

    /// <summary><paramref name="final"/> marks the final confirmation (<c>T − final_confirmation_minutes_before</c>); the first one is the assignment-lead request.</summary>
    public static NotificationRequest ConfirmRequest(Trip trip, Guid driverUserId, int deadlineMinutes, bool final) =>
        new(NotificationTypes.ScheduledConfirmRequest, driverUserId,
            NotificationPlaceholders.Of(("scheduledAt", trip.ScheduledAt), ("pickupName", trip.PickupName), ("deadlineMinutes", deadlineMinutes)), EntityType, trip.Id,
            Extra(trip, ("final", final), ("deadlineMinutes", deadlineMinutes)));

    public static NotificationRequest ReservationReleased(Trip trip, Guid recipientUserId, ReservationReleaseReason reason) =>
        new(NotificationTypes.ScheduledReservationReleased, recipientUserId,
            NotificationPlaceholders.Of(("scheduledAt", trip.ScheduledAt)).Localized("reason", ReasonAr(reason), ReasonEn(reason)), EntityType, trip.Id,
            Extra(trip, ("reason", System.Text.Json.JsonNamingPolicy.SnakeCaseLower.ConvertName(reason.ToString()))));

    public static NotificationRequest FavoriteRequest(Trip trip, Guid driverUserId, string? passengerFirstName) =>
        new(NotificationTypes.ScheduledFavoriteRequest, driverUserId,
            NotificationPlaceholders.Of(("passengerName", passengerFirstName ?? string.Empty), ("scheduledAt", trip.ScheduledAt)), EntityType, trip.Id, Extra(trip));

    public static NotificationRequest Rematched(Trip trip, Guid passengerUserId) =>
        new(NotificationTypes.ScheduledRematched, passengerUserId, NotificationPlaceholders.Of(("scheduledAt", trip.ScheduledAt)), EntityType, trip.Id, Extra(trip));

    private static string ReasonAr(ReservationReleaseReason reason) => reason switch
    {
        ReservationReleaseReason.DriverReleased => "أخلى الكابتن الحجز",
        ReservationReleaseReason.ConfirmationMissed => "لم يؤكد الكابتن الحجز في الوقت المحدد",
        ReservationReleaseReason.FinalConfirmationMissed => "لم يؤكد الكابتن التأكيد النهائي في الوقت المحدد",
        ReservationReleaseReason.NoShow => "لم يصل الكابتن في الموعد",
        ReservationReleaseReason.TripCancelled => "أُلغيت الرحلة",
        _ => "ألغت الإدارة الحجز",
    };

    private static string ReasonEn(ReservationReleaseReason reason) => reason switch
    {
        ReservationReleaseReason.DriverReleased => "the driver released the reservation",
        ReservationReleaseReason.ConfirmationMissed => "the driver did not confirm in time",
        ReservationReleaseReason.FinalConfirmationMissed => "the driver did not give the final confirmation in time",
        ReservationReleaseReason.NoShow => "the driver did not arrive on time",
        ReservationReleaseReason.TripCancelled => "the trip was cancelled",
        _ => "support released the reservation",
    };

    private static Dictionary<string, object?> Extra(Trip trip, params (string Key, object? Value)[] more)
    {
        var data = new Dictionary<string, object?>
        {
            ["tripId"] = trip.Id, ["tripNumber"] = trip.TripNumber, ["status"] = System.Text.Json.JsonNamingPolicy.SnakeCaseLower.ConvertName(trip.Status.ToString()), ["scheduledAt"] = trip.ScheduledAt,
        };
        foreach (var (key, value) in more)
        {
            data[key] = value;
        }

        return data;
    }
}
