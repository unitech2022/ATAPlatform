using ATA.Domain.Common;

namespace ATA.Domain.Corporate;

public enum CorporateAccountStatus { Pending, Active, Suspended, Closed }

/// <summary><c>corporate_accounts.billing_cycle</c>: only monthly invoicing exists in v1.</summary>
public enum BillingCycle { Monthly }

/// <summary><c>corporate_users.role</c> (JSON <c>corporate_admin</c> / <c>employee</c>).</summary>
public enum CorporateRole { CorporateAdmin, Employee }

public enum CorporateUserStatus { Invited, Active, Disabled }

public enum ZoneMatch { PickupAndDropoff, PickupOrDropoff }

public enum CorporateInvoiceStatus { Draft, Issued, Paid, Overdue, Void }

public enum InvoiceLineType { Trip, CancellationFee, Adjustment }

/// <summary>Row of <c>corporate_accounts</c> (doc 12 §F19.1). The national address is stored as JSON.</summary>
public class CorporateAccount : AuditableEntity
{
    /// <summary><c>CA-#####</c>.</summary>
    public required string AccountNumber { get; set; }
    public required string LegalNameAr { get; set; }
    public required string LegalNameEn { get; set; }
    public required string DisplayName { get; set; }
    /// <summary>Commercial registration: 10 digits.</summary>
    public required string CrNumber { get; set; }
    /// <summary>15 digits starting and ending with 3.</summary>
    public string? VatNumber { get; set; }
    /// <summary>Used only to e-mail invoices (doc 12 decision 5).</summary>
    public required string BillingEmail { get; set; }
    /// <summary>JSON <see cref="NationalAddress"/>.</summary>
    public string? BillingAddress { get; set; }
    public Guid? CityId { get; set; }
    public required string ContactName { get; set; }
    public required string ContactPhone { get; set; }
    public CorporateAccountStatus Status { get; set; } = CorporateAccountStatus.Pending;
    public decimal CreditLimit { get; set; }
    public BillingCycle BillingCycle { get; set; } = BillingCycle.Monthly;
    public int PaymentTermsDays { get; set; } = 30;
    public Guid? DefaultPolicyId { get; set; }
    public string? Notes { get; set; }
    public Guid CreatedBy { get; set; }

    public bool IsActive => Status == CorporateAccountStatus.Active;
}

/// <summary>Saudi national address (<c>billing_address</c> JSON, camelCase).</summary>
public sealed record NationalAddress(
    string? BuildingNumber, string? Street, string? District, string? City, string? PostalCode, string? AdditionalNumber, string CountryCode = "SA");

/// <summary>A member of a company: invited by phone number, bound to a <c>users</c> row on acceptance.</summary>
public class CorporateUser : AuditableEntity
{
    public Guid CorporateAccountId { get; set; }
    public Guid? UserId { get; set; }
    public required string PhoneNumber { get; set; }
    public string? FullName { get; set; }
    public CorporateRole Role { get; set; } = CorporateRole.Employee;
    public string? EmployeeNumber { get; set; }
    public string? Department { get; set; }
    public Guid? CostCenterId { get; set; }
    /// <summary>NULL = the account's default policy.</summary>
    public Guid? PolicyId { get; set; }
    /// <summary>Overrides <c>policy.monthly_budget_per_employee</c>.</summary>
    public decimal? MonthlyBudget { get; set; }
    public CorporateUserStatus Status { get; set; } = CorporateUserStatus.Invited;
    public Guid? InvitedBy { get; set; }
    public DateTime? ActivatedAt { get; set; }
    public DateTime? DisabledAt { get; set; }

    public bool IsAdmin => Role == CorporateRole.CorporateAdmin;
}

/// <summary>SMS / in-app invitation; the token is only stored as a SHA-256 hash.</summary>
public class CorporateInvitation : Entity
{
    public Guid CorporateAccountId { get; set; }
    public Guid CorporateUserId { get; set; }
    public required string PhoneNumber { get; set; }
    public required string TokenHash { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? DeclinedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    /// <summary>Set by <c>CorporateInvitationExpiryJob</c> once <see cref="ExpiresAt"/> passed without an answer.</summary>
    public DateTime? ExpiredAt { get; set; }

    public bool IsOpen => AcceptedAt is null && DeclinedAt is null && RevokedAt is null;

    public bool IsExpiredAt(DateTime now) => ExpiredAt is not null || ExpiresAt <= now;
}

public class CorporateCostCenter : AuditableEntity
{
    public Guid CorporateAccountId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Booking rules of a company (doc 12 §F19.1); every JSON list is optional (NULL = unrestricted).</summary>
public class CorporatePolicy : AuditableEntity
{
    public Guid CorporateAccountId { get; set; }
    public required string Name { get; set; }
    public bool IsDefault { get; set; }
    /// <summary>JSON array of ride category ids.</summary>
    public string? AllowedRideCategoryIds { get; set; }
    /// <summary>JSON array of weekdays 0 (Sunday) – 6 (Saturday).</summary>
    public string? AllowedDays { get; set; }
    /// <summary>JSON array <c>[{ "from": "07:00", "to": "22:00" }]</c> in Riyadh time, evaluated on the pickup time.</summary>
    public string? TimeWindows { get; set; }
    /// <summary>JSON array of zone ids.</summary>
    public string? AllowedZoneIds { get; set; }
    public ZoneMatch ZoneMatch { get; set; } = ZoneMatch.PickupAndDropoff;
    public decimal? MaxFarePerTrip { get; set; }
    public decimal? MonthlyBudgetPerEmployee { get; set; }
    public bool RequirePurpose { get; set; }
    public bool RequireCostCenter { get; set; }
    public bool AllowScheduled { get; set; } = true;
    public bool AllowGuestBooking { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

/// <summary>A manual signed amount (VAT inclusive) billed on the next invoice.</summary>
public class CorporateAdjustment : Entity
{
    public Guid CorporateAccountId { get; set; }
    public decimal Amount { get; set; }
    public required string Description { get; set; }
    public Guid? InvoiceId { get; set; }
    public Guid CreatedBy { get; set; }
}

public class CorporateInvoice : AuditableEntity
{
    /// <summary><c>INV-YYYYMM-#####</c> (the month of the billed period).</summary>
    public required string InvoiceNumber { get; set; }
    public Guid CorporateAccountId { get; set; }
    public DateOnly PeriodStart { get; set; }
    public DateOnly PeriodEnd { get; set; }
    public DateOnly IssueDate { get; set; }
    public DateOnly DueDate { get; set; }
    public string Currency { get; set; } = "SAR";
    public int TripsCount { get; set; }
    public decimal SubtotalExclVat { get; set; }
    public decimal VatRate { get; set; } = CorporateVat.Rate;
    public decimal VatAmount { get; set; }
    public decimal TotalInclVat { get; set; }
    public CorporateInvoiceStatus Status { get; set; } = CorporateInvoiceStatus.Draft;
    /// <summary>JSON <see cref="InvoiceParty"/> of the platform at generation/issue time.</summary>
    public string? SellerSnapshot { get; set; }
    /// <summary>JSON <see cref="InvoiceParty"/> of the company at generation/issue time.</summary>
    public string? BuyerSnapshot { get; set; }
    public Guid? PdfFileId { get; set; }
    public Guid? IssuedBy { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public decimal? PaidAmount { get; set; }
    public string? PaymentReference { get; set; }
    public string? VoidReason { get; set; }
    /// <summary>
    /// Technical column backing "UNIQUE(corporate_account_id, period_start) except void": <c>true</c> while the invoice is not void, <c>NULL</c> once void
    /// (NULLs never collide in a unique index, MySQL has no partial indexes).
    /// </summary>
    public bool? PeriodActive { get; set; } = true;

    public decimal Outstanding => Math.Max(0m, TotalInclVat - (PaidAmount ?? 0m));

    public bool IsOpen => Status is CorporateInvoiceStatus.Issued or CorporateInvoiceStatus.Overdue;
}

/// <summary>Party printed on an invoice (seller or buyer snapshot).</summary>
public sealed record InvoiceParty(string NameAr, string NameEn, string? VatNumber, NationalAddress? Address, string? CrNumber = null);

public class CorporateInvoiceLine : Entity
{
    public Guid InvoiceId { get; set; }
    public InvoiceLineType LineType { get; set; }
    public Guid? TripId { get; set; }
    public string? TripNumber { get; set; }
    public DateTime? TripDate { get; set; }
    public string? EmployeeName { get; set; }
    public string? EmployeeNumber { get; set; }
    public string? Department { get; set; }
    public string? CostCenterCode { get; set; }
    public string? GuestName { get; set; }
    public string? Purpose { get; set; }
    public string? PickupName { get; set; }
    public string? DropoffName { get; set; }
    public required string Description { get; set; }
    public decimal AmountExclVat { get; set; }
    public decimal VatAmount { get; set; }
    public decimal AmountInclVat { get; set; }
}

/// <summary>Reserved for future API access (no authentication scheme consumes it in v1).</summary>
public class CorporateApiKey : Entity
{
    public Guid CorporateAccountId { get; set; }
    public required string Name { get; set; }
    public required string KeyPrefix { get; set; }
    public required string KeyHash { get; set; }
    /// <summary>JSON array, e.g. <c>["bookings:read","bookings:write","reports:read"]</c>.</summary>
    public string Scopes { get; set; } = "[]";
    public DateTime? LastUsedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public Guid CreatedBy { get; set; }
}

/// <summary>VAT arithmetic of the invoices: trip prices are VAT inclusive (15 %).</summary>
public static class CorporateVat
{
    public const decimal Rate = 15.00m;

    /// <summary><c>excl = round(incl / 1.15, 2)</c>, <c>vat = incl − excl</c> (per invoice line).</summary>
    public static (decimal Excl, decimal Vat) Split(decimal inclusive)
    {
        var excl = decimal.Round(inclusive / (1m + Rate / 100m), 2, MidpointRounding.AwayFromZero);
        return (excl, inclusive - excl);
    }
}

/// <summary>Common cut of the ledger/credit exposure of one company.</summary>
public sealed record CorporateExposure(decimal Unbilled, decimal UnpaidInvoices, decimal OverdueAmount, decimal InFlight)
{
    public decimal Used => Unbilled + UnpaidInvoices + InFlight;
}
