using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ATA.Tests.Infrastructure;

namespace ATA.Tests.Integration;

/// <summary>F19 reports: summaries by employee / department / cost centre / month / category, the trips list with filters and the CSV export; plus the admin view of a company's trips.</summary>
public class CorporateReportTests(CorporateFixture fixture) : IClassFixture<CorporateFixture>
{
    [Fact]
    public async Task Summary_groups_billable_trips_and_fees_and_totals_match()
    {
        var billing = await BillingFlow.SeedAsync(fixture, TripFlow.Area(60), "شركة التقارير");
        var portal = billing.Company.Portal;
        var total = billing.EmployeeFare + billing.GuestFare + 5m;

        var byEmployee = await (await portal.GetAsync("/api/v1/corporate/reports/summary?groupBy=employee&from=2026-09-01&to=2026-09-30")).ReadJsonAsync();
        Assert.Equal(3, byEmployee.GetProperty("totals").GetProperty("trips").GetInt32());
        Assert.Equal(total, byEmployee.GetProperty("totals").GetProperty("amount").GetDecimal());
        var rows = byEmployee.GetProperty("rows").EnumerateArray().ToList();
        Assert.Equal(2, rows.Count);
        var employee = rows.Single(r => r.GetProperty("key").GetString() == billing.Employee.MemberId.ToString());
        Assert.Equal("موظف الفاتورة", employee.GetProperty("label").GetString());
        Assert.Equal(2, employee.GetProperty("trips").GetInt32()); // the completed trip and the billed cancellation fee
        Assert.Equal(billing.EmployeeFare + 5m, employee.GetProperty("amount").GetDecimal());
        Assert.Equal(decimal.Round((billing.EmployeeFare + 5m) / 2m, 2), employee.GetProperty("avgFare").GetDecimal());
        var guest = rows.Single(r => r.GetProperty("key").GetString() == "guest");
        Assert.Equal(billing.GuestFare, guest.GetProperty("amount").GetDecimal());
        Assert.True(rows[0].GetProperty("amount").GetDecimal() >= rows[1].GetProperty("amount").GetDecimal(), "rows are ordered by amount");

        var byDepartment = await (await portal.GetAsync("/api/v1/corporate/reports/summary?groupBy=department")).ReadJsonAsync();
        Assert.Equal(["unassigned", "المالية"], byDepartment.GetProperty("rows").EnumerateArray().Select(r => r.GetProperty("key").GetString()).Order(StringComparer.Ordinal).ToList());
        var byCostCenter = await (await portal.GetAsync("/api/v1/corporate/reports/summary?groupBy=cost_center")).ReadJsonAsync();
        Assert.Equal(billing.EmployeeFare, byCostCenter.GetProperty("rows").EnumerateArray().Single(r => r.GetProperty("key").GetString() == "FIN-01").GetProperty("amount").GetDecimal());
        Assert.Equal("none", byCostCenter.GetProperty("rows").EnumerateArray().Single(r => r.GetProperty("key").GetString() == "none").GetProperty("key").GetString());
        var byMonth = await (await portal.GetAsync("/api/v1/corporate/reports/summary?groupBy=month")).ReadJsonAsync();
        Assert.Equal("2026-09", byMonth.GetProperty("rows")[0].GetProperty("key").GetString());
        Assert.Equal(3, byMonth.GetProperty("rows")[0].GetProperty("trips").GetInt32());
        var byCategory = await (await portal.GetAsync("/api/v1/corporate/reports/summary?groupBy=category")).ReadJsonAsync();
        Assert.Equal("economy", byCategory.GetProperty("rows")[0].GetProperty("key").GetString());
        Assert.Equal(total, byCategory.GetProperty("rows")[0].GetProperty("amount").GetDecimal());

        var october = await (await portal.GetAsync("/api/v1/corporate/reports/summary?groupBy=employee&from=2026-10-01&to=2026-10-31")).ReadJsonAsync();
        Assert.Equal(0, october.GetProperty("rows").GetArrayLength());
        Assert.Equal(0m, october.GetProperty("totals").GetProperty("amount").GetDecimal());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await portal.GetAsync("/api/v1/corporate/reports/summary?groupBy=planet")).StatusCode);
        // The default grouping is by employee; other companies see nothing of this data.
        Assert.Equal(2, (await (await portal.GetAsync("/api/v1/corporate/reports/summary")).ReadJsonAsync()).GetProperty("rows").GetArrayLength());
        var other = await CorporateFlow.OnboardAsync(fixture);
        Assert.Equal(0, (await (await other.Portal.GetAsync("/api/v1/corporate/reports/summary?groupBy=employee")).ReadJsonAsync()).GetProperty("totals").GetProperty("trips").GetInt32());
    }

    [Fact]
    public async Task Trips_report_filters_and_csv_export_with_bom_and_the_documented_columns()
    {
        var billing = await BillingFlow.SeedAsync(fixture, TripFlow.Area(61), "شركة التصدير");
        var portal = billing.Company.Portal;

        var all = await (await portal.GetAsync("/api/v1/corporate/reports/trips?from=2026-09-01&to=2026-09-30")).ReadJsonAsync();
        Assert.Equal(3, all.GetProperty("total").GetInt32());
        var completed = all.GetProperty("items").EnumerateArray().Single(r => r.GetProperty("id").GetString() == billing.EmployeeTripId);
        Assert.Equal("completed", completed.GetProperty("status").GetString());
        Assert.Equal("موظف الفاتورة", completed.GetProperty("employee").GetString());
        Assert.Equal("E-1", completed.GetProperty("employeeNumber").GetString());
        Assert.Equal("المالية", completed.GetProperty("department").GetString());
        Assert.Equal("FIN-01", completed.GetProperty("costCenter").GetString());
        Assert.Equal("اجتماع", completed.GetProperty("purpose").GetString());
        Assert.NotNull(completed.GetProperty("category").GetString());
        Assert.Equal(billing.EmployeeFare, completed.GetProperty("amountInclVat").GetDecimal());
        Assert.Equal(billing.EmployeeFare - decimal.Round(billing.EmployeeFare / 1.15m, 2, MidpointRounding.AwayFromZero), completed.GetProperty("vat").GetDecimal());
        Assert.True(completed.GetProperty("distanceKm").GetDecimal() > 5m);
        var cancelled = all.GetProperty("items").EnumerateArray().Single(r => r.GetProperty("id").GetString() == billing.CancelledTripId);
        Assert.Equal("cancelled", cancelled.GetProperty("status").GetString());
        Assert.Equal(5m, cancelled.GetProperty("amountInclVat").GetDecimal());
        var guest = all.GetProperty("items").EnumerateArray().Single(r => r.GetProperty("id").GetString() == billing.GuestTripId);
        Assert.Equal("ضيف الفاتورة", guest.GetProperty("guest").GetString());
        Assert.Equal(JsonValueKind.Null, guest.GetProperty("employee").ValueKind);

        Assert.Equal(2, (await (await portal.GetAsync($"/api/v1/corporate/reports/trips?employeeId={billing.Employee.MemberId}")).ReadJsonAsync()).GetProperty("total").GetInt32());
        Assert.Equal(2, (await (await portal.GetAsync("/api/v1/corporate/reports/trips?department=المالية")).ReadJsonAsync()).GetProperty("total").GetInt32());
        Assert.Equal(0, (await (await portal.GetAsync("/api/v1/corporate/reports/trips?department=لا يوجد")).ReadJsonAsync()).GetProperty("total").GetInt32());
        var page = await (await portal.GetAsync("/api/v1/corporate/reports/trips?pageSize=2&page=2")).ReadJsonAsync();
        Assert.Equal(1, page.GetProperty("items").GetArrayLength());

        var export = await portal.GetAsync("/api/v1/corporate/reports/trips/export?format=csv&from=2026-09-01&to=2026-09-30");
        Assert.Equal(HttpStatusCode.OK, export.StatusCode);
        Assert.Equal("text/csv", export.Content.Headers.ContentType!.MediaType);
        Assert.EndsWith(".csv", export.Content.Headers.ContentDisposition!.FileNameStar ?? export.Content.Headers.ContentDisposition.FileName);
        var bytes = await export.Content.ReadAsByteArrayAsync();
        Assert.Equal([0xEF, 0xBB, 0xBF], bytes[..3]);
        var lines = Encoding.UTF8.GetString(bytes[3..]).TrimEnd().Split('\n');
        Assert.Equal("trip_number,date,employee,employee_number,department,cost_center,guest,purpose,category,pickup,dropoff,distance_km,amount_incl_vat,vat,status", lines[0]);
        Assert.Equal(4, lines.Length);
        var completedLine = lines.Single(l => l.Contains(",completed") && l.Contains("موظف الفاتورة"));
        Assert.Contains("موظف الفاتورة,E-1,المالية,FIN-01,,اجتماع", completedLine);
        Assert.Contains(billing.EmployeeFare.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), completedLine);
        Assert.Contains(lines, l => l.Contains("ضيف الفاتورة") && l.EndsWith("completed", StringComparison.Ordinal));
        Assert.Contains(lines, l => l.EndsWith(",5.00,0.65,cancelled", StringComparison.Ordinal));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await portal.GetAsync("/api/v1/corporate/reports/trips/export?format=xlsx")).StatusCode);
        var filtered = await portal.GetAsync($"/api/v1/corporate/reports/trips/export?format=csv&employeeId={billing.Employee.MemberId}");
        Assert.Equal(3, Encoding.UTF8.GetString((await filtered.Content.ReadAsByteArrayAsync())[3..]).TrimEnd().Split('\n').Length);
    }

    [Fact]
    public async Task Admin_sees_every_trip_of_a_company_with_filters_and_a_csv_export()
    {
        var billing = await BillingFlow.SeedAsync(fixture, TripFlow.Area(62), "شركة المشرف");
        using var admin = await fixture.LoginAdminAsync();
        var id = billing.Company.AccountId;
        var all = await (await admin.GetAsync($"/api/v1/admin/corporate/accounts/{id}/trips")).ReadJsonAsync();
        Assert.Equal(3, all.GetProperty("total").GetInt32());
        Assert.Equal(1, (await (await admin.GetAsync($"/api/v1/admin/corporate/accounts/{id}/trips?status=cancelled")).ReadJsonAsync()).GetProperty("total").GetInt32());
        Assert.Equal(1, (await (await admin.GetAsync($"/api/v1/admin/corporate/accounts/{id}/trips?isGuest=true")).ReadJsonAsync()).GetProperty("total").GetInt32());
        Assert.Equal(2, (await (await admin.GetAsync($"/api/v1/admin/corporate/accounts/{id}/trips?isGuest=false")).ReadJsonAsync()).GetProperty("total").GetInt32());
        Assert.Equal(1, (await (await admin.GetAsync($"/api/v1/admin/corporate/accounts/{id}/trips?search=الفاتورة&isGuest=true")).ReadJsonAsync()).GetProperty("total").GetInt32());
        Assert.Equal(0, (await (await admin.GetAsync($"/api/v1/admin/corporate/accounts/{id}/trips?from=2026-10-01")).ReadJsonAsync()).GetProperty("total").GetInt32());
        var completed = all.GetProperty("items").EnumerateArray().Single(r => r.GetProperty("tripId").GetString() == billing.EmployeeTripId);
        Assert.Equal(billing.EmployeeFare, completed.GetProperty("amountInclVat").GetDecimal());
        Assert.Equal("completed", completed.GetProperty("status").GetString());
        var export = await admin.GetAsync($"/api/v1/admin/corporate/accounts/{id}/trips/export?format=csv");
        Assert.Equal(HttpStatusCode.OK, export.StatusCode);
        Assert.Equal(4, Encoding.UTF8.GetString((await export.Content.ReadAsByteArrayAsync())[3..]).TrimEnd().Split('\n').Length);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/v1/admin/corporate/accounts/{Guid.NewGuid()}/trips")).StatusCode);

        // The account detail carries the dashboard-like summary and the read-only mirrors are available.
        var detail = await (await admin.GetAsync($"/api/v1/admin/corporate/accounts/{id}")).ReadJsonAsync();
        Assert.Equal(2, detail.GetProperty("summary").GetProperty("monthToDate").GetProperty("trips").GetInt32());
        Assert.Equal(billing.EmployeeFare + billing.GuestFare + 5m, detail.GetProperty("summary").GetProperty("monthToDate").GetProperty("spend").GetDecimal());
        Assert.Equal(0, (await (await admin.GetAsync($"/api/v1/admin/corporate/accounts/{id}/policies")).ReadJsonAsync()).GetArrayLength());
        Assert.Equal(1, (await (await admin.GetAsync($"/api/v1/admin/corporate/accounts/{id}/cost-centers")).ReadJsonAsync()).GetArrayLength());
        var employees = await (await admin.GetAsync($"/api/v1/admin/corporate/accounts/{id}/employees")).ReadJsonAsync();
        Assert.Equal(2, employees.GetProperty("total").GetInt32()); // the admin and the employee
        // Admin mirrors of the employee actions.
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/v1/admin/corporate/accounts/{id}/employees/{billing.Employee.MemberId}/disable", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/v1/admin/corporate/accounts/{id}/employees/{billing.Employee.MemberId}/enable", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.PostAsync($"/api/v1/admin/corporate/accounts/{Guid.NewGuid()}/employees/{billing.Employee.MemberId}/disable", null)).StatusCode);
    }
}
