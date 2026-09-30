using System.Text.Json;
using ATA.Domain.Common;
using ATA.Domain.Trips;

namespace ATA.Api.Modules.Trips;

public sealed class TripOptions
{
    public const string Section = "Trips";
    /// <summary>Waiting time at the pickup that is not billed (starts when the driver arrives).</summary>
    public int FreeWaitingMinutes { get; set; } = 3;
    /// <summary>Distance from the pickup beyond which an "arrived" report is recorded with a warning event.</summary>
    public int ArrivalRadiusMeters { get; set; } = 300;
    public int PinMaxAttempts { get; set; } = Trip.DefaultPinMaxAttempts;
}

public sealed class MatchingOptions
{
    public const string Section = "Matching";
    public bool Enabled { get; set; } = true;
    /// <summary>Defaults used when no <c>matching_settings</c> row applies to the trip's zone/category.</summary>
    public int RadiusMeters { get; set; } = 5000;
    public int MaxRadiusMeters { get; set; } = 12000;
    public int RadiusStepMeters { get; set; } = 2500;
    public int OfferTimeoutSeconds { get; set; } = 20;
    public int SearchTimeoutSeconds { get; set; } = 120;
    public int MaxCandidates { get; set; } = 8;
    public bool AllowUpgrade { get; set; }
    public int PollIntervalSeconds { get; set; } = 2;
    /// <summary>Drivers whose last location is older than this are not matched.</summary>
    public int LocationMaxAgeSeconds { get; set; } = 300;
}

public sealed class RealtimeOptions
{
    public const string Section = "Realtime";
    public bool LiveSnapshotEnabled { get; set; } = true;
    public int LiveSnapshotSeconds { get; set; } = 5;
}

public sealed record PlaceDto(string Name, string Address, decimal Lat, decimal Lng);

public sealed record PlaceRequest(string? Name, string? Address, decimal? Lat, decimal? Lng);

/// <summary>
/// <c>promoCode</c> (F15) is optional: an invalid code never fails the quote (see <c>QuoteResponse.promotion</c>). <c>favoriteDriverId</c> (F16, one of the passenger's
/// favourites) adds the favourite-driver discount to the quote, conditional on that driver accepting.
/// </summary>
public sealed record EstimateRequest(PlaceRequest? Pickup, PlaceRequest? Dropoff, List<PlaceRequest>? Stops, Guid? RideCategoryId, BookingType? BookingType, DateTime? ScheduledAt,
    string? PromoCode = null, Guid? FavoriteDriverId = null, Guid? AirportPickupZoneId = null, string? AirportTerminalCode = null, string? FlightNumber = null,
    PaymentMethodKind? PaymentMethod = null, string? TripPurpose = null, Guid? CostCenterId = null);

public sealed record CreateTripRequest(
    PlaceRequest? Pickup,
    PlaceRequest? Dropoff,
    List<PlaceRequest>? Stops,
    Guid? RideCategoryId,
    BookingType? BookingType,
    DateTime? ScheduledAt,
    PaymentMethodKind? PaymentMethod,
    bool? PreferFemaleDriver,
    PricingMode? PricingMode,
    decimal? OfferedPrice,
    string? RiderNote,
    Guid? QuoteId,
    Guid? PaymentMethodId = null,
    string? PromoCode = null,
    Guid? FavoriteDriverId = null,
    Guid? AirportPickupZoneId = null,
    string? AirportTerminalCode = null,
    string? FlightNumber = null,
    string? TripPurpose = null,
    Guid? CostCenterId = null);

public sealed record CancelTripRequest(string? ReasonCode, string? Note, decimal? ExpectedFee = null, int? ExpectedPenaltyPoints = null);

/// <summary>F14: <c>atFault</c> (default <c>none</c>) decides who the cancellation counts against; <c>chargeFee</c> applies the matching rule's fee.</summary>
public sealed record AdminCancelTripRequest(string? Reason, ATA.Domain.Cancellation.AtFault? AtFault = null, bool? ChargeFee = null);

public sealed record RejectOfferRequest(string? ReasonCode);

public sealed record VerifyPinRequest(string? Pin);

public sealed record CompleteTripRequest(decimal? FinalLat, decimal? FinalLng, int? FinalDistanceMeters, int? FinalDurationSeconds);

public sealed record DriverLocationRequest(decimal? Lat, decimal? Lng, decimal? Heading, decimal? Speed, decimal? Accuracy);

public sealed record TripRideCategoryDto(Guid Id, string Code, string Name);

/// <summary><paramref name="IsFavorite"/> (F16): the assigned driver is one of the passenger's favourites — only set in the passenger's and admins' copies.</summary>
public sealed record TripDriverDto(Guid Id, string? FullName, decimal RatingAvg, Guid? PhotoFileId, string PhoneMasked, Gender Gender, bool IsFavorite = false);

public sealed record TripVehicleDto(string Make, string Model, string Color, string PlateNumber);

public sealed record TripTimelineDto(DateTime RequestedAt, DateTime? AssignedAt, DateTime? ArrivedAt, DateTime? StartedAt, DateTime? CompletedAt, DateTime? CancelledAt);

public sealed record TripEventDto(string Type, TripActor Actor, DateTime CreatedAt);

/// <summary>The <c>Trip</c> response object of the F8 contract.</summary>
public sealed record TripDto(
    Guid Id,
    string TripNumber,
    TripStatus Status,
    BookingType BookingType,
    DateTime? ScheduledAt,
    TripRideCategoryDto RideCategory,
    PlaceDto Pickup,
    PlaceDto Dropoff,
    IReadOnlyList<PlaceDto> Stops,
    PaymentMethodKind PaymentMethod,
    PricingMode PricingMode,
    decimal? OfferedPrice,
    decimal EstimatedFare,
    decimal? FinalFare,
    int EstimatedDistanceMeters,
    int EstimatedDurationSeconds,
    TripDriverDto? Driver,
    TripVehicleDto? Vehicle,
    string? Pin,
    int WaitingSeconds,
    CancelledBy? CancelledBy,
    string? CancellationReason,
    TripTimelineDto Timeline,
    IReadOnlyList<TripEventDto> Events,
    ATA.Api.Modules.Payments.TripPaymentDto? Payment = null,
    decimal? CollectCashAmount = null,
    decimal DiscountTotal = 0m,
    ATA.Api.Modules.Cancellation.TripCancellationDto? Cancellation = null,
    ATA.Api.Modules.Promotions.TripPromotionDto? Promotion = null,
    ATA.Api.Modules.Ratings.MyRatingDto? MyRating = null,
    bool CanRate = false,
    DateTime? RateUntil = null,
    ATA.Api.Modules.Favorites.TripFavoriteDto? Favorite = null,
    ATA.Api.Modules.Scheduling.TripSchedulingDto? Scheduling = null,
    ATA.Api.Modules.Airports.TripAirportDto? Airport = null,
    ATA.Api.Modules.Corporate.TripCorporateDto? Corporate = null,
    ATA.Api.Modules.Payments.ReceiptDto? Receipt = null);

public sealed record OfferPassengerDto(string? FirstName, decimal RatingAvg);

public sealed record OfferDto(
    Guid Id,
    Guid TripId,
    PlaceDto Pickup,
    PlaceDto Dropoff,
    IReadOnlyList<PlaceDto> Stops,
    int DistanceToPickupMeters,
    int EtaSeconds,
    int TripDistanceMeters,
    decimal PassengerPrice,
    decimal DriverNetEarnings,
    DateTime ExpiresAt,
    OfferPassengerDto Passenger,
    int Round,
    bool PassengerOffered,
    PaymentMethodKind PaymentMethod = PaymentMethodKind.Cash,
    bool IsFavoriteRequest = false,
    bool Exclusive = false,
    ATA.Api.Modules.Airports.TripAirportDto? Airport = null);

public sealed record DriverLocationEvent(Guid TripId, decimal Lat, decimal Lng, decimal? Heading, int EtaSeconds);

public sealed record AdminTripListItemDto(
    Guid Id,
    string TripNumber,
    TripStatus Status,
    string? PassengerName,
    string PassengerPhone,
    string? DriverName,
    string CategoryName,
    string PickupName,
    string DropoffName,
    decimal EstimatedFare,
    decimal? FinalFare,
    PaymentMethodKind PaymentMethod,
    DateTime RequestedAt);

public sealed record AdminTripPassengerDto(Guid Id, string? FullName, string PhoneNumber, decimal RatingAvg);

public sealed record AdminTripDriverDto(Guid Id, string? FullName, decimal RatingAvg, Guid? PhotoFileId, string PhoneMasked, Gender Gender, string PhoneNumber);

public sealed record AdminTripEventDto(string Type, TripActor Actor, string? ActorName, DateTime CreatedAt, JsonElement? Data);

public sealed record AdminTripOfferDto(Guid Id, Guid DriverId, string? DriverName, OfferStatus Status, int DistanceToPickupMeters, int EtaSeconds, decimal DriverNetEarnings, DateTime SentAt, DateTime? RespondedAt, DateTime ExpiresAt);

public sealed record RoutePointDto(decimal Lat, decimal Lng, DateTime RecordedAt);

/// <summary>The <c>Trip</c> object plus admin-only details (unmasked driver phone, passenger, full events, offers and route).</summary>
public sealed record AdminTripDetailDto(
    Guid Id,
    string TripNumber,
    TripStatus Status,
    BookingType BookingType,
    DateTime? ScheduledAt,
    TripRideCategoryDto RideCategory,
    PlaceDto Pickup,
    PlaceDto Dropoff,
    IReadOnlyList<PlaceDto> Stops,
    PaymentMethodKind PaymentMethod,
    PricingMode PricingMode,
    decimal? OfferedPrice,
    decimal EstimatedFare,
    decimal? FinalFare,
    int EstimatedDistanceMeters,
    int EstimatedDurationSeconds,
    int? FinalDistanceMeters,
    int? FinalDurationSeconds,
    decimal? DriverEarnings,
    AdminTripPassengerDto Passenger,
    AdminTripDriverDto? Driver,
    TripVehicleDto? Vehicle,
    int WaitingSeconds,
    CancelledBy? CancelledBy,
    string? CancellationReason,
    string? RiderNote,
    TripTimelineDto Timeline,
    IReadOnlyList<AdminTripEventDto> Events,
    IReadOnlyList<AdminTripOfferDto> Offers,
    IReadOnlyList<RoutePointDto> Route,
    ATA.Api.Modules.Cancellation.TripCancellationDto? Cancellation = null,
    IReadOnlyList<decimal[]>? PlannedRoute = null,
    ATA.Api.Modules.Promotions.TripPromotionDto? Promotion = null,
    decimal DiscountTotal = 0m,
    IReadOnlyList<ATA.Api.Modules.Payments.DiscountDto>? Discounts = null,
    System.Text.Json.JsonElement? FareBreakdown = null,
    IReadOnlyList<ATA.Api.Modules.Ratings.TripRatingDto>? Ratings = null,
    decimal TierCommissionDiscountPercent = 0m,
    ATA.Api.Modules.Favorites.TripFavoriteDto? Favorite = null,
    ATA.Api.Modules.Scheduling.AdminTripSchedulingDto? Scheduling = null,
    ATA.Api.Modules.Airports.TripAirportDto? Airport = null,
    ATA.Api.Modules.Corporate.TripCorporateDto? Corporate = null);

public sealed record LiveDriverDto(Guid DriverId, string? Name, decimal Lat, decimal Lng, bool IsOnline, string Status, string? CategoryCode, Guid? CurrentTripId, decimal? Heading, DateTime UpdatedAt);

public sealed record LiveTripDto(Guid Id, string TripNumber, TripStatus Status, PlaceDto Pickup, PlaceDto Dropoff, Guid? DriverId, string? PassengerName, DateTime RequestedAt);

public sealed record LiveSnapshotDto(IReadOnlyList<LiveDriverDto> Drivers, IReadOnlyList<LiveTripDto> ActiveTrips, IReadOnlyList<LiveTripDto> SearchingTrips, DateTime GeneratedAt);
