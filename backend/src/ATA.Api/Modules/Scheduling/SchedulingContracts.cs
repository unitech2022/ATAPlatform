using ATA.Api.Modules.Trips;
using ATA.Domain.Cancellation;
using ATA.Domain.Scheduling;

namespace ATA.Api.Modules.Scheduling;

public sealed class SchedulingOptions
{
    public const string Section = "Scheduling";
    /// <summary>Runs <c>ScheduledRideWorker</c> and <c>ScheduledReminderJob</c>; <c>false</c> in tests, which call <c>RunOnceAsync</c> directly.</summary>
    public bool JobsEnabled { get; set; } = true;
    public int WorkerIntervalSeconds { get; set; } = 30;
    public int ReminderIntervalSeconds { get; set; } = 60;
}

// ----- passenger -----

public sealed record SchedulingRulesDto(
    int MaxDaysAhead, int MinLeadMinutes, DateTime MinScheduledAt, DateTime MaxScheduledAt, int FreeCancelMinutesBefore, decimal? LateCancelFee, IReadOnlyList<int> ReminderOffsets);

public sealed record TripReservationDto(
    ReservationStatus Status, string? DriverFirstName, string? DriverPhotoUrl, decimal RatingAvg, TripVehicleDto? Vehicle, DateTime ReservedAt);

/// <summary><c>Trip.scheduling</c> of a scheduled booking.</summary>
public sealed record TripSchedulingDto(DateTime FreeCancelUntil, DateTime SearchStartsAt, TripReservationDto? Reservation);

// ----- driver -----

public sealed record MarketplaceCategoryDto(string Code, string Name);

public sealed record ApproxPointDto(decimal Lat, decimal Lng);

public sealed record MarketplaceTripDto(
    Guid TripId, DateTime ScheduledAt, MarketplaceCategoryDto RideCategory, string? PickupArea, ApproxPointDto PickupApprox, string? DropoffArea, decimal DistanceToPickupKm,
    int TripDistanceMeters, decimal EstimatedFare, decimal DriverNetEarnings, bool IsAirport, bool IsFavoriteRequest, DateTime? ExclusiveUntil);

public sealed record ReservationPlaceDto(string? Name, string? Address, decimal Lat, decimal Lng);

/// <summary>
/// <c>Reservation</c> of doc 11 §F17.4. The exact pickup and the passenger's first name are only given while the reservation is active (or completed); released ones carry
/// approximate coordinates. <see cref="Trip"/> is set by the final confirmation.
/// </summary>
public sealed record ReservationDto(
    Guid Id, Guid TripId, ReservationStatus Status, ReservationSource Source, DateTime ScheduledAt, ReservationPlaceDto Pickup, ReservationPlaceDto Dropoff, string? PassengerFirstName,
    decimal EstimatedFare, decimal DriverNetEarnings, DateTime? ConfirmDeadline, DateTime? FinalConfirmDeadline, DateTime FreeReleaseUntil, DateTime ReservedAt,
    int PenaltyPoints = 0, ReservationReleaseReason? ReleaseReason = null, TripDto? Trip = null);

public sealed record ReleaseReservationRequest(string? Reason);

// ----- admin -----

public sealed record ScheduledRideRuleDto(
    Guid Id, Guid? CityId, Guid? RideCategoryId, int MaxDaysAhead, int MinLeadMinutes, int MaxOpenPerPassenger, bool LockDemandNormal, bool MarketplaceEnabled, int MarketplaceRadiusKm,
    int FavoriteExclusiveMinutes, int DriverAssignmentLeadMinutes, int ConfirmationTimeoutMinutes, int FinalConfirmationMinutesBefore, int FinalConfirmationTimeoutMinutes,
    int SearchStartMinutesBefore, IReadOnlyList<int> RiderReminderOffsets, IReadOnlyList<int> DriverReminderOffsets, int FreeCancelMinutesBefore, CancellationFeeType LateCancelFeeType,
    decimal? LateCancelFeeAmount, decimal? LateCancelFeePercent, decimal LateCancelDriverCompensationPercent, int DriverFreeReleaseMinutesBefore, int DriverLateReleasePenaltyPoints,
    int DriverConfirmationMissedPenaltyPoints, int DriverNoShowPenaltyPoints, int DriverNoShowGraceMinutes, int MaxReservationsPerDriver, int ReservationGapMinutes, bool IsActive,
    DateTime CreatedAt, DateTime UpdatedAt);

/// <summary>Every column of <c>scheduled_ride_rules</c> in camelCase; omitted numbers take the doc 11 defaults.</summary>
public sealed record ScheduledRideRuleUpsertRequest(
    Guid? CityId, Guid? RideCategoryId, int? MaxDaysAhead, int? MinLeadMinutes, int? MaxOpenPerPassenger, bool? LockDemandNormal, bool? MarketplaceEnabled, int? MarketplaceRadiusKm,
    int? FavoriteExclusiveMinutes, int? DriverAssignmentLeadMinutes, int? ConfirmationTimeoutMinutes, int? FinalConfirmationMinutesBefore, int? FinalConfirmationTimeoutMinutes,
    int? SearchStartMinutesBefore, List<int>? RiderReminderOffsets, List<int>? DriverReminderOffsets, int? FreeCancelMinutesBefore, CancellationFeeType? LateCancelFeeType,
    decimal? LateCancelFeeAmount, decimal? LateCancelFeePercent, decimal? LateCancelDriverCompensationPercent, int? DriverFreeReleaseMinutesBefore, int? DriverLateReleasePenaltyPoints,
    int? DriverConfirmationMissedPenaltyPoints, int? DriverNoShowPenaltyPoints, int? DriverNoShowGraceMinutes, int? MaxReservationsPerDriver, int? ReservationGapMinutes, bool? IsActive);

/// <summary>Row of <c>GET /admin/scheduled-trips</c>; <see cref="ReservationStatus"/> is <c>none</c> without an active reservation. <c>rideCategoryId</c>, <c>zoneId</c> (pickup), <c>driverId</c> and <c>status</c> (trip) are extras.</summary>
public sealed record AdminScheduledTripDto(
    Guid TripId, string TripNumber, DateTime ScheduledAt, string? PassengerName, string CategoryName, string PickupName, string DropoffName, string ReservationStatus, string? DriverName,
    int MinutesToPickup, bool AtRisk, Guid RideCategoryId, Guid? ZoneId, Guid? DriverId, string Status);

/// <summary>One reservation of a trip on the admin trip detail (the §F17.2 columns in camelCase plus the driver's name).</summary>
public sealed record AdminReservationDto(
    Guid Id, Guid DriverId, string? DriverName, ReservationSource Source, ReservationStatus Status, DateTime ReservedAt, DateTime? ConfirmRequestedAt, DateTime? ConfirmedAt,
    DateTime? FinalConfirmRequestedAt, DateTime? AssignedAt, DateTime? ReleasedAt, ReservationReleaseReason? ReleaseReason, bool IsLateRelease, int PenaltyPoints);

public sealed record AdminReminderDto(
    Guid Id, ReminderRecipientRole RecipientRole, ReminderKind Kind, int OffsetMinutes, DateTime SendAt, DateTime? SentAt, ReminderStatus Status);

public sealed record AdminReservationBriefDto(ReservationStatus Status, Guid DriverId, string? DriverName, string? DriverFirstName, DateTime ReservedAt);

/// <summary><c>scheduling</c> of the admin trip detail: the passenger's <c>Trip.scheduling</c> plus every reservation and reminder of the trip.</summary>
public sealed record AdminTripSchedulingDto(
    DateTime FreeCancelUntil, DateTime SearchStartsAt, AdminReservationBriefDto? Reservation, IReadOnlyList<AdminReservationDto> Reservations, IReadOnlyList<AdminReminderDto> Reminders);

public sealed record AssignScheduledTripRequest(Guid? DriverId);

public sealed record AdminReleaseReservationRequest(string? Reason);

public sealed record SchedulingStatsDto(
    int Booked, int Completed, int CancelledByPassenger, int CancelledLate, int DriverReleases, int ConfirmationMissed, int DriverNoShows, int Rematched, decimal ScheduledCompletionRate,
    decimal ScheduledCancellationRate, decimal? AvgReservationLeadHours, decimal DriverCommitmentRate, decimal DriverNoShowRate);
