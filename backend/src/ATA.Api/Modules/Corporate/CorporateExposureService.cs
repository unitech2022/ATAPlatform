using ATA.Domain.Cancellation;
using ATA.Domain.Corporate;
using ATA.Domain.Common;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Corporate;

/// <summary>
/// What a company owes or may owe (doc 12 §F19.2 credit limit, §F19.4 receivables / dashboard):
/// <list type="bullet">
/// <item><c>unbilled</c> = completed corporate trips + charged corporate cancellation fees that are not on a non-void invoice + adjustments without an invoice;</item>
/// <item><c>unpaidInvoices</c> = outstanding amount of draft / issued / overdue invoices;</item>
/// <item><c>inFlight</c> = estimated fare of the company's open trips (scheduled or running), so concurrent bookings cannot overshoot the limit together.</item>
/// </list>
/// </summary>
public sealed class CorporateExposureService(AtaDbContext db)
{
    public async Task<CorporateExposure> ForAccountAsync(Guid accountId, CancellationToken ct)
    {
        var tripsQuery = db.Trips.AsNoTracking().Where(t => t.CorporateAccountId == accountId && t.PaymentMethod == PaymentMethodKind.Corporate);
        var unbilledTrips = await tripsQuery
            .Where(t => t.Status == TripStatus.Completed && t.FinalFare != null && !(
                from l in db.CorporateInvoiceLines
                join i in db.CorporateInvoices on l.InvoiceId equals i.Id
                where l.TripId == t.Id && l.LineType == InvoiceLineType.Trip && i.Status != CorporateInvoiceStatus.Void
                select l.Id).Any())
            .SumAsync(t => (decimal?)t.FinalFare, ct) ?? 0m;
        var unbilledFees = await (from e in db.CancellationEvents.AsNoTracking()
                                  join t in db.Trips.AsNoTracking() on e.TripId equals t.Id
                                  where t.CorporateAccountId == accountId && e.FeeMethod == CancellationFeeMethod.Corporate && e.FeeStatus == CancellationFeeStatus.Charged && !(
                                      from l in db.CorporateInvoiceLines
                                      join i in db.CorporateInvoices on l.InvoiceId equals i.Id
                                      where l.TripId == t.Id && l.LineType == InvoiceLineType.CancellationFee && i.Status != CorporateInvoiceStatus.Void
                                      select l.Id).Any()
                                  select (decimal?)e.FeeCharged).SumAsync(ct) ?? 0m;
        var adjustments = await db.CorporateAdjustments.AsNoTracking().Where(a => a.CorporateAccountId == accountId && a.InvoiceId == null).SumAsync(a => (decimal?)a.Amount, ct) ?? 0m;
        var invoices = await db.CorporateInvoices.AsNoTracking()
            .Where(i => i.CorporateAccountId == accountId && (i.Status == CorporateInvoiceStatus.Draft || i.Status == CorporateInvoiceStatus.Issued || i.Status == CorporateInvoiceStatus.Overdue))
            .Select(i => new { i.Status, i.TotalInclVat, i.PaidAmount }).ToListAsync(ct);
        var unpaid = invoices.Sum(i => Math.Max(0m, i.TotalInclVat - (i.PaidAmount ?? 0m)));
        var overdue = invoices.Where(i => i.Status == CorporateInvoiceStatus.Overdue).Sum(i => Math.Max(0m, i.TotalInclVat - (i.PaidAmount ?? 0m)));
        var openStatuses = Trip.OpenStatuses;
        var inFlight = await tripsQuery.Where(t => openStatuses.Contains(t.Status)).SumAsync(t => (decimal?)t.EstimatedFare, ct) ?? 0m;
        return new CorporateExposure(unbilledTrips + unbilledFees + adjustments, unpaid, overdue, inFlight);
    }

    /// <summary>
    /// What one employee spent in the Riyadh month of <paramref name="referenceUtc"/>: final fares of completed corporate trips plus the estimated fare of the open corporate
    /// trips of that month (doc 12 §F19.2 <c>spent</c>). <paramref name="excludeTripId"/> leaves one trip out (the trip being completed).
    /// </summary>
    public async Task<decimal> SpentAsync(Guid corporateUserId, DateTime referenceUtc, Guid? excludeTripId, CancellationToken ct)
    {
        var (start, end) = CorporateRules.MonthBounds(referenceUtc);
        var trips = db.Trips.AsNoTracking().Where(t => t.CorporateUserId == corporateUserId && t.PaymentMethod == PaymentMethodKind.Corporate && t.Id != excludeTripId);
        var completed = await trips.Where(t => t.Status == TripStatus.Completed && t.CompletedAt >= start && t.CompletedAt < end).SumAsync(t => (decimal?)t.FinalFare, ct) ?? 0m;
        var openStatuses = Trip.OpenStatuses;
        var open = await trips.Where(t => openStatuses.Contains(t.Status) && (t.ScheduledAt ?? t.RequestedAt) >= start && (t.ScheduledAt ?? t.RequestedAt) < end)
            .SumAsync(t => (decimal?)t.EstimatedFare, ct) ?? 0m;
        return completed + open;
    }

    /// <summary>Month-to-date spend of several employees at once (employee list, dashboard).</summary>
    public async Task<IReadOnlyDictionary<Guid, decimal>> SpentByEmployeeAsync(IReadOnlyCollection<Guid> corporateUserIds, DateTime referenceUtc, CancellationToken ct)
    {
        if (corporateUserIds.Count == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        var (start, end) = CorporateRules.MonthBounds(referenceUtc);
        var openStatuses = Trip.OpenStatuses;
        var rows = await db.Trips.AsNoTracking()
            .Where(t => t.CorporateUserId != null && corporateUserIds.Contains(t.CorporateUserId.Value) && t.PaymentMethod == PaymentMethodKind.Corporate)
            .Where(t => (t.Status == TripStatus.Completed && t.CompletedAt >= start && t.CompletedAt < end)
                        || (openStatuses.Contains(t.Status) && (t.ScheduledAt ?? t.RequestedAt) >= start && (t.ScheduledAt ?? t.RequestedAt) < end))
            .Select(t => new { Id = t.CorporateUserId!.Value, Amount = t.Status == TripStatus.Completed ? (t.FinalFare ?? 0m) : t.EstimatedFare })
            .ToListAsync(ct);
        return rows.GroupBy(r => r.Id).ToDictionary(g => g.Key, g => g.Sum(r => r.Amount));
    }
}
