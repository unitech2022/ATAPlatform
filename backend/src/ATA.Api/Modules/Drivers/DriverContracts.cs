using ATA.Domain.Catalog;
using ATA.Domain.Common;
using ATA.Domain.Drivers;

namespace ATA.Api.Modules.Drivers;

public sealed record DriverProfileDto(string? FullName, string? NationalId, DateOnly? DateOfBirth, Guid? CityId, Gender Gender, string? Iban);

public sealed record VehicleDto(Guid Id, string Make, string Model, short Year, string Color, string PlateNumber, byte Seats, Guid RideCategoryId);

public sealed record DriverDocumentDto(
    Guid Id, Guid DocumentTypeId, string DocumentTypeCode, string DocumentTypeName, DocumentStatus Status, DateOnly? ExpiresAt,
    string? ReviewNote, Guid FileId, string FileName, DateTime UploadedAt);

public sealed record RequiredDocumentDto(Guid DocumentTypeId, string Code, string Name, DocumentAppliesTo AppliesTo, bool IsRequired, bool RequiresExpiry, bool Uploaded);

public sealed record ApplicationStepsDto(bool ProfileComplete, bool VehicleComplete, bool DocumentsComplete, bool CanSubmit);

public sealed record DriverApplicationDto(
    string ApplicationNumber,
    ApplicationStatus Status,
    string? RejectionReason,
    DateTime? SubmittedAt,
    DateTime? ApprovedAt,
    DriverProfileDto Profile,
    VehicleDto? Vehicle,
    IReadOnlyList<DriverDocumentDto> Documents,
    IReadOnlyList<RequiredDocumentDto> RequiredDocuments,
    ApplicationStepsDto Steps);

public sealed record UpdateDriverProfileRequest(string? FullName, string? NationalId, DateOnly? DateOfBirth, Guid? CityId, Gender? Gender, string? Iban);

public sealed record UpsertVehicleRequest(string? Make, string? Model, short? Year, string? Color, string? PlateNumber, byte? Seats, Guid? RideCategoryId);

public sealed record SubmitResponse(ApplicationStatus Status);

public sealed record DriverStatusDto(bool IsOnline, bool CanGoOnline, string? Reason);

public sealed record UpdateDriverStatusRequest(bool? IsOnline, decimal? Latitude, decimal? Longitude);

public sealed record EarningsTodayDto(decimal Earnings, int Trips, double OnlineHours);

public sealed record EarningsWeekDto(decimal Earnings, decimal Target);

public sealed record EarningsSummaryDto(EarningsTodayDto Today, EarningsWeekDto Week, decimal RatingAvg);

public sealed record DriverTripDto(Guid Id, string PickupName, string DestinationName, DateTime? CompletedAt, string Status, decimal Fare, decimal Earning);
