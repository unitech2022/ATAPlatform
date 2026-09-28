using System.Net.Http.Json;
using System.Text.Json;
using ATA.Api.Modules.Payments.Gateways;
using ATA.Domain.Common;
using ATA.Domain.Identity;
using ATA.Infrastructure.Persistence;
using ATA.Infrastructure.Security;
using ATA.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ATA.Tests.Infrastructure;

/// <summary>Shared steps for the F11/F13 tests: cards, keyed POSTs, webhooks, extra admins and ledger checks.</summary>
public static class PaymentFlow
{
    public static async Task<HttpResponseMessage> PostWithKeyAsync(HttpClient client, string url, object body, string? key = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(body) };
        request.Headers.Add("Idempotency-Key", key ?? Guid.NewGuid().ToString("N"));
        return await client.SendAsync(request);
    }

    public static async Task<string> AddCardAsync(HttpClient passenger, string token, bool setDefault = true)
    {
        var response = await passenger.PostAsJsonAsync("/api/v1/passenger/payment-methods", new { token, setDefault });
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        return (await response.ReadJsonAsync()).GetProperty("id").GetString()!;
    }

    public static object CardTrip((decimal Lat, decimal Lng) area, string paymentMethodId, decimal dropoffDelta = 0.05m) => new
    {
        pickup = new { name = "المنزل", address = "شارع الملك فهد", lat = area.Lat, lng = area.Lng },
        dropoff = new { name = "العمل", address = "طريق الملك عبدالله", lat = area.Lat + dropoffDelta, lng = area.Lng + dropoffDelta },
        stops = Array.Empty<object>(),
        rideCategoryId = SeedIds.RideCategories.Economy,
        bookingType = "now",
        paymentMethod = "card",
        paymentMethodId,
        pricingMode = "fixed",
    };

    /// <summary>A client that does not follow redirects (return pages and the sandbox challenge answer 302 to <c>ata://</c>).</summary>
    public static HttpClient NoRedirectClient(ApiFixture fixture) =>
        fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    public static async Task<HttpResponseMessage> PostWebhookAsync(ApiFixture fixture, object payload, string secret = "sandbox-webhook-secret")
    {
        var raw = JsonSerializer.Serialize(payload);
        using var client = fixture.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/payments/webhooks/sandbox") { Content = new StringContent(raw, System.Text.Encoding.UTF8, "application/json") };
        request.Headers.Add("X-Sandbox-Signature", WebhookSignatures.Compute(raw, secret));
        return await client.SendAsync(request);
    }

    /// <summary>Creates another admin account (all permissions) — used for the four-eyes rule.</summary>
    public static async Task<HttpClient> SecondAdminAsync(ApiFixture fixture, string username)
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
                db.AdminAccounts.Add(new AdminAccount { UserId = user.Id, Username = username, PasswordHash = hasher.Hash("Second@12345"), Permissions = "[\"*\"]" });
                await db.SaveChangesAsync();
            }

            return true;
        });
        return await fixture.LoginAdminAsync(username, "Second@12345");
    }

    /// <summary>Every wallet movement and every journal must be balanced (Σ debit = Σ credit).</summary>
    public static async Task AssertLedgerBalancedAsync(ApiFixture fixture)
    {
        var entries = await fixture.Factory.WithDbAsync(db => db.LedgerEntries.AsNoTracking().ToListAsync());
        Assert.All(entries, e => Assert.True(e.TransactionId is null != e.JournalId is null, "exactly one of transaction_id / journal_id"));
        foreach (var group in entries.GroupBy(e => e.TransactionId ?? e.JournalId))
        {
            Assert.Equal(group.Sum(e => e.Debit), group.Sum(e => e.Credit));
        }
    }

    public static Guid UserId(JsonElement auth) => Guid.Parse(auth.GetProperty("user").GetProperty("id").GetString()!);
}
