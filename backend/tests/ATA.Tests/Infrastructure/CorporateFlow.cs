using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Domain.Common;
using ATA.Domain.Identity;
using ATA.Infrastructure.Email;
using ATA.Infrastructure.Persistence;
using ATA.Infrastructure.Persistence.Seed;
using ATA.Infrastructure.Sms;
using ATA.Infrastructure.Security;
using ATA.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ATA.Tests.Infrastructure;

/// <summary>E-mail sender that records every message (corporate invoices).</summary>
public sealed class RecordingEmailSender : IEmailSender
{
    public ConcurrentBag<EmailMessage> Sent { get; } = [];

    public string Provider => "recording";

    public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        Sent.Add(message);
        return Task.FromResult(EmailSendResult.Sent($"mail-{Guid.NewGuid():N}"));
    }
}

/// <summary>API host with recording SMS / e-mail senders for the F19 tests (invoices stay drafts until an admin issues them, the production default).</summary>
public class CorporateFixture : ApiFixture
{
    public CorporateFixture() : this(null)
    {
    }

    protected CorporateFixture(Dictionary<string, string?>? extra) : base(Merge(extra), RecordingServices)
    {
    }

    private static Dictionary<string, string?> Merge(Dictionary<string, string?>? extra)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Safety:PublicShareRatePerMinute"] = "1000",
            ["Corporate:MaxActiveGuestTripsPerAdmin"] = "3",
        };
        foreach (var (key, value) in extra ?? [])
        {
            settings[key] = value;
        }

        return settings;
    }

    private static void RecordingServices(IServiceCollection services)
    {
        services.RemoveAll<ISmsSender>();
        services.AddSingleton<RecordingSmsSender>();
        services.AddSingleton<ISmsSender>(sp => sp.GetRequiredService<RecordingSmsSender>());
        services.RemoveAll<IEmailSender>();
        services.AddSingleton<RecordingEmailSender>();
        services.AddSingleton<IEmailSender>(sp => sp.GetRequiredService<RecordingEmailSender>());
    }

    public RecordingSmsSender Sms => Factory.Services.GetRequiredService<RecordingSmsSender>();

    public RecordingEmailSender Email => Factory.Services.GetRequiredService<RecordingEmailSender>();
}

/// <summary>Same host with <c>Corporate:AutoIssueInvoices=true</c> and the suspension of late companies after 10 days.</summary>
public sealed class CorporateAutoIssueFixture() : CorporateFixture(new Dictionary<string, string?>
{
    ["Corporate:AutoIssueInvoices"] = "true",
    ["Corporate:SuspendAfterOverdueDays"] = "10",
});

/// <summary>Shared steps of the F19 tests: companies, portal sessions, employees and corporate trip requests.</summary>
public static class CorporateFlow
{
    private static int _crCounter = 100;

    public sealed record Company(ApiFixture Fixture, Guid AccountId, string Number, HttpClient Portal, JsonElement PortalAuth, string AdminPhone, Guid AdminUserId)
    {
        public HttpClient Admin { get; init; } = null!;
    }

    public sealed record Employee(HttpClient Client, JsonElement Auth, string Phone, Guid UserId, Guid MemberId)
    {
        public string Token => Auth.GetProperty("accessToken").GetString()!;
    }

    public static string Normalize(string phone) => PhoneNumber.Normalize(phone);

    public static object AccountInput(string? name = null, decimal creditLimit = 5000m, string? crNumber = null, string? vatNumber = null) => new
    {
        legalNameAr = $"شركة {name ?? "الاختبار"} المحدودة",
        legalNameEn = $"{name ?? "Test"} Company Ltd",
        displayName = name ?? "شركة الاختبار",
        crNumber = crNumber ?? $"10{Interlocked.Increment(ref _crCounter):D8}",
        vatNumber = vatNumber ?? "310000000000003",
        billingEmail = "billing@test-company.sa",
        billingAddress = new { buildingNumber = "1234", street = "King Fahd Road", district = "Al Olaya", city = "Riyadh", postalCode = "12211", additionalNumber = "5678", countryCode = "SA" },
        cityId = SeedIds.CityRiyadh,
        contactName = "Sara Al-Harbi",
        contactPhone = "+966500000100",
        creditLimit,
        billingCycle = "monthly",
        paymentTermsDays = 30,
        notes = "test company",
    };

    public static async Task<JsonElement> CreateAccountAsync(HttpClient platformAdmin, string? name = null, decimal creditLimit = 5000m)
    {
        var response = await platformAdmin.PostAsJsonAsync("/api/v1/admin/corporate/accounts", AccountInput(name, creditLimit));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.ReadJsonAsync();
    }

    /// <summary>
    /// A company admin session: creates the account (<c>pending</c>), invites the first admin, signs in to the portal by OTP (which accepts the invitation) and — unless
    /// <paramref name="activate"/> is <c>false</c> — activates the account.
    /// </summary>
    public static async Task<Company> OnboardAsync(ApiFixture fixture, string? name = null, decimal creditLimit = 5000m, bool activate = true)
    {
        var platformAdmin = await fixture.LoginAdminAsync();
        var account = await CreateAccountAsync(platformAdmin, name, creditLimit);
        var accountId = Guid.Parse(account.GetProperty("id").GetString()!);
        var phone = fixture.NextPhone();
        var invited = await platformAdmin.PostAsJsonAsync($"/api/v1/admin/corporate/accounts/{accountId}/admins", new { phoneNumber = phone, fullName = "مسؤول الشركة" });
        Assert.Equal(HttpStatusCode.Created, invited.StatusCode);
        var (portal, auth) = await fixture.LoginAsync("corporate_admin", phone);
        if (activate)
        {
            (await platformAdmin.PostAsync($"/api/v1/admin/corporate/accounts/{accountId}/activate", null)).EnsureSuccessStatusCode();
        }

        return new Company(fixture, accountId, account.GetProperty("accountNumber").GetString()!, portal, auth, Normalize(phone), PaymentFlow.UserId(auth)) { Admin = platformAdmin };
    }

    /// <summary>Invites an employee from the portal; the employee (an existing rider app user) accepts in the app unless <paramref name="accept"/> is <c>false</c>.</summary>
    public static async Task<Employee> AddEmployeeAsync(ApiFixture fixture, Company company, string name = "موظف", Action<Dictionary<string, object?>>? configure = null, bool accept = true)
    {
        var phone = fixture.NextPhone();
        var (client, auth) = await fixture.LoginAsync("passenger", phone);
        (await client.PatchAsJsonAsync("/api/v1/me", new { fullName = name })).EnsureSuccessStatusCode();
        var body = new Dictionary<string, object?> { ["phoneNumber"] = phone, ["fullName"] = name, ["role"] = "employee" };
        configure?.Invoke(body);
        var invited = await company.Portal.PostAsJsonAsync("/api/v1/corporate/employees", body);
        Assert.Equal(HttpStatusCode.Created, invited.StatusCode);
        var memberId = Guid.Parse((await invited.ReadJsonAsync()).GetProperty("id").GetString()!);
        if (accept)
        {
            await AcceptAsync(client);
        }

        return new Employee(client, auth, Normalize(phone), PaymentFlow.UserId(auth), memberId);
    }

    public static async Task AcceptAsync(HttpClient employee)
    {
        var invitations = await (await employee.GetAsync("/api/v1/passenger/corporate/invitations")).ReadJsonAsync();
        var id = invitations[0].GetProperty("id").GetString();
        Assert.Equal(HttpStatusCode.OK, (await employee.PostAsync($"/api/v1/passenger/corporate/invitations/{id}/accept", null)).StatusCode);
    }

    public static async Task<JsonElement> CreatePolicyAsync(Company company, object body, bool makeDefault = false)
    {
        var response = await company.Portal.PostAsJsonAsync("/api/v1/corporate/policies", body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var policy = await response.ReadJsonAsync();
        if (makeDefault && !policy.GetProperty("isDefault").GetBoolean())
        {
            (await company.Portal.PostAsync($"/api/v1/corporate/policies/{policy.GetProperty("id").GetString()}/default", null)).EnsureSuccessStatusCode();
        }

        return policy;
    }

    public static async Task<JsonElement> CreateCostCenterAsync(Company company, string code = "IT-01", string name = "IT")
    {
        var response = await company.Portal.PostAsJsonAsync("/api/v1/corporate/cost-centers", new { code, name });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.ReadJsonAsync();
    }

    /// <summary>A corporate trip request (<c>POST /passenger/trips</c>) from <paramref name="area"/> to 0.05° away.</summary>
    public static object TripRequest((decimal Lat, decimal Lng) area, string? purpose = "اجتماع عميل", Guid? costCenterId = null, Guid? category = null, string bookingType = "now", DateTime? scheduledAt = null,
        string? promoCode = null, string pricingMode = "fixed", decimal? offeredPrice = null, string? quoteId = null) => new
    {
        pickup = new { name = "المنزل", address = "شارع الملك فهد", lat = area.Lat, lng = area.Lng },
        dropoff = new { name = "العمل", address = "طريق الملك عبدالله", lat = area.Lat + 0.05m, lng = area.Lng + 0.05m },
        stops = Array.Empty<object>(),
        rideCategoryId = category ?? SeedIds.RideCategories.Economy,
        bookingType,
        scheduledAt,
        paymentMethod = "corporate",
        pricingMode,
        offeredPrice,
        quoteId,
        promoCode,
        tripPurpose = purpose,
        costCenterId,
    };

    /// <summary>The portal booking body for an employee or a guest.</summary>
    public static object PortalBooking((decimal Lat, decimal Lng) area, Guid? employeeId = null, string? guestName = null, string? guestPhone = null, string? purpose = "اجتماع عميل", Guid? costCenterId = null,
        string bookingType = "now", DateTime? scheduledAt = null, string? quoteId = null) => new
    {
        employeeId,
        guest = guestName is null ? null : new { name = guestName, phoneNumber = guestPhone },
        pickup = new { name = "المنزل", address = "شارع الملك فهد", lat = area.Lat, lng = area.Lng },
        dropoff = new { name = "العمل", address = "طريق الملك عبدالله", lat = area.Lat + 0.05m, lng = area.Lng + 0.05m },
        stops = Array.Empty<object>(),
        rideCategoryId = SeedIds.RideCategories.Economy,
        bookingType,
        scheduledAt,
        quoteId,
        tripPurpose = purpose,
        costCenterId,
        riderNote = "بوابة 2",
    };

    public static async Task<string> ViolationRuleAsync(HttpResponseMessage response, string expectedCode = "corporate_policy_violation")
    {
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = (await response.ReadJsonAsync()).GetProperty("error");
        Assert.Equal(expectedCode, error.GetProperty("code").GetString());
        return string.Join(',', error.GetProperty("details").GetProperty("violations").EnumerateArray().Select(v => v.GetProperty("rule").GetString()).Order());
    }

    /// <summary>Completes a trip: the employee (or admin) requests, the driver accepts and drives it, returns the trip id.</summary>
    public static async Task<string> CompleteCorporateTripAsync(ApiFixture fixture, (decimal Lat, decimal Lng) area, SafetyFlow.Party employee, SafetyFlow.Party driver, object? request = null)
    {
        (await driver.Client.PutAsJsonAsync("/api/v1/driver/location", new { lat = area.Lat, lng = area.Lng, heading = 0, accuracy = 5 })).EnsureSuccessStatusCode();
        var trip = await TripFlow.RequestAndAssignAsync(fixture, employee.Client, driver.Client, request ?? TripRequest(area));
        var tripId = trip.GetProperty("id").GetString()!;
        var pin = (await (await employee.Client.GetAsync($"/api/v1/passenger/trips/{tripId}")).ReadJsonAsync()).GetProperty("pin").GetString()!;
        await TripFlow.DriveAsync(driver.Client, tripId, pin);
        var completed = await driver.Client.PostAsJsonAsync($"/api/v1/driver/trips/{tripId}/complete", new { });
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        return tripId;
    }

    public static SafetyFlow.Party Party(Employee employee) => new(employee.Client, employee.Auth);

    /// <summary>An admin account holding only <paramref name="permissions"/> (the seeded admin holds <c>*</c>).</summary>
    public static async Task<HttpClient> LimitedAdminAsync(ApiFixture fixture, string username, params string[] permissions)
    {
        await fixture.Factory.WithDbAsync(async db =>
        {
            if (!await db.AdminAccounts.AnyAsync(a => a.Username == username))
            {
                using var scope = fixture.Factory.Services.CreateScope();
                var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
                var user = new User { PhoneNumber = $"+9665{Random.Shared.Next(10000000, 99999999)}", FullName = $"Admin {username}", PhoneVerifiedAt = DateTime.UtcNow };
                user.Roles.Add(new UserRole { UserId = user.Id, Role = Role.Admin });
                db.Users.Add(user);
                db.AdminAccounts.Add(new AdminAccount { UserId = user.Id, Username = username, PasswordHash = hasher.Hash("Limited@12345"), Permissions = JsonSerializer.Serialize(permissions) });
                await db.SaveChangesAsync();
            }

            return true;
        });
        return await fixture.LoginAdminAsync(username, "Limited@12345");
    }

    public static async Task<decimal> LedgerBalanceAsync(ApiFixture fixture, string account) =>
        await fixture.Factory.WithDbAsync(async db => (await db.LedgerEntries.AsNoTracking().Where(e => e.Account == account).ToListAsync()).Sum(e => e.Credit - e.Debit));
}
