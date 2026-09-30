using ATA.Api.Modules.Payments;
using ATA.Domain.Cancellation;
using ATA.Domain.Common;
using ATA.Domain.Corporate;
using ATA.Domain.Trips;
using ATA.Domain.Wallet;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Corporate;

/// <summary>
/// Reverses a corporate charge that was waived: a cancellation fee approved on appeal (doc 09 §F14.3). A fee that is not on a (non-void) invoice yet is simply
/// removed from billing; one that was invoiced is credited with a negative <c>corporate_adjustments</c> row on the next invoice. Either way the receivable is reversed
/// in the ledger. The caller saves.
/// </summary>
public sealed class CorporateCreditService(AtaDbContext db, LedgerService ledger)
{
    public async Task WaiveCancellationFeeAsync(CancellationEvent cancellation, Trip trip, string reason, Guid? createdBy, CancellationToken ct)
    {
        if (trip.CorporateAccountId is not { } accountId || cancellation.FeeCharged <= 0)
        {
            return;
        }

        var fee = cancellation.FeeCharged;
        var billed = await (from l in db.CorporateInvoiceLines
                            join i in db.CorporateInvoices on l.InvoiceId equals i.Id
                            where l.TripId == trip.Id && l.LineType == InvoiceLineType.CancellationFee && i.Status != CorporateInvoiceStatus.Void
                            select l.Id).AnyAsync(ct);
        if (billed)
        {
            db.CorporateAdjustments.Add(new CorporateAdjustment
            {
                CorporateAccountId = accountId, Amount = -fee, Description = Truncate($"Cancellation fee waived {trip.TripNumber}: {reason}", 255), CreatedBy = createdBy ?? Guid.Empty,
            });
            await ledger.JournalAsync(JournalType.Manual, LedgerAccounts.Refunds, LedgerAccounts.CorporateReceivable(accountId), fee, "cancellation", cancellation.Id,
                $"cancellation:{cancellation.Id}:corporate_waiver", $"Trip {trip.TripNumber} cancellation fee waived (credit note)", ct, createdBy);
        }
        else
        {
            await ledger.JournalAsync(JournalType.Manual, LedgerAccounts.CancellationFees, LedgerAccounts.CorporateReceivable(accountId), fee, "cancellation", cancellation.Id,
                $"cancellation:{cancellation.Id}:corporate_waiver", $"Trip {trip.TripNumber} cancellation fee waived", ct, createdBy);
        }

        cancellation.FeeStatus = CancellationFeeStatus.Refunded;
    }

    private static string Truncate(string value, int max) => value.Length > max ? value[..max] : value;
}
