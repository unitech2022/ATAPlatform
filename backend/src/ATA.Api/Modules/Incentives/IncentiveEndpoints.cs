using ATA.Api.Common;
using ATA.Domain.Incentives;

namespace ATA.Api.Modules.Incentives;

public static class IncentiveEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        var driver = api.MapGroup("/driver").WithTags("Driver").RequireAuthorization(Policies.Driver);
        driver.MapGet("/tier", async (TierService service, HttpContext http, CancellationToken ct) => Results.Ok(await service.ForDriverAsync(http.GetLanguage(), ct)))
            .Produces<DriverTierDto>();
        driver.MapGet("/incentives", async (string? status, IncentiveService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.ListForDriverAsync(status, http.GetLanguage(), ct)))
            .Produces<List<DriverIncentiveDto>>();
        driver.MapGet("/incentives/{id:guid}", async (Guid id, IncentiveService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.GetForDriverAsync(id, http.GetLanguage(), ct)))
            .Produces<DriverIncentiveDetailDto>();
        driver.MapPost("/incentives/{id:guid}/opt-in", async (Guid id, IncentiveService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.OptInAsync(id, http.GetLanguage(), ct)))
            .Produces<DriverIncentiveDetailDto>()
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);

        var admin = api.MapGroup("/admin").WithTags("Admin incentives").RequireAuthorization(Policies.Admin).RequirePermission(Permissions.IncentivesManage);
        admin.MapGet("/driver-tier-rules", async (TierService service, CancellationToken ct) => Results.Ok(await service.RulesAsync(ct)))
            .Produces<List<TierRuleDto>>();
        admin.MapPut("/driver-tier-rules/{id:guid}", async (Guid id, TierRuleUpdateRequest request, TierService service, CancellationToken ct) =>
                Results.Ok(await service.UpdateRuleAsync(id, request, ct)))
            .Produces<TierRuleDto>();
        admin.MapPost("/driver-tiers/recalculate", async (TierService service, CancellationToken ct) =>
                Results.Accepted("/api/v1/admin/driver-tier-rules", await service.RecalculateAllAsync(ct)))
            .Produces<TierRecalculationDto>(StatusCodes.Status202Accepted);
        admin.MapGet("/drivers/{id:guid}/tier-history", async (Guid id, TierService service, CancellationToken ct) => Results.Ok(await service.HistoryAsync(id, ct)))
            .Produces<List<TierHistoryDto>>();
        admin.MapPost("/drivers/{id:guid}/tier", async (Guid id, SetTierRequest request, TierService service, CancellationToken ct) =>
                Results.Ok(await service.SetTierAsync(id, request, ct)))
            .Produces<TierHistoryDto>();
        admin.MapGet("/drivers/{id:guid}/incentives", async (Guid id, IncentiveAdminService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.ForDriverAsync(id, http.GetLanguage(), ct)))
            .Produces<List<DriverIncentiveProgressAdminDto>>();

        var incentives = admin.MapGroup("/incentives");
        incentives.MapGet("/", async (string? status, Guid? cityId, int? page, int? pageSize, IncentiveAdminService service, CancellationToken ct) =>
                Results.Ok(await service.ListAsync(status, cityId, Paging.From(page, pageSize), ct)))
            .Produces<PagedResult<IncentiveDto>>();
        incentives.MapPost("/", async (IncentiveUpsertRequest request, IncentiveAdminService service, CancellationToken ct) =>
            {
                var created = await service.CreateAsync(request, ct);
                return Results.Created($"/api/v1/admin/incentives/{created.Id}", created);
            })
            .Produces<IncentiveDto>(StatusCodes.Status201Created);
        incentives.MapGet("/{id:guid}", async (Guid id, IncentiveAdminService service, CancellationToken ct) => Results.Ok(await service.GetAsync(id, ct)))
            .Produces<IncentiveDto>();
        incentives.MapPut("/{id:guid}", async (Guid id, IncentiveUpsertRequest request, IncentiveAdminService service, CancellationToken ct) =>
                Results.Ok(await service.UpdateAsync(id, request, ct)))
            .Produces<IncentiveDto>();
        incentives.MapPost("/{id:guid}/deactivate", async (Guid id, IncentiveAdminService service, CancellationToken ct) =>
                Results.Ok(await service.SetActiveAsync(id, false, ct)))
            .Produces<IncentiveDto>();
        incentives.MapPost("/{id:guid}/activate", async (Guid id, IncentiveAdminService service, CancellationToken ct) =>
                Results.Ok(await service.SetActiveAsync(id, true, ct)))
            .Produces<IncentiveDto>();
        incentives.MapGet("/{id:guid}/progress", async (Guid id, string? status, int? page, int? pageSize, IncentiveAdminService service, CancellationToken ct) =>
                Results.Ok(await service.ProgressAsync(id, QueryEnum.Parse<IncentiveProgressStatus>(status, "status"), Paging.From(page, pageSize), ct)))
            .Produces<PagedResult<IncentiveProgressDto>>();
        admin.MapPost("/incentive-progress/{id:guid}/void", async (Guid id, VoidProgressRequest request, IncentiveAdminService service, CancellationToken ct) =>
                Results.Ok(await service.VoidAsync(id, request, ct)))
            .Produces<IncentiveProgressDto>()
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);
    }
}
