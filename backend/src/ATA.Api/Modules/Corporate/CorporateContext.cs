using ATA.Api.Common;
using ATA.Domain.Common;
using ATA.Domain.Corporate;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Corporate;

/// <summary>The company and the admin membership behind a portal request.</summary>
public sealed record CorporateScope(CorporateAccount Account, CorporateUser Member, Guid UserId);

/// <summary>
/// Resolves the <c>corp</c> claim of a <c>/corporate/*</c> request (doc 12 §F19.3): every query is scoped to that company, and the caller must still be an active
/// <c>corporate_admin</c> of it (a demoted or disabled admin loses access at once, before the token expires). A <c>closed</c> company answers
/// <c>403 corporate_account_inactive</c>; suspended companies stay readable, only bookings are refused.
/// </summary>
public sealed class CorporateContext(AtaDbContext db, ICurrentUser currentUser)
{
    private CorporateScope? _scope;

    public async Task<CorporateScope> RequireAsync(CancellationToken ct)
    {
        if (_scope is not null)
        {
            return _scope;
        }

        var accountId = currentUser.CorporateAccountId ?? throw new DomainException(ErrorCodes.CorporateNotMember);
        var userId = currentUser.UserId;
        var member = await db.CorporateUsers.FirstOrDefaultAsync(
            m => m.CorporateAccountId == accountId && m.UserId == userId && m.Role == CorporateRole.CorporateAdmin && m.Status == CorporateUserStatus.Active, ct)
            ?? throw new DomainException(ErrorCodes.CorporateNotMember);
        var account = await db.CorporateAccounts.FirstAsync(a => a.Id == accountId, ct);
        if (account.Status == CorporateAccountStatus.Closed)
        {
            throw new DomainException(ErrorCodes.CorporateAccountInactive, new { status = account.Status });
        }

        return _scope = new CorporateScope(account, member, userId);
    }

    /// <summary>Bookings need an <c>active</c> company (a pending, suspended or closed one answers <c>403 corporate_account_inactive</c>).</summary>
    public static void EnsureBookable(CorporateAccount account)
    {
        if (account.Status != CorporateAccountStatus.Active)
        {
            throw new DomainException(ErrorCodes.CorporateAccountInactive, new { status = account.Status });
        }
    }
}
