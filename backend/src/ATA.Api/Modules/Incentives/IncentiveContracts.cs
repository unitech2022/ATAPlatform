using ATA.Domain.Drivers;
using ATA.Domain.Incentives;

namespace ATA.Api.Modules.Incentives;

public sealed class TiersOptions
{
    public const string Section = "Tiers";
    /// <summary>Completed trips are counted over the last <c>PeriodDays</c> days.</summary>
    public int PeriodDays { get; set; } = 28;
    /// <summary><c>DriverTierRecalcJob</c>: local (Riyadh) day of week (0 = Sunday) and hour.</summary>
    public int RecalcDayOfWeek { get; set; }
    public int RecalcHourLocal { get; set; } = 3;
}

public sealed class IncentivesOptions
{
    public const string Section = "Incentives";
    /// <summary>An achieved period is paid this many hours after it ends (fraud checks).</summary>
    public int PayoutDelayHours { get; set; } = 2;
    /// <summary>Runs <c>IncentivePeriodJob</c>, <c>IncentivePayoutJob</c> and <c>DriverTierRecalcJob</c> — <c>false</c> in tests.</summary>
    public bool JobsEnabled { get; set; } = true;
}

// ----- tiers -----

public sealed record TierMetricsDto(int CompletedTrips, decimal RatingAvg, decimal AcceptanceRate, decimal CancellationRate)
{
    public static TierMetricsDto From(TierMetrics m) => new(m.CompletedTrips, m.RatingAvg, m.AcceptanceRate, m.CancellationRate);
}

public sealed record TierRequirementsDto(int MinCompletedTrips, decimal MinRatingAvg, decimal MinAcceptanceRate, decimal MaxCancellationRate);

public sealed record TierBenefitsDto(decimal CommissionDiscountPercent, string? Text);

/// <summary><c>GET /driver/tier</c>.</summary>
public sealed record DriverTierDto(DriverTier Tier, DriverTier? NextTier, int PeriodDays, TierMetricsDto Metrics, TierRequirementsDto? NextRequirements, TierBenefitsDto Benefits,
    DateTime RecalculatesAt);

public sealed record TierRuleDto(
    Guid Id, DriverTier Tier, int MinCompletedTrips, decimal MinRatingAvg, decimal MinAcceptanceRate, decimal MaxCancellationRate, decimal CommissionDiscountPercent,
    decimal MatchingNorm, string? BenefitsAr, string? BenefitsEn, int SortOrder, DateTime UpdatedAt, int DriversCount);

public sealed record TierRuleUpdateRequest(
    int? MinCompletedTrips, decimal? MinRatingAvg, decimal? MinAcceptanceRate, decimal? MaxCancellationRate, decimal? CommissionDiscountPercent, decimal? MatchingNorm,
    string? BenefitsAr, string? BenefitsEn, int? SortOrder);

public sealed record TierHistoryDto(Guid Id, DriverTier? FromTier, DriverTier ToTier, TierMetricsDto? Metrics, TierChangeReason Reason, DateTime ComputedAt, string? Note, string? ActorName);

public sealed record SetTierRequest(DriverTier? Tier, string? Reason);

public sealed record TierRecalculationDto(int Evaluated, int Changed);

// ----- incentives (driver) -----

public sealed record IncentiveWindowDto(IReadOnlyList<int>? DaysOfWeek, string? From, string? To);

public sealed record IncentiveZoneDto(Guid Id, string Name);

public sealed record IncentiveZonePolygonDto(Guid Id, string Name, System.Text.Json.JsonElement Polygon);

public sealed record DriverIncentiveProgressSummaryDto(int CompletedTrips, IncentiveProgressStatus Status, decimal? RewardAmount, DateTime? PaidAt);

/// <summary>Item of <c>GET /driver/incentives</c>.</summary>
public sealed record DriverIncentiveDto(
    Guid Id, string Name, string? Description, IncentiveType Type, int TargetTrips, decimal RewardAmount, DateTime PeriodStart, DateTime PeriodEnd,
    IncentiveWindowDto? Window, IReadOnlyList<IncentiveZoneDto>? Zones, IReadOnlyList<string>? RideCategoryCodes, bool RequiresOptIn, bool OptedIn,
    DriverIncentiveProgressSummaryDto? Progress, decimal RewardMultiplier = 1m, decimal? EffectiveRewardAmount = null);

/// <summary><c>GET /driver/incentives/{id}</c>: the list item plus the zone polygons for the map.</summary>
public sealed record DriverIncentiveDetailDto(
    Guid Id, string Name, string? Description, IncentiveType Type, int TargetTrips, decimal RewardAmount, DateTime PeriodStart, DateTime PeriodEnd,
    IncentiveWindowDto? Window, IReadOnlyList<IncentiveZoneDto>? Zones, IReadOnlyList<string>? RideCategoryCodes, bool RequiresOptIn, bool OptedIn,
    DriverIncentiveProgressSummaryDto? Progress, IReadOnlyList<IncentiveZonePolygonDto>? ZonesPolygons, decimal RewardMultiplier = 1m, decimal? EffectiveRewardAmount = null);

// ----- incentives (admin) -----

public sealed record IncentiveDto(
    Guid Id, string NameAr, string NameEn, string? DescriptionAr, string? DescriptionEn, IncentiveType Type, Guid CityId, IReadOnlyList<Guid>? ZoneIds,
    IReadOnlyList<Guid>? RideCategoryIds, int TargetTrips, decimal RewardAmount, decimal? MinTripFare, DateTime StartsAt, DateTime EndsAt, IReadOnlyList<int>? DaysOfWeek,
    string? DailyFrom, string? DailyTo, DriverTier? MinTier, decimal? MinRating, bool RequiresOptIn, int? MaxParticipants, decimal? BudgetAmount, decimal SpentAmount,
    bool NotifyOnPublish, bool IsActive, string Status, DateTime? PublishedAt, Guid? CreatedBy, DateTime CreatedAt, DateTime UpdatedAt,
    int ParticipantsCount, int AchievedCount, int PaidCount);

public sealed record IncentiveUpsertRequest(
    string? NameAr, string? NameEn, string? DescriptionAr, string? DescriptionEn, IncentiveType? Type, Guid? CityId, List<Guid>? ZoneIds, List<Guid>? RideCategoryIds,
    int? TargetTrips, decimal? RewardAmount, decimal? MinTripFare, DateTime? StartsAt, DateTime? EndsAt, List<int>? DaysOfWeek, string? DailyFrom, string? DailyTo,
    DriverTier? MinTier, decimal? MinRating, bool? RequiresOptIn, int? MaxParticipants, decimal? BudgetAmount, bool? NotifyOnPublish, bool? IsActive);

public sealed record IncentiveProgressDto(
    Guid Id, string? DriverName, DateTime PeriodStart, int CompletedTrips, IncentiveProgressStatus Status, decimal? RewardAmount, decimal? IncentiveMultiplier,
    DateTime? PaidAt, Guid DriverId, DateTime PeriodEnd, DateTime? AchievedAt, string? VoidedReason, DateTime? OptedInAt);

/// <summary>Row of <c>GET /admin/drivers/{id}/incentives</c> (not in doc 10): a driver's progress across incentives.</summary>
public sealed record DriverIncentiveProgressAdminDto(
    Guid Id, Guid IncentiveId, string Name, string IncentiveName, IncentiveType Type, int TargetTrips, int CompletedTrips, DateTime PeriodStart, DateTime PeriodEnd,
    IncentiveProgressStatus Status, decimal? RewardAmount, decimal? IncentiveMultiplier, DateTime? PaidAt);

public sealed record VoidProgressRequest(string? Reason);
