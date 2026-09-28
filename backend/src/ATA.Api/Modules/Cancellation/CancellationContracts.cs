using ATA.Domain.Cancellation;
using ATA.Domain.Common;
using ATA.Domain.Trips;

namespace ATA.Api.Modules.Cancellation;

public sealed class CancellationOptions
{
    public const string Section = "Cancellation";
    /// <summary>Minimum wait at the pickup before a driver can report a no-show (never below the free waiting time).</summary>
    public int NoShowWaitMinutes { get; set; } = 5;
    public int ExcuseReviewSlaHours { get; set; } = 48;
}

public sealed class ReliabilityOptions
{
    public const string Section = "Reliability";
    public int WindowDays { get; set; } = 30;
    public int PointsExpiryDays { get; set; } = 30;
    /// <summary>Runs <c>ReliabilityRecalcJob</c> (daily 02:00 Riyadh) and <c>RestrictionExpiryJob</c> (every 5 min); <c>false</c> in tests.</summary>
    public bool JobsEnabled { get; set; } = true;
    public int RecalcHourLocal { get; set; } = 2;
}

public sealed record CancelPreviewRequest(string? ReasonCode);

/// <summary><c>POST …/cancel/preview</c>: the fee (passenger) or the penalty points (driver) the cancellation would cost right now.</summary>
public sealed record CancelPreviewDto(CancellationStage Stage, BookingType BookingType, decimal Fee, int PenaltyPoints, bool IsFree, DateTime? FreeUntil, bool RequiresReview, string Message);

public sealed record NoShowRequest(decimal? Lat, decimal? Lng);

public sealed record CancellationReasonDto(string Code, string Name, bool RequiresNote, bool IsExcusable, bool IsEmergency, IReadOnlyList<CancellationStage>? Stages);

/// <summary><c>Trip.cancellation</c>: the passenger sees the fee, the driver the compensation; admins see everything.</summary>
public sealed record TripCancellationDto(
    Guid? EventId,
    TripActor? Actor,
    CancellationStage Stage,
    string ReasonCode,
    string? ReasonName,
    string? Note,
    AtFault AtFault,
    decimal? Fee,
    decimal? FeeCharged,
    CancellationFeeStatus FeeStatus,
    decimal? Compensation,
    int? PenaltyPoints,
    ExcuseStatus ExcuseStatus,
    string? ReviewNote);

public sealed record ReliabilityNextLevelDto(RestrictionLevel Level, int? MinPenaltyPoints, decimal? MinCancellationRate);

public sealed record ReliabilityEventDto(Guid Id, Guid TripId, string? TripNumber, CancellationStage Stage, string ReasonCode, string? ReasonName, AtFault AtFault, decimal FeeCharged,
    int PenaltyPoints, ExcuseStatus ExcuseStatus, bool CountsTowardRate, DateTime CreatedAt);

public sealed record ReliabilityEffectsDto(decimal MatchingFactor, decimal IncentiveMultiplier);

public sealed record ReliabilitySummaryDto(
    Role Role,
    RestrictionLevel Level,
    DateTime? RestrictedUntil,
    int WindowDays,
    int TripsAccepted,
    int TripsCompleted,
    int CancellationsAtFault,
    decimal CancellationRate,
    decimal ReliabilityRate,
    int NoShowCount,
    int PenaltyPoints,
    ReliabilityNextLevelDto? NextLevel,
    IReadOnlyList<ReliabilityEventDto> RecentEvents,
    int? OffersReceived = null,
    int? OffersAccepted = null,
    decimal? AcceptanceRate = null,
    ReliabilityEffectsDto? Effects = null);

// ----- admin -----

public sealed record AdminCancellationReasonDto(Guid Id, string Code, CancellationActor Actor, string NameAr, string NameEn, IReadOnlyList<CancellationStage>? Stages, bool IsExcusable,
    bool IsEmergency, bool RequiresNote, bool IsSelectable, int SortOrder, bool IsActive, DateTime CreatedAt, DateTime UpdatedAt);

public sealed record CancellationReasonRequest(string? Code, CancellationActor? Actor, string? NameAr, string? NameEn, List<CancellationStage>? Stages, bool? IsExcusable,
    bool? IsEmergency, bool? RequiresNote, bool? IsSelectable, int? SortOrder, bool? IsActive);

public sealed record CancellationRuleDto(Guid Id, string Name, CancellationActor Actor, CancellationStage Stage, BookingType? BookingType, Guid? RideCategoryId, Guid? ZoneId,
    int FreeWindowSeconds, CancellationFeeType FeeType, decimal? FeeAmount, decimal? FeePercent, decimal? MinFee, decimal? MaxFee, decimal DriverCompensationPercent,
    int PenaltyPoints, int Priority, bool IsActive, DateTime CreatedAt, DateTime UpdatedAt);

public sealed record CancellationRuleRequest(string? Name, CancellationActor? Actor, CancellationStage? Stage, BookingType? BookingType, Guid? RideCategoryId, Guid? ZoneId,
    int? FreeWindowSeconds, CancellationFeeType? FeeType, decimal? FeeAmount, decimal? FeePercent, decimal? MinFee, decimal? MaxFee, decimal? DriverCompensationPercent,
    int? PenaltyPoints, int? Priority, bool? IsActive);

public sealed record SimulateCancellationRequest(CancellationActor? Actor, CancellationStage? Stage, BookingType? BookingType, Guid? RideCategoryId, Guid? ZoneId,
    int? SecondsSinceAnchor, decimal? EstimatedFare);

public sealed record SimulateCancellationResult(Guid? RuleId, string? RuleName, decimal Fee, decimal Compensation, int PenaltyPoints, bool IsFree);

public sealed record ReliabilityThresholdDto(Guid Id, Role Role, RestrictionLevel Level, int? MinPenaltyPoints, decimal? MinCancellationRate, int MinTripsForRate, int? RestrictionHours,
    decimal? DeprioritizeFactor, decimal? IncentiveReductionPercent, int SortOrder, bool IsActive);

public sealed record ReliabilityThresholdRequest(int? MinPenaltyPoints, decimal? MinCancellationRate, int? MinTripsForRate, int? RestrictionHours, decimal? DeprioritizeFactor,
    decimal? IncentiveReductionPercent, int? SortOrder, bool? IsActive);

public sealed record AdminCancellationEventDto(Guid Id, Guid TripId, string TripNumber, TripActor Actor, Guid? UserId, string? UserName, AtFault AtFault, CancellationStage Stage,
    string ReasonCode, string? ReasonName, string? Note, decimal EstimatedFare, decimal FeeAmount, decimal FeeCharged, CancellationFeeStatus FeeStatus, CancellationFeeMethod? FeeMethod,
    decimal CompensationAmount, int PenaltyPoints, bool CountsTowardRate, ExcuseStatus ExcuseStatus, string? ReviewedByName, DateTime? ReviewedAt, string? ReviewNote, DateTime CreatedAt);

public sealed record ExcuseQueueItemDto(Guid Id, Guid TripId, string TripNumber, TripActor Actor, Guid? UserId, string? UserName, AtFault AtFault, CancellationStage Stage,
    string ReasonCode, string? ReasonName, string? Note, decimal EstimatedFare, decimal FeeAmount, decimal FeeCharged, CancellationFeeStatus FeeStatus, CancellationFeeMethod? FeeMethod,
    decimal CompensationAmount, int PenaltyPoints, bool CountsTowardRate, ExcuseStatus ExcuseStatus, string? ReviewedByName, DateTime? ReviewedAt, string? ReviewNote, DateTime CreatedAt,
    double AgeHours, bool SlaBreached, int PendingPenaltyPoints);

public sealed record ReviewExcuseRequest(string? Decision, string? Note);

public sealed record ReliabilityProfileListItemDto(Guid UserId, string? Name, string? Phone, Role Role, RestrictionLevel Level, DateTime? RestrictedUntil, decimal CancellationRate,
    decimal ReliabilityRate, int PenaltyPoints, int NoShowCount, int TripsAccepted, DateTime? LastComputedAt);

public sealed record ReliabilityAdjustmentDto(Guid Id, ReliabilityAction Action, int? Points, RestrictionLevel? Level, DateTime? Until, string Reason, string? CreatedByName, DateTime CreatedAt);

public sealed record ReliabilityProfileDetailDto(Guid UserId, string? Name, string? Phone, Role Role, RestrictionLevel Level, DateTime? RestrictedUntil, decimal CancellationRate,
    decimal ReliabilityRate, int PenaltyPoints, int NoShowCount, int TripsAccepted, DateTime? LastComputedAt, int WindowDays, int TripsRequested, int TripsCompleted,
    int CancellationsAtFault, int? OffersReceived, int? OffersAccepted, decimal? AcceptanceRate, DateTime? LevelChangedAt, ReliabilityNextLevelDto? NextLevel,
    ReliabilityEffectsDto Effects, IReadOnlyList<ReliabilityEventDto> Events, IReadOnlyList<ReliabilityAdjustmentDto> Adjustments);

public sealed record ReliabilityAdjustRequest(Role? Role, ReliabilityAction? Action, int? Points, RestrictionLevel? Level, DateTime? Until, string? Reason);

/// <summary><c>GET /admin/cancellations/stats</c>: KPIs of §F14.7, rates in [0, 1].</summary>
public sealed record CancellationStatsDto(
    DateOnly? From,
    DateOnly? To,
    int TotalCancellations,
    int TripsAssigned,
    decimal PassengerCancellationRate,
    decimal DriverCancellationRate,
    decimal CancellationFeeRevenue,
    decimal RepeatCancellationRate,
    decimal DriverReliabilityRate,
    decimal PassengerReliabilityRate,
    decimal ExcuseApprovalRate,
    decimal NoShowRate);
