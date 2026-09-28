using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Infrastructure.Sms;
using ATA.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ATA.Tests.Infrastructure;

/// <summary>SMS sender that records every message (trusted contacts, ops hotline).</summary>
public sealed class RecordingSmsSender : ISmsSender
{
    public ConcurrentBag<(string Phone, string Message)> Sent { get; } = [];

    public string Provider => "recording";

    public Task<SmsSendResult> SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default)
    {
        Sent.Add((phoneNumber, message));
        return Task.FromResult(new SmsSendResult(true, $"sms-{Guid.NewGuid():N}", null, null, false));
    }
}

/// <summary>API host with a recording SMS sender and an ops hotline number (F12/F14 tests).</summary>
public sealed class SafetyFixture() : ApiFixture(new Dictionary<string, string?>
{
    ["Safety:OpsHotlinePhones:0"] = "+966511111111",
    ["Safety:PublicShareRatePerMinute"] = "1000",
}, services =>
{
    services.RemoveAll<ISmsSender>();
    services.AddSingleton<RecordingSmsSender>();
    services.AddSingleton<ISmsSender>(sp => sp.GetRequiredService<RecordingSmsSender>());
})
{
    public RecordingSmsSender Sms => Factory.Services.GetRequiredService<RecordingSmsSender>();
}

/// <summary>Shared steps for the safety / cancellation tests.</summary>
public static class SafetyFlow
{
    public sealed record Party(HttpClient Client, JsonElement Auth)
    {
        public Guid UserId => PaymentFlow.UserId(Auth);
        public string Phone => Auth.GetProperty("user").GetProperty("phoneNumber").GetString()!;
        public string Token => Auth.GetProperty("accessToken").GetString()!;
    }

    public sealed record Ride(Party Passenger, Party Driver, Guid DriverId, string TripId, string TripNumber);

    public static async Task<Party> PassengerAsync(ApiFixture fixture, string fullName = "سارة أحمد")
    {
        var (client, auth) = await fixture.LoginAsync("passenger");
        (await client.PatchAsJsonAsync("/api/v1/me", new { fullName })).EnsureSuccessStatusCode();
        return new Party(client, auth);
    }

    public static async Task<(Party Driver, Guid DriverId)> OnlineDriverAsync(ApiFixture fixture, (decimal Lat, decimal Lng) at, string name = "محمد العتيبي")
    {
        var (client, auth) = await fixture.LoginAsync("driver");
        var driverId = await TripFlow.ApproveDriverAsync(fixture, client, name);
        await TripFlow.GoOnlineAsync(client, at.Lat, at.Lng);
        return (new Party(client, auth), driverId);
    }

    /// <summary>Passenger + online driver in <paramref name="area"/>, trip requested and accepted (<c>driver_assigned</c>).</summary>
    public static async Task<Ride> AssignedRideAsync(ApiFixture fixture, (decimal Lat, decimal Lng) area, string paymentMethod = "cash", Party? passenger = null)
    {
        passenger ??= await PassengerAsync(fixture);
        var (driver, driverId) = await OnlineDriverAsync(fixture, area);
        var trip = await TripFlow.RequestAndAssignAsync(fixture, passenger.Client, driver.Client, TripFlow.Request(area, paymentMethod: paymentMethod));
        return new Ride(passenger, driver, driverId, trip.GetProperty("id").GetString()!, trip.GetProperty("tripNumber").GetString()!);
    }

    /// <summary>Another ride with an existing (free, online) driver so no other driver of the area can take the offer.</summary>
    public static async Task<Ride> NextRideAsync(ApiFixture fixture, (decimal Lat, decimal Lng) area, Party passenger, Party driver, Guid driverId, string paymentMethod = "cash")
    {
        // Keep the driver's location fresh (the fake clock may have moved past Matching:LocationMaxAgeSeconds).
        (await driver.Client.PutAsJsonAsync("/api/v1/driver/location", new { lat = area.Lat, lng = area.Lng, heading = 0, accuracy = 5 })).EnsureSuccessStatusCode();
        var trip = await TripFlow.RequestAndAssignAsync(fixture, passenger.Client, driver.Client, TripFlow.Request(area, paymentMethod: paymentMethod));
        return new Ride(passenger, driver, driverId, trip.GetProperty("id").GetString()!, trip.GetProperty("tripNumber").GetString()!);
    }

    public static async Task<string> PinAsync(Ride ride) =>
        (await (await ride.Passenger.Client.GetAsync($"/api/v1/passenger/trips/{ride.TripId}")).ReadJsonAsync()).GetProperty("pin").GetString()!;

    /// <summary>Drives the ride to <c>in_trip</c>.</summary>
    public static async Task StartAsync(Ride ride) => await TripFlow.DriveAsync(ride.Driver.Client, ride.TripId, await PinAsync(ride));

    public static async Task CompleteAsync(Ride ride)
    {
        await StartAsync(ride);
        (await ride.Driver.Client.PostAsJsonAsync($"/api/v1/driver/trips/{ride.TripId}/complete", new { })).EnsureSuccessStatusCode();
    }

    public static async Task<HttpClient> OnDutyAdminAsync(ApiFixture fixture)
    {
        var admin = await fixture.LoginAdminAsync();
        (await admin.PutAsJsonAsync("/api/v1/admin/me/duty", new { onDuty = true })).EnsureSuccessStatusCode();
        return admin;
    }

    public static async Task<Guid> AdminUserIdAsync(ApiFixture fixture, string username = "admin") =>
        await fixture.Factory.WithDbAsync(db => db.AdminAccounts.Where(a => a.Username == username).Select(a => a.UserId).FirstAsync());

    public static async Task<decimal> WalletBalanceAsync(ApiFixture fixture, Guid userId, ATA.Domain.Wallet.WalletKind kind) =>
        await fixture.Factory.WithDbAsync(db => db.Wallets.Where(w => w.UserId == userId && w.Kind == kind).Select(w => (decimal?)w.Balance).FirstOrDefaultAsync()) ?? 0m;
}
