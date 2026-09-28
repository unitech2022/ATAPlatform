using System.Text.Json;
using ATA.Api.Modules.Drivers;
using ATA.Api.Modules.Identity;
using ATA.Domain.Common;
using ATA.Domain.Drivers;

namespace ATA.Api.Modules.Admin;

public sealed record DashboardSummaryDto(int PendingDriverApplications, int ApprovedDrivers, int OnlineDrivers, int Passengers, int TripsToday, int UsersToday);

public sealed record AdminDriverListItemDto(Guid Id, string ApplicationNumber, string? FullName, string PhoneNumber, ApplicationStatus Status, string? CityName, string? Vehicle, DateTime? SubmittedAt, int DocumentsPending);

public sealed record StatusHistoryEntryDto(Guid Id, string Action, ApplicationStatus? FromStatus, ApplicationStatus? ToStatus, string? ActorName, string? Reason, DateTime CreatedAt);

public sealed record AdminDriverDetailDto(
    Guid Id,
    string ApplicationNumber,
    ApplicationStatus Status,
    string? RejectionReason,
    DateTime? SubmittedAt,
    DateTime? ApprovedAt,
    DriverProfileDto Profile,
    VehicleDto? Vehicle,
    IReadOnlyList<DriverDocumentDto> Documents,
    IReadOnlyList<RequiredDocumentDto> RequiredDocuments,
    ApplicationStepsDto Steps,
    UserDto User,
    IReadOnlyList<StatusHistoryEntryDto> StatusHistory);

public sealed record ReviewRequest(string? Action);

public sealed record ReasonRequest(string? Reason);

public sealed record DriverStatusChangeDto(Guid Id, ApplicationStatus Status);

public sealed record VerifyDocumentRequest(DocumentStatus? Status, string? Note);

public sealed record AdminPassengerListItemDto(Guid Id, string? FullName, string PhoneNumber, UserStatus Status, DateTime CreatedAt, int TripsCount);

public sealed record UserStatusChangeDto(Guid UserId, UserStatus Status);

public sealed record RideCategoryAdminDto(Guid Id, string Code, string NameAr, string NameEn, string? DescriptionAr, string? DescriptionEn, string? Icon, byte Seats, byte MaxStops, int SortOrder, bool IsActive);

public sealed record RideCategoryUpsertRequest(string? Code, string? NameAr, string? NameEn, string? DescriptionAr, string? DescriptionEn, string? Icon, byte? Seats, byte? MaxStops, int? SortOrder, bool? IsActive);

public sealed record AuditLogDto(Guid Id, Guid? ActorUserId, string? ActorRole, string Action, string EntityType, Guid? EntityId, JsonElement? Before, JsonElement? After, string? IpAddress, DateTime CreatedAt);
