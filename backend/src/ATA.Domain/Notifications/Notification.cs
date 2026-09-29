using ATA.Domain.Common;

namespace ATA.Domain.Notifications;

/// <summary>
/// Event codes written to <c>notifications.type</c> (doc 08 §F13.2). Rows created before F13 keep their legacy snake_case type
/// (e.g. <c>trip_completed</c>); clients normalise them with <see cref="NotificationEvents.NormalizeLegacy"/>.
/// </summary>
public static class NotificationTypes
{
    public const string DriverApplicationApproved = "driver.application.approved";
    public const string DriverApplicationRejected = "driver.application.rejected";
    public const string DriverSuspended = "driver.suspended";
    public const string DriverReinstated = "driver.reinstated";
    public const string DriverUnderReview = "driver.application.under_review";
    public const string TripDriverAssigned = "trip.driver_assigned";
    public const string TripDriverArrived = "trip.driver_arrived";
    public const string TripStarted = "trip.started";
    public const string TripCompleted = "trip.completed";
    public const string TripCancelled = "trip.cancelled";
    public const string TripNoDrivers = "trip.no_drivers";
    public const string TripPaymentActionRequired = "trip.payment_action_required";
    public const string OfferReceived = "offer.received";
    public const string PaymentSucceeded = "payment.succeeded";
    public const string PaymentFailed = "payment.failed";
    public const string PaymentRefunded = "payment.refunded";
    public const string WalletTopup = "wallet.topup";
    public const string PayoutStatus = "payout.status";
    public const string SettlementReady = "settlement.ready";
    public const string DocumentExpiring = "document.expiring";
    public const string DocumentExpired = "document.expired";
    public const string SafetyCheck = "safety.check";
    public const string SafetyAlert = "safety.alert";
    public const string SafetySosContact = "safety.sos_contact";
    public const string SafetyTripShared = "safety.trip_shared";
    public const string SafetyCaseUpdate = "safety.case_update";
    public const string TripMessage = "trip.message";
    public const string LostItemReported = "lost_item.reported";
    public const string LostItemUpdate = "lost_item.update";
    public const string CancellationFeeCharged = "cancellation.fee_charged";
    public const string CancellationCompensation = "cancellation.compensation";
    public const string ReliabilityWarning = "reliability.warning";
    public const string ReliabilityRestricted = "reliability.restricted";
    public const string CampaignBroadcast = "campaign.broadcast";
    public const string RatingReminder = "rating.reminder";
    public const string IncentiveNew = "incentive.new";
    public const string IncentiveAchieved = "incentive.achieved";
    public const string DriverTierChanged = "driver.tier_changed";
    public const string PromoNew = "promo.new";
}

public class Notification : Entity
{
    public Guid UserId { get; set; }
    public required string Type { get; set; }
    public NotificationCategory Category { get; set; } = NotificationCategory.System;
    public Guid? CampaignId { get; set; }
    public required string TitleAr { get; set; }
    public required string TitleEn { get; set; }
    public required string BodyAr { get; set; }
    public required string BodyEn { get; set; }
    /// <summary>JSON object with extra payload for the client (<c>eventCode</c>, <c>deepLink</c>, <c>entityType</c>, <c>entityId</c>, …).</summary>
    public string? Data { get; set; }
    public DateTime? ReadAt { get; set; }
}
