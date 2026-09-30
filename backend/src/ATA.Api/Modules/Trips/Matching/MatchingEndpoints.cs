using ATA.Api.Common;
using ATA.Domain.Common;

namespace ATA.Api.Modules.Trips.Matching;

public static class MatchingEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        var admin = api.MapGroup("/admin").WithTags("Admin").RequireAuthorization(Policies.Admin);

        var settings = admin.MapGroup("/matching-settings");
        settings.MapGet("/", async (MatchingAdminService service, CancellationToken ct) => Results.Ok(await service.ListSettingsAsync(ct))).RequirePermission(Permissions.PricingView).Produces<List<MatchingSettingsDto>>();
        settings.MapGet("/{id:guid}", async (Guid id, MatchingAdminService service, CancellationToken ct) => Results.Ok(await service.GetSettingsAsync(id, ct))).RequirePermission(Permissions.PricingView).Produces<MatchingSettingsDto>();
        settings.MapPost("/", async (MatchingSettingsUpsertRequest request, MatchingAdminService service, CancellationToken ct) =>
            {
                var created = await service.CreateSettingsAsync(request, ct);
                return Results.Created($"/api/v1/admin/matching-settings/{created.Id}", created);
            })
            .RequirePermission(Permissions.MatchingEdit)
            .Produces<MatchingSettingsDto>(StatusCodes.Status201Created);
        settings.MapPut("/{id:guid}", async (Guid id, MatchingSettingsUpsertRequest request, MatchingAdminService service, CancellationToken ct) =>
            Results.Ok(await service.UpdateSettingsAsync(id, request, ct))).RequirePermission(Permissions.MatchingEdit).Produces<MatchingSettingsDto>();
        settings.MapDelete("/{id:guid}", async (Guid id, MatchingAdminService service, CancellationToken ct) =>
            {
                await service.DeleteSettingsAsync(id, ct);
                return Results.NoContent();
            })
            .RequirePermission(Permissions.MatchingEdit)
            .Produces(StatusCodes.Status204NoContent);

        admin.MapGet("/trips/{id:guid}/matching", async (Guid id, MatchingAdminService service, CancellationToken ct) =>
                Results.Ok(await service.GetTripMatchingAsync(id, ct)))
            .RequirePermission(Permissions.TripsView)
            .Produces<TripMatchingDto>();

        admin.MapGet("/matching/stats", async (DateOnly? from, DateOnly? to, MatchingAdminService service, IClock clock, CancellationToken ct) =>
                Results.Ok(await service.GetStatsAsync(from, to, clock.UtcNow, ct)))
            .RequirePermission(Permissions.ReportsView)
            .Produces<MatchingStatsDto>();
    }
}
