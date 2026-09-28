using ATA.Domain.Common;
using ATA.Domain.Wallet;

namespace ATA.Tests.Unit;

public class WalletPostingTests
{
    [Fact]
    public void Credit_produces_balanced_entries_and_updates_balance()
    {
        var wallet = new Wallet { UserId = Guid.NewGuid(), Kind = WalletKind.Passenger };
        var (tx, entries) = wallet.Post(TransactionType.Topup, TransactionDirection.Credit, 100m, LedgerAccounts.GatewayClearing, "test", "key-1");

        Assert.Equal(100m, wallet.Balance);
        Assert.Equal(100m, tx.BalanceAfter);
        Assert.Equal(entries.Sum(e => e.Debit), entries.Sum(e => e.Credit));
        Assert.Contains(entries, e => e.Account == wallet.LedgerAccount && e.Credit == 100m);
        Assert.Contains(entries, e => e.Account == LedgerAccounts.GatewayClearing && e.Debit == 100m);
    }

    [Fact]
    public void Debit_beyond_balance_is_rejected()
    {
        var wallet = new Wallet { UserId = Guid.NewGuid(), Kind = WalletKind.Passenger, Balance = 20m };
        var ex = Assert.Throws<DomainException>(() => wallet.Post(TransactionType.TripPayment, TransactionDirection.Debit, 50m, LedgerAccounts.PlatformCash, null, null));
        Assert.Equal(ErrorCodes.InsufficientBalance, ex.Code);
        Assert.Equal(20m, wallet.Balance);
    }
}
