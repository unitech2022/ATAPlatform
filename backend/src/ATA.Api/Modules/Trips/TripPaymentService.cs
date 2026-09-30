using ATA.Api.Modules.Notifications;
using ATA.Api.Modules.Payments;
using ATA.Domain.Common;
using ATA.Domain.Notifications;
using ATA.Domain.Trips;
using ATA.Domain.Wallet;

namespace ATA.Api.Modules.Trips;

/// <summary>
/// Settles a completed trip through the double-entry ledger (doc 08 §F11.3). Must be called inside the completion transaction; the caller saves.
/// <list type="bullet">
/// <item><c>wallet</c>: passenger <c>trip_payment</c> and driver <c>trip_earning</c> against <c>trip_revenue</c>; an insufficient balance falls back to cash.</item>
/// <item><c>card</c>: captured → <c>trip_card_capture</c> journal (<c>gateway_clearing → trip_revenue</c>) + driver <c>trip_earning</c>; a final decline falls
/// back to cash (<c>payment_fallback_cash</c>, <c>card_capture_failed</c>, <c>payment.failed</c>); an unknown result keeps the trip on card with the capture
/// pending (the driver is paid anyway — the platform carries the collection risk).</item>
/// <item><c>corporate</c> (F19): <c>trip_corporate_charge</c> journal <c>corporate_receivable:{account} ← trip_revenue</c> + driver <c>trip_earning</c> out of <c>trip_revenue</c>.</item>
/// <item><c>cash</c>: driver <c>trip_earning</c> against <c>cash_collected</c>, then <c>cash_collection</c> of the whole fare (overdraft allowed): the negative
/// balance is the cash commission the driver owes.</item>
/// </list>
/// </summary>
public sealed class TripPaymentService(LedgerService ledger, TripEventRecorder events, INotificationDispatcher notifications)
{
    public const string ReferenceType = "trip";

    public async Task SettleAsync(Trip trip, TripParticipants participants, decimal fare, decimal driverEarnings, CardCaptureOutcome? card, CancellationToken ct)
    {
        var requested = trip.PaymentMethod;
        if (fare <= 0 && requested != PaymentMethodKind.Cash)
        {
            // F15: a fully discounted fare collects nothing; the driver share is still paid out of trip_revenue (the discount journal funds it).
            await CreditDriverAsync(trip, participants, driverEarnings, LedgerAccounts.TripRevenue, ct);
            events.Add(trip.Id, TripEventTypes.PaymentRecorded, TripActor.System, data: new { method = requested, amount = 0m, driverEarnings });
            return;
        }

        if (requested == PaymentMethodKind.Wallet)
        {
            var passengerWallet = await ledger.GetOrCreateWalletAsync(participants.PassengerUserId, WalletKind.Passenger, ct);
            if (passengerWallet.Status != WalletStatus.Closed && passengerWallet.Balance >= fare)
            {
                await ledger.PostAsync(passengerWallet, TransactionType.TripPayment, TransactionDirection.Debit, fare, LedgerAccounts.TripRevenue,
                    $"Trip {trip.TripNumber}", $"trip:{trip.Id}:payment", ReferenceType, trip.Id, ct);
                await CreditDriverAsync(trip, participants, driverEarnings, LedgerAccounts.TripRevenue, ct);
                events.Add(trip.Id, TripEventTypes.PaymentRecorded, TripActor.System, data: new { method = PaymentMethodKind.Wallet, amount = fare, driverEarnings });
                return;
            }

            trip.PaymentMethod = PaymentMethodKind.Cash;
            events.Add(trip.Id, TripEventTypes.PaymentFallbackCash, TripActor.System,
                data: new { requested, reason = ErrorCodes.InsufficientBalance, balance = passengerWallet.Balance, amount = fare });
        }
        else if (requested == PaymentMethodKind.Corporate && trip.CorporateAccountId is { } accountId)
        {
            // F19: the company is charged through the ledger (receivable ← trip_revenue) and the driver share is paid out of trip_revenue; the invoice bills it later.
            await ledger.JournalAsync(JournalType.TripCorporateCharge, LedgerAccounts.CorporateReceivable(accountId), LedgerAccounts.TripRevenue, fare, ReferenceType, trip.Id,
                $"trip:{trip.Id}:corporate", $"Trip {trip.TripNumber} corporate charge", ct);
            await CreditDriverAsync(trip, participants, driverEarnings, LedgerAccounts.TripRevenue, ct);
            events.Add(trip.Id, TripEventTypes.PaymentRecorded, TripActor.System, data: new { method = PaymentMethodKind.Corporate, amount = fare, driverEarnings, corporateAccountId = accountId });
            return;
        }
        else if (requested == PaymentMethodKind.Card)
        {
            switch (card?.Kind)
            {
                case CardCaptureKind.Captured:
                    await ledger.JournalAsync(JournalType.TripCardCapture, LedgerAccounts.GatewayClearing, LedgerAccounts.TripRevenue, fare, ReferenceType, trip.Id,
                        $"trip:{trip.Id}:capture", $"Trip {trip.TripNumber} card capture", ct);
                    await CreditDriverAsync(trip, participants, driverEarnings, LedgerAccounts.TripRevenue, ct);
                    events.Add(trip.Id, TripEventTypes.PaymentRecorded, TripActor.System,
                        data: new { method = PaymentMethodKind.Card, amount = fare, driverEarnings, paymentId = card.Payment?.Id });
                    return;
                case CardCaptureKind.Pending:
                    await CreditDriverAsync(trip, participants, driverEarnings, LedgerAccounts.TripRevenue, ct);
                    events.Add(trip.Id, TripEventTypes.PaymentCapturePending, TripActor.System,
                        data: new { method = PaymentMethodKind.Card, amount = fare, driverEarnings, paymentId = card.Payment?.Id, card.FailureCode });
                    return;
                default:
                    trip.PaymentMethod = PaymentMethodKind.Cash;
                    var reason = card?.Kind == CardCaptureKind.Declined ? "card_capture_failed" : "card_unavailable";
                    events.Add(trip.Id, TripEventTypes.PaymentFallbackCash, TripActor.System,
                        data: new { requested, reason, amount = fare, paymentId = card?.Payment?.Id, failureCode = card?.FailureCode });
                    await notifications.DispatchAsync(new NotificationRequest(NotificationTypes.PaymentFailed, participants.PassengerUserId,
                        NotificationPlaceholders.Of().Money("amount", fare).Localized("reason", "سيتم الدفع نقداً للكابتن", "please pay the driver in cash"),
                        "trip", trip.Id, new Dictionary<string, object?> { ["tripNumber"] = trip.TripNumber, ["paymentId"] = card?.Payment?.Id }), ct);
                    break;
            }
        }

        await CreditDriverAsync(trip, participants, driverEarnings, LedgerAccounts.CashCollected, ct, cash: true);
        if (participants.DriverUserId is { } driverUserId && fare > 0)
        {
            var driverWallet = await ledger.GetOrCreateWalletAsync(driverUserId, WalletKind.Driver, ct);
            await ledger.PostAsync(driverWallet, TransactionType.CashCollection, TransactionDirection.Debit, fare, LedgerAccounts.CashCollected,
                $"Trip {trip.TripNumber} cash collected", $"trip:{trip.Id}:cash", ReferenceType, trip.Id, ct, allowOverdraft: true);
        }

        events.Add(trip.Id, TripEventTypes.PaymentRecorded, TripActor.System, data: new { method = PaymentMethodKind.Cash, amount = fare, driverEarnings });
    }

    private async Task CreditDriverAsync(Trip trip, TripParticipants participants, decimal driverEarnings, string counterpart, CancellationToken ct, bool cash = false)
    {
        if (participants.DriverUserId is not { } driverUserId || driverEarnings <= 0)
        {
            return;
        }

        var driverWallet = await ledger.GetOrCreateWalletAsync(driverUserId, WalletKind.Driver, ct);
        await ledger.PostAsync(driverWallet, TransactionType.TripEarning, TransactionDirection.Credit, driverEarnings, counterpart,
            cash ? $"Trip {trip.TripNumber} (cash)" : $"Trip {trip.TripNumber}", $"trip:{trip.Id}:earning", ReferenceType, trip.Id, ct);
    }
}
