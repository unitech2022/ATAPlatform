using ATA.Api.Common;
using ATA.Domain.Common;

namespace ATA.Api.Modules.Trips.Matching;

public static class MatchingEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        var admin = api.MapGroup("/admin").WithTags("Admin").RequireAuthorization(Policies.Admin);

        var settings = admin.MapGroup("/matching-settings");
        settings.MapGet("/", async (MatchingAdminService service, CancellationToken ct) => Results.Ok(await service.ListSettingsAsync(ct))).Produces<List<MatchingSettingsDto>>();
        settings.MapGet("/{id:guid}", async (Guid id, MatchingAdminService service, CancellationToken ct) => Results.Ok(await service.GetSettingsAsync(id, ct))).Produces<MatchingSettingsDto>();
        settings.MapPost("/", async (MatchingSettingsUpsertRequest request, MatchingAdminService service, CancellationToken ct) =>
            {
                var created = await service.CreateSettingsAsync(request, ct);
                return Results.Created($"/api/v1/admin/matching-settings/{created.Id}", created);
            })
            .Produces<MatchingSettingsDto>(StatusCodes.Status201Created);
        settings.MapPut("/{id:guid}", async (Guid id, MatchingSettingsUpsertRequest request, MatchingAdminService service, CancellationToken ct) =>
            Results.Ok(await service.UpdateSettingsAsync(id, request, ct))).Produces<MatchingSettingsDto>();
        settings.MapDelete("/{id:guid}", async (Guid id, MatchingAdminService service, CancellationToken ct) =>
            {
                await service.DeleteSettingsAsync(id, ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);

        admin.MapGet("/trips/{id:guid}/matching", async (Guid id, MatchingAdminService service, CancellationToken ct) =>
                Results.Ok(await service.GetTripMatchingAsync(id, ct)))
            .Produces<TripMatchingDto>();

        admin.MapGet("/matching/stats", async (DateOnly? from, DateOnly? to, MatchingAdminService service, IClock clock, CancellationToken ct) =>
                Results.Ok(await service.GetStatsAsync(from, to, clock.UtcNow, ct)))
            .Produces<MatchingStatsDto>();
    }
}
