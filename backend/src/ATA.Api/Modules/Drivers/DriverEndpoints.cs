using ATA.Api.Common;
using ATA.Api.Modules.Payments;
using Microsoft.Extensions.Options;
using ATA.Domain.Common;
using ATA.Domain.Drivers;
using ATA.Domain.Trips;
using ATA.Domain.Wallet;
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
    }
}

/// <summary>Online/offline state with status logs, and the earnings summary (completed trips + online hours from status logs).</summary>
public sealed class DriverStatusService(AtaDbContext db, ICurrentUser currentUser, IClock clock, IOptions<PayoutsOptions> payouts)
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
        if (isOnline && !driver.IsOnline)
        {
            // Cash debt rule (F11): a driver whose cash commission debt exceeds Payouts:MaxCashDebt must top up before going online.
            var balance = await db.Wallets.AsNoTracking().Where(w => w.UserId == driver.UserId && w.Kind == WalletKind.Driver).Select(w => (decimal?)w.Balance).FirstOrDefaultAsync(ct) ?? 0m;
            var limit = payouts.Value.MaxCashDebt;
            if (-balance > limit)
            {
                throw new DomainException(ErrorCodes.CashDebtLimitExceeded, new { cashDebt = -balance, limit });
            }
        }

        if (driver.IsOnline != isOnline)
        {
            var now = clock.UtcNow;
            driver.IsOnline = isOnline;
            driver.LastOnlineAt = now;
            db.DriverStatusLogs.Add(new DriverStatusLog
            {
                DriverId = driver.Id, IsOnline = isOnline, ChangedAt = now, Latitude = request.Latitude, Longitude = request.Longitude,
            });
            await SyncLocationAsync(driver, request, now, ct);
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

        // Saudi work week starts on Sunday.
        var weekStart = dayStart.AddDays(-(int)dayStart.DayOfWeek);
        var completed = await db.Trips.AsNoTracking()
            .Where(t => t.DriverId == driver.Id && t.Status == TripStatus.Completed && t.CompletedAt >= weekStart)
            .Select(t => new { t.CompletedAt, Earnings = t.DriverEarnings ?? 0m })
            .ToListAsync(ct);
        var today = completed.Where(t => t.CompletedAt >= dayStart).ToList();

        return new EarningsSummaryDto(
            new EarningsTodayDto(today.Sum(t => t.Earnings), today.Count, Math.Round(online.TotalHours, 2)),
            new EarningsWeekDto(completed.Sum(t => t.Earnings), WeeklyTarget),
            driver.RatingAvg);
    }

    /// <summary>Keeps <c>driver_locations.is_online</c> in step with the driver flag so the matcher sees the change immediately.</summary>
    private async Task SyncLocationAsync(DriverProfile driver, UpdateDriverStatusRequest request, DateTime now, CancellationToken ct)
    {
        var location = await db.DriverLocations.FirstOrDefaultAsync(l => l.DriverId == driver.Id, ct);
        if (location is null)
        {
            if (request.Latitude is null || request.Longitude is null)
            {
                return;
            }

            location = new DriverLocation { DriverId = driver.Id };
            db.DriverLocations.Add(location);
        }

        if (request.Latitude is { } lat && request.Longitude is { } lng)
        {
            location.Lat = lat;
            location.Lng = lng;
        }

        location.IsOnline = driver.IsOnline;
        location.CurrentTripId = driver.CurrentTripId;
        location.UpdatedAt = now;
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
