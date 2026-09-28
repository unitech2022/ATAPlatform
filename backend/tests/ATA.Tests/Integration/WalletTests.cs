using System.Net;
using System.Net.Http.Json;
using ATA.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ATA.Tests.Integration;

public class WalletTests(ApiFixture fixture) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Topup_writes_balanced_ledger_and_is_idempotent()
    {
        var (client, _) = await fixture.LoginAsync("passenger");

        var wallet = await (await client.GetAsync("/api/v1/wallet")).ReadJsonAsync();
        Assert.Equal(0m, wallet.GetProperty("balance").GetDecimal());
        Assert.Equal("SAR", wallet.GetProperty("currency").GetString());
        Assert.Equal("passenger", wallet.GetProperty("kind").GetString());
        var walletId = Guid.Parse(wallet.GetProperty("id").GetString()!);

        var missingKey = await client.PostAsJsonAsync("/api/v1/wallet/topups", new { amount = 100, method = "sandbox" });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, missingKey.StatusCode);
        Assert.Equal("validation_failed", await missingKey.ErrorCodeAsync());

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/wallet/topups") { Content = JsonContent.Create(new { amount = 100, method = "sandbox" }) };
        request.Headers.Add("Idempotency-Key", "topup-1");
        var first = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var firstBody = await first.ReadJsonAsync();
        Assert.Equal(100m, firstBody.GetProperty("balance").GetDecimal());
        var transactionId = firstBody.GetProperty("transactionId").GetString();

        using var replay = new HttpRequestMessage(HttpMethod.Post, "/api/v1/wallet/topups") { Content = JsonContent.Create(new { amount = 100, method = "sandbox" }) };
        replay.Headers.Add("Idempotency-Key", "topup-1");
        var second = await client.SendAsync(replay);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        var secondBody = await second.ReadJsonAsync();
        Assert.Equal(transactionId, secondBody.GetProperty("transactionId").GetString());
        Assert.Equal(100m, secondBody.GetProperty("balance").GetDecimal());

        var transactions = await fixture.Factory.WithDbAsync(db => db.WalletTransactions.Where(t => t.WalletId == walletId).ToListAsync());
        Assert.Single(transactions);
        Assert.Equal("topup", transactions[0].Type.ToString().ToLowerInvariant());

        var entries = await fixture.Factory.WithDbAsync(db => db.LedgerEntries.Where(e => e.TransactionId == transactions[0].Id).ToListAsync());
        Assert.Equal(2, entries.Count);
        Assert.Equal(entries.Sum(e => e.Debit), entries.Sum(e => e.Credit));
        Assert.Contains(entries, e => e.Account == $"passenger_wallet:{walletId}" && e.Credit == 100m);
        Assert.Contains(entries, e => e.Account == "gateway_clearing" && e.Debit == 100m);

        var balance = await fixture.Factory.WithDbAsync(db => db.Wallets.Where(w => w.Id == walletId).Select(w => w.Balance).FirstAsync());
        Assert.Equal(100m, balance);

        var history = await (await client.GetAsync("/api/v1/wallet/transactions")).ReadJsonAsync();
        Assert.Equal(1, history.GetProperty("total").GetInt32());
        Assert.Equal("credit", history.GetProperty("items")[0].GetProperty("direction").GetString());
    }

    [Theory]
    [InlineData(5, "sandbox")]
    [InlineData(6000, "sandbox")]
    [InlineData(100, "card")]
    public async Task Topup_validates_amount_and_method(decimal amount, string method)
    {
        var (client, _) = await fixture.LoginAsync("passenger");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/wallet/topups") { Content = JsonContent.Create(new { amount, method }) };
        request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("validation_failed", await response.ErrorCodeAsync());
    }

    [Fact]
    public async Task Passenger_cannot_read_driver_wallet()
    {
        var (client, _) = await fixture.LoginAsync("passenger");
        var response = await client.GetAsync("/api/v1/wallet?kind=driver");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
