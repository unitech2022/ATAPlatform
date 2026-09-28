using ATA.Api.Common;
using ATA.Domain.Common;
using ATA.Domain.Drivers;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Drivers;

public static class DriverEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/driver").WithTags("Driver").RequireAuthorization(Policies.Driver);

        group.MapGet("/application", async (DriverApplicationService service, HttpContext http, CancellationToken ct) =>
            {
                var driver = await service.LoadOwnAsync(ct);
                return Results.Ok(await service.BuildApplicationAsync(driver.Id, http.GetLanguage(), ct));
            })
            .Produces<DriverApplicationDto>();

        group.MapPut("/application/profile", async (UpdateDriverProfileRequest request, DriverApplicationService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.UpdateProfileAsync(request, http.GetLanguage(), ct)))
            .Produces<DriverApplicationDto>();

        group.MapPut("/application/vehicle", async (UpsertVehicleRequest request, DriverApplicationService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.UpsertVehicleAsync(request, http.GetLanguage(), ct)))
            .Produces<DriverApplicationDto>();

        group.MapPost("/documents", async (HttpRequest request, DriverApplicationService service, HttpContext http, CancellationToken ct) =>
            {
                if (!request.HasFormContentType)
                {
                    throw new DomainException(ErrorCodes.ValidationFailed, new { body = "multipart/form-data expected" });
                }

                var form = await request.ReadFormAsync(ct);
                Guid? typeId = Guid.TryParse(form["documentTypeId"], out var parsedType) ? parsedType : null;
                DateOnly? expiresAt = DateOnly.TryParse(form["expiresAt"], System.Globalization.CultureInfo.InvariantCulture, out var parsedDate) ? parsedDate : null;
                if (!string.IsNullOrEmpty(form["expiresAt"]) && expiresAt is null)
                {
                    throw new DomainException(ErrorCodes.ValidationFailed, new { expiresAt = "must be a date (yyyy-MM-dd)" });
                }

                var dto = await service.UploadDocumentAsync(typeId, form.Files.GetFile("file"), expiresAt, http.GetLanguage(), ct);
                return Results.Created($"/api/v1/driver/application", dto);
            })
            .DisableAntiforgery()
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<DriverDocumentDto>(StatusCodes.Status201Created);

        group.MapDelete("/documents/{id:guid}", async (Guid id, DriverApplicationService service, CancellationToken ct) =>
            {
                await service.DeleteDocumentAsync(id, ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);

        group.MapPost("/application/submit", async (DriverApplicationService service, CancellationToken ct) =>
                Results.Ok(await service.SubmitAsync(ct)))
            .Produces<SubmitResponse>()
            .Produces<ErrorEnvelope>(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/status", async (DriverStatusService service, CancellationToken ct) => Results.Ok(await service.GetStatusAsync(ct)))
            .Produces<DriverStatusDto>();

        group.MapPut("/status", async (UpdateDriverStatusRequest request, DriverStatusService service, CancellationToken ct) =>
                Results.Ok(await service.UpdateStatusAsync(request, ct)))
            .Produces<DriverStatusDto>()
            .Produces<ErrorEnvelope>(StatusCodes.Status403Forbidden);

        group.MapGet("/earnings/summary", async (DriverStatusService service, CancellationToken ct) => Results.Ok(await service.GetEarningsSummaryAsync(ct)))
            .Produces<EarningsSummaryDto>();

        group.MapGet("/trips", (int? page, int? pageSize) => Results.Ok(Paging.From(page, pageSize).Result<DriverTripDto>([], 0)))
            .Produces<PagedResult<DriverTripDto>>();
    }
}

/// <summary>Online/offline state with status logs, and the step-1 earnings summary (online hours only).</summary>
public sealed class DriverStatusService(AtaDbContext db, ICurrentUser currentUser, IClock clock)
{
    public const decimal WeeklyTarget = 2500m;

    public async Task<DriverStatusDto> GetStatusAsync(CancellationToken ct)
    {
        var driver = await LoadAsync(ct);
        return ToDto(driver);
    }

    public async Task<DriverStatusDto> UpdateStatusAsync(UpdateDriverStatusRequest request, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.IsOnline), request.IsOnline)
            .Rule(nameof(request.Latitude), request.Latitude is null or (>= -90 and <= 90), "out of range")
            .Rule(nameof(request.Longitude), request.Longitude is null or (>= -180 and <= 180), "out of range")
            .ThrowIfInvalid();

        var driver = await LoadAsync(ct);
        if (driver.ApplicationStatus != ApplicationStatus.Approved)
        {
            throw new DomainException(ErrorCodes.DriverNotApproved, new { status = driver.ApplicationStatus });
        }

        var isOnline = request.IsOnline!.Value;
        if (driver.IsOnline != isOnline)
        {
            var now = clock.UtcNow;
            driver.IsOnline = isOnline;
            driver.LastOnlineAt = now;
            db.DriverStatusLogs.Add(new DriverStatusLog
            {
                DriverId = driver.Id, IsOnline = isOnline, ChangedAt = now, Latitude = request.Latitude, Longitude = request.Longitude,
            });
            await db.SaveChangesAsync(ct);
        }

        return ToDto(driver);
    }

    public async Task<EarningsSummaryDto> GetEarningsSummaryAsync(CancellationToken ct)
    {
        var driver = await LoadAsync(ct);
        var now = clock.UtcNow;
        var dayStart = now.Date;
        var logs = await db.DriverStatusLogs.AsNoTracking()
            .Where(l => l.DriverId == driver.Id && l.ChangedAt >= dayStart)
            .OrderBy(l => l.ChangedAt)
            .Select(l => new { l.IsOnline, l.ChangedAt })
            .ToListAsync(ct);
        var wasOnlineAtMidnight = await db.DriverStatusLogs.AsNoTracking()
            .Where(l => l.DriverId == driver.Id && l.ChangedAt < dayStart)
            .OrderByDescending(l => l.ChangedAt)
            .Select(l => (bool?)l.IsOnline)
            .FirstOrDefaultAsync(ct) ?? false;

        var online = TimeSpan.Zero;
        var cursorOnline = wasOnlineAtMidnight;
        var cursorAt = dayStart;
        foreach (var log in logs)
        {
            if (cursorOnline) online += log.ChangedAt - cursorAt;
            cursorOnline = log.IsOnline;
            cursorAt = log.ChangedAt;
        }

        if (cursorOnline) online += now - cursorAt;

        return new EarningsSummaryDto(
            new EarningsTodayDto(0m, 0, Math.Round(online.TotalHours, 2)),
            new EarningsWeekDto(0m, WeeklyTarget),
            driver.RatingAvg);
    }

    private async Task<DriverProfile> LoadAsync(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        return await db.Drivers.FirstOrDefaultAsync(d => d.UserId == userId, ct) ?? throw new DomainException(ErrorCodes.Forbidden);
    }

    private static DriverStatusDto ToDto(DriverProfile driver)
    {
        var approved = driver.ApplicationStatus == ApplicationStatus.Approved;
        return new DriverStatusDto(driver.IsOnline, approved, approved ? null : ErrorCodes.DriverNotApproved);
    }
}
