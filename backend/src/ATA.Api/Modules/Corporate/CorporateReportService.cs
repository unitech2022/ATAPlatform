using System.Globalization;
using System.Text;
using ATA.Api.Common;
using ATA.Api.Modules.Payments;
using ATA.Domain.Cancellation;
using ATA.Domain.Common;
using ATA.Domain.Corporate;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Corporate;

public enum ReportGroupBy { Employee, Department, CostCenter, Month, Category }

/// <summary>
/// Company reports (doc 12 §F19.4): the billable movements of the company — completed trips (final fare, VAT inclusive) and cancelled trips whose fee was billed to the company —
/// summarised by employee / department / cost centre / month / category, listed per trip and exported as CSV (UTF-8 BOM). Dates are Riyadh days. Also the admin view of a
/// company's trips (every status).
/// </summary>
public sealed class CorporateReportService(AtaDbContext db)
{
    private const int MaxExportRows = 100_000;

    private sealed record Row(
        Guid TripId, string TripNumber, DateTime Date, Guid? EmployeeId, string? Employee, string? EmployeeNumber, string? Department, Guid? CostCenterId, string? CostCenter, string? Guest, string? Purpose,
        string? CategoryCode, string? Category, string Pickup, string Dropoff, decimal? DistanceKm, decimal Amount, TripStatus Status);

    // ----- reports -----

    public async Task<ReportSummaryDto> SummaryAsync(Guid accountId, DateOnly? from, DateOnly? to, ReportGroupBy groupBy, Language lang, CancellationToken ct)
    {
        var rows = await RowsAsync(accountId, from, to, null, null, null, lang, ct);
        var groups = rows.GroupBy(r => KeyOf(r, groupBy, lang)).Select(g => new ReportSummaryRowDto(g.Key.Key, g.Key.Label, g.Count(), g.Sum(r => r.Amount), decimal.Round(g.Sum(r => r.Amount) / g.Count(), 2)))
            .OrderBy(r => groupBy == ReportGroupBy.Month ? r.Key : null).ThenByDescending(r => r.Amount).ThenBy(r => r.Label).ToList();
        return new ReportSummaryDto(groups, new ReportTotalsDto(rows.Count, rows.Sum(r => r.Amount)));
    }

    public async Task<PagedResult<ReportTripRowDto>> TripsAsync(Guid accountId, DateOnly? from, DateOnly? to, Guid? employeeId, string? department, Guid? costCenterId, Paging paging, Language lang, CancellationToken ct)
    {
        var rows = await RowsAsync(accountId, from, to, employeeId, department, costCenterId, lang, ct);
        var page = rows.OrderByDescending(r => r.Date).Skip(paging.Skip).Take(paging.PageSize).Select(r =>
        {
            var (_, vat) = CorporateVat.Split(r.Amount);
            return new ReportTripRowDto(r.TripId, r.TripNumber, r.Date, r.Employee, r.EmployeeNumber, r.Department, r.CostCenter, r.Guest, r.Purpose, r.Category, r.Pickup, r.Dropoff, r.DistanceKm, r.Amount, vat, r.Status);
        }).ToList();
        return paging.Result(page, rows.Count);
    }

    public async Task<CsvFile> ExportTripsAsync(Guid accountId, DateOnly? from, DateOnly? to, Guid? employeeId, string? department, Guid? costCenterId, Language lang, CancellationToken ct)
    {
        var rows = (await RowsAsync(accountId, from, to, employeeId, department, costCenterId, lang, ct)).OrderBy(r => r.Date).ToList();
        if (rows.Count > MaxExportRows)
        {
            throw new DomainException(ErrorCodes.ValidationFailed, new { range = $"too_many_rows:{MaxExportRows}" });
        }

        var csv = new StringBuilder();
        csv.Append(Csv.Row("trip_number", "date", "employee", "employee_number", "department", "cost_center", "guest", "purpose", "category", "pickup", "dropoff", "distance_km", "amount_incl_vat", "vat", "status"));
        foreach (var r in rows)
        {
            csv.Append(Csv.Row(r.TripNumber, LocalDate(r.Date), r.Employee, r.EmployeeNumber, r.Department, r.CostCenter, r.Guest, r.Purpose, r.Category, r.Pickup, r.Dropoff,
                r.DistanceKm?.ToString("0.00", CultureInfo.InvariantCulture), Money(r.Amount), Money(CorporateVat.Split(r.Amount).Vat), System.Text.Json.JsonNamingPolicy.SnakeCaseLower.ConvertName(r.Status.ToString())));
        }

        return new CsvFile($"corporate-trips-{from:yyyyMMdd}-{to:yyyyMMdd}.csv", Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray());
    }

    private async Task<List<Row>> RowsAsync(Guid accountId, DateOnly? from, DateOnly? to, Guid? employeeId, string? department, Guid? costCenterId, Language lang, CancellationToken ct)
    {
        var (fromAt, toAt) = CorporateRules.DayRange(from, to);
        var completedQuery = db.Trips.AsNoTracking().Where(t => t.CorporateAccountId == accountId && t.PaymentMethod == PaymentMethodKind.Corporate && t.Status == TripStatus.Completed && t.FinalFare != null);
        var feeQuery = from e in db.CancellationEvents.AsNoTracking()
                       join t in db.Trips.AsNoTracking() on e.TripId equals t.Id
                       where t.CorporateAccountId == accountId && e.FeeMethod == CancellationFeeMethod.Corporate && e.FeeStatus == CancellationFeeStatus.Charged && e.FeeCharged > 0
                       select new { Trip = t, e.FeeCharged, e.CreatedAt };
        if (fromAt is { } f)
        {
            completedQuery = completedQuery.Where(t => t.CompletedAt >= f);
            feeQuery = feeQuery.Where(x => x.CreatedAt >= f);
        }

        if (toAt is { } u)
        {
            completedQuery = completedQuery.Where(t => t.CompletedAt < u);
            feeQuery = feeQuery.Where(x => x.CreatedAt < u);
        }

        if (employeeId is { } emp)
        {
            completedQuery = completedQuery.Where(t => t.CorporateUserId == emp);
            feeQuery = feeQuery.Where(x => x.Trip.CorporateUserId == emp);
        }

        if (costCenterId is { } cc)
        {
            completedQuery = completedQuery.Where(t => t.CostCenterId == cc);
            feeQuery = feeQuery.Where(x => x.Trip.CostCenterId == cc);
        }

        if (!string.IsNullOrWhiteSpace(department))
        {
            var dept = department.Trim();
            var memberIds = db.CorporateUsers.Where(m => m.CorporateAccountId == accountId && m.Department == dept).Select(m => m.Id);
            completedQuery = completedQuery.Where(t => t.CorporateUserId != null && memberIds.Contains(t.CorporateUserId.Value));
            feeQuery = feeQuery.Where(x => x.Trip.CorporateUserId != null && memberIds.Contains(x.Trip.CorporateUserId.Value));
        }

        var completed = await completedQuery.ToListAsync(ct);
        var fees = await feeQuery.ToListAsync(ct);
        var members = await (from m in db.CorporateUsers.AsNoTracking()
                             join u2 in db.Users.AsNoTracking() on m.UserId equals u2.Id into users
                             from u2 in users.DefaultIfEmpty()
                             where m.CorporateAccountId == accountId
                             select new { m.Id, Name = m.FullName ?? (u2 == null ? null : u2.FullName), m.EmployeeNumber, m.Department }).ToDictionaryAsync(x => x.Id, ct);
        var costCenters = await db.CorporateCostCenters.AsNoTracking().Where(c => c.CorporateAccountId == accountId).ToDictionaryAsync(c => c.Id, c => c.Code, ct);
        var categories = await db.RideCategories.AsNoTracking().ToDictionaryAsync(c => c.Id, c => new { c.Code, Name = lang.Pick(c.NameAr, c.NameEn) }, ct);

        Row Make(Trip t, DateTime date, decimal amount, TripStatus status)
        {
            var member = t.CorporateUserId is { } id ? members.GetValueOrDefault(id) : null;
            var category = categories.GetValueOrDefault(t.RideCategoryId);
            return new Row(t.Id, t.TripNumber, date, t.CorporateUserId, member?.Name, member?.EmployeeNumber, member?.Department, t.CostCenterId, t.CostCenterId is { } c ? costCenters.GetValueOrDefault(c) : null,
                t.IsGuest ? t.GuestName : null, t.TripPurpose, category?.Code, category?.Name, t.PickupName, t.DropoffName,
                t.FinalDistanceM is { } m ? decimal.Round(m / 1000m, 2) : null, amount, status);
        }

        return completed.Select(t => Make(t, t.CompletedAt!.Value, t.FinalFare!.Value, TripStatus.Completed))
            .Concat(fees.Select(x => Make(x.Trip, x.CreatedAt, x.FeeCharged, x.Trip.Status))).ToList();
    }

    private (string Key, string Label) KeyOf(Row r, ReportGroupBy groupBy, Language lang) => groupBy switch
    {
        ReportGroupBy.Employee => r.EmployeeId is { } id ? (id.ToString(), r.Employee ?? r.EmployeeNumber ?? id.ToString()) : ("guest", lang.Pick("ضيوف", "Guests")),
        ReportGroupBy.Department => string.IsNullOrWhiteSpace(r.Department) ? ("unassigned", lang.Pick("بدون قسم", "No department")) : (r.Department, r.Department),
        ReportGroupBy.CostCenter => r.CostCenter is { } code ? (code, code) : ("none", lang.Pick("بدون مركز تكلفة", "No cost center")),
        ReportGroupBy.Month => (Formats.ToRiyadh(r.Date).ToString("yyyy-MM", CultureInfo.InvariantCulture), Formats.ToRiyadh(r.Date).ToString("yyyy-MM", CultureInfo.InvariantCulture)),
        _ => (r.CategoryCode ?? "unknown", r.Category ?? "—"),
    };

    // ----- admin view of a company's trips -----

    public async Task<PagedResult<AdminCorporateTripRowDto>> AdminTripsAsync(
        Guid accountId, TripStatus? status, bool? isGuest, DateOnly? from, DateOnly? to, string? search, Paging paging, Language lang, CancellationToken ct)
    {
        var rows = await AdminRowsAsync(accountId, status, isGuest, from, to, search, lang, false, paging, ct);
        return rows;
    }

    public async Task<CsvFile> ExportAdminTripsAsync(Guid accountId, TripStatus? status, bool? isGuest, DateOnly? from, DateOnly? to, string? search, Language lang, CancellationToken ct)
    {
        var page = await AdminRowsAsync(accountId, status, isGuest, from, to, search, lang, true, new Paging(1, MaxExportRows), ct);
        var csv = new StringBuilder();
        csv.Append(Csv.Row("trip_number", "date", "employee", "employee_number", "department", "cost_center", "guest", "purpose", "category", "pickup", "dropoff", "distance_km", "amount_incl_vat", "vat", "status"));
        foreach (var r in page.Items)
        {
            csv.Append(Csv.Row(r.TripNumber, LocalDate(r.Date), r.Employee, r.EmployeeNumber, r.Department, r.CostCenter, r.Guest, r.Purpose, r.Category, r.Pickup, r.Dropoff,
                r.DistanceKm?.ToString("0.00", CultureInfo.InvariantCulture), r.AmountInclVat is { } a ? Money(a) : null, r.Vat is { } v ? Money(v) : null,
                System.Text.Json.JsonNamingPolicy.SnakeCaseLower.ConvertName(r.Status.ToString())));
        }

        return new CsvFile($"corporate-trips-{accountId:N}.csv", Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray());
    }

    private async Task<PagedResult<AdminCorporateTripRowDto>> AdminRowsAsync(
        Guid accountId, TripStatus? status, bool? isGuest, DateOnly? from, DateOnly? to, string? search, Language lang, bool all, Paging paging, CancellationToken ct)
    {
        var (fromAt, toAt) = CorporateRules.DayRange(from, to);
        var query = db.Trips.AsNoTracking().Where(t => t.CorporateAccountId == accountId);
        if (status is not null) query = query.Where(t => t.Status == status);
        if (isGuest is { } g) query = query.Where(t => t.IsGuest == g);
        if (fromAt is { } f) query = query.Where(t => t.RequestedAt >= f);
        if (toAt is { } u) query = query.Where(t => t.RequestedAt < u);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var memberIds = db.CorporateUsers.Where(m => m.CorporateAccountId == accountId && ((m.FullName != null && m.FullName.Contains(term)) || m.PhoneNumber.Contains(term) || (m.EmployeeNumber != null && m.EmployeeNumber.Contains(term)))).Select(m => m.Id);
            query = query.Where(t => t.TripNumber.Contains(term) || (t.GuestName != null && t.GuestName.Contains(term)) || (t.CorporateUserId != null && memberIds.Contains(t.CorporateUserId.Value)));
        }

        var total = await query.CountAsync(ct);
        var trips = await query.OrderByDescending(t => t.RequestedAt).Skip(all ? 0 : paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        var members = await (from m in db.CorporateUsers.AsNoTracking()
                             join u2 in db.Users.AsNoTracking() on m.UserId equals u2.Id into users
                             from u2 in users.DefaultIfEmpty()
                             where m.CorporateAccountId == accountId
                             select new { m.Id, Name = m.FullName ?? (u2 == null ? null : u2.FullName), m.EmployeeNumber, m.Department }).ToDictionaryAsync(x => x.Id, ct);
        var costCenters = await db.CorporateCostCenters.AsNoTracking().Where(c => c.CorporateAccountId == accountId).ToDictionaryAsync(c => c.Id, c => c.Code, ct);
        var categories = await db.RideCategories.AsNoTracking().ToDictionaryAsync(c => c.Id, c => lang.Pick(c.NameAr, c.NameEn), ct);
        var items = trips.Select(t =>
        {
            var member = t.CorporateUserId is { } id ? members.GetValueOrDefault(id) : null;
            var amount = t.Status == TripStatus.Completed ? t.FinalFare : null;
            return new AdminCorporateTripRowDto(t.Id, t.TripNumber, t.CompletedAt ?? t.ScheduledAt ?? t.RequestedAt, member?.Name, member?.EmployeeNumber, member?.Department,
                t.CostCenterId is { } c ? costCenters.GetValueOrDefault(c) : null, t.IsGuest ? t.GuestName : null, t.TripPurpose, categories.GetValueOrDefault(t.RideCategoryId), t.PickupName, t.DropoffName,
                t.FinalDistanceM is { } m ? decimal.Round(m / 1000m, 2) : null, amount, amount is { } a ? CorporateVat.Split(a).Vat : null, t.Status);
        }).ToList();
        return paging.Result(items, total);
    }

    private static string LocalDate(DateTime utc) => Formats.ToRiyadh(utc).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

    private static string Money(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
}
