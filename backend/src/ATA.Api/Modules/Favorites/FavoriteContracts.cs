using ATA.Domain.Common;
using ATA.Domain.Trips;

namespace ATA.Api.Modules.Favorites;

public sealed class FavoritesOptions
{
    public const string Section = "Favorites";
    /// <summary>Favourite drivers one passenger may keep (<c>422 favorites_limit</c> beyond it).</summary>
    public int MaxPerPassenger { get; set; } = 20;
    /// <summary>How long the favourite driver has to answer the exclusive first offer before normal matching takes over.</summary>
    public int ExclusiveOfferTimeoutSeconds { get; set; } = 30;
    /// <summary>Radius of the "available favourites" list and of the exclusive round; <c>null</c> = the zone/category matching radius (<c>matching_settings.radius_meters</c>).</summary>
    public int? AvailabilityRadiusMeters { get; set; }
}

// ----- passenger -----

public sealed record FavoriteVehicleDto(string Make, string Model, string Color);

public sealed record FavoriteDriverDto(
    Guid DriverId, string? FirstName, string? PhotoUrl, decimal RatingAvg, FavoriteVehicleDto? Vehicle, string? RideCategoryCode, int TripsTogether, DateTime? LastTripAt,
    DateTime CreatedAt);

/// <summary>Exactly one of <see cref="DriverId"/> / <see cref="TripId"/>.</summary>
public sealed record AddFavoriteRequest(Guid? DriverId, Guid? TripId);

/// <summary>The rule that would discount a trip with this favourite (<c>percent</c> of the fare up to <c>maxAmount</c>).</summary>
public sealed record FavoriteDiscountDto(decimal Percent, decimal MaxAmount, bool StackableWithPromotions);

public sealed record AvailableFavoriteDto(
    Guid DriverId, string? FirstName, string? PhotoUrl, decimal RatingAvg, FavoriteVehicleDto? Vehicle, int EtaMinutes, FavoriteDiscountDto? Discount, bool AvailableNow = true);

public sealed record DriverFavoritesCountDto(int Count);

/// <summary><c>Trip.favorite</c>: <see cref="DiscountRuleId"/> and <see cref="DiscountRuleName"/> (the rule pinned at acceptance) are only sent to admins.</summary>
public sealed record TripFavoriteDto(Guid DriverId, string? DriverName, FavoriteStatus Status, bool DiscountApplied, Guid? DiscountRuleId = null, string? DiscountRuleName = null);

// ----- admin -----

public sealed record FavoriteDiscountRuleDto(
    Guid Id, string Name, decimal DiscountPercent, decimal MaxDiscountAmount, decimal? MinFare, bool StackableWithPromotions, DateTime ValidFrom, DateTime? ValidTo,
    IReadOnlyList<Guid>? RideCategoryIds, IReadOnlyList<Guid>? ZoneIds, IReadOnlyList<BookingType>? BookingTypes, int Priority, bool IsActive, string Status,
    DateTime CreatedAt, DateTime UpdatedAt, string? CreatedByName = null);

public sealed record FavoriteDiscountRuleUpsertRequest(
    string? Name, decimal? DiscountPercent, decimal? MaxDiscountAmount, decimal? MinFare, bool? StackableWithPromotions, DateTime? ValidFrom, DateTime? ValidTo,
    List<Guid>? RideCategoryIds, List<Guid>? ZoneIds, List<BookingType>? BookingTypes, int? Priority, bool? IsActive);

public sealed record FavoriteTopDriverDto(Guid DriverId, string? Name, int FavoritesCount, int FavoriteTrips);

public sealed record FavoriteStatsDto(
    int FavoriteRequests, int Accepted, int Fallback, decimal FavoriteBookingRate, int DiscountUsageCount, decimal DiscountTotal, IReadOnlyList<FavoriteTopDriverDto> TopDrivers);
