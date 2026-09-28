using System.Globalization;
using ATA.Api.Common;
using ATA.Api.Modules.Notifications;
using ATA.Domain.Common;
using ATA.Domain.Notifications;
using ATA.Domain.Safety;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using ATA.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Safety;

/// <summary>The trip parties of a trip resolved to user ids.</summary>
public sealed record TripParties(Trip Trip, Guid PassengerUserId, Guid? DriverUserId)
{
    public SafetyReporterRole? RoleOf(Guid userId) =>
        userId == PassengerUserId ? SafetyReporterRole.Passenger : DriverUserId == userId ? SafetyReporterRole.Driver : null;

    public Guid? OtherParty(Guid userId) => userId == PassengerUserId ? DriverUserId : userId == DriverUserId ? PassengerUserId : null;
}

/// <summary>Rider/driver safety features (doc 09 §F12.3, §F12.5, §F12.6): SOS, safety reports, own cases and the "are you OK?" answers.</summary>
public sealed class SafetyService(
    AtaDbContext db,
    ICurrentUser currentUser,
    IClock clock,
    SafetyCaseFactory cases,
    TripShareService shares,
    INotificationDispatcher notifications,
    IOptions<SafetyOptions> options)
{
    private readonly SafetyOptions _options = options.Value;

    public async Task<(SosResponse Response, bool Created)> SosAsync(SosRequest request, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.Lat), request.Lat)
            .Require(nameof(request.Lng), request.Lng)
            .Rule(nameof(request.Lat), request.Lat is null or (>= -90 and <= 90), "out of range")
            .Rule(nameof(request.Lng), request.Lng is null or (>= -180 and <= 180), "out of range")
            .Rule(nameof(request.Note), request.Note is null || request.Note.Length <= 2000, "max_length:2000")
            .Rule(nameof(request.Role), request.Role is null or RoleNames.Passenger or RoleNames.Driver, "must be passenger|driver")
            .ThrowIfInvalid();

        var userId = currentUser.UserId;
        var now = clock.UtcNow;
        TripParties? parties = null;
        SafetyReporterRole role;
        if (request.TripId is { } tripId)
        {
            parties = await PartiesAsync(tripId, ct) ?? throw new DomainException(ErrorCodes.NotFound);
            role = parties.RoleOf(userId) ?? throw new DomainException(ErrorCodes.NotFound);
        }
        else
        {
            role = ResolveRole(request.Role);
        }

        var dedupAfter = now.AddMinutes(-_options.SosDedupMinutes);
        var tripKey = parties?.Trip.Id;
        var duplicate = await db.SafetyCases.FirstOrDefaultAsync(c => c.Type == SafetyCaseType.Sos && c.ReporterUserId == userId && c.TripId == tripKey
                                                                     && c.Status != SafetyCaseStatus.Resolved && c.OpenedAt >= dedupAfter, ct);
        if (duplicate is not null)
        {
            cases.AddNote(duplicate.Id, null, SafetyNoteKind.System, "ضغط متكرر على زر الطوارئ / repeated SOS press");
            duplicate.LastLat = request.Lat;
            duplicate.LastLng = request.Lng;
            duplicate.LastLocationAt = now;
            cases.Touch(duplicate.Id);
            await db.SaveChangesAsync(ct);
            await cases.PublishAsync(ct);
            return (new SosResponse(duplicate.Id, duplicate.CaseNumber, duplicate.Status, _options.EmergencyNumber, duplicate.ContactsNotified), false);
        }

        var userName = await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.FullName ?? u.PhoneNumber).FirstOrDefaultAsync(ct);
        var safetyCase = new SafetyCase
        {
            CaseNumber = string.Empty,
            Type = SafetyCaseType.Sos,
            Source = role == SafetyReporterRole.Driver ? SafetyCaseSource.DriverSos : SafetyCaseSource.RiderSos,
            Priority = SafetyPriority.Critical,
            TripId = tripKey,
            ReporterUserId = userId,
            ReporterRole = role,
            Description = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            Lat = request.Lat,
            Lng = request.Lng,
            LastLat = request.Lat,
            LastLng = request.Lng,
            LastLocationAt = now,
            OpenedAt = now,
        };

        if (request.NotifyTrustedContacts ?? true)
        {
            var contacts = await db.TrustedContacts.AsNoTracking().Where(c => c.UserId == userId && c.NotifyOnSos).OrderBy(c => c.CreatedAt).ToListAsync(ct);
            var mapsUrl = string.Create(CultureInfo.InvariantCulture, $"https://maps.google.com/?q={request.Lat},{request.Lng}");
            foreach (var contact in contacts)
            {
                string? shareUrl = null;
                if (parties is { Trip.IsTerminal: false })
                {
                    shareUrl = shares.UrlOf(shares.Add(parties.Trip.Id, userId, contact.Id, TripShareChannel.Sms).Token);
                }

                await notifications.DispatchAsync(new NotificationRequest(NotificationTypes.SafetySosContact, Guid.Empty,
                    NotificationPlaceholders.Of(("userName", userName ?? string.Empty), ("mapsUrl", mapsUrl), ("shareUrl", shareUrl ?? mapsUrl)),
                    "case", safetyCase.Id, RecipientPhoneOverride: contact.PhoneNumber), ct);
            }

            safetyCase.ContactsNotified = (byte)Math.Min(contacts.Count, byte.MaxValue);
        }

        await cases.OpenAsync(safetyCase, $"SOS ({SafetyLabels.Snake(role)}) · accuracy {request.Accuracy?.ToString(CultureInfo.InvariantCulture) ?? "-"} m", notifyOps: true, userName, ct);
        await cases.PublishAsync(ct);
        return (new SosResponse(safetyCase.Id, safetyCase.CaseNumber, safetyCase.Status, _options.EmergencyNumber, safetyCase.ContactsNotified), true);
    }

    public async Task UpdateSosLocationAsync(Guid caseId, SosLocationRequest request, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.Lat), request.Lat)
            .Require(nameof(request.Lng), request.Lng)
            .Rule(nameof(request.Lat), request.Lat is null or (>= -90 and <= 90), "out of range")
            .Rule(nameof(request.Lng), request.Lng is null or (>= -180 and <= 180), "out of range")
            .ThrowIfInvalid();
        var safetyCase = await LoadOwnAsync(caseId, ct);
        safetyCase.EnsureOpen();
        safetyCase.LastLat = request.Lat;
        safetyCase.LastLng = request.Lng;
        safetyCase.LastLocationAt = clock.UtcNow;
        cases.Touch(safetyCase.Id);
        await db.SaveChangesAsync(ct);
        await cases.PublishAsync(ct);
    }

    public async Task<SafetyCaseSummaryDto> CancelSosAsync(Guid caseId, SosCancelRequest request, CancellationToken ct)
    {
        new Validator().Rule(nameof(request.Reason), request.Reason is "accidental" or "resolved", "must be accidental|resolved").ThrowIfInvalid();
        var safetyCase = await LoadOwnAsync(caseId, ct);
        safetyCase.EnsureOpen();
        if (safetyCase.ReporterCancelledAt is null)
        {
            safetyCase.ReporterCancelledAt = clock.UtcNow;
            if (safetyCase.Priority == SafetyPriority.Critical)
            {
                safetyCase.Priority = SafetyPriority.High;
            }

            cases.AddNote(safetyCase.Id, currentUser.UserId, SafetyNoteKind.System,
                request.Reason == "accidental" ? "ألغى المُبلِّغ البلاغ (ضغط بالخطأ) / reporter cancelled: accidental" : "ألغى المُبلِّغ البلاغ (تم الحل) / reporter cancelled: resolved");
            cases.Touch(safetyCase.Id);
            await db.SaveChangesAsync(ct);
            await cases.PublishAsync(ct);
        }

        return (await SummariesAsync([safetyCase], ct))[0];
    }

    public async Task<SafetyCaseSummaryDto> ReportAsync(SafetyReportRequest request, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.TripId), request.TripId)
            .Require(nameof(request.Category), request.Category)
            .Require(nameof(request.Description), request.Description, 2000)
            .Rule(nameof(request.FileIds), request.FileIds is null || request.FileIds.Count <= 10, "max:10")
            .ThrowIfInvalid();
        var userId = currentUser.UserId;
        var parties = await PartiesAsync(request.TripId!.Value, ct);
        var role = parties?.RoleOf(userId) ?? throw new DomainException(ErrorCodes.NotFound);
        var trip = parties.Trip;
        var reference = trip.CompletedAt ?? trip.CancelledAt ?? trip.RequestedAt;
        if (clock.UtcNow > reference.AddDays(_options.ReportWindowDays))
        {
            throw new DomainException(ErrorCodes.ValidationFailed, new Dictionary<string, string> { ["tripId"] = "window_closed" });
        }

        var fileIds = request.FileIds?.Distinct().ToList() ?? [];
        if (fileIds.Count > 0)
        {
            var owned = await db.StoredFiles.AsNoTracking().CountAsync(f => fileIds.Contains(f.Id) && f.OwnerUserId == userId, ct);
            new Validator().Rule(nameof(request.FileIds), owned == fileIds.Count, "unknown file").ThrowIfInvalid();
        }

        var safetyCase = new SafetyCase
        {
            CaseNumber = string.Empty,
            Type = SafetyCaseType.SafetyReport,
            Source = SafetyCaseSource.Report,
            Priority = request.Category == SafetyReportCategory.Harassment ? SafetyPriority.High : SafetyPriority.Medium,
            TripId = trip.Id,
            ReporterUserId = userId,
            ReporterRole = role,
            SubjectUserId = parties.OtherParty(userId),
            ReportCategory = request.Category,
            Description = request.Description!.Trim(),
        };
        foreach (var fileId in fileIds)
        {
            db.SafetyCaseAttachments.Add(new SafetyCaseAttachment { CaseId = safetyCase.Id, FileId = fileId, UploadedBy = userId });
        }

        await cases.OpenAsync(safetyCase, $"Safety report: {SafetyLabels.Snake(request.Category!.Value)}", notifyOps: false, null, ct);
        await cases.PublishAsync(ct);
        return (await SummariesAsync([safetyCase], ct))[0];
    }

    public async Task<PagedResult<SafetyCaseSummaryDto>> ListMineAsync(Paging paging, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var query = db.SafetyCases.AsNoTracking().Where(c => c.ReporterUserId == userId);
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(c => c.OpenedAt).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        return paging.Result(await SummariesAsync(rows, ct), total);
    }

    public async Task<SafetyCaseSummaryDto> GetMineAsync(Guid caseId, CancellationToken ct) => (await SummariesAsync([await LoadOwnAsync(caseId, ct)], ct))[0];

    /// <summary>The newest <c>pending_rider</c> alert on one of the passenger's trips, or <c>null</c>.</summary>
    public async Task<SafetyAlertDto?> PendingAlertAsync(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var alert = await (from a in db.SafetyAlerts.AsNoTracking()
                           join t in db.Trips.AsNoTracking() on a.TripId equals t.Id
                           join p in db.Passengers.AsNoTracking() on t.PassengerId equals p.Id
                           where p.UserId == userId && a.Status == SafetyAlertStatus.PendingRider
                           orderby a.DetectedAt descending
                           select a).FirstOrDefaultAsync(ct);
        return alert is null ? null : ToDto(alert);
    }

    public async Task<SafetyAlertDto> RespondAlertAsync(Guid alertId, AlertRespondRequest request, CancellationToken ct)
    {
        new Validator().Require(nameof(request.Response), request.Response).ThrowIfInvalid();
        var userId = currentUser.UserId;
        var alert = await db.SafetyAlerts.FirstOrDefaultAsync(a => a.Id == alertId, ct) ?? throw new DomainException(ErrorCodes.NotFound);
        var parties = await PartiesAsync(alert.TripId, ct);
        if (parties is null || parties.PassengerUserId != userId)
        {
            throw new DomainException(ErrorCodes.NotFound);
        }

        var now = clock.UtcNow;
        alert.Close(request.Response == SafetyAlertResponse.Ok ? SafetyAlertStatus.ResolvedOk : SafetyAlertStatus.Escalated, now);
        alert.Response = request.Response;
        alert.RespondedAt = now;
        if (request.Response == SafetyAlertResponse.NeedHelp)
        {
            var userName = await db.Users.AsNoTracking().Where(u => u.Id == userId).Select(u => u.FullName ?? u.PhoneNumber).FirstOrDefaultAsync(ct);
            var safetyCase = await cases.AddAsync(SafetyMonitor.CaseFor(alert, userId, SafetyReporterRole.Passenger, request.Lat ?? alert.Lat, request.Lng ?? alert.Lng, now),
                $"The passenger asked for help after a {SafetyLabels.Snake(alert.Type)} alert", notifyOps: true, userName, ct);
            alert.SafetyCaseId = safetyCase.Id;
        }

        await db.SaveChangesAsync(ct);
        await cases.PublishAsync(ct);
        return ToDto(alert);
    }

    public async Task<TripParties?> PartiesAsync(Guid tripId, CancellationToken ct)
    {
        var row = await (from t in db.Trips
                         join p in db.Passengers on t.PassengerId equals p.Id
                         where t.Id == tripId
                         select new { Trip = t, p.UserId }).FirstOrDefaultAsync(ct);
        if (row is null)
        {
            return null;
        }

        Guid? driverUserId = row.Trip.DriverId is { } driverId
            ? await db.Drivers.AsNoTracking().Where(d => d.Id == driverId).Select(d => (Guid?)d.UserId).FirstOrDefaultAsync(ct)
            : null;
        return new TripParties(row.Trip, row.UserId, driverUserId);
    }

    public async Task<IReadOnlyList<SafetyCaseSummaryDto>> SummariesAsync(IReadOnlyList<SafetyCase> rows, CancellationToken ct)
    {
        var ids = rows.Select(c => c.Id).ToList();
        var tripIds = rows.Where(c => c.TripId != null).Select(c => c.TripId!.Value).ToList();
        var numbers = await db.Trips.AsNoTracking().Where(t => tripIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.TripNumber, ct);
        var notes = await db.SafetyCaseNotes.AsNoTracking().Where(n => ids.Contains(n.CaseId) && !n.IsInternal).OrderBy(n => n.CreatedAt).ToListAsync(ct);
        return rows.Select(c => new SafetyCaseSummaryDto(c.Id, c.CaseNumber, c.Type, c.Status, c.Priority, c.TripId, c.TripId is { } t ? numbers.GetValueOrDefault(t) : null,
            c.OpenedAt, c.ResolvedAt, notes.Where(n => n.CaseId == c.Id).Select(n => new PublicNoteDto(n.Body, n.CreatedAt)).ToList())).ToList();
    }

    public static SafetyAlertDto ToDto(SafetyAlert a) => new(a.Id, a.TripId, a.Type, a.Status, a.DetectedAt, a.RespondBy);

    private SafetyReporterRole ResolveRole(string? requested)
    {
        if (requested == RoleNames.Driver && currentUser.HasRole(RoleNames.Driver)) return SafetyReporterRole.Driver;
        if (requested == RoleNames.Passenger && currentUser.HasRole(RoleNames.Passenger)) return SafetyReporterRole.Passenger;
        if (requested is not null) throw new DomainException(ErrorCodes.ValidationFailed, new Dictionary<string, string> { ["role"] = "not_granted" });
        var isDriver = currentUser.HasRole(RoleNames.Driver);
        var isPassenger = currentUser.HasRole(RoleNames.Passenger);
        return (isDriver, isPassenger) switch
        {
            (true, false) => SafetyReporterRole.Driver,
            (false, true) => SafetyReporterRole.Passenger,
            _ => throw new DomainException(ErrorCodes.ValidationFailed, new Dictionary<string, string> { ["role"] = "required" }),
        };
    }

    private async Task<SafetyCase> LoadOwnAsync(Guid caseId, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        return await db.SafetyCases.FirstOrDefaultAsync(c => c.Id == caseId && c.ReporterUserId == userId, ct) ?? throw new DomainException(ErrorCodes.NotFound);
    }
}
