using ATA.Api.Common;
using ATA.Api.Modules.Drivers;
using ATA.Domain.Drivers;

namespace ATA.Api.Modules.Admin;

public static class AdminEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        var admin = api.MapGroup("/admin").WithTags("Admin").RequireAuthorization(Policies.Admin);

        admin.MapGet("/dashboard/summary", async (AdminService service, CancellationToken ct) => Results.Ok(await service.GetDashboardAsync(ct)))
            .RequirePermission(Permissions.DashboardView)
            .Produces<DashboardSummaryDto>();

        var drivers = admin.MapGroup("/drivers");
        drivers.MapGet("/", async (string? status, string? search, int? page, int? pageSize, AdminDriverService service, CancellationToken ct) =>
                Results.Ok(await service.ListAsync(QueryEnum.Parse<ApplicationStatus>(status, "status"), search, Paging.From(page, pageSize), ct)))
            .RequirePermission(Permissions.DriversView)
            .Produces<PagedResult<AdminDriverListItemDto>>();
        drivers.MapGet("/{id:guid}", async (Guid id, AdminDriverService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.GetAsync(id, http.GetLanguage(), ct)))
            .RequirePermission(Permissions.DriversView)
            .Produces<AdminDriverDetailDto>();
        drivers.MapPost("/{id:guid}/review", async (Guid id, ReviewRequest request, AdminDriverService service, CancellationToken ct) =>
                Results.Ok(await service.StartReviewAsync(id, request, ct)))
            .RequirePermission(Permissions.DriversReview)
            .Produces<DriverStatusChangeDto>();
        drivers.MapPost("/{id:guid}/approve", async (Guid id, AdminDriverService service, CancellationToken ct) =>
                Results.Ok(await service.ApproveAsync(id, ct)))
            .RequirePermission(Permissions.DriversReview)
            .Produces<DriverStatusChangeDto>()
            .Produces<ErrorEnvelope>(StatusCodes.Status422UnprocessableEntity);
        drivers.MapPost("/{id:guid}/reject", async (Guid id, ReasonRequest request, AdminDriverService service, CancellationToken ct) =>
                Results.Ok(await service.RejectAsync(id, request, ct)))
            .RequirePermission(Permissions.DriversReview)
            .Produces<DriverStatusChangeDto>();
        drivers.MapPost("/{id:guid}/suspend", async (Guid id, ReasonRequest request, AdminDriverService service, CancellationToken ct) =>
                Results.Ok(await service.SuspendAsync(id, request, ct)))
            .RequirePermission(Permissions.DriversReview)
            .Produces<DriverStatusChangeDto>();
        drivers.MapPost("/{id:guid}/reinstate", async (Guid id, AdminDriverService service, CancellationToken ct) =>
                Results.Ok(await service.ReinstateAsync(id, ct)))
            .RequirePermission(Permissions.DriversReview)
            .Produces<DriverStatusChangeDto>();

        admin.MapPost("/documents/{id:guid}/verify", async (Guid id, VerifyDocumentRequest request, AdminDriverService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.VerifyDocumentAsync(id, request, http.GetLanguage(), ct)))
            .RequirePermission(Permissions.DriversReview)
            .Produces<DriverDocumentDto>();

        admin.MapGet("/passengers", async (string? search, int? page, int? pageSize, AdminService service, CancellationToken ct) =>
                Results.Ok(await service.ListPassengersAsync(search, Paging.From(page, pageSize), ct)))
            .RequirePermission(Permissions.PassengersView)
            .Produces<PagedResult<AdminPassengerListItemDto>>();

        admin.MapPost("/users/{userId:guid}/suspend", async (Guid userId, ReasonRequest request, AdminService service, CancellationToken ct) =>
                Results.Ok(await service.SuspendUserAsync(userId, request, ct)))
            .RequirePermission(Permissions.UsersSuspend)
            .Produces<UserStatusChangeDto>();
        admin.MapPost("/users/{userId:guid}/reinstate", async (Guid userId, AdminService service, CancellationToken ct) =>
                Results.Ok(await service.ReinstateUserAsync(userId, ct)))
            .RequirePermission(Permissions.UsersSuspend)
            .Produces<UserStatusChangeDto>();

        var categories = admin.MapGroup("/ride-categories");
        // Reading ride categories is open to every admin (doc 12 §F20.2); writes need catalog.manage.
        categories.MapGet("/", async (AdminService service, CancellationToken ct) => Results.Ok(await service.ListRideCategoriesAsync(ct)))
            .AllowAnyAdmin()
            .Produces<List<RideCategoryAdminDto>>();
        categories.MapPost("/", async (RideCategoryUpsertRequest request, AdminService service, CancellationToken ct) =>
            {
                var created = await service.CreateRideCategoryAsync(request, ct);
                return Results.Created($"/api/v1/admin/ride-categories/{created.Id}", created);
            })
            .RequirePermission(Permissions.CatalogManage)
            .Produces<RideCategoryAdminDto>(StatusCodes.Status201Created);
        categories.MapPut("/{id:guid}", async (Guid id, RideCategoryUpsertRequest request, AdminService service, CancellationToken ct) =>
                Results.Ok(await service.UpdateRideCategoryAsync(id, request, ct)))
            .RequirePermission(Permissions.CatalogManage)
            .Produces<RideCategoryAdminDto>();
        categories.MapDelete("/{id:guid}", async (Guid id, AdminService service, CancellationToken ct) =>
            {
                await service.DeleteRideCategoryAsync(id, ct);
                return Results.NoContent();
            })
            .RequirePermission(Permissions.CatalogManage)
            .Produces(StatusCodes.Status204NoContent);

        admin.MapGet("/audit-logs", async (string? entityType, Guid? entityId, int? page, int? pageSize, AdminService service, CancellationToken ct) =>
                Results.Ok(await service.ListAuditLogsAsync(entityType, entityId, Paging.From(page, pageSize), ct)))
            .RequirePermission(Permissions.AuditView)
            .Produces<PagedResult<AuditLogDto>>();
    }
}
