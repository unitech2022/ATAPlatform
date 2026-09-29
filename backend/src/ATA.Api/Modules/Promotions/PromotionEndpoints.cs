using ATA.Api.Common;
using ATA.Domain.Promotions;

namespace ATA.Api.Modules.Promotions;

public static class PromotionEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        var passenger = api.MapGroup("/passenger/promotions").WithTags("Passenger").RequireAuthorization(Policies.Passenger);
        passenger.MapGet("/", async (string? status, PromotionService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.ListForPassengerAsync(status, http.GetLanguage(), ct)))
            .Produces<List<PassengerPromotionDto>>();
        passenger.MapPost("/validate", async (ValidatePromoRequest request, PromotionService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.ValidateAsync(request, http.GetLanguage(), ct)))
            .Produces<ValidatePromoResponse>()
            .Produces<ErrorEnvelope>(StatusCodes.Status404NotFound)
            .Produces<ErrorEnvelope>(StatusCodes.Status422UnprocessableEntity);

        var admin = api.MapGroup("/admin").WithTags("Admin promotions").RequireAuthorization(Policies.Admin).RequirePermission(Permissions.PromotionsManage);
        var promotions = admin.MapGroup("/promotions");
        promotions.MapGet("/", async (string? status, string? search, int? page, int? pageSize, PromotionAdminService service, CancellationToken ct) =>
                Results.Ok(await service.ListAsync(status, search, Paging.From(page, pageSize), ct)))
            .Produces<PagedResult<PromotionListItemDto>>();
        promotions.MapPost("/", async (PromotionUpsertRequest request, PromotionAdminService service, CancellationToken ct) =>
            {
                var created = await service.CreateAsync(request, ct);
                return Results.Created($"/api/v1/admin/promotions/{created.Id}", created);
            })
            .Produces<PromotionDto>(StatusCodes.Status201Created)
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);
        promotions.MapGet("/{id:guid}", async (Guid id, PromotionAdminService service, CancellationToken ct) => Results.Ok(await service.GetAsync(id, ct)))
            .Produces<PromotionDto>();
        promotions.MapPut("/{id:guid}", async (Guid id, PromotionUpsertRequest request, PromotionAdminService service, CancellationToken ct) =>
                Results.Ok(await service.UpdateAsync(id, request, ct)))
            .Produces<PromotionDto>()
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);
        promotions.MapPost("/{id:guid}/deactivate", async (Guid id, PromotionAdminService service, CancellationToken ct) =>
                Results.Ok(await service.SetActiveAsync(id, false, ct)))
            .Produces<PromotionDto>();
        promotions.MapPost("/{id:guid}/activate", async (Guid id, PromotionAdminService service, CancellationToken ct) =>
                Results.Ok(await service.SetActiveAsync(id, true, ct)))
            .Produces<PromotionDto>();
        promotions.MapGet("/{id:guid}/redemptions", async (Guid id, string? status, DateOnly? from, DateOnly? to, int? page, int? pageSize, PromotionAdminService service, CancellationToken ct) =>
                Results.Ok(await service.RedemptionsAsync(id, QueryEnum.Parse<RedemptionStatus>(status, "status"), from, to, Paging.From(page, pageSize), ct)))
            .Produces<PagedResult<PromotionRedemptionDto>>();
        promotions.MapGet("/{id:guid}/stats", async (Guid id, PromotionAdminService service, CancellationToken ct) => Results.Ok(await service.StatsAsync(id, ct)))
            .Produces<PromotionStatsDto>();

        // Not in doc 10 §F15.6: redemptions across every promotion (dashboard "redemptions today").
        admin.MapGet("/promotion-redemptions", async (Guid? promotionId, string? status, DateOnly? from, DateOnly? to, int? page, int? pageSize, PromotionAdminService service, CancellationToken ct) =>
                Results.Ok(await service.RedemptionsAsync(promotionId, QueryEnum.Parse<RedemptionStatus>(status, "status"), from, to, Paging.From(page, pageSize), ct)))
            .Produces<PagedResult<PromotionRedemptionDto>>();
    }
}
