using ATA.Api.Common;

namespace ATA.Api.Modules.Favorites;

public static class FavoriteEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        var passenger = api.MapGroup("/passenger/favorite-drivers").WithTags("Passenger").RequireAuthorization(Policies.Passenger);
        passenger.MapGet("/", async (FavoriteService service, CancellationToken ct) => Results.Ok(await service.ListAsync(ct)))
            .Produces<List<FavoriteDriverDto>>();
        passenger.MapPost("/", async (AddFavoriteRequest request, FavoriteService service, CancellationToken ct) =>
            {
                var favorite = await service.AddAsync(request, ct);
                return Results.Created($"/api/v1/passenger/favorite-drivers/{favorite.DriverId}", favorite);
            })
            .Produces<FavoriteDriverDto>(StatusCodes.Status201Created)
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict)
            .Produces<ErrorEnvelope>(StatusCodes.Status422UnprocessableEntity);
        passenger.MapDelete("/{driverId:guid}", async (Guid driverId, FavoriteService service, CancellationToken ct) =>
            {
                await service.RemoveAsync(driverId, ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);
        passenger.MapGet("/available", async (decimal? lat, decimal? lng, Guid? rideCategoryId, FavoriteService service, CancellationToken ct) =>
                Results.Ok(await service.AvailableAsync(lat, lng, rideCategoryId, ct)))
            .Produces<List<AvailableFavoriteDto>>();
        passenger.MapGet("/{driverId:guid}/photo", async (Guid driverId, FavoriteService service, CancellationToken ct) =>
            {
                var (content, contentType) = await service.PhotoAsync(driverId, ct);
                return Results.Stream(content, contentType);
            })
            .Produces(StatusCodes.Status200OK, contentType: "image/jpeg");

        api.MapGet("/driver/favorites/count", async (FavoriteService service, CancellationToken ct) => Results.Ok(await service.DriverCountAsync(ct)))
            .WithTags("Driver").RequireAuthorization(Policies.Driver)
            .Produces<DriverFavoritesCountDto>();

        var admin = api.MapGroup("/admin").WithTags("Admin favorites").RequireAuthorization(Policies.Admin).RequirePermission(Permissions.FavoritesManage);
        var rules = admin.MapGroup("/favorite-discount-rules");
        rules.MapGet("/", async (FavoriteAdminService service, CancellationToken ct) => Results.Ok(await service.ListAsync(ct)))
            .Produces<List<FavoriteDiscountRuleDto>>();
        rules.MapPost("/", async (FavoriteDiscountRuleUpsertRequest request, FavoriteAdminService service, CancellationToken ct) =>
            {
                var created = await service.CreateAsync(request, ct);
                return Results.Created($"/api/v1/admin/favorite-discount-rules/{created.Id}", created);
            })
            .Produces<FavoriteDiscountRuleDto>(StatusCodes.Status201Created);
        rules.MapGet("/{id:guid}", async (Guid id, FavoriteAdminService service, CancellationToken ct) => Results.Ok(await service.GetAsync(id, ct)))
            .Produces<FavoriteDiscountRuleDto>();
        rules.MapPut("/{id:guid}", async (Guid id, FavoriteDiscountRuleUpsertRequest request, FavoriteAdminService service, CancellationToken ct) =>
                Results.Ok(await service.UpdateAsync(id, request, ct)))
            .Produces<FavoriteDiscountRuleDto>();
        rules.MapDelete("/{id:guid}", async (Guid id, FavoriteAdminService service, CancellationToken ct) =>
            {
                await service.DeleteAsync(id, ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);
        admin.MapGet("/favorites/stats", async (DateOnly? from, DateOnly? to, Guid? cityId, FavoriteAdminService service, CancellationToken ct) =>
                Results.Ok(await service.StatsAsync(from, to, cityId, ct)))
            .Produces<FavoriteStatsDto>();
    }
}
