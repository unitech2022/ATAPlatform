using System.Text.Json;
using ATA.Api.Common;
using ATA.Domain.Common;
using ATA.Domain.Promotions;
using ATA.Domain.Trips;

namespace ATA.Api.Modules.Promotions;

public sealed class PromotionsOptions
{
    public const string Section = "Promotions";
    /// <summary>The fare a discount can never go below (<c>discount = min(Σ discounts, base − MinPayableFare)</c>).</summary>
    public decimal MinPayableFare { get; set; }
}

/// <summary>JSON array columns (<c>ride_category_ids</c>, <c>zone_ids</c>, <c>payment_methods</c>, <c>booking_types</c>, <c>tags</c>…).</summary>
public static class JsonLists
{
    public static List<T>? Parse<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<List<T>>(json, JsonDefaults.Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Null or empty → <c>null</c> (no restriction).</summary>
    public static string? Serialize<T>(IEnumerable<T>? values)
    {
        var list = values?.Distinct().ToList();
        return list is null || list.Count == 0 ? null : JsonSerializer.Serialize(list, JsonDefaults.Options);
    }
}

// ----- passenger -----

public sealed record PassengerPromotionDto(
    string Code, string Name, string? Description, PromotionType Type, decimal Value, decimal? MaxDiscount, decimal? MinFare, DateTime ValidTo, bool FirstTripOnly,
    IReadOnlyList<string>? RideCategoryCodes, IReadOnlyList<PaymentMethodKind>? PaymentMethods, string Status = "available");

public sealed record ValidatePromoRequest(string? Code, Guid? QuoteId, Guid? RideCategoryId, PaymentMethodKind? PaymentMethod, BookingType? BookingType);

public sealed record PromoSummaryDto(string Code, string Name, PromotionType Type, decimal Value, decimal? MaxDiscount, bool IsStackable);

public sealed record ValidatePromoResponse(bool Valid, PromoSummaryDto Promotion, decimal? DiscountAmount, decimal? TotalBefore, decimal? TotalAfter);

/// <summary><c>promotion</c> of <c>POST /pricing/quote</c>: an invalid code does not fail the quote.</summary>
public sealed record QuotePromotionDto(string Code, bool Valid, string? Reason);

/// <summary><c>Trip.promotion</c>.</summary>
public sealed record TripPromotionDto(string Code, RedemptionStatus Status, decimal? DiscountAmount, Guid? PromotionId = null, decimal? ReservedAmount = null);

// ----- admin -----

public sealed record PromotionListItemDto(
    Guid Id, string Code, string NameAr, string NameEn, PromotionType Type, decimal Value, DateTime ValidFrom, DateTime ValidTo, int UsageCount, int? TotalUsageLimit,
    decimal SpentAmount, decimal? BudgetAmount, bool IsActive, string Status);

public sealed record PromotionDto(
    Guid Id, string Code, string NameAr, string NameEn, string? DescriptionAr, string? DescriptionEn, PromotionType Type, decimal Value, decimal? MaxDiscount,
    decimal? MinFare, DateTime ValidFrom, DateTime ValidTo, int? TotalUsageLimit, int PerUserLimit, int UsageCount, decimal? BudgetAmount, decimal SpentAmount,
    bool FirstTripOnly, bool NewUsersOnly, int NewUserDays, Guid? CityId, IReadOnlyList<Guid>? RideCategoryIds, IReadOnlyList<Guid>? ZoneIds,
    IReadOnlyList<PaymentMethodKind>? PaymentMethods, IReadOnlyList<BookingType>? BookingTypes, bool IsStackable, bool IsPublic, bool IsActive, string Status,
    Guid? CreatedBy, string? CreatedByName, DateTime CreatedAt, DateTime UpdatedAt);

public sealed record PromotionUpsertRequest(
    string? Code, string? NameAr, string? NameEn, string? DescriptionAr, string? DescriptionEn, PromotionType? Type, decimal? Value, decimal? MaxDiscount,
    decimal? MinFare, DateTime? ValidFrom, DateTime? ValidTo, int? TotalUsageLimit, int? PerUserLimit, decimal? BudgetAmount, bool? FirstTripOnly,
    bool? NewUsersOnly, int? NewUserDays, Guid? CityId, List<Guid>? RideCategoryIds, List<Guid>? ZoneIds, List<PaymentMethodKind>? PaymentMethods,
    List<BookingType>? BookingTypes, bool? IsStackable, bool? IsPublic, bool? IsActive);

public sealed record PromotionRedemptionDto(
    Guid Id, string? PassengerName, string? PhoneMasked, string TripNumber, RedemptionStatus Status, decimal? ReservedAmount, decimal? DiscountAmount,
    DateTime ReservedAt, DateTime? AppliedAt, RedemptionReleaseReason? ReleaseReason, Guid TripId, DateTime? ReleasedAt, Guid PromotionId, string PromotionCode);

public sealed record PromotionStatsDto(int Reserved, int Applied, int Released, decimal TotalDiscount, int UniqueUsers, int FirstTripConversions);
