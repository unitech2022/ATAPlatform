using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Api.Common;
using ATA.Domain.Corporate;
using ATA.Infrastructure.Persistence.Seed;
using ATA.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Integration;

/// <summary>F19 policies and cost centres, the evaluation rules, the monthly budget and the credit limit.</summary>
public class CorporatePolicyTests(CorporateFixture fixture) : IClassFixture<CorporateFixture>
{
    private static object Square((decimal Lat, decimal Lng) c, decimal half = 0.03m) => new[]
    {
        new[] { c.Lat - half, c.Lng - half }, new[] { c.Lat - half, c.Lng + half }, new[] { c.Lat + half, c.Lng + half }, new[] { c.Lat + half, c.Lng - half }, new[] { c.Lat - half, c.Lng - half },
    };

    private static object Policy(string name = "سياسة", Action<Dictionary<string, object?>>? configure = null)
    {
        var body = new Dictionary<string, object?> { ["name"] = name };
        configure?.Invoke(body);
        return body;
    }

    private static async Task<HttpResponseMessage> QuoteAsync(HttpClient employee, (decimal Lat, decimal Lng) area, string? purpose = "اجتماع", Guid? costCenter = null, string bookingType = "now", DateTime? scheduledAt = null, Guid? category = null) =>
        await employee.PostAsJsonAsync("/api/v1/pricing/quote", new
        {
            pickup = new { name = "المنزل", address = "شارع", lat = area.Lat, lng = area.Lng },
            dropoff = new { name = "العمل", address = "طريق", lat = area.Lat + 0.05m, lng = area.Lng + 0.05m },
            stops = Array.Empty<object>(), rideCategoryId = category ?? SeedIds.RideCategories.Economy, bookingType, scheduledAt, paymentMethod = "corporate", tripPurpose = purpose, costCenterId = costCenter,
        });

    private static async Task<string> QuoteRulesAsync(HttpResponseMessage response, bool expectAllowed = false)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var corporate = (await response.ReadJsonAsync()).GetProperty("corporate");
        Assert.Equal(expectAllowed, corporate.GetProperty("allowed").GetBoolean());
        return string.Join(',', corporate.GetProperty("violations").EnumerateArray().Select(v => v.GetProperty("rule").GetString()).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task Policy_crud_default_handling_and_validation()
    {
        var company = await CorporateFlow.OnboardAsync(fixture);
        var first = await CorporateFlow.CreatePolicyAsync(company, Policy("الأولى", b =>
        {
            b["allowedRideCategoryIds"] = new[] { SeedIds.RideCategories.Economy };
            b["allowedDays"] = new[] { 0, 1, 2, 3, 4 };
            b["timeWindows"] = new[] { new { from = "07:00", to = "22:00" } };
            b["maxFarePerTrip"] = 150m;
            b["monthlyBudgetPerEmployee"] = 1500m;
            b["requirePurpose"] = true;
        }));
        Assert.True(first.GetProperty("isDefault").GetBoolean(), "the first policy of a company is its default");
        Assert.Equal("pickup_and_dropoff", first.GetProperty("zoneMatch").GetString());
        Assert.True(first.GetProperty("allowScheduled").GetBoolean());
        Assert.True(first.GetProperty("allowGuestBooking").GetBoolean());
        Assert.Equal("22:00", first.GetProperty("timeWindows")[0].GetProperty("to").GetString());
        var second = await CorporateFlow.CreatePolicyAsync(company, Policy("الثانية"));
        Assert.False(second.GetProperty("isDefault").GetBoolean());

        var account = await (await company.Portal.GetAsync("/api/v1/corporate/account")).ReadJsonAsync();
        Assert.Equal(first.GetProperty("id").GetString(), account.GetProperty("defaultPolicyId").GetString());
        var setDefault = await company.Portal.PostAsync($"/api/v1/corporate/policies/{second.GetProperty("id").GetString()}/default", null);
        Assert.Equal(HttpStatusCode.OK, setDefault.StatusCode);
        var listed = await (await company.Portal.GetAsync("/api/v1/corporate/policies")).ReadJsonAsync();
        Assert.Equal(second.GetProperty("id").GetString(), listed[0].GetProperty("id").GetString());
        Assert.Equal(1, listed.EnumerateArray().Count(p => p.GetProperty("isDefault").GetBoolean()));

        Assert.Equal(HttpStatusCode.Conflict, (await company.Portal.DeleteAsync($"/api/v1/corporate/policies/{second.GetProperty("id").GetString()}")).StatusCode);
        var employee = await CorporateFlow.AddEmployeeAsync(fixture, company, "موظف", b => b["policyId"] = first.GetProperty("id").GetString());
        Assert.Equal("الأولى", (await (await company.Portal.GetAsync($"/api/v1/corporate/employees/{employee.MemberId}")).ReadJsonAsync()).GetProperty("policyName").GetString());
        Assert.Equal(HttpStatusCode.NoContent, (await company.Portal.DeleteAsync($"/api/v1/corporate/policies/{first.GetProperty("id").GetString()}")).StatusCode);
        Assert.Equal("الثانية", (await (await company.Portal.GetAsync($"/api/v1/corporate/employees/{employee.MemberId}")).ReadJsonAsync()).GetProperty("policyName").GetString());

        var updated = await company.Portal.PutAsJsonAsync($"/api/v1/corporate/policies/{second.GetProperty("id").GetString()}", Policy("الثانية معدلة", b => { b["maxFarePerTrip"] = 80m; b["allowGuestBooking"] = false; }));
        Assert.Equal(80m, (await updated.ReadJsonAsync()).GetProperty("maxFarePerTrip").GetDecimal());
        Assert.Equal(HttpStatusCode.Conflict, (await company.Portal.PutAsJsonAsync($"/api/v1/corporate/policies/{second.GetProperty("id").GetString()}", Policy("x", b => b["isActive"] = false))).StatusCode);

        foreach (var invalid in new object[]
        {
            Policy("", null), Policy("x", b => b["allowedDays"] = new[] { 7 }), Policy("x", b => b["timeWindows"] = new[] { new { from = "7am", to = "22:00" } }),
            Policy("x", b => b["allowedRideCategoryIds"] = new[] { Guid.NewGuid() }), Policy("x", b => b["allowedZoneIds"] = new[] { Guid.NewGuid() }), Policy("x", b => b["maxFarePerTrip"] = 0m),
        })
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, (await company.Portal.PostAsJsonAsync("/api/v1/corporate/policies", invalid)).StatusCode);
        }

        var audit = await fixture.Factory.WithDbAsync(db => db.AuditLogs.AsNoTracking().Where(a => a.EntityId == company.AccountId && a.Action.StartsWith("corporate_policy.")).Select(a => a.Action).Distinct().ToListAsync());
        Assert.Equivalent(new[] { "corporate_policy.create", "corporate_policy.default", "corporate_policy.delete", "corporate_policy.update" }, audit);
    }

    [Fact]
    public async Task Cost_centers_are_unique_per_company_and_used_ones_are_deactivated_not_deleted()
    {
        var company = await CorporateFlow.OnboardAsync(fixture);
        var it = await CorporateFlow.CreateCostCenterAsync(company, "IT-01", "تقنية المعلومات");
        Assert.Equal(HttpStatusCode.Conflict, (await company.Portal.PostAsJsonAsync("/api/v1/corporate/cost-centers", new { code = "it-01", name = "مكرر" })).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await company.Portal.PostAsJsonAsync("/api/v1/corporate/cost-centers", new { code = "", name = "x" })).StatusCode);
        var other = await CorporateFlow.OnboardAsync(fixture);
        Assert.Equal(HttpStatusCode.Created, (await other.Portal.PostAsJsonAsync("/api/v1/corporate/cost-centers", new { code = "IT-01", name = "شركة أخرى" })).StatusCode);

        var renamed = await company.Portal.PutAsJsonAsync($"/api/v1/corporate/cost-centers/{it.GetProperty("id").GetString()}", new { code = "IT-01", name = "تقنية", isActive = true });
        Assert.Equal("تقنية", (await renamed.ReadJsonAsync()).GetProperty("name").GetString());
        var unused = await CorporateFlow.CreateCostCenterAsync(company, "TMP", "مؤقت");
        Assert.Equal(HttpStatusCode.NoContent, (await company.Portal.DeleteAsync($"/api/v1/corporate/cost-centers/{unused.GetProperty("id").GetString()}")).StatusCode);
        Assert.Equal(1, (await (await company.Portal.GetAsync("/api/v1/corporate/cost-centers")).ReadJsonAsync()).GetArrayLength());
    }

    [Fact]
    public async Task Every_policy_rule_produces_its_own_violation_in_the_quote_and_the_trip_request()
    {
        var area = TripFlow.Area(20);
        using var admin = await fixture.LoginAdminAsync();
        var far = TripFlow.Area(24);
        var zone = await (await admin.PostAsJsonAsync("/api/v1/admin/zones", new { code = "corp_zone_far", nameAr = "بعيدة", nameEn = "Far", polygon = Square(far), priority = 1 })).ReadJsonAsync();
        var pickupZone = await (await admin.PostAsJsonAsync("/api/v1/admin/zones", new { code = "corp_zone_pickup", nameAr = "الالتقاط", nameEn = "Pickup", polygon = Square((area.Lat, area.Lng), 0.02m), priority = 3 })).ReadJsonAsync();
        var dropoffZone = await (await admin.PostAsJsonAsync("/api/v1/admin/zones", new { code = "corp_zone_dropoff", nameAr = "الوجهة", nameEn = "Dropoff", polygon = Square((area.Lat + 0.05m, area.Lng + 0.05m), 0.01m), priority = 3 })).ReadJsonAsync();
        var company = await CorporateFlow.OnboardAsync(fixture, creditLimit: 100000m);
        var employee = await CorporateFlow.AddEmployeeAsync(fixture, company);
        var center = await CorporateFlow.CreateCostCenterAsync(company, "CC-1", "مركز");
        var inactive = await CorporateFlow.CreateCostCenterAsync(company, "CC-2", "موقوف");
        await company.Portal.PutAsJsonAsync($"/api/v1/corporate/cost-centers/{inactive.GetProperty("id").GetString()}", new { code = "CC-2", name = "موقوف", isActive = false });
        var policy = await CorporateFlow.CreatePolicyAsync(company, Policy("الكل"));
        var policyId = policy.GetProperty("id").GetString();
        var clock = fixture.Factory.Clock;
        var local = ATA.Api.Common.Formats.ToRiyadh(clock.UtcNow);

        async Task SetPolicy(Action<Dictionary<string, object?>> configure) =>
            Assert.Equal(HttpStatusCode.OK, (await company.Portal.PutAsJsonAsync($"/api/v1/corporate/policies/{policyId}", Policy("الكل", configure))).StatusCode);

        async Task Expect(string rules, HttpResponseMessage? quote = null, string? purpose = "اجتماع", Guid? costCenter = null, string bookingType = "now", DateTime? scheduledAt = null, Guid? category = null)
        {
            Assert.Equal(rules, await QuoteRulesAsync(quote ?? await QuoteAsync(employee.Client, area, purpose, costCenter, bookingType, scheduledAt, category)));
            var create = await employee.Client.PostAsJsonAsync("/api/v1/passenger/trips", CorporateFlow.TripRequest(area, purpose, costCenter, category, bookingType, scheduledAt));
            Assert.Equal(rules, await CorporateFlow.ViolationRuleAsync(create));
        }

        // no rules → allowed
        Assert.Equal(string.Empty, await QuoteRulesAsync(await QuoteAsync(employee.Client, area), expectAllowed: true));

        await SetPolicy(b => b["allowedRideCategoryIds"] = new[] { SeedIds.RideCategories.Comfort });
        await Expect("category");
        var categoryError = (await (await employee.Client.PostAsJsonAsync("/api/v1/passenger/trips", CorporateFlow.TripRequest(area))).ReadJsonAsync()).GetProperty("error").GetProperty("details").GetProperty("violations")[0];
        Assert.Equal(["comfort"], categoryError.GetProperty("allowed").EnumerateArray().Select(x => x.GetString()).ToList());
        Assert.Equal(string.Empty, await QuoteRulesAsync(await QuoteAsync(employee.Client, area, category: SeedIds.RideCategories.Comfort), expectAllowed: true));

        var otherDays = Enumerable.Range(0, 7).Where(d => d != (int)local.DayOfWeek).ToArray();
        await SetPolicy(b => b["allowedDays"] = otherDays);
        await Expect("day");
        await SetPolicy(b => b["allowedDays"] = new[] { (int)local.DayOfWeek });
        Assert.Equal(string.Empty, await QuoteRulesAsync(await QuoteAsync(employee.Client, area), expectAllowed: true));

        await SetPolicy(b => b["timeWindows"] = new[] { new { from = "01:00", to = "02:00" } });
        await Expect("time_window");
        // A window that wraps past midnight and the exact boundaries.
        await SetPolicy(b => b["timeWindows"] = new[] { new { from = local.AddHours(-1).ToString("HH:mm"), to = local.AddHours(1).ToString("HH:mm") } });
        Assert.Equal(string.Empty, await QuoteRulesAsync(await QuoteAsync(employee.Client, area), expectAllowed: true));

        await SetPolicy(b => b["allowedZoneIds"] = new[] { Guid.Parse(zone.GetProperty("id").GetString()!) });
        await Expect("zone");
        var pickupId = Guid.Parse(pickupZone.GetProperty("id").GetString()!);
        var dropoffId = Guid.Parse(dropoffZone.GetProperty("id").GetString()!);
        await SetPolicy(b => b["allowedZoneIds"] = new[] { pickupId });
        await Expect("zone"); // pickup_and_dropoff: the dropoff is in the city default zone
        await SetPolicy(b => { b["allowedZoneIds"] = new[] { pickupId }; b["zoneMatch"] = "pickup_or_dropoff"; });
        Assert.Equal(string.Empty, await QuoteRulesAsync(await QuoteAsync(employee.Client, area), expectAllowed: true));
        await SetPolicy(b => { b["allowedZoneIds"] = new[] { pickupId, dropoffId }; });
        Assert.Equal(string.Empty, await QuoteRulesAsync(await QuoteAsync(employee.Client, area), expectAllowed: true));
        await SetPolicy(_ => { });

        await SetPolicy(b => b["maxFarePerTrip"] = 5m);
        await Expect("max_fare");
        var maxFare = (await (await employee.Client.PostAsJsonAsync("/api/v1/passenger/trips", CorporateFlow.TripRequest(area))).ReadJsonAsync()).GetProperty("error").GetProperty("details").GetProperty("violations")[0];
        Assert.Equal(5m, maxFare.GetProperty("limit").GetDecimal());
        await SetPolicy(_ => { });

        var scheduledAt = clock.UtcNow.AddHours(3);
        await SetPolicy(b => b["allowScheduled"] = false);
        await Expect("scheduled", bookingType: "scheduled", scheduledAt: scheduledAt);
        Assert.Equal(string.Empty, await QuoteRulesAsync(await QuoteAsync(employee.Client, area), expectAllowed: true));
        await SetPolicy(_ => { });

        await SetPolicy(b => b["requirePurpose"] = true);
        await Expect("purpose_required", purpose: null);
        await Expect("purpose_required", purpose: "   ");
        await SetPolicy(b => b["requireCostCenter"] = true);
        await Expect("cost_center_required", purpose: "x");
        await Expect("cost_center_required", costCenter: Guid.Parse(inactive.GetProperty("id").GetString()!));
        Assert.Equal(string.Empty, await QuoteRulesAsync(await QuoteAsync(employee.Client, area, "اجتماع", Guid.Parse(center.GetProperty("id").GetString()!)), expectAllowed: true));

        // Several at once, in the order of the rule list.
        await SetPolicy(b => { b["allowedRideCategoryIds"] = new[] { SeedIds.RideCategories.Comfort }; b["maxFarePerTrip"] = 5m; b["requirePurpose"] = true; });
        await Expect("category,max_fare,purpose_required", purpose: null);

        // A cost centre of another company is a validation error, not a policy violation.
        var foreign = await CorporateFlow.OnboardAsync(fixture);
        var foreignCenter = await CorporateFlow.CreateCostCenterAsync(foreign, "F-1", "أجنبي");
        var bad = await employee.Client.PostAsJsonAsync("/api/v1/passenger/trips", CorporateFlow.TripRequest(area, "x", Guid.Parse(foreignCenter.GetProperty("id").GetString()!)));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, bad.StatusCode);
        Assert.Equal("validation_failed", await bad.ErrorCodeAsync());
    }

    [Fact]
    public async Task Monthly_budget_counts_in_flight_trips_and_blocks_the_booking_with_the_remaining_amount()
    {
        var area = TripFlow.Area(21);
        var company = await CorporateFlow.OnboardAsync(fixture, creditLimit: 100000m);
        var employee = await CorporateFlow.AddEmployeeAsync(fixture, company, "موظف الميزانية", b => b["monthlyBudget"] = 60m);
        var at = fixture.Factory.Clock.UtcNow.AddHours(4);
        var quote = await (await QuoteAsync(employee.Client, area)).ReadJsonAsync();
        var fare = quote.GetProperty("categories").EnumerateArray().Single(c => c.GetProperty("code").GetString() == "economy").GetProperty("total").GetDecimal();
        Assert.InRange(fare, 12m, 40m);
        Assert.Equal(60m, quote.GetProperty("corporate").GetProperty("remainingBudget").GetDecimal());

        // Scheduled trips are not "active" trips, so several can be open; each one's estimate counts against the month.
        var booked = 0m;
        HttpResponseMessage? refused = null;
        for (var i = 0; i < 4 && refused is null; i++)
        {
            var response = await employee.Client.PostAsJsonAsync("/api/v1/passenger/trips", CorporateFlow.TripRequest(area, bookingType: "scheduled", scheduledAt: at.AddMinutes(i * 10)));
            if (response.StatusCode == HttpStatusCode.Created)
            {
                booked += fare;
            }
            else
            {
                refused = response;
            }
        }

        Assert.NotNull(refused);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, refused.StatusCode);
        var error = (await refused.ReadJsonAsync()).GetProperty("error");
        Assert.Equal("corporate_budget_exceeded", error.GetProperty("code").GetString());
        Assert.Equal(60m - booked, error.GetProperty("details").GetProperty("remaining").GetDecimal());
        Assert.True(booked + fare > 60m && booked <= 60m);

        var membership = await (await employee.Client.GetAsync("/api/v1/passenger/corporate")).ReadJsonAsync();
        Assert.Equal(booked, membership.GetProperty("budget").GetProperty("spent").GetDecimal());
        Assert.Equal(60m - booked, membership.GetProperty("budget").GetProperty("remaining").GetDecimal());
        var roster = await (await company.Portal.GetAsync($"/api/v1/corporate/employees/{employee.MemberId}")).ReadJsonAsync();
        Assert.Equal(booked, roster.GetProperty("spentThisMonth").GetDecimal());

        // The quote says so too, with the budget pseudo-rule; raising the personal budget unblocks.
        var blockedQuote = (await (await QuoteAsync(employee.Client, area)).ReadJsonAsync()).GetProperty("corporate");
        Assert.False(blockedQuote.GetProperty("allowed").GetBoolean());
        Assert.Contains(blockedQuote.GetProperty("violations").EnumerateArray(), v => v.GetProperty("rule").GetString() == "budget_exceeded");
        Assert.Equal(HttpStatusCode.OK, (await company.Portal.PutAsJsonAsync($"/api/v1/corporate/employees/{employee.MemberId}", new { fullName = "موظف الميزانية", role = "employee", monthlyBudget = 500m })).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await employee.Client.PostAsJsonAsync("/api/v1/passenger/trips", CorporateFlow.TripRequest(area, bookingType: "scheduled", scheduledAt: at.AddHours(1)))).StatusCode);

        // The personal budget overrides the policy's (a looser policy budget does not help a tighter personal one).
        var policy = await CorporateFlow.CreatePolicyAsync(company, Policy("ميزانية السياسة", b => b["monthlyBudgetPerEmployee"] = 10m), makeDefault: true);
        var second = await CorporateFlow.AddEmployeeAsync(fixture, company, "موظف السياسة");
        var refusedByPolicy = await second.Client.PostAsJsonAsync("/api/v1/passenger/trips", CorporateFlow.TripRequest(area));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, refusedByPolicy.StatusCode);
        Assert.Equal("corporate_budget_exceeded", await refusedByPolicy.ErrorCodeAsync());
        Assert.Equal(System.Text.Json.JsonValueKind.Object, policy.ValueKind);
    }

    [Fact]
    public async Task Credit_limit_covers_unbilled_and_in_flight_amounts_of_the_whole_company()
    {
        var area = TripFlow.Area(22);
        var company = await CorporateFlow.OnboardAsync(fixture, creditLimit: 70m);
        var first = await CorporateFlow.AddEmployeeAsync(fixture, company, "أول");
        var second = await CorporateFlow.AddEmployeeAsync(fixture, company, "ثان");
        var third = await CorporateFlow.AddEmployeeAsync(fixture, company, "ثالث");
        var at = fixture.Factory.Clock.UtcNow.AddHours(5);
        Assert.Equal(HttpStatusCode.Created, (await first.Client.PostAsJsonAsync("/api/v1/passenger/trips", CorporateFlow.TripRequest(area, bookingType: "scheduled", scheduledAt: at))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await second.Client.PostAsJsonAsync("/api/v1/passenger/trips", CorporateFlow.TripRequest(area, bookingType: "scheduled", scheduledAt: at.AddMinutes(5)))).StatusCode);
        var refused = await third.Client.PostAsJsonAsync("/api/v1/passenger/trips", CorporateFlow.TripRequest(area, bookingType: "scheduled", scheduledAt: at.AddMinutes(10)));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, refused.StatusCode);
        var error = (await refused.ReadJsonAsync()).GetProperty("error");
        Assert.Equal("corporate_credit_limit_exceeded", error.GetProperty("code").GetString());
        Assert.Equal(70m, error.GetProperty("details").GetProperty("creditLimit").GetDecimal());
        Assert.True(error.GetProperty("details").GetProperty("used").GetDecimal() > 20m);
        var quote = (await (await QuoteAsync(third.Client, area)).ReadJsonAsync()).GetProperty("corporate");
        Assert.Contains(quote.GetProperty("violations").EnumerateArray(), v => v.GetProperty("rule").GetString() == "credit_limit_exceeded");

        // The dashboard shows the used credit; raising the limit lets the booking through.
        var dashboard = await (await company.Portal.GetAsync("/api/v1/corporate/dashboard")).ReadJsonAsync();
        Assert.Equal(70m, dashboard.GetProperty("creditLimit").GetDecimal());
        Assert.True(dashboard.GetProperty("creditUsed").GetDecimal() > 20m);
        using var admin = await fixture.LoginAdminAsync();
        var account = await (await admin.GetAsync($"/api/v1/admin/corporate/accounts/{company.AccountId}")).ReadJsonAsync();
        Assert.Equal(dashboard.GetProperty("creditUsed").GetDecimal(), account.GetProperty("summary").GetProperty("creditUsed").GetDecimal());
        Assert.Equal(HttpStatusCode.OK, (await admin.PutAsJsonAsync($"/api/v1/admin/corporate/accounts/{company.AccountId}", CorporateFlow.AccountInput("x", 500m, account.GetProperty("crNumber").GetString()))).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await third.Client.PostAsJsonAsync("/api/v1/passenger/trips", CorporateFlow.TripRequest(area, bookingType: "scheduled", scheduledAt: at.AddMinutes(10)))).StatusCode);

        var receivables = await (await admin.GetAsync("/api/v1/admin/corporate/receivables")).ReadJsonAsync();
        var row = receivables.EnumerateArray().Single(r => r.GetProperty("accountId").GetString() == company.AccountId.ToString());
        Assert.Equal(500m, row.GetProperty("creditLimit").GetDecimal());
        Assert.Equal(0m, row.GetProperty("unbilled").GetDecimal()); // nothing completed yet: the open trips are in flight, not billable
    }

    [Fact]
    public async Task Quote_and_membership_views_for_the_rider_app()
    {
        var area = TripFlow.Area(23);
        var company = await CorporateFlow.OnboardAsync(fixture, creditLimit: 100000m);
        var center = await CorporateFlow.CreateCostCenterAsync(company, "IT-01", "IT");
        await CorporateFlow.CreatePolicyAsync(company, Policy("العامة", b =>
        {
            b["allowedRideCategoryIds"] = new[] { SeedIds.RideCategories.Economy, SeedIds.RideCategories.Comfort };
            b["allowedDays"] = new[] { 0, 1, 2, 3, 4 };
            b["timeWindows"] = new[] { new { from = "06:00", to = "23:00" } };
            b["maxFarePerTrip"] = 150m;
            b["requirePurpose"] = true;
            b["monthlyBudgetPerEmployee"] = 1500m;
        }));
        var employee = await CorporateFlow.AddEmployeeAsync(fixture, company, "موظف", b => { b["employeeNumber"] = "E-5"; b["department"] = "IT"; });
        var view = await (await employee.Client.GetAsync("/api/v1/passenger/corporate")).ReadJsonAsync();
        var policy = view.GetProperty("policy");
        Assert.Equal("العامة", policy.GetProperty("name").GetString());
        Assert.Equal(["comfort", "economy"], policy.GetProperty("allowedRideCategoryCodes").EnumerateArray().Select(x => x.GetString()).Order(StringComparer.Ordinal).ToList());
        Assert.Equal([0, 1, 2, 3, 4], policy.GetProperty("allowedDays").EnumerateArray().Select(x => x.GetInt32()).ToList());
        Assert.Equal("06:00", policy.GetProperty("timeWindows")[0].GetProperty("from").GetString());
        Assert.Equal(150m, policy.GetProperty("maxFarePerTrip").GetDecimal());
        Assert.True(policy.GetProperty("requirePurpose").GetBoolean());
        Assert.False(policy.GetProperty("requireCostCenter").GetBoolean());
        Assert.True(policy.GetProperty("allowScheduled").GetBoolean());
        Assert.Equal("IT", view.GetProperty("membership").GetProperty("department").GetString());
        Assert.Equal("employee", view.GetProperty("membership").GetProperty("role").GetString());
        Assert.Equal(center.GetProperty("id").GetString(), view.GetProperty("costCenters")[0].GetProperty("id").GetString());
        Assert.Equal(1500m, view.GetProperty("budget").GetProperty("monthly").GetDecimal());

        // Without a purpose the quote is refused by the policy with the rule the app turns into a message.
        var quote = await (await QuoteAsync(employee.Client, area, purpose: null)).ReadJsonAsync();
        var corporate = quote.GetProperty("corporate");
        Assert.False(corporate.GetProperty("allowed").GetBoolean());
        Assert.Equal("purpose_required", corporate.GetProperty("violations")[0].GetProperty("rule").GetString());
        Assert.Equal(1500m, corporate.GetProperty("remainingBudget").GetDecimal());
        // A non-corporate quote has no corporate block; a rider who is not a member gets 403 for a corporate quote.
        var plain = await (await employee.Client.PostAsJsonAsync("/api/v1/pricing/quote", new
        {
            pickup = new { name = "a", address = "b", lat = area.Lat, lng = area.Lng }, dropoff = new { name = "c", address = "d", lat = area.Lat + 0.05m, lng = area.Lng + 0.05m }, stops = Array.Empty<object>(), bookingType = "now",
        })).ReadJsonAsync();
        Assert.Equal(JsonValueKind.Null, plain.GetProperty("corporate").ValueKind);
        var (stranger, _) = await fixture.LoginAsync("passenger");
        var denied = await QuoteAsync(stranger, area);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal("corporate_not_member", await denied.ErrorCodeAsync());
        var notMember = await stranger.PostAsJsonAsync("/api/v1/passenger/trips", CorporateFlow.TripRequest(area));
        Assert.Equal("corporate_not_member", await notMember.ErrorCodeAsync());

        // `corporate` can never be the default payment method.
        var preference = await employee.Client.PatchAsJsonAsync("/api/v1/passenger/preferences", new { defaultPaymentMethod = "corporate" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, preference.StatusCode);

        // Inactive companies refuse bookings and quotes.
        using var admin = await fixture.LoginAdminAsync();
        Assert.Equal(HttpStatusCode.OK, (await admin.PostAsJsonAsync($"/api/v1/admin/corporate/accounts/{company.AccountId}/suspend", new { reason = "test" })).StatusCode);
        var suspended = await employee.Client.PostAsJsonAsync("/api/v1/passenger/trips", CorporateFlow.TripRequest(area));
        Assert.Equal(HttpStatusCode.Forbidden, suspended.StatusCode);
        Assert.Equal("corporate_account_inactive", await suspended.ErrorCodeAsync());
    }
}
