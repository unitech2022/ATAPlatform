using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Domain.Common;
using ATA.Domain.Ratings;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Ratings;

/// <summary>Admin ratings console (<c>ratings.manage</c>): list with filters, hide/unhide (recomputes the average), flag reviews. Audited.</summary>
public sealed class RatingAdminService(AtaDbContext db, IClock clock, ICurrentUser currentUser, AuditService audit, RatingService ratings)
{
    public async Task<PagedResult<AdminRatingDto>> ListAsync(
        RatingRole? raterRole, int? stars, bool? flagged, Guid? userId, string? tag, string? status, string? search, DateOnly? from, DateOnly? to, Paging paging, CancellationToken ct)
    {
        var query = from r in db.Ratings.AsNoTracking()
                    join t in db.Trips.AsNoTracking() on r.TripId equals t.Id
                    select new { r, t.TripNumber, Flagged = db.RatingFlags.Any(f => f.RatingId == r.Id) };
        if (raterRole is { } role) query = query.Where(x => x.r.RaterRole == role);
        if (stars is { } s) query = query.Where(x => x.r.Stars == s);
        if (flagged is { } fl) query = query.Where(x => x.Flagged == fl);
        if (userId is { } uid) query = query.Where(x => x.r.RaterUserId == uid || x.r.RateeUserId == uid);
        if (!string.IsNullOrWhiteSpace(tag))
        {
            var quoted = $"\"{tag.Trim()}\"";
            query = query.Where(x => x.r.Tags.Contains(quoted));
        }

        query = status switch
        {
            null or "" or "all" => query,
            "visible" => query.Where(x => x.r.Status == RatingStatus.Visible),
            "hidden" => query.Where(x => x.r.Status == RatingStatus.Hidden),
            "flagged" => query.Where(x => x.Flagged),
            _ => throw new DomainException(ErrorCodes.ValidationFailed, new Dictionary<string, string> { ["status"] = "must be visible|hidden|flagged" }),
        };
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.TripNumber.Contains(term));
        }

        if (from is { } f)
        {
            var fromAt = Formats.RiyadhMidnightUtc(f);
            query = query.Where(x => x.r.CreatedAt >= fromAt);
        }

        if (to is { } tt)
        {
            var toAt = Formats.RiyadhMidnightUtc(tt.AddDays(1));
            query = query.Where(x => x.r.CreatedAt < toAt);
        }

        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.r.CreatedAt).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        var names = await NamesAsync(rows.SelectMany(x => new[] { x.r.RaterUserId, x.r.RateeUserId }).Concat(rows.Where(x => x.r.HiddenBy != null).Select(x => x.r.HiddenBy!.Value)), ct);
        return paging.Result(rows.Select(x => ToDto(x.r, x.TripNumber, x.Flagged, names)).ToList(), total);
    }

    public async Task<AdminRatingDto> HideAsync(Guid id, HideRatingRequest request, CancellationToken ct)
    {
        new Validator().Require(nameof(request.Reason), request?.Reason, 500).ThrowIfInvalid();
        var rating = Guard.NotFound(await db.Ratings.FirstOrDefaultAsync(r => r.Id == id, ct));
        return await SetStatusAsync(rating, RatingStatus.Hidden, request!.Reason!.Trim(), ct);
    }

    public async Task<AdminRatingDto> UnhideAsync(Guid id, CancellationToken ct)
    {
        var rating = Guard.NotFound(await db.Ratings.FirstOrDefaultAsync(r => r.Id == id, ct));
        return await SetStatusAsync(rating, RatingStatus.Visible, null, ct);
    }

    private async Task<AdminRatingDto> SetStatusAsync(Rating rating, RatingStatus status, string? reason, CancellationToken ct)
    {
        if (rating.Status != status)
        {
            var before = new { rating.Status, rating.HiddenReason };
            rating.Status = status;
            rating.HiddenBy = status == RatingStatus.Hidden ? currentUser.UserId : null;
            rating.HiddenReason = reason;
            rating.HiddenAt = status == RatingStatus.Hidden ? clock.UtcNow : null;
            await db.InTransactionAsync(async () =>
            {
                audit.Log(status == RatingStatus.Hidden ? "rating.hide" : "rating.unhide", "rating", rating.Id, before, new { rating.Status, reason });
                await db.SaveChangesAsync(ct);
                await ratings.RecomputeAsync(rating.RateeUserId, rating.RateeRole, ct);
                await db.SaveChangesAsync(ct);
            }, ct);
        }

        var tripNumber = await db.Trips.AsNoTracking().Where(t => t.Id == rating.TripId).Select(t => t.TripNumber).FirstAsync(ct);
        var flagged = await db.RatingFlags.AnyAsync(f => f.RatingId == rating.Id, ct);
        var names = await NamesAsync(new[] { rating.RaterUserId, rating.RateeUserId }.Concat(rating.HiddenBy is { } h ? [h] : []), ct);
        return ToDto(rating, tripNumber, flagged, names);
    }

    public async Task<PagedResult<RatingFlagDto>> FlagsAsync(RatingFlagStatus? status, RatingFlagType? type, Paging paging, CancellationToken ct)
    {
        var query = db.RatingFlags.AsNoTracking().AsQueryable();
        if (status is { } s) query = query.Where(f => f.Status == s);
        if (type is { } t) query = query.Where(f => f.Type == t);
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(f => f.CreatedAt).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        var items = new List<RatingFlagDto>(rows.Count);
        foreach (var flag in rows)
        {
            items.Add(await ToDtoAsync(flag, ct));
        }

        return paging.Result(items, total);
    }

    /// <summary>
    /// <c>dismiss</c> → dismissed; <c>warn</c> → actioned (<c>warned</c>); <c>suspension_review</c> → actioned + a note on the driver's history for the review
    /// queue in <c>/drivers/:id</c> (never suspends automatically). Audited <c>rating_flag.review</c>.
    /// </summary>
    public async Task<RatingFlagDto> ReviewAsync(Guid id, ReviewRatingFlagRequest request, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.Action), request?.Action, 40)
            .Rule(nameof(request.Action), request?.Action is null or "dismiss" or "warn" or "suspension_review", "must be dismiss|warn|suspension_review")
            .Rule(nameof(request.Note), request?.Note is null || request.Note.Length <= 500, "max_length:500")
            .ThrowIfInvalid();
        var flag = Guard.NotFound(await db.RatingFlags.FirstOrDefaultAsync(f => f.Id == id, ct));
        if (flag.Status != RatingFlagStatus.Open)
        {
            throw new DomainException(ErrorCodes.Conflict, new { status = flag.Status });
        }

        var before = new { flag.Status, flag.Action };
        var note = string.IsNullOrWhiteSpace(request!.Note) ? null : request.Note.Trim();
        (flag.Status, flag.Action) = request.Action switch
        {
            "dismiss" => (RatingFlagStatus.Dismissed, RatingFlagAction.None),
            "warn" => (RatingFlagStatus.Actioned, RatingFlagAction.Warned),
            _ => (RatingFlagStatus.Actioned, RatingFlagAction.SuspensionReview),
        };
        flag.Note = note;
        flag.ReviewedBy = currentUser.UserId;
        flag.ReviewedAt = clock.UtcNow;
        audit.Log("rating_flag.review", "rating_flag", flag.Id, before, new { flag.Status, flag.Action, note });
        if (flag.Action == RatingFlagAction.SuspensionReview && flag.Role == RatingRole.Driver)
        {
            var driverId = await db.Drivers.AsNoTracking().Where(d => d.UserId == flag.UserId).Select(d => (Guid?)d.Id).FirstOrDefaultAsync(ct);
            if (driverId is { } did)
            {
                audit.Log("driver.suspension_review", "driver", did, null, new { reason = note ?? "rating_flag", flagId = flag.Id, flagType = flag.Type });
            }
        }

        await db.SaveChangesAsync(ct);
        return await ToDtoAsync(flag, ct);
    }

    private async Task<RatingFlagDto> ToDtoAsync(RatingFlag flag, CancellationToken ct)
    {
        var userName = await db.Users.AsNoTracking().Where(u => u.Id == flag.UserId).Select(u => u.FullName).FirstOrDefaultAsync(ct);
        var reviewer = flag.ReviewedBy is { } rb ? await db.Users.AsNoTracking().Where(u => u.Id == rb).Select(u => u.FullName).FirstOrDefaultAsync(ct) : null;
        Guid? driverId = null;
        int? ratingCount;
        if (flag.Role == RatingRole.Driver)
        {
            var driver = await db.Drivers.AsNoTracking().Where(d => d.UserId == flag.UserId).Select(d => new { d.Id, d.RatingCount }).FirstOrDefaultAsync(ct);
            driverId = driver?.Id;
            ratingCount = driver?.RatingCount;
        }
        else
        {
            ratingCount = await db.Passengers.AsNoTracking().Where(p => p.UserId == flag.UserId).Select(p => (int?)p.RatingCount).FirstOrDefaultAsync(ct);
        }

        RatingFlagRatingDto? rating = null;
        if (flag.RatingId is { } ratingId)
        {
            rating = await (from r in db.Ratings.AsNoTracking()
                            join t in db.Trips.AsNoTracking() on r.TripId equals t.Id
                            where r.Id == ratingId
                            select new { r.Stars, r.Comment, r.Tags, t.TripNumber, TripId = t.Id })
                .Select(x => new RatingFlagRatingDto(x.Stars, x.Comment, RatingService.ParseTags(x.Tags), x.TripNumber, x.TripId)).FirstOrDefaultAsync(ct);
        }

        return new RatingFlagDto(flag.Id, flag.UserId, flag.Role, flag.Type, flag.RatingId, flag.Value, flag.Status, flag.Action, flag.ReviewedAt, flag.Note, flag.CreatedAt,
            userName, driverId, reviewer, ratingCount, rating);
    }

    private async Task<Dictionary<Guid, string?>> NamesAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        var list = ids.Distinct().ToList();
        return list.Count == 0 ? [] : await db.Users.AsNoTracking().Where(u => list.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName, ct);
    }

    private static AdminRatingDto ToDto(Rating r, string tripNumber, bool flagged, Dictionary<Guid, string?> names) => new(
        r.Id, tripNumber, names.GetValueOrDefault(r.RaterUserId), r.RaterRole, names.GetValueOrDefault(r.RateeUserId), r.Stars, RatingService.ParseTags(r.Tags), r.Comment,
        r.Status, r.CreatedAt, r.TripId, r.RaterUserId, r.RateeUserId, r.RateeRole, r.CommentHidden, r.HiddenReason,
        r.HiddenBy is { } h ? names.GetValueOrDefault(h) : null, r.HiddenAt, flagged);
}
