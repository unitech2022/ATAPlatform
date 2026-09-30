using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ATA.Domain.Corporate;
using ATA.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Integration;

/// <summary>F19 jobs with the production defaults (invoices stay drafts): the monthly invoice job and the overdue job.</summary>
public class CorporateInvoiceJobTests(CorporateFixture fixture) : IClassFixture<CorporateFixture>
{
    [Fact]
    public async Task Monthly_job_bills_the_previous_month_once_for_companies_with_movements_and_leaves_drafts()
    {
        var billing = await BillingFlow.SeedAsync(fixture, TripFlow.Area(50), "شركة الشهرية");
        var company = billing.Company;
        var quiet = await CorporateFlow.OnboardAsync(fixture, "شركة هادئة");
        using var preAdmin = await fixture.LoginAdminAsync();
        // Only an adjustment: movements too, so this company is invoiced as well.
        var adjustmentOnly = await CorporateFlow.OnboardAsync(fixture, "تعديل فقط");
        await preAdmin.PostAsJsonAsync($"/api/v1/admin/corporate/accounts/{adjustmentOnly.AccountId}/adjustments", new { amount = 115m, description = "رسوم اشتراك" });
        // A pending company is never invoiced.
        var pending = await CorporateFlow.OnboardAsync(fixture, "معلقة", activate: false);
        await preAdmin.PostAsJsonAsync($"/api/v1/admin/corporate/accounts/{pending.AccountId}/adjustments", new { amount = 10m, description = "x" });

        // Before 04:00 Riyadh on the 1st nothing is billed, even though September has ended.
        fixture.Factory.Clock.Set(new DateTime(2026, 9, 30, 23, 0, 0, DateTimeKind.Utc));
        Assert.Equal(0, await fixture.Factory.RunCorporateInvoiceJobAsync());
        fixture.Factory.Clock.Set(new DateTime(2026, 10, 1, 1, 0, 0, DateTimeKind.Utc));
        Assert.Equal(2, await fixture.Factory.RunCorporateInvoiceJobAsync());
        Assert.Equal(0, await fixture.Factory.RunCorporateInvoiceJobAsync());

        var invoices = await fixture.Factory.WithDbAsync(db => db.CorporateInvoices.AsNoTracking().OrderBy(i => i.InvoiceNumber).ToListAsync());
        Assert.Equal(2, invoices.Count);
        Assert.All(invoices, i =>
        {
            Assert.Equal(new DateOnly(2026, 9, 1), i.PeriodStart);
            Assert.Equal(new DateOnly(2026, 9, 30), i.PeriodEnd);
            Assert.Equal(CorporateInvoiceStatus.Draft, i.Status);
            Assert.Equal(new DateOnly(2026, 10, 1), i.IssueDate);
            Assert.Equal(new DateOnly(2026, 10, 31), i.DueDate);
            Assert.Null(i.PdfFileId);
        });
        Assert.Equal(["INV-202609-00001", "INV-202609-00002"], invoices.Select(i => i.InvoiceNumber).ToList());
        var main = invoices.Single(i => i.CorporateAccountId == company.AccountId);
        Assert.Equal(billing.EmployeeFare + billing.GuestFare + 5m, main.TotalInclVat);
        Assert.Equal(2, main.TripsCount);
        var adjustmentInvoice = invoices.Single(i => i.CorporateAccountId == adjustmentOnly.AccountId);
        Assert.Equal(115m, adjustmentInvoice.TotalInclVat);
        Assert.Equal(100m, adjustmentInvoice.SubtotalExclVat);
        Assert.Equal(15m, adjustmentInvoice.VatAmount);
        Assert.Equal(0, adjustmentInvoice.TripsCount);
        Assert.DoesNotContain(invoices, i => i.CorporateAccountId == quiet.AccountId || i.CorporateAccountId == pending.AccountId);
        var audit = await fixture.Factory.WithDbAsync(db => db.AuditLogs.AsNoTracking().Where(a => a.Action == "corporate_invoice.generate" && a.ActorRole == "system").CountAsync());
        Assert.Equal(2, audit);

        // A trip completed in October belongs to the October invoice only.
        using var admin = await fixture.LoginAdminAsync();
        var october = await admin.PostAsJsonAsync($"/api/v1/admin/corporate/accounts/{company.AccountId}/invoices/generate", new { periodStart = "2026-10-01" });
        Assert.Equal(HttpStatusCode.Conflict, october.StatusCode);

        // Issue the draft, let the due date pass: the overdue job flips it (audit actor system) and the company sees it as overdue.
        var id = main.Id;
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/v1/admin/corporate/invoices/{id}/issue", null)).StatusCode);
        fixture.Factory.Clock.Set(new DateTime(2026, 10, 31, 20, 0, 0, DateTimeKind.Utc)); // 23:00 on the due date: not overdue yet
        Assert.Equal(0, await fixture.Factory.RunCorporateOverdueJobAsync());
        fixture.Factory.Clock.Set(new DateTime(2026, 11, 1, 1, 0, 0, DateTimeKind.Utc));
        Assert.Equal(1, await fixture.Factory.RunCorporateOverdueJobAsync());
        Assert.Equal(0, await fixture.Factory.RunCorporateOverdueJobAsync());
        var overdue = await fixture.Factory.WithDbAsync(db => db.CorporateInvoices.AsNoTracking().SingleAsync(i => i.Id == id));
        Assert.Equal(CorporateInvoiceStatus.Overdue, overdue.Status);
        // SuspendAfterOverdueDays = 0 (default): the account stays active.
        Assert.Equal(CorporateAccountStatus.Active, (await fixture.Factory.WithDbAsync(db => db.CorporateAccounts.AsNoTracking().SingleAsync(a => a.Id == company.AccountId))).Status);
        using var admin2 = await fixture.LoginAdminAsync();
        var receivable = (await (await admin2.GetAsync("/api/v1/admin/corporate/receivables")).ReadJsonAsync()).EnumerateArray().Single(r => r.GetProperty("accountId").GetString() == company.AccountId.ToString());
        Assert.Equal(main.TotalInclVat, receivable.GetProperty("overdueAmount").GetDecimal());
        Assert.Equal(main.TotalInclVat, receivable.GetProperty("unpaidInvoices").GetDecimal());
        var (portal, _) = await fixture.LoginAsync("corporate_admin", company.AdminPhone);
        Assert.Equal("overdue", (await (await portal.GetAsync($"/api/v1/corporate/invoices/{id}")).ReadJsonAsync()).GetProperty("status").GetString());
        var dashboard = await (await portal.GetAsync("/api/v1/corporate/dashboard")).ReadJsonAsync();
        Assert.Equal(1, dashboard.GetProperty("openInvoices").GetProperty("count").GetInt32());
        Assert.Equal(main.TotalInclVat, dashboard.GetProperty("openInvoices").GetProperty("amount").GetDecimal());
        // An overdue invoice can still be paid.
        var paid = await admin2.PostAsJsonAsync($"/api/v1/admin/corporate/invoices/{id}/mark-paid", new { amount = main.TotalInclVat, reference = "LATE-1" });
        Assert.Equal("paid", (await paid.ReadJsonAsync()).GetProperty("status").GetString());
    }
}

/// <summary>F19 with <c>AutoIssueInvoices=true</c> and <c>SuspendAfterOverdueDays=10</c>.</summary>
public class CorporateAutoIssueTests(CorporateAutoIssueFixture fixture) : IClassFixture<CorporateAutoIssueFixture>
{
    [Fact]
    public async Task Job_issues_the_invoice_at_once_with_pdf_notification_and_email_then_overdue_suspends_the_company()
    {
        var billing = await BillingFlow.SeedAsync(fixture, TripFlow.Area(51), "شركة التلقائية");
        var company = billing.Company;
        fixture.Factory.Clock.Set(new DateTime(2026, 10, 1, 1, 30, 0, DateTimeKind.Utc));
        Assert.Equal(1, await fixture.Factory.RunCorporateInvoiceJobAsync());
        var invoice = await fixture.Factory.WithDbAsync(db => db.CorporateInvoices.AsNoTracking().SingleAsync(i => i.CorporateAccountId == company.AccountId));
        Assert.Equal(CorporateInvoiceStatus.Issued, invoice.Status);
        Assert.NotNull(invoice.PdfFileId);
        Assert.NotNull(invoice.IssuedAt);
        Assert.Null(invoice.IssuedBy);
        Assert.Equal(new DateOnly(2026, 10, 31), invoice.DueDate);
        var issueAudit = await fixture.Factory.WithDbAsync(db => db.AuditLogs.AsNoTracking().Where(a => a.Action == "corporate_invoice.issue" && a.EntityId == company.AccountId).SingleAsync());
        Assert.Equal("system", issueAudit.ActorRole);
        await fixture.Factory.RunNotificationWorkerAsync();
        Assert.Contains(fixture.Sms.Sent, m => m.Phone == company.AdminPhone && m.Message.Contains(invoice.InvoiceNumber));
        Assert.Contains(fixture.Email.Sent, m => m.Subject.Contains(invoice.InvoiceNumber) && m.To == "billing@test-company.sa");
        var (portal, _) = await fixture.LoginAsync("corporate_admin", company.AdminPhone);
        var pdf = await portal.GetAsync($"/api/v1/corporate/invoices/{invoice.Id}/pdf");
        Assert.Equal("%PDF-", Encoding.ASCII.GetString((await pdf.Content.ReadAsByteArrayAsync())[..5]));

        // 5 days overdue: flagged overdue but not suspended yet (limit 10 days); 12 days overdue: the company is suspended by the system.
        fixture.Factory.Clock.Set(new DateTime(2026, 11, 5, 1, 0, 0, DateTimeKind.Utc));
        Assert.Equal(1, await fixture.Factory.RunCorporateOverdueJobAsync());
        Assert.Equal(CorporateAccountStatus.Active, (await fixture.Factory.WithDbAsync(db => db.CorporateAccounts.AsNoTracking().SingleAsync(a => a.Id == company.AccountId))).Status);
        fixture.Factory.Clock.Set(new DateTime(2026, 11, 13, 1, 0, 0, DateTimeKind.Utc));
        Assert.Equal(1, await fixture.Factory.RunCorporateOverdueJobAsync());
        Assert.Equal(CorporateAccountStatus.Suspended, (await fixture.Factory.WithDbAsync(db => db.CorporateAccounts.AsNoTracking().SingleAsync(a => a.Id == company.AccountId))).Status);
        var suspension = await fixture.Factory.WithDbAsync(db => db.AuditLogs.AsNoTracking().Where(a => a.Action == "corporate_account.suspend" && a.EntityId == company.AccountId).SingleAsync());
        Assert.Equal("system", suspension.ActorRole);
        Assert.Contains("overdue_invoice", suspension.AfterJson);
        Assert.Equal(0, await fixture.Factory.RunCorporateOverdueJobAsync());

        // Suspended: employees cannot book, the portal still reads; once paid an admin can reactivate.
        var (employee, _) = await fixture.LoginAsync("passenger", "0" + billing.Employee.Phone[4..]);
        var blocked = await employee.PostAsJsonAsync("/api/v1/passenger/trips", CorporateFlow.TripRequest(TripFlow.Area(51)));
        Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);
        Assert.Equal("corporate_account_inactive", await blocked.ErrorCodeAsync());
        var (portal2, _) = await fixture.LoginAsync("corporate_admin", company.AdminPhone);
        Assert.Equal(HttpStatusCode.OK, (await portal2.GetAsync($"/api/v1/corporate/invoices/{invoice.Id}")).StatusCode);
        using var admin = await fixture.LoginAdminAsync();
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync($"/api/v1/admin/corporate/invoices/{invoice.Id}/mark-paid", new { amount = invoice.TotalInclVat, reference = "BANK-9" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/v1/admin/corporate/accounts/{company.AccountId}/activate", null)).StatusCode);
    }
}
