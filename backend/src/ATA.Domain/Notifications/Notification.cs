using ATA.Domain.Common;

namespace ATA.Domain.Notifications;

public static class NotificationTypes
{
    public const string DriverApplicationApproved = "driver_application_approved";
    public const string DriverApplicationRejected = "driver_application_rejected";
    public const string DriverSuspended = "driver_suspended";
    public const string DriverReinstated = "driver_reinstated";
    public const string DriverUnderReview = "driver_application_under_review";
    public const string TripDriverAssigned = "trip_driver_assigned";
    public const string TripDriverArrived = "trip_driver_arrived";
    public const string TripCompleted = "trip_completed";
    public const string TripCancelled = "trip_cancelled";
    public const string TripNoDrivers = "trip_no_drivers";
}

public class Notification : Entity
{
    public Guid UserId { get; set; }
    public required string Type { get; set; }
    public required string TitleAr { get; set; }
    public required string TitleEn { get; set; }
    public required string BodyAr { get; set; }
    public required string BodyEn { get; set; }
    /// <summary>JSON object with extra payload for the client.</summary>
    public string? Data { get; set; }
    public DateTime? ReadAt { get; set; }
}
