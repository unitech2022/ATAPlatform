using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ATA.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Integration;

public class SettlementTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Settlement_nets_cash_debt_against_card_earnings_and_satisfies_the_balance_invariant()
    {
        var area = TripFlow.Area(0);
        var (driver, driverAuth) = await fixture.LoginAsync("driver");
        var driverId = await TripFlow.ApproveDriverAsync(fixture, driver, "كابتن التسوية");
        await TripFlow.GoOnlineAsync(driver, area.Lat, area.Lng);

        var (cashPassenger, _) = await fixture.LoginAsync("passenger");
        var cash = await CompleteAsync(cashPassenger, driver, TripFlow.Request(area));
        var (cardPassenger, _) = await fixture.LoginAsync("passenger");
        var cardId = await PaymentFlow.AddCardAsync(cardPassenger, "tok_sandbox_visa");
        var card = await CompleteAsync(cardPassenger, driver, PaymentFlow.CardTrip(area, cardId));
        Assert.Equal("cash", cash.GetProperty("paymentMethod").GetString());
        Assert.Equal("card", card.GetProperty("paymentMethod").GetString());
        var cashFare = cash.GetProperty("finalFare").GetDecimal();
        var cardFare = card.GetProperty("finalFare").GetDecimal();
        var earnings = await fixture.Factory.WithDbAsync(db => db.Trips.Where(t => t.DriverId == driverId).SumAsync(t => t.DriverEarnings ?? 0m));

        using var admin = await fixture.LoginAdminAsync();
        var generated = await admin.PostAsJsonAsync("/api/v1/admin/settlement-batches", new { periodStart = "2026-09-28T00:00:00Z", periodEnd = "2026-09-29T00:00:00Z" });
        Assert.Equal(HttpStatusCode.Accepted, generated.StatusCode);
        var batch = await generated.ReadJsonAsync();
        var batchId = batch.GetProperty("id").GetString();
        Assert.Equal("ready", batch.GetProperty("status").GetString());
        Assert.Equal("SB-20260928", batch.GetProperty("batchNumber").GetString());

        var rows = await (await admin.GetAsync($"/api/v1/admin/settlement-batches/{batchId}/settlements?search=كابتن التسوية")).ReadJsonAsync();
        var row = Assert.Single(rows.GetProperty("items").EnumerateArray());
        Assert.Equal(2, row.GetProperty("tripsCount").GetInt32());
        Assert.Equal(cashFare + cardFare, row.GetProperty("grossFares").GetDecimal());
        Assert.Equal(earnings, row.GetProperty("earnings").GetDecimal());
        Assert.Equal(cashFare + cardFare - earnings, row.GetProperty("commission").GetDecimal());
        Assert.Equal(cashFare, row.GetProperty("cashCollected").GetDecimal());
        var net = row.GetProperty("netAmount").GetDecimal();
        Assert.Equal(earnings - cashFare, net);
        decimal D(string name) => row.GetProperty(name).GetDecimal();
        Assert.Equal(D("openingBalance") + net + D("topups") - D("fees") - D("payoutsInPeriod"), D("closingBalance"));
        var balance = (await (await driver.GetAsync("/api/v1/wallet?kind=driver")).ReadJsonAsync()).GetProperty("balance").GetDecimal();
        Assert.Equal(balance, D("closingBalance"));
        Assert.Equal(balance > 0 ? "payable_to_driver" : balance < 0 ? "due_from_driver" : "zero", row.GetProperty("direction").GetString());

        var overlap = await admin.PostAsJsonAsync("/api/v1/admin/settlement-batches", new { periodStart = "2026-09-28T12:00:00Z", periodEnd = "2026-09-29T00:00:00Z" });
        Assert.Equal(HttpStatusCode.Conflict, overlap.StatusCode);
        Assert.Equal("settlement_period_overlap", await overlap.ErrorCodeAsync());

        var csv = await admin.GetAsync($"/api/v1/admin/settlement-batches/{batchId}/export?format=csv");
        var bytes = await csv.Content.ReadAsByteArrayAsync();
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes[..3]);
        var text = System.Text.Encoding.UTF8.GetString(bytes[3..]);
        Assert.StartsWith("driver_name,phone,trips,gross_fares,earnings,commission,cash_collected", text);
        Assert.Contains("كابتن التسوية", text);

        var finalized = await (await admin.PostAsync($"/api/v1/admin/settlement-batches/{batchId}/finalize", null)).ReadJsonAsync();
        Assert.Equal("finalized", finalized.GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsync($"/api/v1/admin/settlement-batches/{batchId}/regenerate", null)).StatusCode);
        var mine = await (await driver.GetAsync("/api/v1/driver/settlements")).ReadJsonAsync();
        Assert.Equal(1, mine.GetProperty("total").GetInt32());
        var detail = await (await driver.GetAsync($"/api/v1/driver/settlements/{mine.GetProperty("items")[0].GetProperty("id").GetString()}")).ReadJsonAsync();
        Assert.Equal(net, detail.GetProperty("netAmount").GetDecimal());
        var driverUserId = PaymentFlow.UserId(driverAuth);
        Assert.True(await fixture.Factory.WithDbAsync(db => db.Notifications.AnyAsync(n => n.UserId == driverUserId && n.Type == "settlement.ready")));

        var statement = await (await driver.GetAsync("/api/v1/driver/earnings/statement?from=2026-09-28&to=2026-09-28")).ReadJsonAsync();
        Assert.Equal(2, statement.GetProperty("totals").GetProperty("trips").GetInt32());
        Assert.Equal(net, statement.GetProperty("totals").GetProperty("net").GetDecimal());
        var tripEarnings = await (await driver.GetAsync($"/api/v1/driver/trips/{cash.GetProperty("id").GetString()}/earnings")).ReadJsonAsync();
        Assert.Equal(cashFare, tripEarnings.GetProperty("cashCollected").GetDecimal());
        await PaymentFlow.AssertLedgerBalancedAsync(fixture);
    }

    private async Task<JsonElement> CompleteAsync(HttpClient passenger, HttpClient driver, object request)
    {
        var trip = await TripFlow.RequestAndAssignAsync(fixture, passenger, driver, request);
        var tripId = trip.GetProperty("id").GetString()!;
        var pin = (await (await passenger.GetAsync($"/api/v1/passenger/trips/{tripId}")).ReadJsonAsync()).GetProperty("pin").GetString()!;
        await TripFlow.DriveAsync(driver, tripId, pin);
        var completed = await driver.PostAsJsonAsync($"/api/v1/driver/trips/{tripId}/complete", new { });
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        return await completed.ReadJsonAsync();
    }
}
