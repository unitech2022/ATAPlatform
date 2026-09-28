using ATA.Api.Common;
using ATA.Domain.Notifications;

namespace ATA.Api.Modules.Notifications;

public static class NotificationAdminEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        var admin = api.MapGroup("/admin").WithTags("Admin notifications").RequireAuthorization(Policies.Admin);

        admin.MapGet("/notification-events", (NotificationAdminService service) => Results.Ok(service.Events()))
            .RequirePermission(Permissions.NotificationsView)
            .Produces<List<NotificationEventDto>>();

        admin.MapGet("/notification-templates", async (string? code, string? channel, bool? isActive, NotificationAdminService service, CancellationToken ct) =>
                Results.Ok(await service.ListTemplatesAsync(code, QueryEnum.Parse<NotificationChannel>(channel, "channel"), isActive, ct)))
            .RequirePermission(Permissions.NotificationsView)
            .Produces<List<NotificationTemplateDto>>();
        admin.MapPost("/notification-templates", async (CreateTemplateRequest request, NotificationAdminService service, CancellationToken ct) =>
            {
                var template = await service.CreateTemplateAsync(request, ct);
                return Results.Created($"/api/v1/admin/notification-templates/{template.Id}", template);
            })
            .RequirePermission(Permissions.NotificationsManage)
            .Produces<NotificationTemplateDto>(StatusCodes.Status201Created)
            .Produces<ErrorEnvelope>(StatusCodes.Status422UnprocessableEntity);
        admin.MapPut("/notification-templates/{id:guid}", async (Guid id, UpdateTemplateRequest request, NotificationAdminService service, CancellationToken ct) =>
                Results.Ok(await service.UpdateTemplateAsync(id, request, ct)))
            .RequirePermission(Permissions.NotificationsManage)
            .Produces<NotificationTemplateDto>();
        admin.MapPost("/notification-templates/{id:guid}/preview", async (Guid id, PreviewTemplateRequest request, NotificationAdminService service, CancellationToken ct) =>
                Results.Ok(await service.PreviewAsync(id, request, ct)))
            .RequirePermission(Permissions.NotificationsView)
            .Produces<TemplatePreviewDto>();
        admin.MapPost("/notification-templates/{id:guid}/test", async (Guid id, TestTemplateRequest request, NotificationAdminService service, CancellationToken ct) =>
            {
                await service.SendTestAsync(id, request, ct);
                return Results.Accepted();
            })
            .RequirePermission(Permissions.NotificationsManage)
            .Produces(StatusCodes.Status202Accepted);

        var campaigns = admin.MapGroup("/notification-campaigns");
        campaigns.MapGet("/", async (string? status, int? page, int? pageSize, CampaignService service, CancellationToken ct) =>
                Results.Ok(await service.ListAsync(QueryEnum.Parse<CampaignStatus>(status, "status"), Paging.From(page, pageSize), ct)))
            .RequirePermission(Permissions.NotificationsManage)
            .Produces<PagedResult<CampaignDto>>();
        campaigns.MapPost("/", async (CampaignUpsertRequest request, CampaignService service, CancellationToken ct) =>
            {
                var campaign = await service.CreateAsync(request, ct);
                return Results.Created($"/api/v1/admin/notification-campaigns/{campaign.Id}", campaign);
            })
            .RequirePermission(Permissions.NotificationsManage)
            .Produces<CampaignDto>(StatusCodes.Status201Created);
        campaigns.MapPost("/audience-preview", async (AudiencePreviewRequest request, CampaignService service, CancellationToken ct) =>
                Results.Ok(await service.PreviewAsync(request, ct)))
            .RequirePermission(Permissions.NotificationsManage)
            .Produces<AudiencePreviewDto>();
        campaigns.MapGet("/{id:guid}", async (Guid id, CampaignService service, CancellationToken ct) => Results.Ok(await service.GetAsync(id, ct)))
            .RequirePermission(Permissions.NotificationsManage)
            .Produces<CampaignDto>();
        campaigns.MapPut("/{id:guid}", async (Guid id, CampaignUpsertRequest request, CampaignService service, CancellationToken ct) =>
                Results.Ok(await service.UpdateAsync(id, request, ct)))
            .RequirePermission(Permissions.NotificationsManage)
            .Produces<CampaignDto>()
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);
        campaigns.MapDelete("/{id:guid}", async (Guid id, CampaignService service, CancellationToken ct) =>
            {
                await service.DeleteAsync(id, ct);
                return Results.NoContent();
            })
            .RequirePermission(Permissions.NotificationsManage)
            .Produces(StatusCodes.Status204NoContent);
        campaigns.MapPost("/{id:guid}/schedule", async (Guid id, ScheduleCampaignRequest request, CampaignService service, CancellationToken ct) =>
                Results.Ok(await service.ScheduleAsync(id, request, ct)))
            .RequirePermission(Permissions.NotificationsManage)
            .Produces<CampaignDto>();
        campaigns.MapPost("/{id:guid}/send-now", async (Guid id, CampaignService service, CancellationToken ct) => Results.Ok(await service.SendNowAsync(id, ct)))
            .RequirePermission(Permissions.NotificationsManage)
            .Produces<CampaignDto>();
        campaigns.MapPost("/{id:guid}/cancel", async (Guid id, CampaignService service, CancellationToken ct) => Results.Ok(await service.CancelAsync(id, ct)))
            .RequirePermission(Permissions.NotificationsManage)
            .Produces<CampaignDto>();

        admin.MapGet("/notification-deliveries", async (Guid? userId, string? eventCode, string? channel, string? status, Guid? campaignId, DateOnly? from, DateOnly? to,
                int? page, int? pageSize, NotificationAdminService service, CancellationToken ct) =>
                Results.Ok(await service.ListDeliveriesAsync(userId, eventCode, QueryEnum.Parse<NotificationChannel>(channel, "channel"), QueryEnum.Parse<DeliveryStatus>(status, "status"),
                    campaignId, from, to, Paging.From(page, pageSize), ct)))
            .RequirePermission(Permissions.NotificationsView)
            .Produces<PagedResult<DeliveryDto>>();
        admin.MapPost("/notification-deliveries/{id:guid}/retry", async (Guid id, NotificationAdminService service, CancellationToken ct) =>
            {
                await service.RetryDeliveryAsync(id, ct);
                return Results.Accepted();
            })
            .RequirePermission(Permissions.NotificationsManage)
            .Produces(StatusCodes.Status202Accepted);

        admin.MapGet("/me/duty", async (NotificationAdminService service, CancellationToken ct) => Results.Ok(await service.GetDutyAsync(ct)))
            .RequirePermission(Permissions.SafetyManage)
            .Produces<DutyDto>();
        admin.MapPut("/me/duty", async (DutyDto request, NotificationAdminService service, CancellationToken ct) => Results.Ok(await service.SetDutyAsync(request, ct)))
            .RequirePermission(Permissions.SafetyManage)
            .Produces<DutyDto>();
    }
}
