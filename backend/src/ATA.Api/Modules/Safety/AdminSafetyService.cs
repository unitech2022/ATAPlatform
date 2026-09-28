using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Api.Modules.Notifications;
using ATA.Api.Modules.Trips;
using ATA.Domain.Common;
using ATA.Domain.Notifications;
using ATA.Domain.Safety;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Safety;

/// <summary>The safety centre of the admin console (doc 09 §F12.7 "الإدارة"); every write is audited and the first action stamps <c>first_response_at</c>.</summary>
public sealed class AdminSafetyService(
    AtaDbContext db,
    ICurrentUser currentUser,
    IClock clock,
    SafetyCaseFactory cases,
    TrustedContactService contacts,
    TripShareService shares,
    INotificationDispatcher notifications,
    AuditService audit)
{
    public const string EntityType = "safety_case";

    public async Task<SafetySummaryDto> SummaryAsync(CancellationToken ct)
    {
        var open = await db.SafetyCases.AsNoTracking().Where(c => c.Status != SafetyCaseStatus.Resolved)
            .Select(c => new { c.Priority, c.AssignedToUserId }).ToListAsync(ct);
        var responses = await db.SafetyCases.AsNoTracking().Where(c => c.FirstResponseAt != null).Select(c => new { c.OpenedAt, c.FirstResponseAt }).ToListAsync(ct);
        var pendingAlerts = await db.SafetyAlerts.CountAsync(a => a.Status == SafetyAlertStatus.PendingRider, ct);
        var onDuty = await db.AdminAccounts.CountAsync(a => a.OnDuty && a.IsActive, ct);
        int Count(SafetyPriority p) => open.Count(c => c.Priority == p);
        int? average = responses.Count == 0 ? null : (int)Math.Round(responses.Average(r => (r.FirstResponseAt!.Value - r.OpenedAt).TotalSeconds));
        return new SafetySummaryDto(new OpenCasesDto(Count(SafetyPriority.Critical), Count(SafetyPriority.High), Count(SafetyPriority.Medium), Count(SafetyPriority.Low)),
            open.Count(c => c.AssignedToUserId == null), average, pendingAlerts, onDuty);
    }

    public async Task<PagedResult<AdminSafetyCaseListItemDto>> ListAsync(SafetyCaseStatus? status, SafetyPriority? priority, SafetyCaseType? type, string? assignedTo,
        DateOnly? from, DateOnly? to, string? search, Paging paging, CancellationToken ct)
    {
        var query = db.SafetyCases.AsNoTracking().AsQueryable();
        if (status is not null) query = query.Where(c => c.Status == status);
        if (priority is not null) query = query.Where(c => c.Priority == priority);
        if (type is not null) query = query.Where(c => c.Type == type);
        if (!string.IsNullOrWhiteSpace(assignedTo))
        {
            if (assignedTo == "me")
            {
                var me = currentUser.UserId;
                query = query.Where(c => c.AssignedToUserId == me);
            }
            else if (assignedTo == "unassigned")
            {
                query = query.Where(c => c.AssignedToUserId == null);
            }
            else if (Guid.TryParse(assignedTo, out var assignee))
            {
                query = query.Where(c => c.AssignedToUserId == assignee);
            }
            else
            {
                throw new DomainException(ErrorCodes.ValidationFailed, new Dictionary<string, string> { ["assignedTo"] = "must be me|unassigned|{userId}" });
            }
        }

        if (from is { } f)
        {
            var fromAt = f.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(c => c.OpenedAt >= fromAt);
        }

        if (to is { } t)
        {
            var toAt = t.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(c => c.OpenedAt < toAt);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var phone = PhoneNumber.TryNormalize(term, out var normalized) ? normalized : term;
            var tripIds = db.Trips.Where(tr => tr.TripNumber.Contains(term)).Select(tr => tr.Id);
            var userIds = db.Users.Where(u => u.PhoneNumber.Contains(phone) || (u.FullName != null && u.FullName.Contains(term))).Select(u => u.Id);
            query = query.Where(c => c.CaseNumber.Contains(term) || (c.TripId != null && tripIds.Contains(c.TripId.Value)) || (c.ReporterUserId != null && userIds.Contains(c.ReporterUserId.Value)));
        }

        var total = await query.CountAsync(ct);
        // Critical first, then the oldest (the queue order of the safety centre).
        var rows = await query
            .OrderBy(c => c.Status == SafetyCaseStatus.Resolved ? 1 : 0)
            .ThenBy(c => c.Priority == SafetyPriority.Critical ? 0 : c.Priority == SafetyPriority.High ? 1 : c.Priority == SafetyPriority.Medium ? 2 : 3)
            .ThenBy(c => c.OpenedAt)
            .Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        return paging.Result(await cases.ToListItemsAsync(rows, ct), total);
    }

    public async Task<AdminSafetyCaseDetailDto> GetAsync(Guid id, Language lang, CancellationToken ct)
    {
        var safetyCase = Guard.NotFound(await db.SafetyCases.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct));
        return await BuildDetailAsync(safetyCase, lang, ct);
    }

    public async Task<AdminSafetyCaseDetailDto> AssignAsync(Guid id, AssignCaseRequest request, Language lang, CancellationToken ct)
    {
        var safetyCase = await LoadOpenAsync(id, ct);
        var assignee = request.UserId ?? currentUser.UserId;
        var isAgent = await db.AdminAccounts.AsNoTracking().AnyAsync(a => a.UserId == assignee && a.IsActive, ct);
        new Validator().Rule(nameof(request.UserId), isAgent, "not an active admin").ThrowIfInvalid();
        var before = Snapshot(safetyCase);
        var now = clock.UtcNow;
        safetyCase.AssignedToUserId = assignee;
        safetyCase.AssignedAt = now;
        safetyCase.MarkResponded(now);
        if (safetyCase.Status == SafetyCaseStatus.Open)
        {
            safetyCase.Status = SafetyCaseStatus.InProgress;
        }

        var name = await db.Users.AsNoTracking().Where(u => u.Id == assignee).Select(u => u.FullName ?? u.PhoneNumber).FirstOrDefaultAsync(ct);
        cases.AddNote(safetyCase.Id, currentUser.UserId, SafetyNoteKind.Assignment, $"Assigned to {name}");
        audit.Log("safety_case.assign", EntityType, safetyCase.Id, before, Snapshot(safetyCase));
        return await SaveAndBuildAsync(safetyCase, lang, ct);
    }

    public async Task<AdminSafetyCaseDetailDto> SetStatusAsync(Guid id, CaseStatusRequest request, Language lang, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.Status), request.Status)
            .Rule(nameof(request.Status), request.Status is null or SafetyCaseStatus.InProgress or SafetyCaseStatus.Escalated, "must be in_progress|escalated")
            .Rule(nameof(request.EscalatedTo), request.Status != SafetyCaseStatus.Escalated || request.EscalatedTo is not null, "required for escalated")
            .Rule(nameof(request.Note), request.Note is null || request.Note.Length <= 2000, "max_length:2000")
            .ThrowIfInvalid();
        var safetyCase = await LoadOpenAsync(id, ct);
        var before = Snapshot(safetyCase);
        safetyCase.Status = request.Status!.Value;
        if (request.Status == SafetyCaseStatus.Escalated)
        {
            safetyCase.EscalatedTo = request.EscalatedTo;
        }

        safetyCase.MarkResponded(clock.UtcNow);
        var text = request.Status == SafetyCaseStatus.Escalated ? $"Escalated to {SafetyLabels.Snake(request.EscalatedTo!.Value)}" : "In progress";
        cases.AddNote(safetyCase.Id, currentUser.UserId, SafetyNoteKind.StatusChange, string.IsNullOrWhiteSpace(request.Note) ? text : $"{text}: {request.Note.Trim()}");
        audit.Log("safety_case.status", EntityType, safetyCase.Id, before, Snapshot(safetyCase));
        return await SaveAndBuildAsync(safetyCase, lang, ct);
    }

    public async Task<AdminSafetyCaseDetailDto> AddNoteAsync(Guid id, CaseNoteRequest request, Language lang, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.Body), request.Body, 2000)
            .Rule(nameof(request.Kind), request.Kind is null or SafetyNoteKind.Note or SafetyNoteKind.ContactAttempt, "must be note|contact_attempt")
            .ThrowIfInvalid();
        var safetyCase = Guard.NotFound(await db.SafetyCases.FirstOrDefaultAsync(c => c.Id == id, ct));
        var isInternal = request.IsInternal ?? true;
        safetyCase.MarkResponded(clock.UtcNow);
        var note = cases.AddNote(safetyCase.Id, currentUser.UserId, request.Kind ?? SafetyNoteKind.Note, request.Body!.Trim(), isInternal);
        if (!isInternal && safetyCase.ReporterUserId is { } reporter)
        {
            await NotifyReporterAsync(safetyCase, reporter, ct);
        }

        audit.Log("safety_case.note", EntityType, safetyCase.Id, null, new { noteId = note.Id, note.Kind, note.IsInternal });
        return await SaveAndBuildAsync(safetyCase, lang, ct);
    }

    public async Task<AdminSafetyCaseDetailDto> ResolveAsync(Guid id, ResolveCaseRequest request, Language lang, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.ResolutionCode), request.ResolutionCode)
            .Require(nameof(request.Resolution), request.Resolution, 2000)
            .ThrowIfInvalid();
        var safetyCase = await LoadOpenAsync(id, ct);
        var before = Snapshot(safetyCase);
        var now = clock.UtcNow;
        safetyCase.Status = SafetyCaseStatus.Resolved;
        safetyCase.ResolutionCode = request.ResolutionCode;
        safetyCase.Resolution = request.Resolution!.Trim();
        safetyCase.ResolvedAt = now;
        safetyCase.MarkResponded(now);
        cases.AddNote(safetyCase.Id, currentUser.UserId, SafetyNoteKind.StatusChange, $"Resolved ({SafetyLabels.Snake(request.ResolutionCode!.Value)}): {safetyCase.Resolution}");
        if (safetyCase.ReporterUserId is { } reporter)
        {
            await NotifyReporterAsync(safetyCase, reporter, ct);
        }

        audit.Log("safety_case.resolve", EntityType, safetyCase.Id, before, Snapshot(safetyCase));
        return await SaveAndBuildAsync(safetyCase, lang, ct);
    }

    /// <summary>Manual case (e.g. after a phone call), <c>source = admin</c>.</summary>
    public async Task<AdminSafetyCaseDetailDto> CreateAsync(AdminCreateCaseRequest request, Language lang, CancellationToken ct)
    {
        new Validator()
            .Rule(nameof(request.Type), request.Type is null or SafetyCaseType.SafetyReport, "must be safety_report")
            .Require(nameof(request.Priority), request.Priority)
            .Require(nameof(request.Description), request.Description, 2000)
            .ThrowIfInvalid();
        if (request.TripId is { } tripId)
        {
            Guard.NotFound(await db.Trips.AsNoTracking().Select(t => new { t.Id }).FirstOrDefaultAsync(t => t.Id == tripId, ct));
        }

        if (request.SubjectUserId is { } subjectId)
        {
            new Validator().Rule(nameof(request.SubjectUserId), await db.Users.AsNoTracking().AnyAsync(u => u.Id == subjectId, ct), "unknown user").ThrowIfInvalid();
        }

        var safetyCase = new SafetyCase
        {
            CaseNumber = string.Empty, Type = SafetyCaseType.SafetyReport, Source = SafetyCaseSource.Admin, Priority = request.Priority!.Value, TripId = request.TripId,
            ReporterUserId = currentUser.UserId, ReporterRole = SafetyReporterRole.Admin, SubjectUserId = request.SubjectUserId, Description = request.Description!.Trim(),
        };
        audit.Log("safety_case.create", EntityType, safetyCase.Id, null, Snapshot(safetyCase));
        await cases.OpenAsync(safetyCase, "Created by operations", notifyOps: false, null, ct);
        await cases.PublishAsync(ct);
        return await BuildDetailAsync(safetyCase, lang, ct);
    }

    public async Task<PagedResult<AdminSafetyAlertDto>> AlertsAsync(SafetyAlertStatus? status, SafetyAlertType? type, Guid? tripId, DateOnly? from, DateOnly? to, string? search, Paging paging, CancellationToken ct)
    {
        var query = db.SafetyAlerts.AsNoTracking().AsQueryable();
        if (status is not null) query = query.Where(a => a.Status == status);
        if (type is not null) query = query.Where(a => a.Type == type);
        if (tripId is not null) query = query.Where(a => a.TripId == tripId);
        if (from is { } f)
        {
            var fromAt = f.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(a => a.DetectedAt >= fromAt);
        }

        if (to is { } t)
        {
            var toAt = t.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            query = query.Where(a => a.DetectedAt < toAt);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var tripIds = db.Trips.Where(tr => tr.TripNumber.Contains(term)).Select(tr => tr.Id);
            query = query.Where(a => tripIds.Contains(a.TripId));
        }

        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(a => a.DetectedAt).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        return paging.Result(await AlertDtosAsync(rows, ct), total);
    }

    public async Task<AdminSafetyAlertDto> DismissAlertAsync(Guid id, DismissAlertRequest request, CancellationToken ct)
    {
        new Validator().Require(nameof(request.Note), request.Note, 500).ThrowIfInvalid();
        var alert = Guard.NotFound(await db.SafetyAlerts.FirstOrDefaultAsync(a => a.Id == id, ct));
        var before = new { alert.Status };
        alert.Close(SafetyAlertStatus.Dismissed, clock.UtcNow);
        alert.DismissedBy = currentUser.UserId;
        audit.Log("safety_alert.dismiss", "safety_alert", alert.Id, before, new { alert.Status, note = request.Note!.Trim() });
        await db.SaveChangesAsync(ct);
        return (await AlertDtosAsync([alert], ct))[0];
    }

    /// <summary>Trusted contacts of a user, readable only while that user is party to an open case; every read is audited.</summary>
    public async Task<IReadOnlyList<TrustedContactDto>> TrustedContactsAsync(Guid userId, CancellationToken ct)
    {
        var openCase = await db.SafetyCases.AsNoTracking()
            .Where(c => c.Status != SafetyCaseStatus.Resolved && (c.ReporterUserId == userId || c.SubjectUserId == userId))
            .Select(c => (Guid?)c.Id).FirstOrDefaultAsync(ct);
        if (openCase is null)
        {
            var tripCase = await (from c in db.SafetyCases.AsNoTracking()
                                  join t in db.Trips.AsNoTracking() on c.TripId equals t.Id
                                  join p in db.Passengers.AsNoTracking() on t.PassengerId equals p.Id
                                  where c.Status != SafetyCaseStatus.Resolved && p.UserId == userId
                                  select (Guid?)c.Id).FirstOrDefaultAsync(ct);
            openCase = tripCase ?? throw new DomainException(ErrorCodes.Forbidden, new { reason = "no_open_case" });
        }

        var list = await contacts.ListForUserAsync(userId, ct);
        audit.Log("trusted_contacts.view", "user", userId, null, new { caseId = openCase, contacts = list.Count });
        await db.SaveChangesAsync(ct);
        return list;
    }

    public async Task<IReadOnlyList<TripShareDto>> TripSharesAsync(Guid tripId, CancellationToken ct)
    {
        Guard.NotFound(await db.Trips.AsNoTracking().Select(t => new { t.Id }).FirstOrDefaultAsync(t => t.Id == tripId, ct));
        return await shares.ListForTripAsync(tripId, ct);
    }

    private async Task NotifyReporterAsync(SafetyCase safetyCase, Guid reporter, CancellationToken ct)
    {
        var (ar, en) = SafetyLabels.Status(safetyCase.Status);
        await notifications.DispatchAsync(new NotificationRequest(NotificationTypes.SafetyCaseUpdate, reporter,
            NotificationPlaceholders.Of(("caseNumber", safetyCase.CaseNumber)).Localized("status", ar, en), "case", safetyCase.Id), ct);
    }

    private async Task<AdminSafetyCaseDetailDto> SaveAndBuildAsync(SafetyCase safetyCase, Language lang, CancellationToken ct)
    {
        cases.Touch(safetyCase.Id);
        await db.SaveChangesAsync(ct);
        await cases.PublishAsync(ct);
        return await BuildDetailAsync(safetyCase, lang, ct);
    }

    private async Task<SafetyCase> LoadOpenAsync(Guid id, CancellationToken ct)
    {
        var safetyCase = Guard.NotFound(await db.SafetyCases.FirstOrDefaultAsync(c => c.Id == id, ct));
        safetyCase.EnsureOpen();
        return safetyCase;
    }

    private async Task<AdminSafetyCaseDetailDto> BuildDetailAsync(SafetyCase c, Language lang, CancellationToken ct)
    {
        var item = (await cases.ToListItemsAsync([c], ct))[0];
        var userIds = new[] { c.ReporterUserId, c.SubjectUserId }.Where(x => x != null).Select(x => x!.Value).ToList();
        var users = await db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => new { u.FullName, u.PhoneNumber }, ct);

        SafetyCaseTripDto? trip = null;
        SafetyLiveLocationDto? live = null;
        var alerts = new List<AdminSafetyAlertDto>();
        var sharesCount = 0;
        if (c.TripId is { } tripId && await db.Trips.AsNoTracking().Include(t => t.Stops).FirstOrDefaultAsync(t => t.Id == tripId, ct) is { } row)
        {
            var category = await db.RideCategories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == row.RideCategoryId, ct);
            var passenger = await (from p in db.Passengers.AsNoTracking() join u in db.Users.AsNoTracking() on p.UserId equals u.Id where p.Id == row.PassengerId
                                   select new SafetyPartyDto(p.Id, u.Id, u.FullName, u.PhoneNumber)).FirstOrDefaultAsync(ct);
            var driver = row.DriverId is { } driverId
                ? await (from d in db.Drivers.AsNoTracking() join u in db.Users.AsNoTracking() on d.UserId equals u.Id where d.Id == driverId
                         select new SafetyPartyDto(d.Id, u.Id, u.FullName, u.PhoneNumber)).FirstOrDefaultAsync(ct)
                : null;
            var vehicle = row.VehicleId is { } vehicleId
                ? await db.Vehicles.AsNoTracking().Where(v => v.Id == vehicleId).Select(v => new TripVehicleDto(v.Make, v.Model, v.Color, v.PlateNumber)).FirstOrDefaultAsync(ct)
                : null;
            trip = new SafetyCaseTripDto(row.Id, row.TripNumber, row.Status,
                new PlaceDto(row.PickupName, row.PickupAddress, row.PickupLat, row.PickupLng), new PlaceDto(row.DropoffName, row.DropoffAddress, row.DropoffLat, row.DropoffLng),
                row.Stops.OrderBy(s => s.Sequence).Select(s => new PlaceDto(s.Name, s.Address, s.Lat, s.Lng)).ToList(), PlannedRoutes.Of(row, row.Stops),
                category is null ? null : new TripRideCategoryDto(category.Id, category.Code, lang.Pick(category.NameAr, category.NameEn)), passenger, driver, vehicle, row.StartedAt);
            if (row.DriverId is { } liveDriverId && !row.IsTerminal
                && await db.DriverLocations.AsNoTracking().FirstOrDefaultAsync(l => l.DriverId == liveDriverId, ct) is { } location)
            {
                live = new SafetyLiveLocationDto(location.Lat, location.Lng, location.Heading, location.UpdatedAt, "driver");
            }

            alerts = (await AlertDtosAsync(await db.SafetyAlerts.AsNoTracking().Where(a => a.TripId == tripId).OrderByDescending(a => a.DetectedAt).ToListAsync(ct), ct)).ToList();
            sharesCount = await db.TripShares.CountAsync(s => s.TripId == tripId, ct);
        }

        if (live is null && c.LastLat is { } lastLat && c.LastLng is { } lastLng)
        {
            live = new SafetyLiveLocationDto(lastLat, lastLng, null, c.LastLocationAt, "reporter");
        }

        var noteRows = await db.SafetyCaseNotes.AsNoTracking().Where(n => n.CaseId == c.Id).OrderBy(n => n.CreatedAt).ThenBy(n => n.Id).ToListAsync(ct);
        var attachmentRows = await (from a in db.SafetyCaseAttachments.AsNoTracking()
                                    join f in db.StoredFiles.AsNoTracking() on a.FileId equals f.Id
                                    where a.CaseId == c.Id
                                    orderby a.CreatedAt
                                    select new { a, f.OriginalName }).ToListAsync(ct);
        var authorIds = noteRows.Where(n => n.AuthorUserId != null).Select(n => n.AuthorUserId!.Value).Concat(attachmentRows.Select(x => x.a.UploadedBy)).Distinct().ToList();
        var authors = await db.Users.AsNoTracking().Where(u => authorIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.PhoneNumber, ct);

        return new AdminSafetyCaseDetailDto(
            item.Id, item.CaseNumber, item.Type, item.Source, item.Priority, item.Status, item.TripId, item.TripNumber, item.ReporterName, item.ReporterRole, item.AssignedToName,
            item.OpenedAt, item.FirstResponseAt, item.AgeSeconds,
            c.ReporterUserId, c.ReporterUserId is { } r ? users.GetValueOrDefault(r)?.PhoneNumber : null,
            c.SubjectUserId, c.SubjectUserId is { } s ? users.GetValueOrDefault(s)?.FullName : null, c.ReportCategory, c.Description,
            c.Lat, c.Lng, c.LastLat, c.LastLng, c.LastLocationAt, c.ContactsNotified, c.AssignedToUserId, c.AssignedAt, c.EscalatedTo, c.ResolutionCode, c.Resolution,
            c.ReporterCancelledAt, c.SupportTicketId, c.ResolvedAt, trip, live, alerts,
            noteRows.Select(n => new SafetyCaseNoteDto(n.Id, n.Kind, n.Body, n.IsInternal, n.AuthorUserId, n.AuthorUserId is { } a ? authors.GetValueOrDefault(a) : null, n.CreatedAt)).ToList(),
            attachmentRows.Select(x => new SafetyCaseAttachmentDto(x.a.Id, x.a.FileId, x.OriginalName, authors.GetValueOrDefault(x.a.UploadedBy), x.a.CreatedAt)).ToList(),
            sharesCount, c.ContactsNotified);
    }

    private async Task<IReadOnlyList<AdminSafetyAlertDto>> AlertDtosAsync(IReadOnlyList<SafetyAlert> rows, CancellationToken ct)
    {
        var tripIds = rows.Select(a => a.TripId).Distinct().ToList();
        var caseIds = rows.Where(a => a.SafetyCaseId != null).Select(a => a.SafetyCaseId!.Value).Distinct().ToList();
        var dismissers = rows.Where(a => a.DismissedBy != null).Select(a => a.DismissedBy!.Value).Distinct().ToList();
        var numbers = await db.Trips.AsNoTracking().Where(t => tripIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.TripNumber, ct);
        var caseNumbers = await db.SafetyCases.AsNoTracking().Where(c => caseIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.CaseNumber, ct);
        var names = await db.Users.AsNoTracking().Where(u => dismissers.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.PhoneNumber, ct);
        return rows.Select(a => new AdminSafetyAlertDto(a.Id, a.TripId, numbers.GetValueOrDefault(a.TripId), a.Type, a.Status, a.DetectedAt, a.Lat, a.Lng, SafetyJson.Metrics(a.Metrics),
            a.PromptedAt, a.RespondBy, a.RespondedAt, a.Response, a.SafetyCaseId, a.SafetyCaseId is { } cid ? caseNumbers.GetValueOrDefault(cid) : null,
            a.DismissedBy is { } d ? names.GetValueOrDefault(d) : null, a.CreatedAt)).ToList();
    }

    private static object Snapshot(SafetyCase c) => new { c.Status, c.Priority, c.AssignedToUserId, c.EscalatedTo, c.ResolutionCode, c.FirstResponseAt };
}
