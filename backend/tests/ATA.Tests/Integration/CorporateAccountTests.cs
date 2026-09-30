using System.Net;
using System.Net.Http.Json;
using System.Text;
using ATA.Domain.Corporate;
using ATA.Infrastructure.Persistence.Seed;
using ATA.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Integration;

/// <summary>F19 platform side: company creation, validation, lifecycle, permissions, audits and the portal account.</summary>
public class CorporateAccountTests(CorporateFixture fixture) : IClassFixture<CorporateFixture>
{
    [Fact]
    public async Task Admin_creates_a_pending_company_with_a_sequential_account_number_and_audit()
    {
        using var admin = await fixture.LoginAdminAsync();
        var created = await CorporateFlow.CreateAccountAsync(admin, "شركة البناء", 12000m);
        Assert.Equal("pending", created.GetProperty("status").GetString());
        Assert.Matches("^CA-[0-9]{5}$", created.GetProperty("accountNumber").GetString());
        Assert.Equal(12000m, created.GetProperty("creditLimit").GetDecimal());
        Assert.Equal("monthly", created.GetProperty("billingCycle").GetString());
        Assert.Equal(30, created.GetProperty("paymentTermsDays").GetInt32());
        Assert.Equal("12211", created.GetProperty("billingAddress").GetProperty("postalCode").GetString());
        Assert.Equal("+966500000100", created.GetProperty("contactPhone").GetString());
        var id = Guid.Parse(created.GetProperty("id").GetString()!);

        var second = await CorporateFlow.CreateAccountAsync(admin, "شركة ثانية");
        var firstNumber = int.Parse(created.GetProperty("accountNumber").GetString()![3..]);
        Assert.Equal(firstNumber + 1, int.Parse(second.GetProperty("accountNumber").GetString()![3..]));

        var listed = await (await admin.GetAsync("/api/v1/admin/corporate/accounts?status=pending&search=البناء")).ReadJsonAsync();
        Assert.Equal(1, listed.GetProperty("total").GetInt32());
        Assert.NotNull(listed.GetProperty("items")[0].GetProperty("cityName").GetString());
        var detail = await (await admin.GetAsync($"/api/v1/admin/corporate/accounts/{id}")).ReadJsonAsync();
        Assert.Equal("test company", detail.GetProperty("notes").GetString());
        Assert.Equal(0, detail.GetProperty("summary").GetProperty("activeEmployees").GetInt32());

        var audit = await fixture.Factory.WithDbAsync(db => db.AuditLogs.AsNoTracking().Where(a => a.EntityType == "corporate_account" && a.EntityId == id).Select(a => a.Action).ToListAsync());
        Assert.Equal(["corporate_account.create"], audit);

        var updated = await admin.PutAsJsonAsync($"/api/v1/admin/corporate/accounts/{id}", CorporateFlow.AccountInput("شركة البناء", 9000m, created.GetProperty("crNumber").GetString()));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        Assert.Equal(9000m, (await updated.ReadJsonAsync()).GetProperty("creditLimit").GetDecimal());
    }

    [Fact]
    public async Task Company_validation_follows_the_column_rules()
    {
        using var admin = await fixture.LoginAdminAsync();
        async Task<JsonFieldErrors> Post(object body)
        {
            var response = await admin.PostAsJsonAsync("/api/v1/admin/corporate/accounts", body);
            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
            var details = (await response.ReadJsonAsync()).GetProperty("error").GetProperty("details");
            return new JsonFieldErrors(details.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal).ToList());
        }

        var bad = await Post(new
        {
            legalNameAr = "", legalNameEn = "X", displayName = "X", crNumber = "12345", vatNumber = "123456789012345", billingEmail = "not-an-email", contactName = "X", contactPhone = "0500",
            creditLimit = -1, paymentTermsDays = 400,
        });
        Assert.Equal(["billingEmail", "contactPhone", "crNumber", "creditLimit", "legalNameAr", "paymentTermsDays", "vatNumber"], bad.Fields);

        // The CR number is unique.
        var existing = await CorporateFlow.CreateAccountAsync(admin);
        var duplicate = await admin.PostAsJsonAsync("/api/v1/admin/corporate/accounts", CorporateFlow.AccountInput(crNumber: existing.GetProperty("crNumber").GetString()));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var vatOk = await admin.PostAsJsonAsync("/api/v1/admin/corporate/accounts", CorporateFlow.AccountInput(vatNumber: "300000000000003"));
        Assert.Equal(HttpStatusCode.Created, vatOk.StatusCode);
    }

    private sealed record JsonFieldErrors(List<string> Fields);

    [Fact]
    public async Task Activation_needs_an_active_admin_and_suspend_and_close_take_a_reason()
    {
        using var admin = await fixture.LoginAdminAsync();
        var created = await CorporateFlow.CreateAccountAsync(admin);
        var id = created.GetProperty("id").GetString();
        var blocked = await admin.PostAsync($"/api/v1/admin/corporate/accounts/{id}/activate", null);
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        Assert.Equal("no_active_admin", (await blocked.ReadJsonAsync()).GetProperty("error").GetProperty("details").GetProperty("reason").GetString());

        var phone = fixture.NextPhone();
        Assert.Equal(HttpStatusCode.Created, (await admin.PostAsJsonAsync($"/api/v1/admin/corporate/accounts/{id}/admins", new { phoneNumber = phone, fullName = "مدير" })).StatusCode);
        // An invited (not yet accepted) admin does not count.
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsync($"/api/v1/admin/corporate/accounts/{id}/activate", null)).StatusCode);
        await fixture.LoginAsync("corporate_admin", phone);
        var activated = await admin.PostAsync($"/api/v1/admin/corporate/accounts/{id}/activate", null);
        Assert.Equal(HttpStatusCode.OK, activated.StatusCode);
        Assert.Equal("active", (await activated.ReadJsonAsync()).GetProperty("status").GetString());

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await admin.PostAsJsonAsync($"/api/v1/admin/corporate/accounts/{id}/suspend", new { reason = "" })).StatusCode);
        var suspended = await admin.PostAsJsonAsync($"/api/v1/admin/corporate/accounts/{id}/suspend", new { reason = "فاتورة متأخرة" });
        Assert.Equal("suspended", (await suspended.ReadJsonAsync()).GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsync($"/api/v1/admin/corporate/accounts/{id}/activate", null)).StatusCode);
        var closed = await admin.PostAsJsonAsync($"/api/v1/admin/corporate/accounts/{id}/close", new { reason = "إنهاء العقد" });
        Assert.Equal("closed", (await closed.ReadJsonAsync()).GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsync($"/api/v1/admin/corporate/accounts/{id}/activate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsJsonAsync($"/api/v1/admin/corporate/accounts/{id}/close", new { reason = "again" })).StatusCode);

        var actions = await fixture.Factory.WithDbAsync(db => db.AuditLogs.AsNoTracking().Where(a => a.EntityType == "corporate_account" && a.EntityId == Guid.Parse(id!)).OrderBy(a => a.CreatedAt).Select(a => a.Action).ToListAsync());
        Assert.Contains("corporate_account.activate", actions);
        Assert.Contains("corporate_account.suspend", actions);
        Assert.Contains("corporate_account.close", actions);
        Assert.Contains("corporate_user.invite", actions);
    }

    [Fact]
    public async Task Admin_endpoints_need_corporate_manage_and_receivables_also_payments_view()
    {
        var noPermission = await CorporateFlow.LimitedAdminAsync(fixture, "corp-none", "trips.view");
        var forbidden = await noPermission.GetAsync("/api/v1/admin/corporate/accounts");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        var error = (await forbidden.ReadJsonAsync()).GetProperty("error");
        Assert.Equal("forbidden", error.GetProperty("code").GetString());
        Assert.Equal("corporate.manage", error.GetProperty("details").GetProperty("permission").GetString());
        Assert.Equal(HttpStatusCode.Forbidden, (await noPermission.PostAsJsonAsync("/api/v1/admin/corporate/accounts", CorporateFlow.AccountInput())).StatusCode);

        var manageOnly = await CorporateFlow.LimitedAdminAsync(fixture, "corp-manage", "corporate.manage");
        Assert.Equal(HttpStatusCode.OK, (await manageOnly.GetAsync("/api/v1/admin/corporate/accounts")).StatusCode);
        var receivables = await manageOnly.GetAsync("/api/v1/admin/corporate/receivables");
        Assert.Equal(HttpStatusCode.Forbidden, receivables.StatusCode);
        Assert.Equal("payments.view", (await receivables.ReadJsonAsync()).GetProperty("error").GetProperty("details").GetProperty("permission").GetString());

        var both = await CorporateFlow.LimitedAdminAsync(fixture, "corp-both", "corporate.manage", "payments.view");
        Assert.Equal(HttpStatusCode.OK, (await both.GetAsync("/api/v1/admin/corporate/receivables")).StatusCode);
        using var superAdmin = await fixture.LoginAdminAsync();
        Assert.Equal(HttpStatusCode.OK, (await superAdmin.GetAsync("/api/v1/admin/corporate/receivables")).StatusCode);

        // Riders, drivers and company admins cannot reach /admin/corporate.
        var (passenger, _) = await fixture.LoginAsync("passenger");
        Assert.Equal(HttpStatusCode.Forbidden, (await passenger.GetAsync("/api/v1/admin/corporate/accounts")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await fixture.CreateClient().GetAsync("/api/v1/admin/corporate/accounts")).StatusCode);
        var company = await CorporateFlow.OnboardAsync(fixture);
        Assert.Equal(HttpStatusCode.Forbidden, (await company.Portal.GetAsync("/api/v1/admin/corporate/accounts")).StatusCode);
    }

    [Fact]
    public async Task Portal_account_shows_legal_fields_read_only_and_updates_billing_details_with_an_audit()
    {
        var company = await CorporateFlow.OnboardAsync(fixture, "شركة الإعدادات", 7000m);
        var account = await (await company.Portal.GetAsync("/api/v1/corporate/account")).ReadJsonAsync();
        Assert.Equal(company.Number, account.GetProperty("accountNumber").GetString());
        Assert.Equal("active", account.GetProperty("status").GetString());
        Assert.Equal(7000m, account.GetProperty("creditLimit").GetDecimal());
        Assert.Equal("310000000000003", account.GetProperty("vatNumber").GetString());

        var updated = await company.Portal.PutAsJsonAsync("/api/v1/corporate/account", new
        {
            billingEmail = "accounts@new-mail.sa", contactName = "ليلى", contactPhone = "0551234567",
            billingAddress = new { buildingNumber = "4321", street = "Prince Sultan", district = "Al Malqa", city = "Riyadh", postalCode = "13521", additionalNumber = "8765", countryCode = "SA" },
            legalNameEn = "ignored", creditLimit = 999999,
        });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var body = await updated.ReadJsonAsync();
        Assert.Equal("accounts@new-mail.sa", body.GetProperty("billingEmail").GetString());
        Assert.Equal("+966551234567", body.GetProperty("contactPhone").GetString());
        Assert.Equal("Al Malqa", body.GetProperty("billingAddress").GetProperty("district").GetString());
        Assert.Equal(7000m, body.GetProperty("creditLimit").GetDecimal());
        Assert.Equal(company.Number, body.GetProperty("accountNumber").GetString());

        var invalid = await company.Portal.PutAsJsonAsync("/api/v1/corporate/account", new
        {
            billingEmail = "x", contactName = "", contactPhone = "1",
            billingAddress = new { buildingNumber = "12", street = "", district = "x", city = "x", postalCode = "1", additionalNumber = "1" },
        });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);

        var row = await fixture.Factory.WithDbAsync(db => db.AuditLogs.AsNoTracking().Where(a => a.EntityId == company.AccountId && a.Action == "corporate_account.update").SingleAsync());
        Assert.Equal("corporate_admin", row.ActorRole);
        Assert.Equal(company.AdminUserId, row.ActorUserId);
    }

    [Fact]
    public async Task Employees_csv_import_reports_the_skipped_rows_with_reasons()
    {
        var company = await CorporateFlow.OnboardAsync(fixture, "شركة الاستيراد");
        await CorporateFlow.CreateCostCenterAsync(company, "FIN-01", "المالية");
        var existing = fixture.NextPhone();
        Assert.Equal(HttpStatusCode.Created, (await company.Portal.PostAsJsonAsync("/api/v1/corporate/employees", new { phoneNumber = existing, fullName = "موجود", role = "employee" })).StatusCode);
        var csv = new StringBuilder("﻿phone_number,full_name,employee_number,department,cost_center_code,monthly_budget,role\n");
        var good1 = fixture.NextPhone();
        var good2 = fixture.NextPhone();
        csv.AppendLine($"{good1},\"Sara, Ahmed\",E-1,Finance,FIN-01,1500,employee");
        csv.AppendLine($"{good2},Khaled,E-2,Sales,,250.50,corporate_admin");
        csv.AppendLine("12345,Bad Phone,,,,,");
        csv.AppendLine($"{fixture.NextPhone()},,E-3,,,,");
        csv.AppendLine($"{good1},Duplicate,,,,,");
        csv.AppendLine($"{existing},Existing,,,,,");
        csv.AppendLine($"{fixture.NextPhone()},Role,,,,,manager");
        csv.AppendLine($"{fixture.NextPhone()},CostCenter,,,NOPE,,");
        csv.AppendLine($"{fixture.NextPhone()},Budget,,,,abc,");
        using var form = new MultipartFormDataContent { { new ByteArrayContent(Encoding.UTF8.GetBytes(csv.ToString())), "file", "employees.csv" } };
        var response = await company.Portal.PostAsync("/api/v1/corporate/employees/import", form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.ReadJsonAsync();
        Assert.Equal(2, result.GetProperty("created").GetInt32());
        var skipped = result.GetProperty("skipped").EnumerateArray().Select(s => $"{s.GetProperty("row").GetInt32()}:{s.GetProperty("reason").GetString()}").ToList();
        Assert.Equal(["4:phone_invalid", "5:full_name_required", "6:duplicate_in_file", "7:already_member", "8:role_invalid", "9:cost_center_unknown", "10:monthly_budget_invalid"], skipped);

        var list = await (await company.Portal.GetAsync("/api/v1/corporate/employees?search=Sara&status=invited")).ReadJsonAsync();
        var sara = list.GetProperty("items")[0];
        Assert.Equal("Sara, Ahmed", sara.GetProperty("fullName").GetString());
        Assert.Equal("FIN-01", sara.GetProperty("costCenter").GetString());
        Assert.Equal(1500m, sara.GetProperty("monthlyBudget").GetDecimal());
        Assert.NotEqual(System.Text.Json.JsonValueKind.Null, sara.GetProperty("invitationExpiresAt").ValueKind);
        var khaled = await (await company.Portal.GetAsync($"/api/v1/corporate/employees?search={CorporateFlow.Normalize(good2)}")).ReadJsonAsync();
        Assert.Equal("corporate_admin", khaled.GetProperty("items")[0].GetProperty("role").GetString());

        var noFile = await company.Portal.PostAsync("/api/v1/corporate/employees/import", new MultipartFormDataContent());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, noFile.StatusCode);
        using var noHeader = new MultipartFormDataContent { { new ByteArrayContent(Encoding.UTF8.GetBytes("name\nx\n")), "file", "bad.csv" } };
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await company.Portal.PostAsync("/api/v1/corporate/employees/import", noHeader)).StatusCode);
    }

    [Fact]
    public async Task Api_keys_are_off_by_default_and_the_demo_seed_is_disabled_in_tests()
    {
        var company = await CorporateFlow.OnboardAsync(fixture);
        Assert.Equal(HttpStatusCode.NotFound, (await company.Portal.GetAsync("/api/v1/corporate/api-keys")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await company.Portal.PostAsJsonAsync("/api/v1/corporate/api-keys", new { name = "erp" })).StatusCode);
        Assert.False(await fixture.Factory.WithDbAsync(db => db.CorporateAccounts.AnyAsync(a => a.Id == SeedIds.DemoCorporate.Account)));
    }
}

/// <summary>Company API keys (<c>Corporate:ApiKeysEnabled=true</c>): shown in full once, stored hashed, revocable.</summary>
public class CorporateApiKeyTests : IClassFixture<CorporateApiKeyTests.ApiKeyFixture>
{
    private readonly ApiKeyFixture _fixture;

    public CorporateApiKeyTests(ApiKeyFixture fixture) => _fixture = fixture;

    public sealed class ApiKeyFixture() : CorporateFixture(new Dictionary<string, string?> { ["Corporate:ApiKeysEnabled"] = "true" });

    [Fact]
    public async Task Key_is_returned_once_and_stored_as_a_hash_and_can_be_revoked()
    {
        var company = await CorporateFlow.OnboardAsync(_fixture);
        var created = await company.Portal.PostAsJsonAsync("/api/v1/corporate/api-keys", new { name = "ERP", scopes = new[] { "reports:read" } });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var key = await created.ReadJsonAsync();
        var full = key.GetProperty("key").GetString()!;
        Assert.Matches("^ata_live_[A-Za-z0-9]{40}$", full);
        Assert.StartsWith(key.GetProperty("keyPrefix").GetString()!, full["ata_live_".Length..]);
        Assert.Equal(["reports:read"], key.GetProperty("scopes").EnumerateArray().Select(s => s.GetString()).ToList());

        var listed = await (await company.Portal.GetAsync("/api/v1/corporate/api-keys")).ReadJsonAsync();
        Assert.Equal(1, listed.GetArrayLength());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, listed[0].GetProperty("key").ValueKind);
        var stored = await _fixture.Factory.WithDbAsync(db => db.CorporateApiKeys.AsNoTracking().SingleAsync(k => k.CorporateAccountId == company.AccountId));
        Assert.DoesNotContain(full, stored.KeyHash);
        Assert.Equal(64, stored.KeyHash.Length);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await company.Portal.PostAsJsonAsync("/api/v1/corporate/api-keys", new { name = "x", scopes = new[] { "admin:all" } })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await company.Portal.DeleteAsync($"/api/v1/corporate/api-keys/{key.GetProperty("id").GetString()}")).StatusCode);
        var after = await (await company.Portal.GetAsync("/api/v1/corporate/api-keys")).ReadJsonAsync();
        Assert.NotEqual(System.Text.Json.JsonValueKind.Null, after[0].GetProperty("revokedAt").ValueKind);

        // Another company cannot revoke it.
        var other = await CorporateFlow.OnboardAsync(_fixture);
        Assert.Equal(HttpStatusCode.NotFound, (await other.Portal.DeleteAsync($"/api/v1/corporate/api-keys/{key.GetProperty("id").GetString()}")).StatusCode);
    }
}
