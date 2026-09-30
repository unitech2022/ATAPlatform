using System.Text.Json;
using ATA.Domain.Cancellation;
using ATA.Domain.Common;

namespace ATA.Domain.Scheduling;

/// <summary>Who took a scheduled trip (<c>scheduled_ride_reservations.source</c>): the marketplace, the requested favourite driver or an admin.</summary>
public enum ReservationSource { Marketplace, Favorite, Admin }

/// <summary>Lifecycle of a driver reservation: <c>reserved</c> → <c>confirmed</c> (first confirmation) → <c>assigned</c> (final confirmation) → <c>completed</c>.</summary>
public enum ReservationStatus { Reserved, Confirmed, Assigned, Released, NoShow, Completed, Cancelled }

public enum ReservationReleaseReason { DriverReleased, ConfirmationMissed, FinalConfirmationMissed, NoShow, TripCancelled, Admin }

public enum ReminderRecipientRole { Passenger, Driver }

public enum ReminderKind { Reminder, ConfirmRequest, FinalConfirmRequest }

public enum ReminderStatus { Pending, Sent, Skipped, Cancelled }

/// <summary>
/// Row of <c>scheduled_ride_rules</c> (doc 11 §F17.2). Resolution: city + category → city → category → global (both null), active rows only; with no
/// active row the code defaults of <see cref="Default"/> apply. Every time offset is in minutes.
/// </summary>
public class ScheduledRideRule : AuditableEntity
{
    public const int DefaultMaxDaysAhead = 7;
    public const int DefaultMinLeadMinutes = 30;
    public const int DefaultMaxOpenPerPassenger = 3;
    public const string DefaultRiderReminderOffsets = "[1440,60,15]";
    public const string DefaultDriverReminderOffsets = "[1440,180]";
    /// <summary>Reminders (and confirmation requests) sent later than this after their <c>send_at</c> are skipped by the reminder job.</summary>
    public const int ReminderLateSkipMinutes = 10;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public Guid? CityId { get; set; }
    public Guid? RideCategoryId { get; set; }
    public int MaxDaysAhead { get; set; } = DefaultMaxDaysAhead;
    public int MinLeadMinutes { get; set; } = DefaultMinLeadMinutes;
    public int MaxOpenPerPassenger { get; set; } = DefaultMaxOpenPerPassenger;
    /// <summary>Scheduled trips are priced with a fixed <c>normal</c> demand level (no surge).</summary>
    public bool LockDemandNormal { get; set; } = true;
    public bool MarketplaceEnabled { get; set; } = true;
    public int MarketplaceRadiusKm { get; set; } = 30;
    /// <summary>How long after booking a requested favourite driver is the only one who sees the trip in the marketplace.</summary>
    public int FavoriteExclusiveMinutes { get; set; } = 30;
    /// <summary>First confirmation is requested at <c>T − this</c>.</summary>
    public int DriverAssignmentLeadMinutes { get; set; } = 60;
    public int ConfirmationTimeoutMinutes { get; set; } = 10;
    /// <summary>Final confirmation is requested at <c>T − this</c>.</summary>
    public int FinalConfirmationMinutesBefore { get; set; } = 15;
    public int FinalConfirmationTimeoutMinutes { get; set; } = 5;
    /// <summary>The normal search starts at <c>T − this</c> for a trip without a finally confirmed driver.</summary>
    public int SearchStartMinutesBefore { get; set; } = 10;
    public string RiderReminderOffsets { get; set; } = DefaultRiderReminderOffsets;
    public string DriverReminderOffsets { get; set; } = DefaultDriverReminderOffsets;
    public int FreeCancelMinutesBefore { get; set; } = 60;
    public CancellationFeeType LateCancelFeeType { get; set; } = CancellationFeeType.Fixed;
    public decimal? LateCancelFeeAmount { get; set; } = 10m;
    public decimal? LateCancelFeePercent { get; set; }
    public decimal LateCancelDriverCompensationPercent { get; set; } = 50m;
    public int DriverFreeReleaseMinutesBefore { get; set; } = 120;
    public int DriverLateReleasePenaltyPoints { get; set; } = 3;
    public int DriverConfirmationMissedPenaltyPoints { get; set; } = 3;
    public int DriverNoShowPenaltyPoints { get; set; } = 6;
    public int DriverNoShowGraceMinutes { get; set; } = 10;
    public int MaxReservationsPerDriver { get; set; } = 5;
    public int ReservationGapMinutes { get; set; } = 30;
    public bool IsActive { get; set; } = true;

    /// <summary>The in-code rule used when no active row applies (the seeded global row has the same values).</summary>
    public static ScheduledRideRule Default() => new();

    public IReadOnlyList<int> RiderOffsets() => ParseOffsets(RiderReminderOffsets);

    public IReadOnlyList<int> DriverOffsets() => ParseOffsets(DriverReminderOffsets);

    public static IReadOnlyList<int> ParseOffsets(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return (JsonSerializer.Deserialize<List<int>>(json, Json) ?? []).Where(o => o > 0).Distinct().OrderByDescending(o => o).ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string SerializeOffsets(IEnumerable<int> offsets) => JsonSerializer.Serialize(offsets.Distinct().OrderByDescending(o => o).ToList(), Json);

    public DateTime MinScheduledAt(DateTime now) => now.AddMinutes(MinLeadMinutes);

    public DateTime MaxScheduledAt(DateTime now) => now.AddDays(MaxDaysAhead);

    public DateTime FreeCancelUntil(DateTime scheduledAt) => scheduledAt.AddMinutes(-FreeCancelMinutesBefore);

    public DateTime SearchStartsAt(DateTime scheduledAt) => scheduledAt.AddMinutes(-SearchStartMinutesBefore);

    public DateTime FirstConfirmationAt(DateTime scheduledAt) => scheduledAt.AddMinutes(-DriverAssignmentLeadMinutes);

    public DateTime FinalConfirmationAt(DateTime scheduledAt) => scheduledAt.AddMinutes(-FinalConfirmationMinutesBefore);

    public DateTime FreeReleaseUntil(DateTime scheduledAt) => scheduledAt.AddMinutes(-DriverFreeReleaseMinutesBefore);

    /// <summary>The late-cancellation fee for a trip whose estimated fare is <paramref name="estimatedFare"/> (pricing-rule fees are supplied by the caller).</summary>
    public decimal LateCancelFee(decimal estimatedFare, decimal pricingRuleFee)
    {
        var fee = LateCancelFeeType switch
        {
            CancellationFeeType.Fixed => LateCancelFeeAmount ?? 0m,
            CancellationFeeType.Percent => estimatedFare * (LateCancelFeePercent ?? 0m) / 100m,
            CancellationFeeType.PricingRule => pricingRuleFee,
            _ => 0m,
        };
        return decimal.Round(Math.Max(0m, Math.Min(fee, estimatedFare)), 2, MidpointRounding.AwayFromZero);
    }
}

/// <summary>Row of <c>scheduled_ride_reservations</c> (doc 11 §F17.2): one driver's claim on one scheduled trip; at most one active reservation per trip.</summary>
public class ScheduledRideReservation : AuditableEntity
{
    public Guid TripId { get; set; }
    public Guid DriverId { get; set; }
    public ReservationSource Source { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Reserved;
    public DateTime ReservedAt { get; set; }
    public DateTime? ConfirmRequestedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? FinalConfirmRequestedAt { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? ReleasedAt { get; set; }
    public ReservationReleaseReason? ReleaseReason { get; set; }
    public bool IsLateRelease { get; set; }
    public int PenaltyPoints { get; set; }

    public bool IsActive => Status is ReservationStatus.Reserved or ReservationStatus.Confirmed or ReservationStatus.Assigned;

    /// <summary>Active and not yet finally confirmed (the trip is still <c>scheduled</c>).</summary>
    public bool IsPreAssignment => Status is ReservationStatus.Reserved or ReservationStatus.Confirmed;

    public static readonly ReservationStatus[] ActiveStatuses = [ReservationStatus.Reserved, ReservationStatus.Confirmed, ReservationStatus.Assigned];

    /// <summary>When the first confirmation stops being accepted (null until it was requested).</summary>
    public DateTime? ConfirmDeadline(ScheduledRideRule rule) =>
        Status == ReservationStatus.Reserved && ConfirmRequestedAt is { } requested ? requested.AddMinutes(rule.ConfirmationTimeoutMinutes) : null;

    public DateTime? FinalConfirmDeadline(ScheduledRideRule rule) =>
        Status == ReservationStatus.Confirmed && FinalConfirmRequestedAt is { } requested ? requested.AddMinutes(rule.FinalConfirmationTimeoutMinutes) : null;

    /// <summary>At-fault driver reservations for reliability (doc 11 §F17.3.10): late releases, missed confirmations and no-shows.</summary>
    public bool IsDriverFault =>
        Status == ReservationStatus.NoShow
        || (Status == ReservationStatus.Released
            && (IsLateRelease || ReleaseReason is ReservationReleaseReason.ConfirmationMissed or ReservationReleaseReason.FinalConfirmationMissed));
}

/// <summary>Row of <c>scheduled_ride_reminders</c> (doc 11 §F17.2): a notification due at <c>send_at</c> for the rider or the reserved driver.</summary>
public class ScheduledRideReminder : Entity
{
    public Guid TripId { get; set; }
    public Guid? ReservationId { get; set; }
    public Guid RecipientUserId { get; set; }
    public ReminderRecipientRole RecipientRole { get; set; }
    public ReminderKind Kind { get; set; }
    public int OffsetMinutes { get; set; }
    public DateTime SendAt { get; set; }
    public DateTime? SentAt { get; set; }
    public ReminderStatus Status { get; set; } = ReminderStatus.Pending;
}
