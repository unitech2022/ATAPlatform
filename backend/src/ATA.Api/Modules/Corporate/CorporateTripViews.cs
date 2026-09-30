using ATA.Api.Modules.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using ATA.Domain.Trips;

namespace ATA.Api.Modules.Corporate;

/// <summary>Builds <c>Trip.corporate</c> (doc 12 §F19.4) for the viewer: riders see the purpose and cost centre, drivers only the guest's first name, the company and admins the people.</summary>
public sealed class CorporateTripViews(AtaDbContext db)
{
    public async Task<TripCorporateDto?> BuildAsync(Trip trip, TripViewer viewer, CancellationToken ct)
    {
        if (trip.CorporateAccountId is not { } accountId)
        {
            return null;
        }

        var account = await db.CorporateAccounts.AsNoTracking().Where(a => a.Id == accountId).Select(a => new { a.DisplayName }).FirstAsync(ct);
        if (viewer == TripViewer.Driver)
        {
            return new TripCorporateDto(account.DisplayName, null, null, trip.IsGuest, trip.IsGuest ? CorporateRules.FirstName(trip.GuestName) : null, null);
        }

        var costCenter = trip.CostCenterId is { } costCenterId
            ? await db.CorporateCostCenters.AsNoTracking().Where(c => c.Id == costCenterId).Select(c => c.Code).FirstOrDefaultAsync(ct)
            : null;
        var member = trip.CorporateUserId is { } memberId
            ? await (from m in db.CorporateUsers.AsNoTracking()
                     join u in db.Users.AsNoTracking() on m.UserId equals u.Id into users
                     from u in users.DefaultIfEmpty()
                     where m.Id == memberId
                     select new { Name = m.FullName ?? (u == null ? null : u.FullName), m.EmployeeNumber, m.Department, m.PolicyId }).FirstOrDefaultAsync(ct)
            : null;
        if (viewer is TripViewer.Passenger)
        {
            return new TripCorporateDto(account.DisplayName, trip.TripPurpose, costCenter, trip.IsGuest, trip.GuestName, member?.Name);
        }

        string? policyName = null;
        if (viewer == TripViewer.Admin)
        {
            var policyId = member?.PolicyId;
            policyName = policyId is { } pid ? await db.CorporatePolicies.AsNoTracking().Where(p => p.Id == pid).Select(p => p.Name).FirstOrDefaultAsync(ct) : null;
        }

        return new TripCorporateDto(account.DisplayName, trip.TripPurpose, costCenter, trip.IsGuest, trip.GuestName, member?.Name, accountId, member?.EmployeeNumber, member?.Department,
            trip.GuestPhone, policyName);
    }
}
