using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Api.Modules.Notifications;
using ATA.Domain.Common;
using ATA.Domain.Notifications;
using ATA.Domain.Safety;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Safety;

/// <summary>Lost items (doc 09 §F12.6): passenger report within <c>Safety:LostItemWindowDays</c>, driver answer, operations follow-up.</summary>
public sealed class LostItemService(
    AtaDbContext db,
    ICurrentUser currentUser,
    IClock clock,
    INotificationDispatcher notifications,
    AuditService audit,
    IOptions<SafetyOptions> options)
{
    public const string EntityType = "lost_item";

    public async Task<LostItemDto> ReportAsync(Guid tripId, LostItemRequest request, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.ItemCategory), request.ItemCategory)
            .Require(nameof(request.Description), request.Description, 1000)
            .ThrowIfInvalid();
        var userId = currentUser.UserId;
        var trip = await (from t in db.Trips.AsNoTracking()
                          join p in db.Passengers.AsNoTracking() on t.PassengerId equals p.Id
                          where t.Id == tripId && p.UserId == userId
                          select t).FirstOrDefaultAsync(ct) ?? throw new DomainException(ErrorCodes.NotFound);
        if (trip.Status != TripStatus.Completed || trip.CompletedAt is not { } completedAt)
        {
            throw new DomainException(ErrorCodes.Conflict, new { status = trip.Status });
        }

        var now = clock.UtcNow;
        if (now > completedAt.AddDays(options.Value.LostItemWindowDays))
        {
            throw new DomainException(ErrorCodes.LostItemWindowClosed, new { windowDays = options.Value.LostItemWindowDays });
        }

        var phone = string.IsNullOrWhiteSpace(request.ContactPhone)
            ? await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.PhoneNumber).FirstAsync(ct)
            : PhoneNumber.Normalize(request.ContactPhone);
        var report = new LostItemReport
        {
            ReportNumber = string.Empty, TripId = trip.Id, ReporterUserId = userId, DriverId = trip.DriverId, ItemCategory = request.ItemCategory!.Value,
            Description = request.Description!.Trim(), ContactPhone = phone,
        };
        db.LostItemReports.Add(report);
        if (trip.DriverId is { } driverId && await db.Drivers.AsNoTracking().Where(d => d.Id == driverId).Select(d => (Guid?)d.UserId).FirstOrDefaultAsync(ct) is { } driverUserId)
        {
            var (ar, en) = SafetyLabels.ItemCategory(report.ItemCategory);
            await notifications.DispatchAsync(new NotificationRequest(NotificationTypes.LostItemReported, driverUserId,
                NotificationPlaceholders.Of(("tripNumber", trip.TripNumber)).Localized("itemCategory", ar, en), "report", report.Id,
                new Dictionary<string, object?> { ["tripId"] = trip.Id }), ct);
        }

        for (var attempt = 0; ; attempt++)
        {
            report.ReportNumber = await SequenceNumbers.NextAsync(db.LostItemReports.Select(r => r.ReportNumber), $"LI-{now:yyyyMMdd}-", 4, attempt, ct);
            try
            {
                await db.SaveChangesAsync(ct);
                break;
            }
            catch (DbUpdateException) when (attempt < 3)
            {
            }
        }

        return ToDto(report, trip.TripNumber);
    }

    public async Task<PagedResult<LostItemDto>> ListMineAsync(Paging paging, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var query = from r in db.LostItemReports.AsNoTracking()
                    join t in db.Trips.AsNoTracking() on r.TripId equals t.Id
                    where r.ReporterUserId == userId
                    select new { r, t.TripNumber };
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.r.CreatedAt).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        return paging.Result(rows.Select(x => ToDto(x.r, x.TripNumber)).ToList(), total);
    }

    public async Task<PagedResult<DriverLostItemDto>> ListForDriverAsync(LostItemStatus? status, Paging paging, CancellationToken ct)
    {
        var driverId = await DriverIdAsync(ct);
        var query = from r in db.LostItemReports.AsNoTracking()
                    join t in db.Trips.AsNoTracking() on r.TripId equals t.Id
                    where r.DriverId == driverId
                    select new { r, t.TripNumber };
        if (status is not null) query = query.Where(x => x.r.Status == status);
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.r.CreatedAt).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        return paging.Result(rows.Select(x => new DriverLostItemDto(x.r.Id, x.r.ReportNumber, x.TripNumber, x.r.ItemCategory, x.r.Description, x.r.Status, x.r.DriverResponse, x.r.CreatedAt)).ToList(), total);
    }

    public async Task<DriverLostItemDto> RespondAsync(Guid reportId, LostItemRespondRequest request, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.Found), request.Found)
            .Rule(nameof(request.Note), request.Note is null || request.Note.Length <= 500, "max_length:500")
            .ThrowIfInvalid();
        var driverId = await DriverIdAsync(ct);
        var report = Guard.NotFound(await db.LostItemReports.FirstOrDefaultAsync(r => r.Id == reportId && r.DriverId == driverId, ct));
        if (report.DriverResponse is not null || report.Status is LostItemStatus.Closed or LostItemStatus.Returned)
        {
            throw new DomainException(ErrorCodes.Conflict, new { status = report.Status, report.DriverResponse });
        }

        var found = request.Found!.Value;
        report.DriverResponse = found ? LostItemDriverResponse.Found : LostItemDriverResponse.NotFound;
        report.DriverNote = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        report.DriverRespondedAt = clock.UtcNow;
        report.Status = found ? LostItemStatus.Found : LostItemStatus.NotFound;
        await NotifyPassengerAsync(report, ct);
        await db.SaveChangesAsync(ct);
        var tripNumber = await db.Trips.AsNoTracking().Where(t => t.Id == report.TripId).Select(t => t.TripNumber).FirstOrDefaultAsync(ct);
        return new DriverLostItemDto(report.Id, report.ReportNumber, tripNumber, report.ItemCategory, report.Description, report.Status, report.DriverResponse, report.CreatedAt);
    }

    public async Task<PagedResult<AdminLostItemDto>> AdminListAsync(LostItemStatus? status, string? search, Paging paging, CancellationToken ct)
    {
        var query = from r in db.LostItemReports.AsNoTracking()
                    join t in db.Trips.AsNoTracking() on r.TripId equals t.Id
                    join u in db.Users.AsNoTracking() on r.ReporterUserId equals u.Id
                    select new { r, t.TripNumber, u.FullName, u.PhoneNumber };
        if (status is not null) query = query.Where(x => x.r.Status == status);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var phone = PhoneNumber.TryNormalize(term, out var normalized) ? normalized : term;
            query = query.Where(x => x.r.ReportNumber.Contains(term) || x.TripNumber.Contains(term) || x.PhoneNumber.Contains(phone) || (x.FullName != null && x.FullName.Contains(term)));
        }

        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.r.CreatedAt).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        var driverIds = rows.Where(x => x.r.DriverId != null).Select(x => x.r.DriverId!.Value).Distinct().ToList();
        var drivers = await (from d in db.Drivers.AsNoTracking() join u in db.Users.AsNoTracking() on d.UserId equals u.Id where driverIds.Contains(d.Id) select new { d.Id, u.FullName })
            .ToDictionaryAsync(x => x.Id, x => x.FullName, ct);
        return paging.Result(rows.Select(x => ToAdminDto(x.r, x.TripNumber, x.FullName, x.PhoneNumber, x.r.DriverId is { } d ? drivers.GetValueOrDefault(d) : null)).ToList(), total);
    }

    /// <summary>Operations follow-up (<c>driver_contacted → returned → closed</c>…), audited as <c>lost_item.update</c>; the passenger is notified.</summary>
    public async Task<AdminLostItemDto> AdminUpdateAsync(Guid id, LostItemUpdateRequest request, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.Status), request.Status)
            .Rule(nameof(request.Note), request.Note is null || request.Note.Length <= 500, "max_length:500")
            .ThrowIfInvalid();
        var report = Guard.NotFound(await db.LostItemReports.FirstOrDefaultAsync(r => r.Id == id, ct));
        if (report.Status == LostItemStatus.Closed && request.Status != LostItemStatus.Closed)
        {
            throw new DomainException(ErrorCodes.Conflict, new { status = report.Status });
        }

        var before = new { report.Status };
        var changed = report.Status != request.Status;
        report.Status = request.Status!.Value;
        if (report.Status == LostItemStatus.Closed && report.ClosedAt is null)
        {
            report.ClosedAt = clock.UtcNow;
            report.ClosedBy = currentUser.UserId;
        }

        audit.Log("lost_item.update", EntityType, report.Id, before, new { report.Status, note = request.Note?.Trim() });
        if (changed)
        {
            await NotifyPassengerAsync(report, ct);
        }

        await db.SaveChangesAsync(ct);
        return (await AdminListByIdAsync(report.Id, ct))!;
    }

    private async Task<AdminLostItemDto?> AdminListByIdAsync(Guid id, CancellationToken ct)
    {
        var row = await (from r in db.LostItemReports.AsNoTracking()
                         join t in db.Trips.AsNoTracking() on r.TripId equals t.Id
                         join u in db.Users.AsNoTracking() on r.ReporterUserId equals u.Id
                         where r.Id == id
                         select new { r, t.TripNumber, u.FullName, u.PhoneNumber }).FirstOrDefaultAsync(ct);
        if (row is null) return null;
        var driverName = row.r.DriverId is { } driverId
            ? await (from d in db.Drivers.AsNoTracking() join u in db.Users.AsNoTracking() on d.UserId equals u.Id where d.Id == driverId select u.FullName).FirstOrDefaultAsync(ct)
            : null;
        return ToAdminDto(row.r, row.TripNumber, row.FullName, row.PhoneNumber, driverName);
    }

    private async Task NotifyPassengerAsync(LostItemReport report, CancellationToken ct)
    {
        var (ar, en) = SafetyLabels.LostItemStatus(report.Status);
        await notifications.DispatchAsync(new NotificationRequest(NotificationTypes.LostItemUpdate, report.ReporterUserId,
            NotificationPlaceholders.Of(("ticketId", report.SupportTicketId?.ToString() ?? string.Empty)).Localized("status", ar, en), "report", report.Id,
            new Dictionary<string, object?> { ["reportId"] = report.Id, ["tripId"] = report.TripId, ["status"] = SafetyLabels.Snake(report.Status) }), ct);
    }

    private async Task<Guid> DriverIdAsync(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        return await db.Drivers.AsNoTracking().Where(d => d.UserId == userId).Select(d => (Guid?)d.Id).FirstOrDefaultAsync(ct) ?? throw new DomainException(ErrorCodes.Forbidden);
    }

    private static LostItemDto ToDto(LostItemReport r, string? tripNumber) =>
        new(r.Id, r.ReportNumber, r.TripId, tripNumber, r.ItemCategory, r.Description, r.ContactPhone, r.Status, r.DriverResponse, r.DriverNote, r.SupportTicketId, r.CreatedAt);

    private static AdminLostItemDto ToAdminDto(LostItemReport r, string? tripNumber, string? reporterName, string? reporterPhone, string? driverName) =>
        new(r.Id, r.ReportNumber, r.TripId, tripNumber, r.ReporterUserId, reporterName, reporterPhone, r.ContactPhone, r.DriverId, driverName, r.ItemCategory, r.Description,
            r.Status, r.DriverResponse, r.DriverNote, r.DriverRespondedAt, r.SupportTicketId, r.ClosedAt, r.CreatedAt, r.UpdatedAt);
}
