using ATA.Api.Modules.Notifications;
using ATA.Domain.Catalog;
using ATA.Domain.Drivers;
using ATA.Domain.Notifications;
using ATA.Domain.Trips;

namespace ATA.Api.Modules.Trips;

/// <summary>Builds the F8 lifecycle notifications (catalogue codes <c>trip.*</c>) sent through <see cref="INotificationDispatcher"/>.</summary>
public static class TripNotifications
{
    public const string EntityType = "trip";

    public static NotificationRequest DriverAssigned(Trip trip, Guid passengerUserId, string? driverName, Vehicle? vehicle, int etaSeconds) =>
        new(NotificationTypes.TripDriverAssigned, passengerUserId,
            NotificationPlaceholders.Of(
                ("driverName", driverName ?? string.Empty),
                ("vehicle", vehicle is null ? string.Empty : $"{vehicle.Make} {vehicle.Model} {vehicle.Color}"),
                ("plateNumber", vehicle?.PlateNumber ?? string.Empty),
                ("etaMinutes", Math.Max(1, (int)Math.Ceiling(etaSeconds / 60d)))),
            EntityType, trip.Id, Extra(trip));

    public static NotificationRequest DriverArrived(Trip trip, Guid passengerUserId, string? driverName, string? plateNumber, int freeWaitingMinutes) =>
        new(NotificationTypes.TripDriverArrived, passengerUserId,
            NotificationPlaceholders.Of(("driverName", driverName ?? string.Empty), ("plateNumber", plateNumber ?? string.Empty), ("freeWaitingMinutes", freeWaitingMinutes)),
            EntityType, trip.Id, Extra(trip));

    public static NotificationRequest Started(Trip trip, Guid passengerUserId) =>
        new(NotificationTypes.TripStarted, passengerUserId, NotificationPlaceholders.Of(("dropoffName", trip.DropoffName)), EntityType, trip.Id, Extra(trip));

    public static NotificationRequest Completed(Trip trip, Guid passengerUserId, decimal fare) =>
        new(NotificationTypes.TripCompleted, passengerUserId,
            NotificationPlaceholders.Of(("tripNumber", trip.TripNumber)).Money("fare", fare),
            EntityType, trip.Id, Extra(trip, ("finalFare", fare), ("paymentMethod", trip.PaymentMethod.ToString().ToLowerInvariant())));

    public static NotificationRequest Cancelled(Trip trip, Guid recipientUserId, decimal fee = 0m)
    {
        var (ar, en) = trip.CancelledBy switch
        {
            CancelledBy.Passenger => ("ألغاها الراكب", "cancelled by the passenger"),
            CancelledBy.Driver => ("ألغاها الكابتن", "cancelled by the driver"),
            CancelledBy.Admin => ("ألغتها الإدارة", "cancelled by support"),
            _ => ("ألغاها النظام", "cancelled by the system"),
        };
        return new(NotificationTypes.TripCancelled, recipientUserId,
            NotificationPlaceholders.Of(("tripNumber", trip.TripNumber)).Localized("cancelledBy", ar, en).Money("fee", fee),
            EntityType, trip.Id, Extra(trip, ("cancelledBy", trip.CancelledBy?.ToString().ToLowerInvariant())));
    }

    public static NotificationRequest NoDrivers(Trip trip, Guid passengerUserId, RideCategory? category) =>
        new(NotificationTypes.TripNoDrivers, passengerUserId,
            NotificationPlaceholders.Of().Localized("categoryName", category?.NameAr ?? string.Empty, category?.NameEn ?? string.Empty),
            EntityType, trip.Id, Extra(trip));

    /// <summary><c>trip.favorite_fallback</c> (F16): sent when the replacement driver of a favourite request that was rejected / expired is assigned.</summary>
    public static NotificationRequest FavoriteFallback(Trip trip, Guid passengerUserId, string? driverName) =>
        new(NotificationTypes.TripFavoriteFallback, passengerUserId, NotificationPlaceholders.Of(("driverName", driverName ?? string.Empty)), EntityType, trip.Id, Extra(trip));

    public static NotificationRequest PaymentActionRequired(Trip trip, Guid passengerUserId, decimal amount, Guid paymentId) =>
        new(NotificationTypes.TripPaymentActionRequired, passengerUserId, NotificationPlaceholders.Of().Money("amount", amount),
            EntityType, trip.Id, Extra(trip, ("paymentId", paymentId)));

    private static Dictionary<string, object?> Extra(Trip trip, params (string Key, object? Value)[] more)
    {
        var data = new Dictionary<string, object?> { ["tripId"] = trip.Id, ["tripNumber"] = trip.TripNumber, ["status"] = System.Text.Json.JsonNamingPolicy.SnakeCaseLower.ConvertName(trip.Status.ToString()) };
        foreach (var (key, value) in more)
        {
            data[key] = value;
        }

        return data;
    }
}
