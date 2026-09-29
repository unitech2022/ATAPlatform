using ATA.Api.Common;
using ATA.Domain.Ratings;

namespace ATA.Api.Modules.Ratings;

public static class RatingEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        api.MapGet("/catalog/rating-tags", async (string? target, RatingService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.TagsAsync(QueryEnum.Parse<RatingRole>(target, "target"), http.GetLanguage(), ct)))
            .WithTags("Catalog")
            .Produces<List<RatingTagDto>>();

        MapParty(api, "/passenger", Policies.Passenger, RatingRole.Passenger, "Passenger");
        MapParty(api, "/driver", Policies.Driver, RatingRole.Driver, "Driver");

        var admin = api.MapGroup("/admin").WithTags("Admin ratings").RequireAuthorization(Policies.Admin).RequirePermission(Permissions.RatingsManage);
        admin.MapGet("/ratings", async (string? raterRole, int? stars, bool? flagged, Guid? userId, string? tag, string? status, string? search, DateOnly? from, DateOnly? to,
                int? page, int? pageSize, RatingAdminService service, CancellationToken ct) =>
                Results.Ok(await service.ListAsync(QueryEnum.Parse<RatingRole>(raterRole, "raterRole"), stars, flagged, userId, tag, status, search, from, to,
                    Paging.From(page, pageSize), ct)))
            .Produces<PagedResult<AdminRatingDto>>();
        admin.MapPost("/ratings/{id:guid}/hide", async (Guid id, HideRatingRequest request, RatingAdminService service, CancellationToken ct) =>
                Results.Ok(await service.HideAsync(id, request, ct)))
            .Produces<AdminRatingDto>();
        admin.MapPost("/ratings/{id:guid}/unhide", async (Guid id, RatingAdminService service, CancellationToken ct) =>
                Results.Ok(await service.UnhideAsync(id, ct)))
            .Produces<AdminRatingDto>();
        admin.MapGet("/rating-flags", async (string? status, string? type, int? page, int? pageSize, RatingAdminService service, CancellationToken ct) =>
                Results.Ok(await service.FlagsAsync(QueryEnum.Parse<RatingFlagStatus>(status, "status"), QueryEnum.Parse<RatingFlagType>(type, "type"), Paging.From(page, pageSize), ct)))
            .Produces<PagedResult<RatingFlagDto>>();
        admin.MapPost("/rating-flags/{id:guid}/review", async (Guid id, ReviewRatingFlagRequest request, RatingAdminService service, CancellationToken ct) =>
                Results.Ok(await service.ReviewAsync(id, request, ct)))
            .Produces<RatingFlagDto>()
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);
    }

    private static void MapParty(IEndpointRouteBuilder api, string prefix, string policy, RatingRole role, string tag)
    {
        var group = api.MapGroup(prefix).WithTags(tag).RequireAuthorization(policy);
        group.MapPost("/trips/{id:guid}/rating", async (Guid id, SubmitRatingRequest request, RatingService service, CancellationToken ct) =>
            {
                var rating = await service.SubmitAsync(id, role, request, ct);
                return Results.Created($"/api/v1{prefix}/trips/{id}/rating", rating);
            })
            .Produces<RatingDto>(StatusCodes.Status201Created)
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict)
            .Produces<ErrorEnvelope>(StatusCodes.Status422UnprocessableEntity);
        group.MapGet("/ratings/pending", async (RatingService service, CancellationToken ct) => Results.Ok(await service.PendingAsync(role, ct)))
            .Produces<List<PendingRatingDto>>();
        group.MapGet("/ratings/summary", async (RatingService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.SummaryAsync(role, http.GetLanguage(), ct)))
            .Produces<RatingSummaryDto>();
    }
}
