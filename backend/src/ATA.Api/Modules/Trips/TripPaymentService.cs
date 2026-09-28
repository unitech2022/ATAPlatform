using ATA.Domain.Common;
using ATA.Domain.Trips;
using ATA.Domain.Wallet;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Trips;

/// <summary>
/// Settles a completed trip through the double-entry ledger (see README "Ledger accounts"):
/// wallet → passenger wallet debit and driver wallet credit against <c>trip_revenue</c>; cash → driver wallet credit against
/// <c>cash_collected</c>. A wallet with insufficient balance (or a card, which has no gateway yet) falls back to cash and the
/// fallback is recorded in <c>trip_events</c>. Must be called inside the completion transaction; the caller saves.
/// </summary>
public sealed class TripPaymentService(AtaDbContext db, TripEventRecorder events)
{
    public const string ReferenceType = "trip";

    public async Task SettleAsync(Trip trip, TripParticipants participants, decimal fare, decimal driverEarnings, CancellationToken ct)
    {
        var requested = trip.PaymentMethod;
        if (requested == PaymentMethodKind.Wallet)
        {
            var passengerWallet = await GetOrCreateWalletAsync(participants.PassengerUserId, WalletKind.Passenger, ct);
            if (passengerWallet.Status == WalletStatus.Active && passengerWallet.Balance >= fare)
            {
                Post(passengerWallet.Post(TransactionType.TripPayment, TransactionDirection.Debit, fare, LedgerAccounts.TripRevenue,
                    $"Trip {trip.TripNumber}", $"trip:{trip.Id}:payment", ReferenceType, trip.Id));
                if (participants.DriverUserId is { } driverUserId && driverEarnings > 0)
                {
                    var driverWallet = await GetOrCreateWalletAsync(driverUserId, WalletKind.Driver, ct);
                    Post(driverWallet.Post(TransactionType.TripEarning, TransactionDirection.Credit, driverEarnings, LedgerAccounts.TripRevenue,
                        $"Trip {trip.TripNumber}", $"trip:{trip.Id}:earning", ReferenceType, trip.Id));
                }

                events.Add(trip.Id, TripEventTypes.PaymentRecorded, TripActor.System,
                    data: new { method = PaymentMethodKind.Wallet, amount = fare, driverEarnings });
                return;
            }

            trip.PaymentMethod = PaymentMethodKind.Cash;
            events.Add(trip.Id, TripEventTypes.PaymentFallbackCash, TripActor.System,
                data: new { requested, reason = ErrorCodes.InsufficientBalance, balance = passengerWallet.Balance, amount = fare });
        }
        else if (requested == PaymentMethodKind.Card)
        {
            trip.PaymentMethod = PaymentMethodKind.Cash;
            events.Add(trip.Id, TripEventTypes.PaymentFallbackCash, TripActor.System,
                data: new { requested, reason = "card_not_supported", amount = fare });
        }

        if (participants.DriverUserId is { } cashDriverUserId && driverEarnings > 0)
        {
            var driverWallet = await GetOrCreateWalletAsync(cashDriverUserId, WalletKind.Driver, ct);
            Post(driverWallet.Post(TransactionType.TripEarning, TransactionDirection.Credit, driverEarnings, LedgerAccounts.CashCollected,
                $"Trip {trip.TripNumber} (cash)", $"trip:{trip.Id}:earning", ReferenceType, trip.Id));
        }

        events.Add(trip.Id, TripEventTypes.PaymentRecorded, TripActor.System,
            data: new { method = PaymentMethodKind.Cash, amount = fare, driverEarnings });
    }

    private void Post((WalletTransaction Transaction, LedgerEntry[] Entries) posting)
    {
        db.WalletTransactions.Add(posting.Transaction);
        db.LedgerEntries.AddRange(posting.Entries);
    }

    private async Task<Domain.Wallet.Wallet> GetOrCreateWalletAsync(Guid userId, WalletKind kind, CancellationToken ct)
    {
        var wallet = await db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId && w.Kind == kind, ct);
        if (wallet is null)
        {
            wallet = new Domain.Wallet.Wallet { UserId = userId, Kind = kind };
            db.Wallets.Add(wallet);
        }

        return wallet;
    }
}
