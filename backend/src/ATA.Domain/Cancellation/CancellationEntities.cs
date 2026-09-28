using ATA.Domain.Common;
using ATA.Domain.Trips;

namespace ATA.Domain.Cancellation;

public enum CancellationStage { BeforeAccept, AfterAccept, EnRoute, Arrived, Waiting, NoShow, Scheduled }

/// <summary>Who may pick a reason / who a rule applies to (<c>system</c> only for reasons).</summary>
public enum CancellationActor { Passenger, Driver, System }

public enum AtFault { Passenger, Driver, None }

public enum CancellationFeeType { None, Fixed, Percent, PricingRule }

public enum CancellationFeeStatus { None, Charged, PendingReview, Waived, Failed, Refunded }

public enum CancellationFeeMethod { Wallet, Card, Corporate }

public enum ExcuseStatus { NotApplicable, Pending, Approved, Rejected }

public enum RestrictionLevel { None, Warning, MatchingDeprioritized, IncentivesReduced, TemporarilyRestricted, Suspended }

public enum ReliabilityAction { AddPoints, RemovePoints, SetLevel, ClearRestriction }

public static class RestrictionLevels
{
    public static int Severity(RestrictionLevel level) => (int)level;

    public static bool IsRestricting(RestrictionLevel level) => level is RestrictionLevel.TemporarilyRestricted or RestrictionLevel.Suspended;
}

/// <summary>Row of <c>cancellation_reasons</c>. <c>stages</c> is a JSON array of stage names (null = every stage).</summary>
public class CancellationReason : AuditableEntity
{
    public required string Code { get; set; }
    public CancellationActor Actor { get; set; }
    public required string NameAr { get; set; }
    public required string NameEn { get; set; }
    public string? Stages { get; set; }
    public bool IsExcusable { get; set; }
    public bool IsEmergency { get; set; }
    public bool RequiresNote { get; set; }
    public bool IsSelectable { get; set; } = true;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Emergency reasons are treated as excusable (review before any fee or points).</summary>
    public bool NeedsReview => IsExcusable || IsEmergency;
}

/// <summary>Row of <c>cancellation_rules</c>: fee, driver compensation and penalty points for an actor and stage.</summary>
public class CancellationRule : AuditableEntity
{
    public required string Name { get; set; }
    public CancellationActor Actor { get; set; }
    public CancellationStage Stage { get; set; }
    public BookingType? BookingType { get; set; }
    public Guid? RideCategoryId { get; set; }
    public Guid? ZoneId { get; set; }
    public int FreeWindowSeconds { get; set; }
    public CancellationFeeType FeeType { get; set; }
    public decimal? FeeAmount { get; set; }
    public decimal? FeePercent { get; set; }
    public decimal? MinFee { get; set; }
    public decimal? MaxFee { get; set; }
    public decimal DriverCompensationPercent { get; set; }
    public int PenaltyPoints { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>One row per cancelled trip (<c>cancellation_events</c>, unique trip).</summary>
public class CancellationEvent : Entity
{
    public Guid TripId { get; set; }
    public TripActor Actor { get; set; }
    /// <summary>The user who cancelled (the actor), null for the system.</summary>
    public Guid? UserId { get; set; }
    public AtFault AtFault { get; set; } = AtFault.None;
    public CancellationStage Stage { get; set; }
    public BookingType BookingType { get; set; }
    public Guid? ReasonId { get; set; }
    public required string ReasonCode { get; set; }
    public string? Note { get; set; }
    public Guid? RuleId { get; set; }
    public int? SecondsSinceAccept { get; set; }
    public int? SecondsSinceArrival { get; set; }
    public decimal EstimatedFare { get; set; }
    /// <summary>The computed fee (the potential fee while an excuse is pending).</summary>
    public decimal FeeAmount { get; set; }
    public decimal FeeCharged { get; set; }
    public CancellationFeeStatus FeeStatus { get; set; } = CancellationFeeStatus.None;
    public CancellationFeeMethod? FeeMethod { get; set; }
    public decimal CompensationAmount { get; set; }
    public int PenaltyPoints { get; set; }
    public bool CountsTowardRate { get; set; }
    public ExcuseStatus ExcuseStatus { get; set; } = ExcuseStatus.NotApplicable;
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }

    /// <summary>A system cancellation (<c>no_drivers</c>, <c>payment_failed</c>…): nobody at fault, no fee, not counted.</summary>
    public static CancellationEvent System(Trip trip, string reasonCode, Guid? reasonId = null) => new()
    {
        TripId = trip.Id,
        Actor = TripActor.System,
        AtFault = AtFault.None,
        Stage = trip.Status is TripStatus.Requested or TripStatus.Searching or TripStatus.NoDrivers ? CancellationStage.BeforeAccept : CancellationStage.AfterAccept,
        BookingType = trip.BookingType,
        ReasonId = reasonId,
        ReasonCode = reasonCode,
        EstimatedFare = trip.EstimatedFare,
    };
}

/// <summary>Rolling reliability of a user in one role (<c>reliability_profiles</c>).</summary>
public class ReliabilityProfile : AuditableEntity
{
    public Guid UserId { get; set; }
    public Role Role { get; set; }
    public int WindowDays { get; set; }
    public int TripsRequested { get; set; }
    public int OffersReceived { get; set; }
    public int OffersAccepted { get; set; }
    public int TripsAccepted { get; set; }
    public int TripsCompleted { get; set; }
    public int CancellationsAtFault { get; set; }
    public int NoShowCount { get; set; }
    public decimal CancellationRate { get; set; }
    public decimal? AcceptanceRate { get; set; }
    public decimal ReliabilityRate { get; set; } = 1m;
    public int PenaltyPoints { get; set; }
    public RestrictionLevel RestrictionLevel { get; set; } = RestrictionLevel.None;
    public DateTime? RestrictedUntil { get; set; }
    public DateTime? LevelChangedAt { get; set; }
    public DateTime LastComputedAt { get; set; }
}

/// <summary>One step of the restriction ladder per role (<c>reliability_thresholds</c>); the highest <c>sort_order</c> is the most severe.</summary>
public class ReliabilityThreshold : AuditableEntity
{
    public Role Role { get; set; }
    public RestrictionLevel Level { get; set; }
    public int? MinPenaltyPoints { get; set; }
    public decimal? MinCancellationRate { get; set; }
    public int MinTripsForRate { get; set; } = 10;
    public int? RestrictionHours { get; set; }
    public decimal? DeprioritizeFactor { get; set; }
    public decimal? IncentiveReductionPercent { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public bool IsMetBy(int points, int tripsAccepted, decimal cancellationRate) =>
        (MinPenaltyPoints is { } minPoints && points >= minPoints)
        || (MinCancellationRate is { } minRate && tripsAccepted >= MinTripsForRate && cancellationRate >= minRate);
}

/// <summary>A manual change by operations (<c>reliability_adjustments</c>).</summary>
public class ReliabilityAdjustment : Entity
{
    public Guid UserId { get; set; }
    public Role Role { get; set; }
    public ReliabilityAction Action { get; set; }
    /// <summary>Signed points for <c>add_points</c> / <c>remove_points</c>.</summary>
    public int? Points { get; set; }
    public RestrictionLevel? Level { get; set; }
    public DateTime? Until { get; set; }
    public required string Reason { get; set; }
    public Guid CreatedBy { get; set; }
}
