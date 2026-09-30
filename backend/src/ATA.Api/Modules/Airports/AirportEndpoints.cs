using ATA.Api.Common;
using ATA.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace ATA.Api.Modules.Airports;

public static class AirportEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        api.MapGet("/catalog/airports", async (AirportReadService service, HttpContext http, CancellationToken ct) => Results.Ok(await service.CatalogAsync(http.GetLanguage(), ct)))
            .WithTags("Catalog")
            .Produces<List<AirportCatalogDto>>();

        api.MapGet("/passenger/airports/resolve", async (decimal? lat, decimal? lng, AirportReadService service, HttpContext http, CancellationToken ct) =>
                JsonOrNull(await service.ResolveAsync(lat, lng, http.GetLanguage(), ct)))
            .WithTags("Passenger").RequireAuthorization(Policies.Passenger)
            .Produces<AirportResolveDto>();

        var driver = api.MapGroup("/driver/airport-queue").WithTags("Driver").RequireAuthorization(Policies.Driver);
        driver.MapGet("/", async (AirportQueueService service, HttpContext http, CancellationToken ct) => Results.Ok(await service.StatusAsync(http.GetLanguage(), ct)))
            .Produces<AirportQueueStatusDto>();
        driver.MapPost("/join", async (AirportQueueJoinRequest request, AirportQueueService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.JoinAsync(request, http.GetLanguage(), ct)))
            .Produces<AirportQueueStatusDto>()
            .Produces<ErrorEnvelope>(StatusCodes.Status422UnprocessableEntity);
        driver.MapPost("/leave", async (AirportQueueService service, CancellationToken ct) =>
            {
                await service.LeaveAsync(ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);

        var admin = api.MapGroup("/admin/airports").WithTags("Admin airports").RequireAuthorization(Policies.Admin).RequirePermission(Permissions.AirportManage);
        admin.MapGet("/", async (AirportAdminService service, CancellationToken ct) => Results.Ok(await service.ListAsync(ct))).Produces<List<AdminAirportDto>>();
        admin.MapPost("/", async (AirportUpsertRequest request, AirportAdminService service, CancellationToken ct) =>
            {
                var created = await service.CreateAsync(request, ct);
                return Results.Created($"/api/v1/admin/airports/{created.Id}", created);
            })
            .Produces<AdminAirportDto>(StatusCodes.Status201Created);
        admin.MapGet("/{id:guid}", async (Guid id, AirportAdminService service, CancellationToken ct) => Results.Ok(await service.GetAsync(id, ct))).Produces<AdminAirportDto>();
        admin.MapPut("/{id:guid}", async (Guid id, AirportUpsertRequest request, AirportAdminService service, CancellationToken ct) =>
                Results.Ok(await service.UpdateAsync(id, request, ct)))
            .Produces<AdminAirportDto>();
        admin.MapDelete("/{id:guid}", async (Guid id, AirportAdminService service, CancellationToken ct) =>
            {
                await service.DeleteAsync(id, ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);

        admin.MapGet("/{id:guid}/zones", async (Guid id, AirportAdminService service, CancellationToken ct) => Results.Ok(await service.ZonesAsync(id, ct)))
            .Produces<List<AdminAirportZoneDto>>();
        admin.MapPost("/{id:guid}/zones", async (Guid id, AirportZoneUpsertRequest request, AirportAdminService service, CancellationToken ct) =>
            {
                var created = await service.CreateZoneAsync(id, request, ct);
                return Results.Created($"/api/v1/admin/airports/{id}/zones/{created.Id}", created);
            })
            .Produces<AdminAirportZoneDto>(StatusCodes.Status201Created);
        admin.MapPut("/{id:guid}/zones/{zoneId:guid}", async (Guid id, Guid zoneId, AirportZoneUpsertRequest request, AirportAdminService service, CancellationToken ct) =>
                Results.Ok(await service.UpdateZoneAsync(id, zoneId, request, ct)))
            .Produces<AdminAirportZoneDto>();
        admin.MapDelete("/{id:guid}/zones/{zoneId:guid}", async (Guid id, Guid zoneId, AirportAdminService service, CancellationToken ct) =>
            {
                await service.DeleteZoneAsync(id, zoneId, ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);

        admin.MapGet("/{id:guid}/queue", async (Guid id, AirportAdminService service, CancellationToken ct) => Results.Ok(await service.QueueAsync(id, ct)))
            .Produces<List<AdminAirportQueueEntryDto>>();
        admin.MapDelete("/{id:guid}/queue/{entryId:guid}", async (Guid id, Guid entryId, [FromBody] RemoveQueueEntryRequest? request, AirportAdminService service, CancellationToken ct) =>
            {
                await service.RemoveFromQueueAsync(id, entryId, request, ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);
    }

    /// <summary><c>Results.Ok(null)</c> sends an empty body; the contract requires a JSON <c>null</c> literal.</summary>
    private static IResult JsonOrNull<T>(T? value) where T : class =>
        value is null ? Results.Content("null", "application/json; charset=utf-8") : Results.Ok(value);
}

/// <summary>Runs <c>AirportQueueJob</c> every <c>Airport:JobIntervalSeconds</c> (30 s); off with <c>Airport:JobsEnabled=false</c> (tests call <see cref="RunOnceAsync"/>).</summary>
public sealed class AirportQueueJob(IServiceScopeFactory scopes, ATA.Infrastructure.Locking.IDistributedLock locks, Microsoft.Extensions.Options.IOptions<AirportOptions> options, ILogger<AirportQueueJob> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.JobsEnabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(5, options.Value.JobIntervalSeconds)));
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
                logger.LogError(ex, "Airport queue job failed");
            }
        }
    }

    public async Task<int> RunOnceAsync(CancellationToken ct)
    {
        await using var handle = await locks.TryAcquireAsync("lock:job:airport_queue", TimeSpan.FromMinutes(5), ct);
        if (handle is null) return 0;
        using var scope = scopes.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AirportQueueService>().RunJobAsync(ct);
    }
}
