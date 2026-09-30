using System.Text.Json;
using ATA.Api.Modules.Trips;
using ATA.Domain.Trips;

namespace ATA.Api.Modules.Pricing;

public sealed record ZoneRefDto(Guid Id, string Code, string Name);

public sealed record DemandDto(string Code, string Name, decimal Multiplier, string Color, DemandSource Source);

public sealed record FareBreakdownDto(
    decimal BaseFare, decimal DistanceFare, decimal TimeFare, decimal WaitingFare, bool MinFareApplied,
    decimal TimeMultiplier, string? TimeMultiplierLabel, decimal DemandMultiplier, decimal BookingFee, decimal ServiceFee, decimal Discount,
    IReadOnlyList<ATA.Api.Modules.Payments.DiscountDto>? Discounts = null);

public sealed record QuoteCategoryDto(
    Guid RideCategoryId, string Code, string Name, int? EtaMinutes, decimal Total, decimal DriverNetEarnings, decimal OfferMin, decimal OfferMax,
    FareBreakdownDto Breakdown, DemandDto Demand, Guid? QuoteId, string PricingSource, decimal? TotalBeforeDiscount = null);

/// <summary>Response of <c>POST /pricing/quote</c> (and its alias <c>POST /passenger/trips/estimate</c>) and <c>POST /admin/pricing/simulate</c>.</summary>
public sealed record QuoteResponse(
    Guid? QuoteId, DateTime? ExpiresAt, int DistanceMeters, int DurationSeconds, ZoneRefDto? PickupZone, ZoneRefDto? DropoffZone, DemandDto Demand,
    IReadOnlyList<QuoteCategoryDto> Categories, ATA.Api.Modules.Promotions.QuotePromotionDto? Promotion = null, bool FavoriteDiscountConditional = false,
    ATA.Api.Modules.Corporate.CorporateQuoteDto? Corporate = null);

public sealed record SimulateRequest(PlaceRequest? Pickup, PlaceRequest? Dropoff, List<PlaceRequest>? Stops, Guid? RideCategoryId, BookingType? BookingType, DateTime? ScheduledAt, DateTime? At);

public sealed record DemandAtLocationDto(ZoneRefDto? Zone, DemandDto Demand, DateTime? ComputedAt);

// ----- admin -----

public sealed record ZoneCategorySettingDto(Guid RideCategoryId, string? CategoryCode, bool IsEnabled, decimal SurgeCap);

public sealed record ZoneCategorySettingRequest(Guid? RideCategoryId, bool? IsEnabled, decimal? SurgeCap);

public sealed record ZoneDto(
    Guid Id, Guid CityId, string Code, string NameAr, string NameEn, JsonElement Polygon, decimal CenterLat, decimal CenterLng, int Priority, bool IsActive,
    JsonElement? OperatingHours, IReadOnlyList<ZoneCategorySettingDto> ZoneCategorySettings, DateTime CreatedAt, DateTime UpdatedAt);

public sealed record ZoneUpsertRequest(
    Guid? CityId, string? Code, string? NameAr, string? NameEn, JsonElement? Polygon, decimal? CenterLat, decimal? CenterLng, int? Priority, bool? IsActive,
    JsonElement? OperatingHours, List<ZoneCategorySettingRequest>? ZoneCategorySettings);

public sealed record TimeMultiplierDto(Guid Id, byte? DayOfWeek, string FromTime, string ToTime, decimal Multiplier, string Label);

public sealed record TimeMultiplierRequest(byte? DayOfWeek, string? FromTime, string? ToTime, decimal? Multiplier, string? Label);

public sealed record PricingRuleDto(
    Guid Id, Guid RideCategoryId, string? CategoryCode, Guid? ZoneId, string? ZoneCode, string Name, decimal BaseFare, decimal PerKm, decimal PerMinute, decimal BookingFee,
    decimal ServiceFeePercent, decimal MinFare, decimal WaitingPerMinute, int FreeWaitingMinutes, decimal CancellationFee, decimal DriverSharePercent,
    DateTime EffectiveFrom, DateTime? EffectiveTo, int Priority, bool IsActive, IReadOnlyList<TimeMultiplierDto> TimeMultipliers, DateTime CreatedAt, DateTime UpdatedAt);

public sealed record PricingRuleUpsertRequest(
    Guid? RideCategoryId, Guid? ZoneId, string? Name, decimal? BaseFare, decimal? PerKm, decimal? PerMinute, decimal? BookingFee, decimal? ServiceFeePercent,
    decimal? MinFare, decimal? WaitingPerMinute, int? FreeWaitingMinutes, decimal? CancellationFee, decimal? DriverSharePercent, DateTime? EffectiveFrom,
    DateTime? EffectiveTo, int? Priority, bool? IsActive, List<TimeMultiplierRequest>? TimeMultipliers);

public sealed record DemandLevelDto(Guid Id, string Code, string NameAr, string NameEn, decimal Multiplier, string Color, int SortOrder);

public sealed record DemandLevelUpdateRequest(decimal? Multiplier);

public sealed record DemandRuleDto(
    Guid Id, Guid? ZoneId, string? ZoneCode, Guid? RideCategoryId, string? CategoryCode, string Metric, int WindowMinutes,
    decimal ThresholdModerate, decimal ThresholdHigh, decimal ThresholdVeryHigh, bool IsActive, DateTime CreatedAt, DateTime UpdatedAt);

public sealed record DemandRuleUpsertRequest(
    Guid? ZoneId, Guid? RideCategoryId, string? Metric, int? WindowMinutes, decimal? ThresholdModerate, decimal? ThresholdHigh, decimal? ThresholdVeryHigh, bool? IsActive);

public sealed record DemandOverrideDto(
    Guid Id, Guid ZoneId, string? ZoneCode, Guid? RideCategoryId, string? CategoryCode, Guid DemandLevelId, string DemandLevelCode, string Reason,
    DateTime StartsAt, DateTime EndsAt, Guid? CreatedBy, bool IsActive, DateTime CreatedAt);

public sealed record DemandOverrideUpsertRequest(Guid? ZoneId, Guid? RideCategoryId, Guid? DemandLevelId, string? DemandLevelCode, string? Reason, DateTime? StartsAt, DateTime? EndsAt);

public sealed record DemandCurrentEntryDto(Guid? RideCategoryId, string? CategoryCode, string Code, string Name, decimal Multiplier, string Color, DemandSource Source, decimal? Ratio, int? RequestsCount, int? OnlineDrivers, DateTime? ComputedAt);

public sealed record DemandCurrentZoneDto(Guid ZoneId, string Code, string Name, decimal CenterLat, decimal CenterLng, IReadOnlyList<DemandCurrentEntryDto> Levels);
