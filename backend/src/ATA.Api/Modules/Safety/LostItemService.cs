using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Api.Modules.Notifications;
using ATA.Api.Modules.Support;
using ATA.Domain.Common;
using ATA.Domain.Notifications;
using ATA.Domain.Safety;
using ATA.Domain.Support;
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
    SupportTicketWriter tickets,
    SupportTicketEvents ticketEvents,
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
        EnsureReportable(trip);
        var phone = string.IsNullOrWhiteSpace(request.ContactPhone)
            ? null
            : PhoneNumber.Normalize(request.ContactPhone);
        var description = request.Description!.Trim();

        // F18: every report opens a linked support ticket (type lost_item) whose first message is the description.
        var language = await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.Language).FirstAsync(ct);
        var (itemAr, itemEn) = SafetyLabels.ItemCategory(request.ItemCategory!.Value);
        var subject = language == Language.En ? $"Lost item report - {itemEn} - trip {trip.TripNumber}" : $"بلاغ مفقودات - {itemAr} - الرحلة {trip.TripNumber}";
        var ticket = await tickets.AddAsync(new SupportTicketWriter.Draft(userId, SupportRequesterRole.Passenger, SupportTicketType.LostItem, trip.Id, subject, description, null,
            SupportChannel.App, userId, []), ct);
        var report = await AddLinkedReportAsync(trip, userId, request.ItemCategory!.Value, description, phone, ticket.Id, ct);
        ticket.LostItemReportId = report.Id;
        await tickets.SaveNewAsync(ticket, async (attempt, token) => report.ReportNumber = await NextNumberAsync(attempt, token), ct);
        await ticketEvents.PublishCreatedAsync(ticket, ct);

        return ToDto(report, trip.TripNumber);
    }

    /// <summary>The trip must be <c>completed</c> (<c>409</c>) and not older than <c>Safety:LostItemWindowDays</c> (<c>422 lost_item_window_closed</c>).</summary>
    private void EnsureReportable(Trip trip)
    {
        if (trip.Status != TripStatus.Completed || trip.CompletedAt is not { } completedAt)
        {
            throw new DomainException(ErrorCodes.Conflict, new { status = trip.Status });
        }

        if (clock.UtcNow > completedAt.AddDays(options.Value.LostItemWindowDays))
        {
            throw new DomainException(ErrorCodes.LostItemWindowClosed, new { windowDays = options.Value.LostItemWindowDays });
        }
    }

    /// <summary>
    /// Adds the lost item report linked to <paramref name="ticketId"/> to the unit of work (the caller saves) and tells the driver (<c>lost_item.reported</c>);
    /// used by <c>POST /passenger/trips/{id}/lost-items</c> and by <c>lost_item</c> support tickets. <paramref name="contactPhone"/> defaults to the rider's number.
    /// </summary>
    public async Task<LostItemReport> AddLinkedReportAsync(Trip trip, Guid reporterUserId, LostItemCategory category, string description, string? contactPhone, Guid ticketId, CancellationToken ct)
    {
        EnsureReportable(trip);
        var phone = string.IsNullOrWhiteSpace(contactPhone)
            ? await db.Users.AsNoTracking().Where(u => u.Id == reporterUserId).Select(u => u.PhoneNumber).FirstAsync(ct)
            : PhoneNumber.Normalize(contactPhone);
        var report = new LostItemReport
        {
            ReportNumber = await NextNumberAsync(0, ct), TripId = trip.Id, ReporterUserId = reporterUserId, DriverId = trip.DriverId, ItemCategory = category,
            Description = description, ContactPhone = phone, SupportTicketId = ticketId,
        };
        db.LostItemReports.Add(report);
        if (trip.DriverId is { } driverId && await db.Drivers.AsNoTracking().Where(d => d.Id == driverId).Select(d => (Guid?)d.UserId).FirstOrDefaultAsync(ct) is { } driverUserId)
        {
            var (ar, en) = SafetyLabels.ItemCategory(report.ItemCategory);
            await notifications.DispatchAsync(new NotificationRequest(NotificationTypes.LostItemReported, driverUserId,
                NotificationPlaceholders.Of(("tripNumber", trip.TripNumber)).Localized("itemCategory", ar, en), "report", report.Id,
                new Dictionary<string, object?> { ["tripId"] = trip.Id }), ct);
        }

        return report;
    }

    public async Task<string> NextNumberAsync(int offset, CancellationToken ct) =>
        await SequenceNumbers.NextAsync(db.LostItemReports.Select(r => r.ReportNumber), $"LI-{clock.UtcNow:yyyyMMdd}-", 4, offset, ct);

    /// <summary>F18: a system line in the linked ticket ("lost item update: …") so the rider sees the follow-up in the conversation; the ticket's status is untouched.</summary>
    private async Task AddTicketLineAsync(LostItemReport report, CancellationToken ct)
    {
        if (report.SupportTicketId is not { } ticketId || await db.SupportTickets.FirstOrDefaultAsync(t => t.Id == ticketId, ct) is not { } ticket)
        {
            return;
        }

        var language = await db.Users.AsNoTracking().Where(u => u.Id == report.ReporterUserId).Select(u => u.Language).FirstAsync(ct);
        var (ar, en) = SafetyLabels.LostItemStatus(report.Status);
        tickets.AddSystemMessage(ticket, language == Language.En ? $"Lost item update: {en}" : $"تحديث المفقودات: {ar}");
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
        await AddTicketLineAsync(report, ct);
        await db.SaveChangesAsync(ct);
        if (report.SupportTicketId is { } respondedTicket)
        {
            await ticketEvents.PublishUpdatedAsync(respondedTicket, toUser: true, ct);
        }

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
            await AddTicketLineAsync(report, ct);
        }

        await db.SaveChangesAsync(ct);
        if (changed && report.SupportTicketId is { } updatedTicket)
        {
            await ticketEvents.PublishUpdatedAsync(updatedTicket, toUser: true, ct);
        }

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
