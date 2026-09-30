using ATA.Api.Common;
using ATA.Api.Modules.Trips;
using ATA.Infrastructure.Locking;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Scheduling;

public static class SchedulingEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        var passenger = api.MapGroup("/passenger").WithTags("Passenger").RequireAuthorization(Policies.Passenger);
        passenger.MapGet("/scheduling/rules", async (Guid? rideCategoryId, decimal? lat, decimal? lng, ScheduledRideService service, CancellationToken ct) =>
                Results.Ok(await service.RulesAsync(rideCategoryId, lat, lng, ct)))
            .Produces<SchedulingRulesDto>();
        passenger.MapGet("/trips/scheduled", async (ScheduledRideService service, HttpContext http, CancellationToken ct) => Results.Ok(await service.ListAsync(http.GetLanguage(), ct)))
            .Produces<List<TripDto>>();
        passenger.MapGet("/scheduled/{tripId:guid}/driver-photo", async (Guid tripId, ScheduledRideService service, CancellationToken ct) =>
            {
                var (content, contentType) = await service.DriverPhotoAsync(tripId, ct);
                return Results.Stream(content, contentType);
            })
            .Produces(StatusCodes.Status200OK, contentType: "image/jpeg");

        var driver = api.MapGroup("/driver/scheduled").WithTags("Driver").RequireAuthorization(Policies.Driver);
        driver.MapGet("/marketplace", async (decimal? lat, decimal? lng, DateTime? from, DateTime? to, int? page, int? pageSize, ScheduledDriverService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.MarketplaceAsync(lat, lng, from, to, Paging.From(page, pageSize), http.GetLanguage(), ct)))
            .Produces<PagedResult<MarketplaceTripDto>>();
        driver.MapGet("/", async (string? status, int? page, int? pageSize, ScheduledDriverService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.ListAsync(status, Paging.From(page, pageSize), http.GetLanguage(), ct)))
            .Produces<PagedResult<ReservationDto>>();
        driver.MapPost("/{tripId:guid}/reserve", async (Guid tripId, ScheduledDriverService service, HttpContext http, CancellationToken ct) =>
            {
                var reservation = await service.ReserveAsync(tripId, http.GetLanguage(), ct);
                return Results.Created($"/api/v1/driver/scheduled/{tripId}", reservation);
            })
            .Produces<ReservationDto>(StatusCodes.Status201Created)
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict)
            .Produces<ErrorEnvelope>(StatusCodes.Status422UnprocessableEntity);
        driver.MapPost("/{tripId:guid}/confirm", async (Guid tripId, ScheduledDriverService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.ConfirmAsync(tripId, http.GetLanguage(), ct)))
            .Produces<ReservationDto>()
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);
        driver.MapPost("/{tripId:guid}/release", async (Guid tripId, ReleaseReservationRequest? request, ScheduledDriverService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.ReleaseAsync(tripId, request, http.GetLanguage(), ct)))
            .Produces<ReservationDto>();

        var admin = api.MapGroup("/admin").WithTags("Admin scheduling").RequireAuthorization(Policies.Admin).RequirePermission(Permissions.SchedulingManage);
        var rules = admin.MapGroup("/scheduled-ride-rules");
        rules.MapGet("/", async (ScheduledAdminService service, CancellationToken ct) => Results.Ok(await service.ListRulesAsync(ct))).Produces<List<ScheduledRideRuleDto>>();
        rules.MapPost("/", async (ScheduledRideRuleUpsertRequest request, ScheduledAdminService service, CancellationToken ct) =>
            {
                var created = await service.CreateRuleAsync(request, ct);
                return Results.Created($"/api/v1/admin/scheduled-ride-rules/{created.Id}", created);
            })
            .Produces<ScheduledRideRuleDto>(StatusCodes.Status201Created);
        rules.MapPut("/{id:guid}", async (Guid id, ScheduledRideRuleUpsertRequest request, ScheduledAdminService service, CancellationToken ct) =>
                Results.Ok(await service.UpdateRuleAsync(id, request, ct)))
            .Produces<ScheduledRideRuleDto>();
        rules.MapDelete("/{id:guid}", async (Guid id, ScheduledAdminService service, CancellationToken ct) =>
            {
                await service.DeleteRuleAsync(id, ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);

        admin.MapGet("/scheduled-trips", async (DateOnly? from, DateOnly? to, string? reservation, Guid? cityId, Guid? rideCategoryId, Guid? zoneId, bool? atRisk, int? page, int? pageSize,
                ScheduledAdminService service, CancellationToken ct) =>
                Results.Ok(await service.TripsAsync(from, to, reservation, cityId, rideCategoryId, zoneId, atRisk ?? false, page, pageSize, ct)))
            .Produces<PagedResult<AdminScheduledTripDto>>();
        admin.MapPost("/scheduled-trips/{tripId:guid}/assign", async (Guid tripId, AssignScheduledTripRequest request, ScheduledAdminService service, CancellationToken ct) =>
                Results.Ok(await service.AssignAsync(tripId, request, ct)))
            .Produces<AdminScheduledTripDto>()
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);
        admin.MapPost("/scheduled-trips/{tripId:guid}/release-reservation", async (Guid tripId, AdminReleaseReservationRequest request, ScheduledAdminService service, CancellationToken ct) =>
                Results.Ok(await service.ReleaseAsync(tripId, request, ct)))
            .Produces<AdminScheduledTripDto>();
        admin.MapGet("/scheduling/stats", async (DateOnly? from, DateOnly? to, ScheduledAdminService service, CancellationToken ct) => Results.Ok(await service.StatsAsync(from, to, ct)))
            .Produces<SchedulingStatsDto>();
    }
}

/// <summary><c>ScheduledRideWorker</c> (every 30 s): confirmation requests and timeouts, the start of the normal search, driver no-show and the favourite window; off with <c>Scheduling:JobsEnabled=false</c>.</summary>
public sealed class ScheduledRideWorker(IServiceScopeFactory scopes, IDistributedLock locks, IOptions<SchedulingOptions> options, ILogger<ScheduledRideWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.JobsEnabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(5, options.Value.WorkerIntervalSeconds)));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Scheduled ride worker failed");
            }
        }
    }

    public async Task<int> RunOnceAsync(CancellationToken ct)
    {
        await using var handle = await locks.TryAcquireAsync("lock:job:scheduled_ride_worker", TimeSpan.FromMinutes(5), ct);
        if (handle is null) return 0;
        using var scope = scopes.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ScheduledRideEngine>().RunWorkerPassAsync(ct);
    }
}

/// <summary><c>ScheduledReminderJob</c> (every minute): sends the due <c>scheduled_ride_reminders</c>; off with <c>Scheduling:JobsEnabled=false</c>.</summary>
public sealed class ScheduledReminderJob(IServiceScopeFactory scopes, IDistributedLock locks, IOptions<SchedulingOptions> options, ILogger<ScheduledReminderJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.JobsEnabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(5, options.Value.ReminderIntervalSeconds)));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Scheduled reminder job failed");
            }
        }
    }

    public async Task<int> RunOnceAsync(CancellationToken ct)
    {
        await using var handle = await locks.TryAcquireAsync("lock:job:scheduled_reminders", TimeSpan.FromMinutes(5), ct);
        if (handle is null) return 0;
        using var scope = scopes.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ScheduledRideEngine>().SendDueRemindersAsync(ct);
    }
}
