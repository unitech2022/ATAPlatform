using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ATA.Api.Modules.Corporate;
using ATA.Api.Modules.Trips;
using ATA.Domain.Corporate;
using ATA.Domain.Wallet;
using ATA.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ATA.Tests.Integration;

/// <summary>Shared setup for the invoice tests: a company with an employee trip, a guest trip, a billed cancellation fee and an adjustment.</summary>
internal static class BillingFlow
{
    public sealed record Billing(CorporateFlow.Company Company, CorporateFlow.Employee Employee, string EmployeeTripId, string GuestTripId, string CancelledTripId, decimal EmployeeFare, decimal GuestFare);

    public static async Task<string> CompleteGuestTripAsync(CorporateFixture fixture, CorporateFlow.Company company, SafetyFlow.Party driver, (decimal Lat, decimal Lng) area, string guestName = "ضيف الفاتورة")
    {
        (await driver.Client.PutAsJsonAsync("/api/v1/driver/location", new { lat = area.Lat, lng = area.Lng, heading = 0, accuracy = 5 })).EnsureSuccessStatusCode();
        var booked = await company.Portal.PostAsJsonAsync("/api/v1/corporate/bookings", CorporateFlow.PortalBooking(area, guestName: guestName, guestPhone: fixture.NextPhone(), purpose: "استقبال"));
        Assert.Equal(HttpStatusCode.Created, booked.StatusCode);
        var tripId = (await booked.ReadJsonAsync()).GetProperty("id").GetString()!;
        await fixture.Factory.RunMatcherAsync();
        var offer = await (await driver.Client.GetAsync("/api/v1/driver/offers/active")).ReadJsonAsync();
        (await driver.Client.PostAsync($"/api/v1/driver/offers/{offer.GetProperty("id").GetString()}/accept", null)).EnsureSuccessStatusCode();
        var trip = await RewardsFlow.TripAsync(fixture, tripId);
        var pin = await fixture.Factory.WithServiceAsync<TripPinService, string?>(pins => Task.FromResult<string?>(pins.Reveal(trip)));
        await TripFlow.DriveAsync(driver.Client, tripId, pin!);
        (await driver.Client.PostAsJsonAsync($"/api/v1/driver/trips/{tripId}/complete", new { })).EnsureSuccessStatusCode();
        return tripId;
    }

    public static async Task<Billing> SeedAsync(CorporateFixture fixture, (decimal Lat, decimal Lng) area, string name = "شركة الفواتير")
    {
        var company = await CorporateFlow.OnboardAsync(fixture, name, creditLimit: 100000m);
        var costCenter = await CorporateFlow.CreateCostCenterAsync(company, "FIN-01", "المالية");
        var employee = await CorporateFlow.AddEmployeeAsync(fixture, company, "موظف الفاتورة", b => { b["department"] = "المالية"; b["employeeNumber"] = "E-1"; });
        var (driver, _) = await SafetyFlow.OnlineDriverAsync(fixture, area);
        var t1 = await CorporateFlow.CompleteCorporateTripAsync(fixture, area, CorporateFlow.Party(employee), driver, CorporateFlow.TripRequest(area, "اجتماع", Guid.Parse(costCenter.GetProperty("id").GetString()!)));
        var t2 = await CompleteGuestTripAsync(fixture, company, driver, area);

        // A cancellation after the free window: the 5 SAR fee is billed to the company.
        (await driver.Client.PutAsJsonAsync("/api/v1/driver/location", new { lat = area.Lat, lng = area.Lng, heading = 0, accuracy = 5 })).EnsureSuccessStatusCode();
        var assigned = await TripFlow.RequestAndAssignAsync(fixture, employee.Client, driver.Client, CorporateFlow.TripRequest(area));
        var t3 = assigned.GetProperty("id").GetString()!;
        fixture.Factory.Clock.Advance(TimeSpan.FromSeconds(200));
        (await employee.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{t3}/cancel", new { reasonCode = "changed_mind" })).EnsureSuccessStatusCode();
        var fare1 = (await RewardsFlow.TripAsync(fixture, t1)).FinalFare!.Value;
        var fare2 = (await RewardsFlow.TripAsync(fixture, t2)).FinalFare!.Value;
        return new Billing(company, employee, t1, t2, t3, fare1, fare2);
    }
}

/// <summary>F19 invoices: generation with VAT per line, issue with PDF / notification / e-mail, downloads, payments, void, exposure.</summary>
public class CorporateInvoiceTests(CorporateFixture fixture) : IClassFixture<CorporateFixture>
{
    private static readonly DateOnly Period = new(2026, 9, 1);

    [Fact]
    public async Task Generation_lines_vat_uniqueness_and_draft_visibility()
    {
        var area = TripFlow.Area(40);
        var billing = await BillingFlow.SeedAsync(fixture, area);
        var company = billing.Company;
        using var admin = await fixture.LoginAdminAsync();
        await admin.PostAsJsonAsync($"/api/v1/admin/corporate/accounts/{company.AccountId}/adjustments", new { amount = 11.50m, description = "رسوم خدمة إضافية" });

        // Validation of the period.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PostAsJsonAsync($"/api/v1/admin/corporate/accounts/{company.AccountId}/invoices/generate", new { periodStart = "2026-09-15" })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PostAsJsonAsync($"/api/v1/admin/corporate/accounts/{company.AccountId}/invoices/generate", new { periodStart = "2027-01-01" })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PostAsJsonAsync($"/api/v1/admin/corporate/accounts/{company.AccountId}/invoices/generate", new { })).StatusCode);
        var empty = await admin.PostAsJsonAsync($"/api/v1/admin/corporate/accounts/{company.AccountId}/invoices/generate", new { periodStart = "2026-08-01" });
        Assert.Equal(HttpStatusCode.Conflict, empty.StatusCode);
        Assert.Equal("nothing_to_bill", (await empty.ReadJsonAsync()).GetProperty("error").GetProperty("details").GetProperty("reason").GetString());

        var generated = await admin.PostAsJsonAsync($"/api/v1/admin/corporate/accounts/{company.AccountId}/invoices/generate", new { periodStart = "2026-09-01" });
        Assert.Equal(HttpStatusCode.Created, generated.StatusCode);
        var invoice = await generated.ReadJsonAsync();
        var id = invoice.GetProperty("id").GetString()!;
        Assert.Matches("^INV-202609-[0-9]{5}$", invoice.GetProperty("invoiceNumber").GetString());
        Assert.Equal("draft", invoice.GetProperty("status").GetString());
        Assert.Equal("2026-09-01", invoice.GetProperty("periodStart").GetString());
        Assert.Equal("2026-09-30", invoice.GetProperty("periodEnd").GetString());
        Assert.Equal(2, invoice.GetProperty("tripsCount").GetInt32());
        Assert.Equal(15m, invoice.GetProperty("vatRate").GetDecimal());
        Assert.Equal("SAR", invoice.GetProperty("currency").GetString());
        Assert.Equal(company.AccountId.ToString(), invoice.GetProperty("accountId").GetString());
        Assert.Equal(DateOnly.Parse(invoice.GetProperty("issueDate").GetString()!).AddDays(30), DateOnly.Parse(invoice.GetProperty("dueDate").GetString()!));

        // Lines: 2 trips + the cancellation fee + the adjustment; VAT = incl − round(incl / 1.15, 2) per line; totals are the sums.
        var detail = await (await admin.GetAsync($"/api/v1/admin/corporate/invoices/{id}")).ReadJsonAsync();
        var lines = detail.GetProperty("lines").GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(4, lines.Count);
        Assert.Equal(["adjustment", "cancellation_fee", "trip", "trip"], lines.Select(l => l.GetProperty("lineType").GetString()).Order(StringComparer.Ordinal).ToList());
        foreach (var line in lines)
        {
            var incl = line.GetProperty("amountInclVat").GetDecimal();
            var excl = decimal.Round(incl / 1.15m, 2, MidpointRounding.AwayFromZero);
            Assert.Equal(excl, line.GetProperty("amountExclVat").GetDecimal());
            Assert.Equal(incl - excl, line.GetProperty("vatAmount").GetDecimal());
        }

        Assert.Equal(lines.Sum(l => l.GetProperty("amountInclVat").GetDecimal()), invoice.GetProperty("totalInclVat").GetDecimal());
        Assert.Equal(lines.Sum(l => l.GetProperty("amountExclVat").GetDecimal()), invoice.GetProperty("subtotalExclVat").GetDecimal());
        Assert.Equal(lines.Sum(l => l.GetProperty("vatAmount").GetDecimal()), invoice.GetProperty("vatAmount").GetDecimal());
        Assert.Equal(invoice.GetProperty("subtotalExclVat").GetDecimal() + invoice.GetProperty("vatAmount").GetDecimal(), invoice.GetProperty("totalInclVat").GetDecimal());
        Assert.Equal(billing.EmployeeFare + billing.GuestFare + 5m + 11.50m, invoice.GetProperty("totalInclVat").GetDecimal());
        var tripLine = lines.Single(l => l.GetProperty("tripId").GetString() == billing.EmployeeTripId);
        Assert.Equal("موظف الفاتورة", tripLine.GetProperty("employeeName").GetString());
        Assert.Equal("E-1", tripLine.GetProperty("employeeNumber").GetString());
        Assert.Equal("المالية", tripLine.GetProperty("department").GetString());
        Assert.Equal("FIN-01", tripLine.GetProperty("costCenterCode").GetString());
        Assert.Equal("اجتماع", tripLine.GetProperty("purpose").GetString());
        Assert.Equal("المنزل", tripLine.GetProperty("pickupName").GetString());
        var guestLine = lines.Single(l => l.GetProperty("tripId").GetString() == billing.GuestTripId);
        Assert.Equal("ضيف الفاتورة", guestLine.GetProperty("guestName").GetString());
        Assert.Equal(5m, lines.Single(l => l.GetProperty("lineType").GetString() == "cancellation_fee").GetProperty("amountInclVat").GetDecimal());

        // Generated once per period.
        var again = await admin.PostAsJsonAsync($"/api/v1/admin/corporate/accounts/{company.AccountId}/invoices/generate", new { periodStart = "2026-09-01" });
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
        Assert.Equal("invoice_exists", (await again.ReadJsonAsync()).GetProperty("error").GetProperty("details").GetProperty("reason").GetString());

        // A draft is the platform's working copy: the company sees nothing; the receivable still counts it as unpaid, nothing is unbilled any more.
        Assert.Equal(0, (await (await company.Portal.GetAsync("/api/v1/corporate/invoices")).ReadJsonAsync()).GetProperty("total").GetInt32());
        Assert.Equal(HttpStatusCode.NotFound, (await company.Portal.GetAsync($"/api/v1/corporate/invoices/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await company.Portal.GetAsync($"/api/v1/corporate/invoices/{id}/pdf")).StatusCode);
        var receivable = (await (await admin.GetAsync("/api/v1/admin/corporate/receivables")).ReadJsonAsync()).EnumerateArray().Single(r => r.GetProperty("accountId").GetString() == company.AccountId.ToString());
        Assert.Equal(0m, receivable.GetProperty("unbilled").GetDecimal());
        Assert.Equal(invoice.GetProperty("totalInclVat").GetDecimal(), receivable.GetProperty("unpaidInvoices").GetDecimal());
        var preview = await admin.GetAsync($"/api/v1/admin/corporate/invoices/{id}/pdf");
        Assert.Equal(HttpStatusCode.OK, preview.StatusCode);
        Assert.StartsWith("%PDF-", Encoding.ASCII.GetString((await preview.Content.ReadAsByteArrayAsync())[..5]));
        var row = await fixture.Factory.WithDbAsync(db => db.AuditLogs.AsNoTracking().SingleAsync(a => a.Action == "corporate_invoice.generate" && a.EntityId == company.AccountId));
        Assert.Contains(id, row.AfterJson);
        Assert.Contains(company.AccountId, await fixture.Factory.WithDbAsync(db => db.AuditLogs.AsNoTracking().Where(a => a.Action == "corporate_adjustment.create").Select(a => a.EntityId!.Value).ToListAsync()));
        await PaymentFlow.AssertLedgerBalancedAsync(fixture);
    }

    [Fact]
    public async Task Issue_renders_the_pdf_notifies_the_admins_and_mails_the_billing_address_then_downloads_are_company_scoped()
    {
        var billing = await BillingFlow.SeedAsync(fixture, TripFlow.Area(41), "شركة الإصدار");
        var company = billing.Company;
        using var admin = await fixture.LoginAdminAsync();
        var invoice = await (await admin.PostAsJsonAsync($"/api/v1/admin/corporate/accounts/{company.AccountId}/invoices/generate", new { periodStart = "2026-09-01" })).ReadJsonAsync();
        var id = invoice.GetProperty("id").GetString()!;
        var number = invoice.GetProperty("invoiceNumber").GetString()!;

        var issued = await admin.PostAsync($"/api/v1/admin/corporate/invoices/{id}/issue", null);
        Assert.Equal(HttpStatusCode.OK, issued.StatusCode);
        var body = await issued.ReadJsonAsync();
        Assert.Equal("issued", body.GetProperty("status").GetString());
        Assert.NotEqual(JsonValueKind.Null, body.GetProperty("pdfFileId").ValueKind);
        Assert.NotEqual(JsonValueKind.Null, body.GetProperty("issuedAt").ValueKind);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsync($"/api/v1/admin/corporate/invoices/{id}/issue", null)).StatusCode);

        // The PDF is stored in stored_files (owned by the platform admin who created the company) and readable by its owner through /files.
        var file = await fixture.Factory.WithDbAsync(db => db.StoredFiles.AsNoTracking().SingleAsync(f => f.Id == Guid.Parse(body.GetProperty("pdfFileId").GetString()!)));
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal($"{number}.pdf", file.OriginalName);
        Assert.True(file.SizeBytes > 2000);

        // The company admins are notified (inbox + SMS) with the deep link of the portal page; billing_email gets the PDF.
        await fixture.Factory.RunNotificationWorkerAsync();
        var notification = await fixture.Factory.WithDbAsync(db => db.Notifications.AsNoTracking().SingleAsync(n => n.UserId == company.AdminUserId && n.Type == "corporate.invoice_issued"));
        Assert.Contains(number, notification.BodyAr);
        Assert.Contains($"/business/app/invoices/{id}", notification.Data);
        Assert.Contains(fixture.Sms.Sent, m => m.Phone == company.AdminPhone && m.Message.Contains(number));
        var mail = Assert.Single(fixture.Email.Sent, m => m.Subject.Contains(number));
        Assert.Equal("billing@test-company.sa", mail.To);
        var attachment = Assert.Single(mail.Attachments!);
        Assert.Equal($"{number}.pdf", attachment.FileName);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(attachment.Content[..5]));

        // Download: company portal (attachment with the invoice number as name), admin console; other companies, riders and anonymous callers are refused.
        var pdf = await company.Portal.GetAsync($"/api/v1/corporate/invoices/{id}/pdf");
        Assert.Equal(HttpStatusCode.OK, pdf.StatusCode);
        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType!.MediaType);
        Assert.Equal($"{number}.pdf", pdf.Content.Headers.ContentDisposition!.FileNameStar ?? pdf.Content.Headers.ContentDisposition.FileName);
        var bytes = await pdf.Content.ReadAsByteArrayAsync();
        Assert.Equal("%PDF-", Encoding.ASCII.GetString(bytes[..5]));
        Assert.Contains("%%EOF", Encoding.ASCII.GetString(bytes[^32..]));
        Assert.Equal(file.SizeBytes, bytes.Length);
        var adminPdf = await admin.GetAsync($"/api/v1/admin/corporate/invoices/{id}/pdf");
        Assert.Equal(bytes, await adminPdf.Content.ReadAsByteArrayAsync());
        var other = await CorporateFlow.OnboardAsync(fixture);
        Assert.Equal(HttpStatusCode.NotFound, (await other.Portal.GetAsync($"/api/v1/corporate/invoices/{id}/pdf")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.Portal.GetAsync($"/api/v1/corporate/invoices/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.Portal.GetAsync($"/api/v1/corporate/invoices/{id}/export?format=csv")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await billing.Employee.Client.GetAsync($"/api/v1/corporate/invoices/{id}/pdf")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await billing.Employee.Client.GetAsync($"/api/v1/admin/corporate/invoices/{id}/pdf")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.Portal.GetAsync($"/api/v1/admin/corporate/invoices/{id}/pdf")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await fixture.CreateClient().GetAsync($"/api/v1/corporate/invoices/{id}/pdf")).StatusCode);
        var noPermission = await CorporateFlow.LimitedAdminAsync(fixture, "inv-none", "trips.view");
        Assert.Equal(HttpStatusCode.Forbidden, (await noPermission.GetAsync($"/api/v1/admin/corporate/invoices/{id}/pdf")).StatusCode);

        // Regenerated on demand when the stored file is gone.
        await fixture.Factory.WithDbAsync(async db =>
        {
            var stored = await db.StoredFiles.SingleAsync(f => f.Id == file.Id);
            using var scope = fixture.Factory.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ATA.Infrastructure.Storage.IFileStorage>().DeleteAsync(stored.StorageKey);
            return true;
        });
        var again = await company.Portal.GetAsync($"/api/v1/corporate/invoices/{id}/pdf");
        Assert.Equal(HttpStatusCode.OK, again.StatusCode);
        Assert.Equal("%PDF-", Encoding.ASCII.GetString((await again.Content.ReadAsByteArrayAsync())[..5]));

        // Listing and detail for the company (lines are paged), filters and CSV.
        var list = await (await company.Portal.GetAsync("/api/v1/corporate/invoices?status=issued")).ReadJsonAsync();
        Assert.Equal(1, list.GetProperty("total").GetInt32());
        Assert.Equal(0, (await (await company.Portal.GetAsync("/api/v1/corporate/invoices?status=paid")).ReadJsonAsync()).GetProperty("total").GetInt32());
        var detail = await (await company.Portal.GetAsync($"/api/v1/corporate/invoices/{id}?page=1&pageSize=2")).ReadJsonAsync();
        Assert.Equal(3, detail.GetProperty("lines").GetProperty("total").GetInt32());
        Assert.Equal(2, detail.GetProperty("lines").GetProperty("items").GetArrayLength());
        var csv = await company.Portal.GetAsync($"/api/v1/corporate/invoices/{id}/export?format=csv");
        Assert.Equal(HttpStatusCode.OK, csv.StatusCode);
        var csvBytes = await csv.Content.ReadAsByteArrayAsync();
        Assert.Equal([0xEF, 0xBB, 0xBF], csvBytes[..3]);
        var text = Encoding.UTF8.GetString(csvBytes[3..]);
        Assert.StartsWith("invoice_number,line_type,trip_number,date,employee,employee_number,department,cost_center,guest,purpose,pickup,dropoff,description,amount_excl_vat,vat,amount_incl_vat", text);
        Assert.Equal(1 + 3, text.TrimEnd().Split('\n').Length);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await company.Portal.GetAsync($"/api/v1/corporate/invoices/{id}/export?format=xlsx")).StatusCode);

        var adminList = await (await admin.GetAsync($"/api/v1/admin/corporate/invoices?accountId={company.AccountId}&status=issued&from=2026-09-01&to=2026-09-30")).ReadJsonAsync();
        Assert.Equal(1, adminList.GetProperty("total").GetInt32());
        Assert.Equal("شركة الإصدار", adminList.GetProperty("items")[0].GetProperty("accountName").GetString());
        Assert.Equal(0, (await (await admin.GetAsync($"/api/v1/admin/corporate/invoices?accountId={company.AccountId}&from=2026-10-01")).ReadJsonAsync()).GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task Mark_paid_posts_the_collection_journal_and_a_partial_payment_keeps_the_invoice_open()
    {
        var billing = await BillingFlow.SeedAsync(fixture, TripFlow.Area(42), "شركة التحصيل");
        var company = billing.Company;
        using var admin = await fixture.LoginAdminAsync();
        await admin.PostAsJsonAsync($"/api/v1/admin/corporate/accounts/{company.AccountId}/adjustments", new { amount = -2.30m, description = "خصم تجاري" });
        var invoice = await (await admin.PostAsJsonAsync($"/api/v1/admin/corporate/accounts/{company.AccountId}/invoices/generate", new { periodStart = "2026-09-01" })).ReadJsonAsync();
        var id = invoice.GetProperty("id").GetString()!;
        var total = invoice.GetProperty("totalInclVat").GetDecimal();
        Assert.Equal(billing.EmployeeFare + billing.GuestFare + 5m - 2.30m, total);

        // Only open invoices can be paid.
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"/api/v1/admin/corporate/invoices/{id}/mark-paid", new { amount = 10m, reference = "TRX-1" })).StatusCode);
        await admin.PostAsync($"/api/v1/admin/corporate/invoices/{id}/issue", null);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PostAsJsonAsync($"/api/v1/admin/corporate/invoices/{id}/mark-paid", new { amount = 0m, reference = "TRX-1" })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PostAsJsonAsync($"/api/v1/admin/corporate/invoices/{id}/mark-paid", new { amount = 10m })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PostAsJsonAsync($"/api/v1/admin/corporate/invoices/{id}/mark-paid", new { amount = total + 1m, reference = "TRX-X" })).StatusCode);

        var partial = await (await admin.PostAsJsonAsync($"/api/v1/admin/corporate/invoices/{id}/mark-paid", new { amount = 10m, reference = "TRX-1" })).ReadJsonAsync();
        Assert.Equal("issued", partial.GetProperty("status").GetString());
        Assert.Equal(10m, partial.GetProperty("paidAmount").GetDecimal());
        Assert.Equal(JsonValueKind.Null, partial.GetProperty("paidAt").ValueKind);
        var receivableAfterPartial = (await (await admin.GetAsync("/api/v1/admin/corporate/receivables")).ReadJsonAsync()).EnumerateArray().Single(r => r.GetProperty("accountId").GetString() == company.AccountId.ToString());
        Assert.Equal(total - 10m, receivableAfterPartial.GetProperty("unpaidInvoices").GetDecimal());

        var paid = await (await admin.PostAsJsonAsync($"/api/v1/admin/corporate/invoices/{id}/mark-paid", new { amount = total - 10m, reference = "TRX-2", paidAt = fixture.Factory.Clock.UtcNow })).ReadJsonAsync();
        Assert.Equal("paid", paid.GetProperty("status").GetString());
        Assert.Equal(total, paid.GetProperty("paidAmount").GetDecimal());
        Assert.Equal("TRX-2", paid.GetProperty("paymentReference").GetString());
        Assert.NotEqual(JsonValueKind.Null, paid.GetProperty("paidAt").ValueKind);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"/api/v1/admin/corporate/invoices/{id}/mark-paid", new { amount = 1m, reference = "TRX-3" })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"/api/v1/admin/corporate/invoices/{id}/void", new { reason = "خطأ" })).StatusCode);

        // corporate_invoice_payment: platform_cash ← corporate_receivable; over the lifetime the receivable nets out to zero (trip charges + fee + adjustment = payments).
        var journals = await fixture.Factory.WithDbAsync(db => db.LedgerJournals.AsNoTracking().Where(j => j.ReferenceId == Guid.Parse(id)).OrderBy(j => j.CreatedAt).ToListAsync());
        Assert.Equal(2, journals.Count);
        Assert.All(journals, j => Assert.Equal(JournalType.CorporateInvoicePayment, j.Type));
        var entries = await fixture.Factory.WithDbAsync(db => db.LedgerEntries.AsNoTracking().Where(e => e.JournalId == journals[0].Id).ToListAsync());
        Assert.Equal(10m, entries.Single(e => e.Account == LedgerAccounts.PlatformCash).Debit);
        Assert.Equal(10m, entries.Single(e => e.Account == LedgerAccounts.CorporateReceivable(company.AccountId)).Credit);
        Assert.Equal(0m, await CorporateFlow.LedgerBalanceAsync(fixture, LedgerAccounts.CorporateReceivable(company.AccountId)));
        await PaymentFlow.AssertLedgerBalancedAsync(fixture);
        var row = await fixture.Factory.WithDbAsync(db => db.AuditLogs.AsNoTracking().Where(a => a.Action == "corporate_invoice.mark_paid" && a.EntityId == company.AccountId).ToListAsync());
        Assert.Equal(2, row.Count);

        // The company sees it as paid and the exposure is clear.
        Assert.Equal("paid", (await (await company.Portal.GetAsync($"/api/v1/corporate/invoices/{id}")).ReadJsonAsync()).GetProperty("status").GetString());
        var dashboard = await (await company.Portal.GetAsync("/api/v1/corporate/dashboard")).ReadJsonAsync();
        Assert.Equal(0, dashboard.GetProperty("openInvoices").GetProperty("count").GetInt32());
        Assert.Equal(0m, dashboard.GetProperty("creditUsed").GetDecimal());
    }

    [Fact]
    public async Task Void_releases_the_lines_and_adjustments_for_a_new_invoice_but_not_after_a_payment()
    {
        var billing = await BillingFlow.SeedAsync(fixture, TripFlow.Area(43), "شركة الإلغاء");
        var company = billing.Company;
        using var admin = await fixture.LoginAdminAsync();
        await admin.PostAsJsonAsync($"/api/v1/admin/corporate/accounts/{company.AccountId}/adjustments", new { amount = 23m, description = "تعديل" });
        var first = await (await admin.PostAsJsonAsync($"/api/v1/admin/corporate/accounts/{company.AccountId}/invoices/generate", new { periodStart = "2026-09-01" })).ReadJsonAsync();
        var firstId = first.GetProperty("id").GetString()!;
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PostAsJsonAsync($"/api/v1/admin/corporate/invoices/{firstId}/void", new { reason = "" })).StatusCode);
        var voided = await (await admin.PostAsJsonAsync($"/api/v1/admin/corporate/invoices/{firstId}/void", new { reason = "بيانات خاطئة" })).ReadJsonAsync();
        Assert.Equal("void", voided.GetProperty("status").GetString());
        Assert.Equal("بيانات خاطئة", voided.GetProperty("voidReason").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"/api/v1/admin/corporate/invoices/{firstId}/void", new { reason = "again" })).StatusCode);

        // Everything is unbilled again (trips, fee, adjustment) and a new invoice for the same period can be generated.
        var receivable = (await (await admin.GetAsync("/api/v1/admin/corporate/receivables")).ReadJsonAsync()).EnumerateArray().Single(r => r.GetProperty("accountId").GetString() == company.AccountId.ToString());
        Assert.Equal(first.GetProperty("totalInclVat").GetDecimal(), receivable.GetProperty("unbilled").GetDecimal());
        Assert.Equal(0m, receivable.GetProperty("unpaidInvoices").GetDecimal());
        var second = await admin.PostAsJsonAsync($"/api/v1/admin/corporate/accounts/{company.AccountId}/invoices/generate", new { periodStart = "2026-09-01" });
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        var regenerated = await second.ReadJsonAsync();
        Assert.NotEqual(first.GetProperty("invoiceNumber").GetString(), regenerated.GetProperty("invoiceNumber").GetString());
        Assert.Equal(first.GetProperty("totalInclVat").GetDecimal(), regenerated.GetProperty("totalInclVat").GetDecimal());
        Assert.Equal(2, regenerated.GetProperty("tripsCount").GetInt32());
        Assert.Equal(2, await fixture.Factory.WithDbAsync(db => db.CorporateInvoices.CountAsync(i => i.CorporateAccountId == company.AccountId)));

        // With a payment recorded the invoice can no longer be voided.
        var secondId = regenerated.GetProperty("id").GetString()!;
        await admin.PostAsync($"/api/v1/admin/corporate/invoices/{secondId}/issue", null);
        await admin.PostAsJsonAsync($"/api/v1/admin/corporate/invoices/{secondId}/mark-paid", new { amount = 1m, reference = "P-1" });
        var blocked = await admin.PostAsJsonAsync($"/api/v1/admin/corporate/invoices/{secondId}/void", new { reason = "x" });
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        Assert.Equal("not_voidable", (await blocked.ReadJsonAsync()).GetProperty("error").GetProperty("details").GetProperty("reason").GetString());
        var actions = await fixture.Factory.WithDbAsync(db => db.AuditLogs.AsNoTracking().Where(a => a.EntityId == company.AccountId && a.Action.StartsWith("corporate_invoice.")).Select(a => a.Action).Distinct().ToListAsync());
        Assert.Equivalent(new[] { "corporate_invoice.generate", "corporate_invoice.issue", "corporate_invoice.mark_paid", "corporate_invoice.void" }, actions);
    }

    [Fact]
    public async Task Waiving_a_charged_cancellation_fee_reverses_it_on_the_company_account()
    {
        var area = TripFlow.Area(44);
        var company = await CorporateFlow.OnboardAsync(fixture, "شركة الإعفاء", creditLimit: 100000m);
        var employee = await CorporateFlow.AddEmployeeAsync(fixture, company);
        var (driver, _) = await SafetyFlow.OnlineDriverAsync(fixture, area);
        (await driver.Client.PutAsJsonAsync("/api/v1/driver/location", new { lat = area.Lat, lng = area.Lng, heading = 0, accuracy = 5 })).EnsureSuccessStatusCode();
        var assigned = await TripFlow.RequestAndAssignAsync(fixture, employee.Client, driver.Client, CorporateFlow.TripRequest(area));
        var tripId = assigned.GetProperty("id").GetString()!;
        fixture.Factory.Clock.Advance(TimeSpan.FromSeconds(200));
        (await employee.Client.PostAsJsonAsync($"/api/v1/passenger/trips/{tripId}/cancel", new { reasonCode = "changed_mind" })).EnsureSuccessStatusCode();
        var cancellation = await fixture.Factory.WithDbAsync(db => db.CancellationEvents.AsNoTracking().SingleAsync(e => e.TripId == Guid.Parse(tripId)));
        Assert.Equal(-5m, await CorporateFlow.LedgerBalanceAsync(fixture, LedgerAccounts.CorporateReceivable(company.AccountId)));

        // Approving the appeal never refunds the rider's wallet: the fee is taken off the company's receivable.
        using var admin = await fixture.LoginAdminAsync();
        var reviewed = await admin.PostAsJsonAsync($"/api/v1/admin/cancellations/{cancellation.Id}/review", new { decision = "approve", note = "عذر مقبول" });
        Assert.Equal(HttpStatusCode.OK, reviewed.StatusCode);
        var after = await fixture.Factory.WithDbAsync(db => db.CancellationEvents.AsNoTracking().SingleAsync(e => e.Id == cancellation.Id));
        Assert.Equal(ATA.Domain.Cancellation.CancellationFeeStatus.Refunded, after.FeeStatus);
        Assert.Equal(0m, await CorporateFlow.LedgerBalanceAsync(fixture, LedgerAccounts.CorporateReceivable(company.AccountId)));
        Assert.Empty(await fixture.Factory.WithDbAsync(db => db.Refunds.AsNoTracking().Where(r => r.TripId == Guid.Parse(tripId)).ToListAsync()));
        var receivable = (await (await admin.GetAsync("/api/v1/admin/corporate/receivables")).ReadJsonAsync()).EnumerateArray().Single(r => r.GetProperty("accountId").GetString() == company.AccountId.ToString());
        Assert.Equal(0m, receivable.GetProperty("unbilled").GetDecimal());
        await PaymentFlow.AssertLedgerBalancedAsync(fixture);

        // Refund requests for a corporate trip are refused: ops credit the company with an adjustment instead.
        var refund = await admin.PostAsJsonAsync($"/api/v1/admin/trips/{tripId}/refunds", new { amount = 1m, reasonCode = "goodwill", reason = "x" });
        Assert.NotEqual(HttpStatusCode.Created, refund.StatusCode);
        Assert.Empty(await fixture.Factory.WithDbAsync(db => db.Refunds.AsNoTracking().ToListAsync()));
    }
}
