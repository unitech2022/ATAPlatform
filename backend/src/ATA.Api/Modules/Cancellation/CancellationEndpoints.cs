using ATA.Api.Common;
using ATA.Api.Modules.Trips;
using ATA.Domain.Cancellation;
using ATA.Domain.Common;
using ATA.Domain.Trips;

namespace ATA.Api.Modules.Cancellation;

public static class CancellationEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        api.MapGet("/catalog/cancellation-reasons", async (string? actor, string? stage, TripCancellationService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.ReasonsAsync(QueryEnum.Parse<CancellationActor>(actor, "actor"), QueryEnum.Parse<CancellationStage>(stage, "stage"), http.GetLanguage(), ct)))
            .WithTags("Catalog")
            .Produces<List<CancellationReasonDto>>();

        var passenger = api.MapGroup("/passenger").WithTags("Passenger").RequireAuthorization(Policies.Passenger);
        passenger.MapPost("/trips/{id:guid}/cancel/preview", async (Guid id, CancelPreviewRequest? request, TripCancellationService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.PreviewForPassengerAsync(id, request, http.GetLanguage(), ct)))
            .Produces<CancelPreviewDto>();
        passenger.MapGet("/reliability", async (TripCancellationService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.SummaryAsync(Role.Passenger, http.GetLanguage(), ct)))
            .Produces<ReliabilitySummaryDto>();

        var driver = api.MapGroup("/driver").WithTags("Driver").RequireAuthorization(Policies.Driver);
        driver.MapPost("/trips/{id:guid}/cancel/preview", async (Guid id, CancelPreviewRequest? request, TripCancellationService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.PreviewForDriverAsync(id, request, http.GetLanguage(), ct)))
            .Produces<CancelPreviewDto>();
        driver.MapPost("/trips/{id:guid}/no-show", async (Guid id, NoShowRequest? request, TripCancellationService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.NoShowAsync(id, request, http.GetLanguage(), ct)))
            .Produces<TripDto>()
            .Produces<ErrorEnvelope>(StatusCodes.Status422UnprocessableEntity);
        driver.MapGet("/reliability", async (TripCancellationService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.SummaryAsync(Role.Driver, http.GetLanguage(), ct)))
            .Produces<ReliabilitySummaryDto>();

        MapAdmin(api);
    }

    private static void MapAdmin(IEndpointRouteBuilder api)
    {
        var admin = api.MapGroup("/admin").WithTags("Admin cancellation").RequireAuthorization(Policies.Admin);

        var reasons = admin.MapGroup("/cancellation-reasons").RequirePermission(Permissions.CancellationManage);
        reasons.MapGet("/", async (string? actor, CancellationAdminService service, CancellationToken ct) =>
                Results.Ok(await service.ReasonsAsync(QueryEnum.Parse<CancellationActor>(actor, "actor"), ct)))
            .Produces<List<AdminCancellationReasonDto>>();
        reasons.MapPost("/", async (CancellationReasonRequest request, CancellationAdminService service, CancellationToken ct) =>
            {
                var reason = await service.CreateReasonAsync(request, ct);
                return Results.Created($"/api/v1/admin/cancellation-reasons/{reason.Id}", reason);
            })
            .Produces<AdminCancellationReasonDto>(StatusCodes.Status201Created);
        reasons.MapPut("/{id:guid}", async (Guid id, CancellationReasonRequest request, CancellationAdminService service, CancellationToken ct) =>
                Results.Ok(await service.UpdateReasonAsync(id, request, ct)))
            .Produces<AdminCancellationReasonDto>();
        reasons.MapDelete("/{id:guid}", async (Guid id, CancellationAdminService service, CancellationToken ct) =>
            {
                await service.DeleteReasonAsync(id, ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);

        var rules = admin.MapGroup("/cancellation-rules").RequirePermission(Permissions.CancellationManage);
        rules.MapGet("/", async (string? actor, string? stage, string? bookingType, CancellationAdminService service, CancellationToken ct) =>
                Results.Ok(await service.RulesAsync(QueryEnum.Parse<CancellationActor>(actor, "actor"), QueryEnum.Parse<CancellationStage>(stage, "stage"),
                    QueryEnum.Parse<BookingType>(bookingType, "bookingType"), ct)))
            .Produces<List<CancellationRuleDto>>();
        rules.MapPost("/", async (CancellationRuleRequest request, CancellationAdminService service, CancellationToken ct) =>
            {
                var rule = await service.CreateRuleAsync(request, ct);
                return Results.Created($"/api/v1/admin/cancellation-rules/{rule.Id}", rule);
            })
            .Produces<CancellationRuleDto>(StatusCodes.Status201Created)
            .Produces<ErrorEnvelope>(StatusCodes.Status422UnprocessableEntity);
        rules.MapPost("/simulate", async (SimulateCancellationRequest request, CancellationAdminService service, CancellationToken ct) =>
                Results.Ok(await service.SimulateAsync(request, ct)))
            .Produces<SimulateCancellationResult>();
        rules.MapPut("/{id:guid}", async (Guid id, CancellationRuleRequest request, CancellationAdminService service, CancellationToken ct) =>
                Results.Ok(await service.UpdateRuleAsync(id, request, ct)))
            .Produces<CancellationRuleDto>();
        rules.MapDelete("/{id:guid}", async (Guid id, CancellationAdminService service, CancellationToken ct) =>
            {
                await service.DeleteRuleAsync(id, ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);

        admin.MapGet("/reliability-thresholds", async (string? role, CancellationAdminService service, CancellationToken ct) =>
                Results.Ok(await service.ThresholdsAsync(QueryEnum.Parse<Role>(role, "role"), ct)))
            .RequirePermission(Permissions.CancellationManage)
            .Produces<List<ReliabilityThresholdDto>>();
        admin.MapPut("/reliability-thresholds/{id:guid}", async (Guid id, ReliabilityThresholdRequest request, CancellationAdminService service, CancellationToken ct) =>
                Results.Ok(await service.UpdateThresholdAsync(id, request, ct)))
            .RequirePermission(Permissions.CancellationManage)
            .Produces<ReliabilityThresholdDto>();

        admin.MapGet("/cancellations", async (string? actor, string? stage, string? atFault, string? feeStatus, string? excuseStatus, DateOnly? from, DateOnly? to, string? search,
                int? page, int? pageSize, CancellationAdminService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.EventsAsync(QueryEnum.Parse<TripActor>(actor, "actor"), QueryEnum.Parse<CancellationStage>(stage, "stage"),
                    QueryEnum.Parse<AtFault>(atFault, "atFault"), QueryEnum.Parse<CancellationFeeStatus>(feeStatus, "feeStatus"), QueryEnum.Parse<ExcuseStatus>(excuseStatus, "excuseStatus"),
                    from, to, search, Paging.From(page, pageSize), http.GetLanguage(), ct)))
            .RequirePermission(Permissions.TripsView)
            .Produces<PagedResult<AdminCancellationEventDto>>();
        admin.MapGet("/cancellations/excuses", async (string? status, int? page, int? pageSize, CancellationAdminService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.ExcusesAsync(QueryEnum.Parse<ExcuseStatus>(status, "status"), Paging.From(page, pageSize), http.GetLanguage(), ct)))
            .RequirePermission(Permissions.CancellationReview)
            .Produces<PagedResult<ExcuseQueueItemDto>>();
        admin.MapPost("/cancellations/{eventId:guid}/review", async (Guid eventId, ReviewExcuseRequest request, CancellationAdminService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.ReviewAsync(eventId, request, http.GetLanguage(), ct)))
            .RequirePermission(Permissions.CancellationReview)
            .Produces<AdminCancellationEventDto>()
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);
        admin.MapGet("/cancellations/stats", async (DateOnly? from, DateOnly? to, Guid? cityId, Guid? zoneId, Guid? rideCategoryId, CancellationAdminService service, CancellationToken ct) =>
                Results.Ok(await service.StatsAsync(from, to, cityId, zoneId, rideCategoryId, ct)))
            .RequirePermission(Permissions.ReportsView)
            .Produces<CancellationStatsDto>();

        var profiles = admin.MapGroup("/reliability-profiles").RequirePermission(Permissions.ReliabilityManage);
        profiles.MapGet("/", async (string? role, string? level, string? search, int? page, int? pageSize, CancellationAdminService service, CancellationToken ct) =>
                Results.Ok(await service.ProfilesAsync(QueryEnum.Parse<Role>(role, "role"), QueryEnum.Parse<RestrictionLevel>(level, "level"), search, Paging.From(page, pageSize), ct)))
            .Produces<PagedResult<ReliabilityProfileListItemDto>>();
        profiles.MapGet("/{userId:guid}", async (Guid userId, string? role, CancellationAdminService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.ProfileAsync(userId, QueryEnum.Parse<Role>(role, "role"), http.GetLanguage(), ct)))
            .Produces<ReliabilityProfileDetailDto>();
        profiles.MapPost("/{userId:guid}/adjust", async (Guid userId, ReliabilityAdjustRequest request, CancellationAdminService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.AdjustAsync(userId, request, http.GetLanguage(), ct)))
            .Produces<ReliabilityProfileDetailDto>();
    }
}
