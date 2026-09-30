using System.Text.Json;
using ATA.Api.Modules.Promotions;
using ATA.Domain.Corporate;

namespace ATA.Api.Modules.Corporate;

/// <summary>Mapping helpers between corporate entities and their contracts.</summary>
public static class CorporateDtos
{
    public static string RoleName(CorporateRole role) => role == CorporateRole.CorporateAdmin ? "corporate_admin" : "employee";

    public static CorporateRole? ParseRole(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "employee" => CorporateRole.Employee,
        "corporate_admin" => CorporateRole.CorporateAdmin,
        _ => null,
    };

    public static CorporatePolicyDto ToDto(CorporatePolicy p) => new(
        p.Id, p.Name, p.IsDefault, JsonLists.Parse<Guid>(p.AllowedRideCategoryIds), JsonLists.Parse<int>(p.AllowedDays), JsonLists.Parse<TimeWindowDto>(p.TimeWindows),
        JsonLists.Parse<Guid>(p.AllowedZoneIds), p.ZoneMatch, p.MaxFarePerTrip, p.MonthlyBudgetPerEmployee, p.RequirePurpose, p.RequireCostCenter, p.AllowScheduled,
        p.AllowGuestBooking, p.IsActive);

    public static CostCenterDto ToDto(CorporateCostCenter c) => new(c.Id, c.Code, c.Name, c.IsActive);

    public static CorporateInvoiceDto ToDto(CorporateInvoice i, string? accountName = null, string? accountNumber = null) => new(
        i.Id, i.InvoiceNumber, i.CorporateAccountId, accountName, accountNumber, i.PeriodStart, i.PeriodEnd, i.IssueDate, i.DueDate, i.Currency, i.TripsCount, i.SubtotalExclVat,
        i.VatRate, i.VatAmount, i.TotalInclVat, i.Status, i.PaidAmount, i.PaidAt, i.PaymentReference, i.VoidReason, i.IssuedAt, i.PdfFileId);

    public static InvoiceLineDto ToDto(CorporateInvoiceLine l) => new(
        l.Id, l.LineType, l.TripId, l.TripNumber, l.TripDate, l.EmployeeName, l.EmployeeNumber, l.Department, l.CostCenterCode, l.GuestName, l.Purpose, l.PickupName, l.DropoffName,
        l.Description, l.AmountExclVat, l.VatAmount, l.AmountInclVat);

    public static string Scopes(IEnumerable<string> scopes) => JsonSerializer.Serialize(scopes.Distinct().ToList());
}
