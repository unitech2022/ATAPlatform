using System.Text.Json;
using ATA.Api.Common;
using ATA.Domain.Common;
using ATA.Domain.Support;

namespace ATA.Api.Modules.Support;

public static class SupportEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        MapPublicHelp(api);
        MapUser(api);
        MapAdminSupport(api);
        MapAdminHelp(api);
    }

    /// <summary>The help center for the website and the apps: no authentication, published content only.</summary>
    private static void MapPublicHelp(IEndpointRouteBuilder api)
    {
        var help = api.MapGroup("/help").WithTags("Help").AllowAnonymous();
        help.MapGet("/categories", async (string? audience, HelpService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.CategoriesAsync(audience, http.GetLanguage(), ct)))
            .Produces<List<HelpCategoryDto>>();
        help.MapGet("/articles", async (Guid? categoryId, string? q, string? audience, string? sort, int? page, int? pageSize, HelpService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.ArticlesAsync(categoryId, q, audience, sort, Paging.From(page, pageSize), http.GetLanguage(), ct)))
            .Produces<PagedResult<HelpArticleSummaryDto>>();
        help.MapGet("/articles/{slug}", async (string slug, HelpService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.ArticleAsync(slug, http.GetLanguage(), ct)))
            .Produces<HelpArticleDto>()
            .Produces<ErrorEnvelope>(StatusCodes.Status404NotFound);
        help.MapPost("/articles/{id:guid}/feedback", async (Guid id, HelpFeedbackRequest request, HelpService service, CancellationToken ct) =>
            {
                await service.FeedbackAsync(id, request, ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorEnvelope>(StatusCodes.Status429TooManyRequests);
    }

    /// <summary>Rider / driver tickets (<c>/support</c>): shared by both apps.</summary>
    private static void MapUser(IEndpointRouteBuilder api)
    {
        var support = api.MapGroup("/support").WithTags("Support").RequireAuthorization(Policies.Authenticated);

        support.MapPost("/attachments", async (HttpRequest request, SupportAttachmentService service, CancellationToken ct) =>
            {
                if (!request.HasFormContentType)
                {
                    throw new DomainException(ErrorCodes.ValidationFailed, new { body = "multipart/form-data expected" });
                }

                IFormCollection form;
                try
                {
                    form = await request.ReadFormAsync(ct);
                }
                catch (Exception ex) when (ex is InvalidDataException or BadHttpRequestException)
                {
                    throw new DomainException(ErrorCodes.ValidationFailed, new Dictionary<string, string> { ["file"] = "required" });
                }

                var dto = await service.UploadAsync(form.Files.GetFile("file"), ct);
                return Results.Created($"/api/v1/files/{dto.FileId}", dto);
            })
            .DisableAntiforgery()
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<AttachmentUploadDto>(StatusCodes.Status201Created)
            .Produces<ErrorEnvelope>(StatusCodes.Status422UnprocessableEntity);

        support.MapPost("/tickets", async (CreateTicketRequest request, SupportTicketService service, HttpContext http, CancellationToken ct) =>
            {
                var ticket = await service.CreateAsync(request, http.GetLanguage(), ct);
                return Results.Created($"/api/v1/support/tickets/{ticket.Id}", ticket);
            })
            .Produces<TicketDetailDto>(StatusCodes.Status201Created)
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict)
            .Produces<ErrorEnvelope>(StatusCodes.Status422UnprocessableEntity);
        support.MapGet("/tickets", async (string? status, int? page, int? pageSize, SupportTicketService service, CancellationToken ct) =>
                Results.Ok(await service.ListMineAsync(status, Paging.From(page, pageSize), ct)))
            .Produces<PagedResult<TicketSummaryDto>>();
        support.MapGet("/tickets/{id:guid}", async (Guid id, SupportTicketService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.GetMineAsync(id, http.GetLanguage(), ct)))
            .Produces<TicketDetailDto>();
        support.MapPost("/tickets/{id:guid}/messages", async (Guid id, ReplyRequest request, SupportTicketService service, CancellationToken ct) =>
            {
                var message = await service.ReplyAsync(id, request, ct);
                return Results.Created($"/api/v1/support/tickets/{id}", message);
            })
            .Produces<TicketMessageDto>(StatusCodes.Status201Created)
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);
        support.MapPost("/tickets/{id:guid}/csat", async (Guid id, CsatRequest request, SupportTicketService service, CancellationToken ct) =>
            {
                await service.RateAsync(id, request, ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);
    }

    private static void MapAdminSupport(IEndpointRouteBuilder api)
    {
        var admin = api.MapGroup("/admin").WithTags("Admin support").RequireAuthorization(Policies.Admin);
        var support = admin.MapGroup("/support");

        support.MapGet("/summary", async (SupportAdminService service, CancellationToken ct) => Results.Ok(await service.SummaryAsync(ct)))
            .RequirePermission(Permissions.SupportView)
            .Produces<SupportSummaryDto>();
        support.MapGet("/stats", async (DateOnly? from, DateOnly? to, SupportAdminService service, CancellationToken ct) => Results.Ok(await service.StatsAsync(from, to, ct)))
            .RequirePermission(Permissions.SupportView)
            .Produces<SupportStatsDto>();
        support.MapGet("/tickets", async (string? status, string? type, string? priority, string? channel, Guid? requesterUserId, string? assignedTo, string? sla, string? search, DateOnly? from,
                DateOnly? to, int? page, int? pageSize, SupportAdminService service, CancellationToken ct) =>
                Results.Ok(await service.ListAsync(QueryEnum.Parse<SupportTicketStatus>(status, "status"), QueryEnum.Parse<SupportTicketType>(type, "type"),
                    QueryEnum.Parse<SupportPriority>(priority, "priority"), QueryEnum.Parse<SupportChannel>(channel, "channel"), requesterUserId, assignedTo, sla, search, from, to,
                    Paging.From(page, pageSize), ct)))
            .RequirePermission(Permissions.SupportView)
            .Produces<PagedResult<AdminTicketListItemDto>>();
        support.MapGet("/tickets/{id:guid}", async (Guid id, SupportAdminService service, HttpContext http, CancellationToken ct) => Results.Ok(await service.GetAsync(id, http.GetLanguage(), ct)))
            .RequirePermission(Permissions.SupportView)
            .Produces<AdminTicketDetailDto>();
        support.MapPost("/tickets", async (AdminCreateTicketRequest request, SupportAdminService service, HttpContext http, CancellationToken ct) =>
            {
                var ticket = await service.CreateAsync(request, http.GetLanguage(), ct);
                return Results.Created($"/api/v1/admin/support/tickets/{ticket.Id}", ticket);
            })
            .RequirePermission(Permissions.SupportManage)
            .Produces<AdminTicketDetailDto>(StatusCodes.Status201Created);
        support.MapPost("/tickets/{id:guid}/messages", async (Guid id, AdminMessageRequest request, SupportAdminService service, CancellationToken ct) =>
            {
                var message = await service.AddMessageAsync(id, request, ct);
                return Results.Created($"/api/v1/admin/support/tickets/{id}", message);
            })
            .RequirePermission(Permissions.SupportManage)
            .Produces<AdminTicketMessageDto>(StatusCodes.Status201Created)
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);
        support.MapPost("/tickets/{id:guid}/assign", async (Guid id, AssignTicketRequest? request, SupportAdminService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.AssignAsync(id, request ?? new AssignTicketRequest(null), http.GetLanguage(), ct)))
            .RequirePermission(Permissions.SupportManage)
            .Produces<AdminTicketDetailDto>();
        support.MapPost("/tickets/{id:guid}/status", async (Guid id, TicketStatusRequest request, SupportAdminService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.SetStatusAsync(id, request, http.GetLanguage(), ct)))
            .RequirePermission(Permissions.SupportManage)
            .Produces<AdminTicketDetailDto>()
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);
        support.MapPost("/tickets/{id:guid}/priority", async (Guid id, TicketPriorityRequest request, SupportAdminService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.SetPriorityAsync(id, request, http.GetLanguage(), ct)))
            .RequirePermission(Permissions.SupportManage)
            .Produces<AdminTicketDetailDto>();
        support.MapPost("/tickets/{id:guid}/type", async (Guid id, TicketTypeRequest request, SupportAdminService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.SetTypeAsync(id, request, http.GetLanguage(), ct)))
            .RequirePermission(Permissions.SupportManage)
            .Produces<AdminTicketDetailDto>();

        support.MapGet("/disputes", async (string? status, Guid? tripId, int? page, int? pageSize, FareDisputeService service, CancellationToken ct) =>
                Results.Ok(await service.ListAsync(QueryEnum.Parse<DisputeStatus>(status, "status"), tripId, Paging.From(page, pageSize), ct)))
            .RequirePermission(Permissions.SupportView)
            .Produces<PagedResult<AdminDisputeDto>>();
        support.MapPost("/disputes/{id:guid}/resolve", async (Guid id, ResolveDisputeRequest request, FareDisputeService service, CancellationToken ct) =>
                Results.Ok(await service.ResolveAsync(id, request, ct)))
            .RequirePermission(Permissions.SupportDisputes)
            .Produces<AdminDisputeDto>()
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict)
            .Produces<ErrorEnvelope>(StatusCodes.Status422UnprocessableEntity);

        support.MapGet("/sla-policies", async (SupportAdminService service, CancellationToken ct) => Results.Ok(await service.ListSlaPoliciesAsync(ct)))
            .RequirePermission(Permissions.SupportManage)
            .Produces<List<SlaPolicyDto>>();
        // Accepts the list of policies as a bare array, as { "policies": [...] } or as one policy object.
        support.MapPut("/sla-policies", async (JsonElement body, SupportAdminService service, CancellationToken ct) =>
                Results.Ok(await service.UpdateSlaPoliciesAsync(ParsePolicies(body), ct)))
            .RequirePermission(Permissions.SupportManage)
            .Produces<List<SlaPolicyDto>>();

        var canned = admin.MapGroup("/canned-responses").RequirePermission(Permissions.SupportManage);
        canned.MapGet("/", async (string? ticketType, bool? active, SupportAdminService service, CancellationToken ct) =>
                Results.Ok(await service.ListCannedAsync(QueryEnum.Parse<SupportTicketType>(ticketType, "ticketType"), active, ct)))
            .Produces<List<CannedResponseDto>>();
        canned.MapPost("/", async (CannedResponseUpsertRequest request, SupportAdminService service, CancellationToken ct) =>
            {
                var created = await service.CreateCannedAsync(request, ct);
                return Results.Created($"/api/v1/admin/canned-responses/{created.Id}", created);
            })
            .Produces<CannedResponseDto>(StatusCodes.Status201Created);
        canned.MapPut("/{id:guid}", async (Guid id, CannedResponseUpsertRequest request, SupportAdminService service, CancellationToken ct) =>
                Results.Ok(await service.UpdateCannedAsync(id, request, ct)))
            .Produces<CannedResponseDto>();
        canned.MapDelete("/{id:guid}", async (Guid id, SupportAdminService service, CancellationToken ct) =>
            {
                await service.DeleteCannedAsync(id, ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);
    }

    private static void MapAdminHelp(IEndpointRouteBuilder api)
    {
        var help = api.MapGroup("/admin/help").WithTags("Admin help").RequireAuthorization(Policies.Admin).RequirePermission(Permissions.HelpManage);

        help.MapGet("/categories", async (HelpService service, CancellationToken ct) => Results.Ok(await service.AdminCategoriesAsync(ct)))
            .Produces<List<AdminHelpCategoryDto>>();
        help.MapPost("/categories", async (HelpCategoryUpsertRequest request, HelpService service, CancellationToken ct) =>
            {
                var created = await service.CreateCategoryAsync(request, ct);
                return Results.Created($"/api/v1/admin/help/categories/{created.Id}", created);
            })
            .Produces<AdminHelpCategoryDto>(StatusCodes.Status201Created);
        help.MapPut("/categories/{id:guid}", async (Guid id, HelpCategoryUpsertRequest request, HelpService service, CancellationToken ct) =>
                Results.Ok(await service.UpdateCategoryAsync(id, request, ct)))
            .Produces<AdminHelpCategoryDto>();
        help.MapDelete("/categories/{id:guid}", async (Guid id, HelpService service, CancellationToken ct) =>
            {
                await service.DeleteCategoryAsync(id, ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);

        help.MapGet("/articles", async (Guid? categoryId, string? audience, bool? published, string? q, int? page, int? pageSize, HelpService service, CancellationToken ct) =>
                Results.Ok(await service.AdminArticlesAsync(categoryId, QueryEnum.Parse<HelpAudience>(audience, "audience"), published, q, Paging.From(page, pageSize), ct)))
            .Produces<PagedResult<AdminHelpArticleDto>>();
        help.MapPost("/articles", async (HelpArticleUpsertRequest request, HelpService service, CancellationToken ct) =>
            {
                var created = await service.CreateArticleAsync(request, ct);
                return Results.Created($"/api/v1/admin/help/articles/{created.Id}", created);
            })
            .Produces<AdminHelpArticleDto>(StatusCodes.Status201Created);
        help.MapGet("/articles/{id:guid}", async (Guid id, HelpService service, CancellationToken ct) => Results.Ok(await service.AdminArticleAsync(id, ct)))
            .Produces<AdminHelpArticleDto>();
        help.MapPut("/articles/{id:guid}", async (Guid id, HelpArticleUpsertRequest request, HelpService service, CancellationToken ct) =>
                Results.Ok(await service.UpdateArticleAsync(id, request, ct)))
            .Produces<AdminHelpArticleDto>();
        help.MapDelete("/articles/{id:guid}", async (Guid id, HelpService service, CancellationToken ct) =>
            {
                await service.DeleteArticleAsync(id, ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);
        help.MapPost("/articles/{id:guid}/publish", async (Guid id, HelpService service, CancellationToken ct) => Results.Ok(await service.SetPublishedAsync(id, true, ct)))
            .Produces<AdminHelpArticleDto>();
        help.MapPost("/articles/{id:guid}/unpublish", async (Guid id, HelpService service, CancellationToken ct) => Results.Ok(await service.SetPublishedAsync(id, false, ct)))
            .Produces<AdminHelpArticleDto>();
    }

    private static IReadOnlyList<SlaPolicyUpsertRequest> ParsePolicies(JsonElement body)
    {
        try
        {
            var options = JsonDefaults.Options;
            return body.ValueKind switch
            {
                JsonValueKind.Array => body.Deserialize<List<SlaPolicyUpsertRequest>>(options) ?? [],
                JsonValueKind.Object when body.TryGetProperty("policies", out var policies) && policies.ValueKind == JsonValueKind.Array =>
                    policies.Deserialize<List<SlaPolicyUpsertRequest>>(options) ?? [],
                JsonValueKind.Object => [body.Deserialize<SlaPolicyUpsertRequest>(options)!],
                _ => [],
            };
        }
        catch (JsonException ex)
        {
            throw new DomainException(ErrorCodes.ValidationFailed, new { body = ex.Message });
        }
    }
}
