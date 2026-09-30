using ATA.Api.Common;
using ATA.Api.Modules.Trips;
using ATA.Domain.Common;
using ATA.Domain.Corporate;
using ATA.Domain.Trips;

namespace ATA.Api.Modules.Corporate;

// ----- shared -----

/// <summary>One rule a corporate trip breaks (doc 12 §F19.2): <c>category, day, time_window, zone, max_fare, scheduled, purpose_required, cost_center_required, guest_booking</c>
/// (+ <c>budget_exceeded</c> / <c>credit_limit_exceeded</c> in the quote, which has no separate channel for them).</summary>
public sealed record PolicyViolationDto(string Rule, decimal? Limit = null, object? Allowed = null);

/// <summary><c>QuoteResponse.corporate</c>: whether the company would accept the trip and what is left of the employee's monthly budget.</summary>
public sealed record CorporateQuoteDto(bool Allowed, IReadOnlyList<PolicyViolationDto> Violations, decimal? RemainingBudget);

public sealed record CostCenterRefDto(Guid Id, string Code, string Name);

/// <summary><c>Trip.corporate</c>; <paramref name="EmployeeName"/> is additive (the portal shows who rides). The trailing fields are only filled for the company's and admins' own views.</summary>
public sealed record TripCorporateDto(
    string CompanyName, string? Purpose, string? CostCenter, bool IsGuest, string? GuestName, string? EmployeeName,
    Guid? AccountId = null, string? EmployeeNumber = null, string? Department = null, string? GuestPhone = null, string? PolicyName = null);

// ----- employee (rider app) -----

public sealed record CorporateMembershipDto(Guid CorporateUserId, Guid AccountId, string CompanyName, string Role, string? EmployeeNumber, string? Department, string Status);

public sealed record CorporatePolicySummaryDto(
    string Name, IReadOnlyList<string>? AllowedRideCategoryCodes, IReadOnlyList<TimeWindowDto>? TimeWindows, IReadOnlyList<int>? AllowedDays, decimal? MaxFarePerTrip,
    bool RequirePurpose, bool RequireCostCenter, bool AllowScheduled);

public sealed record CorporateBudgetDto(decimal Monthly, decimal Spent, decimal Remaining);

public sealed record CorporateMembershipResponse(
    CorporateMembershipDto Membership, CorporatePolicySummaryDto? Policy, CorporateBudgetDto? Budget, IReadOnlyList<CostCenterRefDto> CostCenters);

public sealed record CorporateInvitationDto(Guid Id, string CompanyName, string Role, DateTime ExpiresAt);

// ----- company account -----

public sealed record CorporateAccountDto(
    Guid Id, string AccountNumber, string LegalNameAr, string LegalNameEn, string DisplayName, string CrNumber, string? VatNumber, string BillingEmail,
    NationalAddress? BillingAddress, Guid? CityId, string? CityName, string ContactName, string ContactPhone, CorporateAccountStatus Status, decimal CreditLimit,
    BillingCycle BillingCycle, int PaymentTermsDays, Guid? DefaultPolicyId);

public sealed record UpdateCorporateAccountRequest(string? BillingEmail, NationalAddressRequest? BillingAddress, string? ContactName, string? ContactPhone);

public sealed record NationalAddressRequest(string? BuildingNumber, string? Street, string? District, string? City, string? PostalCode, string? AdditionalNumber, string? CountryCode);

public sealed record CorporateOpenInvoicesDto(int Count, decimal Amount);

public sealed record CorporateMonthToDateDto(int Trips, decimal Spend);

public sealed record CorporateDashboardDto(
    CorporateMonthToDateDto MonthToDate, int ActiveEmployees, int InvitedEmployees, decimal? BudgetUtilizationPercent, decimal CreditLimit, decimal CreditUsed,
    CorporateOpenInvoicesDto OpenInvoices, IReadOnlyList<CorporateTripSummaryDto> RecentTrips);

public sealed record CorporateTripSummaryDto(
    Guid Id, string TripNumber, TripStatus Status, string? EmployeeName, string? GuestName, bool IsGuest, string PickupName, string DropoffName, string? CategoryName,
    decimal? Amount, DateTime? ScheduledAt, DateTime? RequestedAt, DateTime? CompletedAt);

// ----- employees -----

/// <summary>Employee row: <c>costCenter</c> is the cost centre code (the id is in <c>costCenterId</c>); <c>invitationExpiresAt</c> is set while the member is invited.</summary>
public sealed record CorporateEmployeeDto(
    Guid Id, string? FullName, string PhoneNumber, CorporateRole Role, string? EmployeeNumber, string? Department, string? CostCenter, Guid? CostCenterId, Guid? PolicyId,
    string? PolicyName, decimal? MonthlyBudget, decimal SpentThisMonth, CorporateUserStatus Status, DateTime? ActivatedAt, DateTime? InvitationExpiresAt);

public sealed record InviteEmployeeRequest(
    string? PhoneNumber, string? FullName, string? Role, string? EmployeeNumber, string? Department, Guid? CostCenterId, Guid? PolicyId, decimal? MonthlyBudget);

/// <summary>PUT body; <c>phoneNumber</c> is accepted (the portal resends it) but the number never changes.</summary>
public sealed record UpdateEmployeeRequest(
    string? PhoneNumber, string? FullName, string? Role, string? EmployeeNumber, string? Department, Guid? CostCenterId, Guid? PolicyId, decimal? MonthlyBudget);

public sealed record ImportSkippedDto(int Row, string Reason);

public sealed record ImportResultDto(int Created, IReadOnlyList<ImportSkippedDto> Skipped);

// ----- policies and cost centres -----

public sealed record TimeWindowDto(string From, string To);

public sealed record CorporatePolicyDto(
    Guid Id, string Name, bool IsDefault, IReadOnlyList<Guid>? AllowedRideCategoryIds, IReadOnlyList<int>? AllowedDays, IReadOnlyList<TimeWindowDto>? TimeWindows,
    IReadOnlyList<Guid>? AllowedZoneIds, ZoneMatch ZoneMatch, decimal? MaxFarePerTrip, decimal? MonthlyBudgetPerEmployee, bool RequirePurpose, bool RequireCostCenter,
    bool AllowScheduled, bool AllowGuestBooking, bool IsActive);

public sealed record CorporatePolicyRequest(
    string? Name, List<Guid>? AllowedRideCategoryIds, List<int>? AllowedDays, List<TimeWindowDto>? TimeWindows, List<Guid>? AllowedZoneIds, ZoneMatch? ZoneMatch,
    decimal? MaxFarePerTrip, decimal? MonthlyBudgetPerEmployee, bool? RequirePurpose, bool? RequireCostCenter, bool? AllowScheduled, bool? AllowGuestBooking, bool? IsActive);

public sealed record CostCenterDto(Guid Id, string Code, string Name, bool IsActive);

public sealed record CostCenterRequest(string? Code, string? Name, bool? IsActive);

// ----- bookings -----

public sealed record CorporateGuestRequest(string? Name, string? PhoneNumber);

/// <summary>Portal quote: the <c>/pricing/quote</c> input plus <c>employeeId</c> (omit for a guest), <c>purpose</c> and <c>costCenterId</c>.</summary>
public sealed record CorporateQuoteRequest(
    PlaceRequest? Pickup, PlaceRequest? Dropoff, List<PlaceRequest>? Stops, Guid? RideCategoryId, BookingType? BookingType, DateTime? ScheduledAt, Guid? EmployeeId, string? Purpose, Guid? CostCenterId);

public sealed record CorporateBookingRequest(
    Guid? EmployeeId, CorporateGuestRequest? Guest, PlaceRequest? Pickup, PlaceRequest? Dropoff, List<PlaceRequest>? Stops, Guid? RideCategoryId, BookingType? BookingType,
    DateTime? ScheduledAt, Guid? QuoteId, string? TripPurpose, Guid? CostCenterId, string? RiderNote);

public sealed record CorporateCancelRequest(string? ReasonCode, string? Note, decimal? ExpectedFee = null);

// ----- invoices and reports -----

public sealed record CorporateInvoiceDto(
    Guid Id, string InvoiceNumber, Guid AccountId, string? AccountName, string? AccountNumber, DateOnly PeriodStart, DateOnly PeriodEnd, DateOnly IssueDate, DateOnly DueDate,
    string Currency, int TripsCount, decimal SubtotalExclVat, decimal VatRate, decimal VatAmount, decimal TotalInclVat, CorporateInvoiceStatus Status, decimal? PaidAmount,
    DateTime? PaidAt, string? PaymentReference, string? VoidReason, DateTime? IssuedAt, Guid? PdfFileId);

public sealed record InvoiceLineDto(
    Guid Id, InvoiceLineType LineType, Guid? TripId, string? TripNumber, DateTime? TripDate, string? EmployeeName, string? EmployeeNumber, string? Department, string? CostCenterCode,
    string? GuestName, string? Purpose, string? PickupName, string? DropoffName, string Description, decimal AmountExclVat, decimal VatAmount, decimal AmountInclVat);

/// <summary>Invoice header plus <c>lines</c> as a page (the portal pages through long invoices).</summary>
public sealed record CorporateInvoiceDetailDto(
    Guid Id, string InvoiceNumber, Guid AccountId, string? AccountName, string? AccountNumber, DateOnly PeriodStart, DateOnly PeriodEnd, DateOnly IssueDate, DateOnly DueDate,
    string Currency, int TripsCount, decimal SubtotalExclVat, decimal VatRate, decimal VatAmount, decimal TotalInclVat, CorporateInvoiceStatus Status, decimal? PaidAmount,
    DateTime? PaidAt, string? PaymentReference, string? VoidReason, DateTime? IssuedAt, Guid? PdfFileId, PagedResult<InvoiceLineDto> Lines);

public sealed record ReportSummaryRowDto(string Key, string Label, int Trips, decimal Amount, decimal AvgFare);

public sealed record ReportTotalsDto(int Trips, decimal Amount);

public sealed record ReportSummaryDto(IReadOnlyList<ReportSummaryRowDto> Rows, ReportTotalsDto Totals);

public sealed record ReportTripRowDto(
    Guid Id, string TripNumber, DateTime Date, string? Employee, string? EmployeeNumber, string? Department, string? CostCenter, string? Guest, string? Purpose, string? Category,
    string Pickup, string Dropoff, decimal? DistanceKm, decimal AmountInclVat, decimal Vat, TripStatus Status);

public sealed record CorporateApiKeyDto(Guid Id, string Name, string KeyPrefix, IReadOnlyList<string> Scopes, DateTime? LastUsedAt, DateTime? ExpiresAt, DateTime? RevokedAt, DateTime CreatedAt, string? Key = null);

public sealed record CreateApiKeyRequest(string? Name, List<string>? Scopes, DateTime? ExpiresAt);

// ----- platform admin -----

public sealed record CorporateAccountInput(
    string? LegalNameAr, string? LegalNameEn, string? DisplayName, string? CrNumber, string? VatNumber, string? BillingEmail, NationalAddressRequest? BillingAddress, Guid? CityId,
    string? ContactName, string? ContactPhone, decimal? CreditLimit, BillingCycle? BillingCycle, int? PaymentTermsDays, string? Notes);

public sealed record AdminCorporateAccountListItemDto(
    Guid Id, string AccountNumber, string DisplayName, string LegalNameAr, string LegalNameEn, string CrNumber, CorporateAccountStatus Status, Guid? CityId, string? CityName,
    decimal CreditLimit, DateTime CreatedAt);

public sealed record CorporateAccountSummaryDto(
    CorporateMonthToDateDto MonthToDate, int ActiveEmployees, int InvitedEmployees, decimal? BudgetUtilizationPercent, decimal CreditUsed, CorporateOpenInvoicesDto OpenInvoices);

public sealed record AdminCorporateAccountDto(
    Guid Id, string AccountNumber, string DisplayName, string LegalNameAr, string LegalNameEn, string CrNumber, CorporateAccountStatus Status, Guid? CityId, string? CityName,
    decimal CreditLimit, DateTime CreatedAt, string? VatNumber, string BillingEmail, NationalAddress? BillingAddress, string ContactName, string ContactPhone, BillingCycle BillingCycle,
    int PaymentTermsDays, Guid? DefaultPolicyId, string? Notes, DateTime UpdatedAt, CorporateAccountSummaryDto? Summary);

public sealed record ReasonRequest(string? Reason);

public sealed record InviteAdminRequest(string? PhoneNumber, string? FullName);

public sealed record AdjustmentRequest(decimal? Amount, string? Description);

public sealed record AdjustmentDto(Guid Id, decimal Amount, string Description, Guid? InvoiceId, Guid CreatedBy, DateTime CreatedAt);

public sealed record GenerateInvoiceRequest(DateOnly? PeriodStart);

public sealed record MarkInvoicePaidRequest(decimal? Amount, string? Reference, DateTime? PaidAt);

public sealed record CorporateReceivableDto(Guid AccountId, string Name, decimal CreditLimit, decimal Unbilled, decimal UnpaidInvoices, decimal OverdueAmount);

public sealed record AdminCorporateTripRowDto(
    Guid TripId, string TripNumber, DateTime Date, string? Employee, string? EmployeeNumber, string? Department, string? CostCenter, string? Guest, string? Purpose, string? Category,
    string? Pickup, string? Dropoff, decimal? DistanceKm, decimal? AmountInclVat, decimal? Vat, TripStatus Status);
