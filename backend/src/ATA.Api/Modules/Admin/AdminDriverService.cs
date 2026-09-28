using System.Text.Json;
using ATA.Api.Common;
using ATA.Api.Modules.Drivers;
using ATA.Api.Modules.Identity;
using ATA.Api.Modules.Notifications;
using ATA.Domain.Common;
using ATA.Domain.Drivers;
using ATA.Domain.Notifications;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Admin;

/// <summary>Driver application review by admins/operations: every transition is audited and notifies the driver.</summary>
public sealed class AdminDriverService(AtaDbContext db, DriverApplicationService applications, AuditService audit, NotificationService notifications, ICurrentUser currentUser, IClock clock)
{
    public const string EntityType = "driver";

    public async Task<PagedResult<AdminDriverListItemDto>> ListAsync(ApplicationStatus? status, string? search, Paging paging, CancellationToken ct)
    {
        var query = from d in db.Drivers.AsNoTracking()
                    join u in db.Users.AsNoTracking() on d.UserId equals u.Id
                    select new { Driver = d, User = u };

        if (status is not null)
        {
            query = query.Where(x => x.Driver.ApplicationStatus == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var phoneTerm = PhoneNumber.TryNormalize(term, out var normalized) ? normalized : term;
            query = query.Where(x => x.Driver.ApplicationNumber.Contains(term) || x.User.PhoneNumber.Contains(phoneTerm) || (x.User.FullName != null && x.User.FullName.Contains(term)));
        }

        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.Driver.SubmittedAt ?? x.Driver.CreatedAt).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        var driverIds = rows.Select(r => r.Driver.Id).ToList();
        var cityIds = rows.Where(r => r.Driver.CityId != null).Select(r => r.Driver.CityId!.Value).Distinct().ToList();
        var cities = await db.Cities.AsNoTracking().Where(c => cityIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, ct);
        var vehicles = await db.Vehicles.AsNoTracking().Where(v => driverIds.Contains(v.DriverId) && v.IsActive).ToListAsync(ct);
        var pending = await db.DriverDocuments.AsNoTracking().Where(d => driverIds.Contains(d.DriverId) && d.Status == DocumentStatus.Pending)
            .GroupBy(d => d.DriverId).Select(g => new { DriverId = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.DriverId, x => x.Count, ct);

        var items = rows.Select(r =>
        {
            var vehicle = vehicles.FirstOrDefault(v => v.DriverId == r.Driver.Id);
            var city = r.Driver.CityId is { } cityId && cities.TryGetValue(cityId, out var c) ? c.NameAr : null;
            return new AdminDriverListItemDto(
                r.Driver.Id, r.Driver.ApplicationNumber, r.User.FullName, r.User.PhoneNumber, r.Driver.ApplicationStatus, city,
                vehicle is null ? null : $"{vehicle.Make} {vehicle.Model} {vehicle.Year} · {vehicle.PlateNumber}",
                r.Driver.SubmittedAt, pending.GetValueOrDefault(r.Driver.Id));
        }).ToList();
        return paging.Result(items, total);
    }

    public async Task<AdminDriverDetailDto> GetAsync(Guid driverId, Language lang, CancellationToken ct)
    {
        var application = await applications.BuildApplicationAsync(driverId, lang, ct);
        var userId = await db.Drivers.AsNoTracking().Where(d => d.Id == driverId).Select(d => d.UserId).FirstAsync(ct);
        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Id == userId, ct);
        var history = await db.AuditLogs.AsNoTracking()
            .Where(a => a.EntityType == EntityType && a.EntityId == driverId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync(ct);

        var actorIds = history.Where(a => a.ActorUserId is not null).Select(a => a.ActorUserId!.Value).Distinct().ToList();
        var actorNames = await db.Users.AsNoTracking()
            .Where(u => actorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        var statusHistory = history.Select(a =>
        {
            var before = ReadStatus(a.BeforeJson);
            var after = ReadStatus(a.AfterJson);
            var reason = ReadReason(a.AfterJson);
            var actorName = a.ActorUserId is { } actorId && actorNames.TryGetValue(actorId, out var name) ? name : null;
            return new StatusHistoryEntryDto(a.Id, a.Action, before, after, actorName, reason, a.CreatedAt);
        }).ToList();

        return new AdminDriverDetailDto(
            driverId, application.ApplicationNumber, application.Status, application.RejectionReason, application.SubmittedAt, application.ApprovedAt,
            application.Profile, application.Vehicle, application.Documents, application.RequiredDocuments, application.Steps,
            UserDto.From(user), statusHistory);
    }

    public async Task<DriverStatusChangeDto> StartReviewAsync(Guid driverId, ReviewRequest request, CancellationToken ct)
    {
        new Validator().Rule(nameof(request.Action), request.Action == "start_review", "must be start_review").ThrowIfInvalid();
        var driver = await LoadAsync(driverId, ct);
        var before = Snapshot(driver);
        driver.StartReview();
        audit.Log("driver.start_review", EntityType, driver.Id, before, Snapshot(driver));
        notifications.Add(driver.UserId, NotificationTypes.DriverUnderReview,
            ("طلبك قيد المراجعة", "Your application is under review"),
            ($"بدأت الإدارة مراجعة طلبك رقم {driver.ApplicationNumber}.", $"Our team started reviewing application {driver.ApplicationNumber}."),
            new { driver.ApplicationNumber, status = driver.ApplicationStatus });
        await db.SaveChangesAsync(ct);
        return new DriverStatusChangeDto(driver.Id, driver.ApplicationStatus);
    }

    public async Task<DriverStatusChangeDto> ApproveAsync(Guid driverId, CancellationToken ct)
    {
        var driver = await LoadAsync(driverId, ct);
        var missing = await applications.MissingVerifiedDocumentsAsync(driver.Id, ct);
        if (missing.Count > 0)
        {
            throw new DomainException(ErrorCodes.ValidationFailed, new { missing = missing.Select(code => $"documents:{code}").ToList() });
        }

        var before = Snapshot(driver);
        driver.Approve(currentUser.UserId, clock.UtcNow);
        audit.Log("driver.approve", EntityType, driver.Id, before, Snapshot(driver));
        notifications.Add(driver.UserId, NotificationTypes.DriverApplicationApproved,
            ("تم تفعيل حسابك", "Your account is activated"),
            ($"تم اعتماد طلبك رقم {driver.ApplicationNumber}. يمكنك الآن استقبال الرحلات.", $"Application {driver.ApplicationNumber} was approved. You can now go online."),
            new { driver.ApplicationNumber, status = driver.ApplicationStatus });
        await db.SaveChangesAsync(ct);
        return new DriverStatusChangeDto(driver.Id, driver.ApplicationStatus);
    }

    public async Task<DriverStatusChangeDto> RejectAsync(Guid driverId, ReasonRequest request, CancellationToken ct)
    {
        var reason = RequireReason(request);
        var driver = await LoadAsync(driverId, ct);
        var before = Snapshot(driver);
        driver.Reject(reason);
        audit.Log("driver.reject", EntityType, driver.Id, before, Snapshot(driver));
        notifications.Add(driver.UserId, NotificationTypes.DriverApplicationRejected,
            ("تم رفض طلبك", "Your application was rejected"),
            ($"تم رفض طلبك رقم {driver.ApplicationNumber}: {reason}", $"Application {driver.ApplicationNumber} was rejected: {reason}"),
            new { driver.ApplicationNumber, status = driver.ApplicationStatus, reason });
        await db.SaveChangesAsync(ct);
        return new DriverStatusChangeDto(driver.Id, driver.ApplicationStatus);
    }

    public async Task<DriverStatusChangeDto> SuspendAsync(Guid driverId, ReasonRequest request, CancellationToken ct)
    {
        var reason = RequireReason(request);
        var driver = await LoadAsync(driverId, ct);
        var before = Snapshot(driver);
        driver.Suspend(reason);
        audit.Log("driver.suspend", EntityType, driver.Id, before, Snapshot(driver));
        notifications.Add(driver.UserId, NotificationTypes.DriverSuspended,
            ("تم تعليق حسابك", "Your account was suspended"),
            ($"تم تعليق حساب السائق: {reason}", $"Your driver account was suspended: {reason}"),
            new { driver.ApplicationNumber, status = driver.ApplicationStatus, reason });
        await db.SaveChangesAsync(ct);
        return new DriverStatusChangeDto(driver.Id, driver.ApplicationStatus);
    }

    public async Task<DriverStatusChangeDto> ReinstateAsync(Guid driverId, CancellationToken ct)
    {
        var driver = await LoadAsync(driverId, ct);
        var before = Snapshot(driver);
        driver.Reinstate();
        audit.Log("driver.reinstate", EntityType, driver.Id, before, Snapshot(driver));
        notifications.Add(driver.UserId, NotificationTypes.DriverReinstated,
            ("تمت إعادة تفعيل حسابك", "Your account was reinstated"),
            ("يمكنك الآن استقبال الرحلات مجدداً.", "You can go online and receive trips again."),
            new { driver.ApplicationNumber, status = driver.ApplicationStatus });
        await db.SaveChangesAsync(ct);
        return new DriverStatusChangeDto(driver.Id, driver.ApplicationStatus);
    }

    public async Task<DriverDocumentDto> VerifyDocumentAsync(Guid documentId, VerifyDocumentRequest request, Language lang, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.Status), request.Status)
            .Rule(nameof(request.Status), request.Status is null or DocumentStatus.Verified or DocumentStatus.Rejected, "must be verified or rejected")
            .Rule(nameof(request.Note), request.Note is null || request.Note.Length <= 1000, "max_length:1000")
            .ThrowIfInvalid();

        var document = Guard.NotFound(await db.DriverDocuments.FirstOrDefaultAsync(d => d.Id == documentId, ct));
        var before = new { document.Status, document.ReviewNote };
        document.Review(request.Status!.Value, request.Note?.Trim(), currentUser.UserId, clock.UtcNow);
        audit.Log("document.verify", "driver_document", document.Id, before, new { document.Status, document.ReviewNote, driverId = document.DriverId });
        await db.SaveChangesAsync(ct);
        return await applications.GetDocumentDtoAsync(document.Id, lang, ct);
    }

    private async Task<DriverProfile> LoadAsync(Guid driverId, CancellationToken ct) =>
        Guard.NotFound(await db.Drivers.FirstOrDefaultAsync(d => d.Id == driverId, ct));

    private static string RequireReason(ReasonRequest request)
    {
        new Validator().Require(nameof(request.Reason), request.Reason, 1000).ThrowIfInvalid();
        return request.Reason!.Trim();
    }

    private static object Snapshot(DriverProfile driver) => new
    {
        status = driver.ApplicationStatus,
        reason = driver.RejectionReason,
        driver.ApprovedAt,
        driver.ApprovedBy,
        driver.IsOnline,
    };

    private static ApplicationStatus? ReadStatus(string? json)
    {
        if (json is null) return null;
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("status", out var status) && status.ValueKind == JsonValueKind.String
            && Enum.TryParse<ApplicationStatus>(status.GetString()!.Replace("_", string.Empty), ignoreCase: true, out var parsed)
            ? parsed
            : null;
    }

    private static string? ReadReason(string? json)
    {
        if (json is null) return null;
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("reason", out var reason) && reason.ValueKind == JsonValueKind.String ? reason.GetString() : null;
    }
}
