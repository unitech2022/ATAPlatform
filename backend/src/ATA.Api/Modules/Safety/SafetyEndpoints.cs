using ATA.Api.Common;
using ATA.Domain.Safety;

namespace ATA.Api.Modules.Safety;

public static class SafetyEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        MapPublic(api);
        MapSafety(api);
        MapChat(api, "/passenger/trips", Policies.Passenger, TripMessageSender.Passenger, "Passenger");
        MapChat(api, "/driver/trips", Policies.Driver, TripMessageSender.Driver, "Driver");
        MapLostItems(api);
        MapAdmin(api);

        api.MapGet("/catalog/chat-quick-replies", (string? role, HttpContext http) =>
            {
                var sender = QueryEnum.Parse<TripMessageSender>(role, "role");
                var lang = http.GetLanguage();
                return Results.Ok(QuickReplies.All.Where(q => sender is null || q.Role == sender).Select(q => new QuickReplyDto(q.Code, lang.Pick(q.Ar, q.En))).ToList());
            })
            .WithTags("Catalog")
            .Produces<List<QuickReplyDto>>();
    }

    private static void MapPublic(IEndpointRouteBuilder api)
    {
        var shares = api.MapGroup("/public/trip-shares").WithTags("Public").AllowAnonymous().RequireRateLimiting(RateLimiting.PublicSharePolicy);

        shares.MapGet("/{token}", async (string token, TripShareService service, HttpContext http, CancellationToken ct) =>
            {
                http.Response.Headers.CacheControl = "no-store";
                http.Response.Headers["X-Robots-Tag"] = "noindex";
                return Results.Ok(await service.GetPublicAsync(token, http.Connection.RemoteIpAddress?.ToString(), http.GetLanguage(), ct));
            })
            .Produces<PublicTripShareDto>()
            .Produces<ErrorEnvelope>(StatusCodes.Status404NotFound)
            .Produces<ErrorEnvelope>(StatusCodes.Status410Gone)
            .Produces<ErrorEnvelope>(StatusCodes.Status429TooManyRequests);

        shares.MapGet("/{token}/driver-photo", async (string token, TripShareService service, CancellationToken ct) =>
            {
                var (content, contentType) = await service.GetDriverPhotoAsync(token, ct);
                return Results.Stream(content, contentType);
            })
            .Produces(StatusCodes.Status200OK, contentType: "image/jpeg");
    }

    private static void MapSafety(IEndpointRouteBuilder api)
    {
        var safety = api.MapGroup("/safety").WithTags("Safety").RequireAuthorization(Policies.Authenticated);

        safety.MapGet("/trusted-contacts", async (TrustedContactService service, CancellationToken ct) => Results.Ok(await service.ListAsync(ct)))
            .Produces<List<TrustedContactDto>>();
        safety.MapPost("/trusted-contacts", async (TrustedContactRequest request, TrustedContactService service, CancellationToken ct) =>
            {
                var contact = await service.CreateAsync(request, ct);
                return Results.Created($"/api/v1/safety/trusted-contacts/{contact.Id}", contact);
            })
            .Produces<TrustedContactDto>(StatusCodes.Status201Created)
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict)
            .Produces<ErrorEnvelope>(StatusCodes.Status422UnprocessableEntity);
        safety.MapPut("/trusted-contacts/{id:guid}", async (Guid id, TrustedContactRequest request, TrustedContactService service, CancellationToken ct) =>
                Results.Ok(await service.UpdateAsync(id, request, ct)))
            .Produces<TrustedContactDto>();
        safety.MapDelete("/trusted-contacts/{id:guid}", async (Guid id, TrustedContactService service, CancellationToken ct) =>
            {
                await service.DeleteAsync(id, ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);

        safety.MapPost("/trips/{tripId:guid}/shares", async (Guid tripId, CreateShareRequest request, TripShareService service, CancellationToken ct) =>
            {
                var created = await service.CreateAsync(tripId, request, ct);
                return Results.Created($"/api/v1/safety/trips/{tripId}/shares", created);
            })
            .Produces<CreateSharesResponse>(StatusCodes.Status201Created)
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);
        safety.MapGet("/trips/{tripId:guid}/shares", async (Guid tripId, TripShareService service, CancellationToken ct) => Results.Ok(await service.ListAsync(tripId, ct)))
            .Produces<List<TripShareDto>>();
        safety.MapDelete("/shares/{id:guid}", async (Guid id, TripShareService service, CancellationToken ct) =>
            {
                await service.RevokeAsync(id, ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);

        safety.MapPost("/sos", async (SosRequest request, SafetyService service, CancellationToken ct) =>
            {
                var (response, created) = await service.SosAsync(request, ct);
                return created ? Results.Created($"/api/v1/safety/cases/{response.CaseId}", response) : Results.Ok(response);
            })
            .Produces<SosResponse>(StatusCodes.Status201Created)
            .Produces<SosResponse>();
        safety.MapPost("/sos/{caseId:guid}/location", async (Guid caseId, SosLocationRequest request, SafetyService service, CancellationToken ct) =>
            {
                await service.UpdateSosLocationAsync(caseId, request, ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);
        safety.MapPost("/sos/{caseId:guid}/cancel", async (Guid caseId, SosCancelRequest request, SafetyService service, CancellationToken ct) =>
                Results.Ok(await service.CancelSosAsync(caseId, request, ct)))
            .Produces<SafetyCaseSummaryDto>();

        safety.MapPost("/reports", async (SafetyReportRequest request, SafetyService service, CancellationToken ct) =>
            {
                var summary = await service.ReportAsync(request, ct);
                return Results.Created($"/api/v1/safety/cases/{summary.Id}", summary);
            })
            .Produces<SafetyCaseSummaryDto>(StatusCodes.Status201Created);
        safety.MapGet("/cases", async (int? page, int? pageSize, SafetyService service, CancellationToken ct) =>
                Results.Ok(await service.ListMineAsync(Paging.From(page, pageSize), ct)))
            .Produces<PagedResult<SafetyCaseSummaryDto>>();
        safety.MapGet("/cases/{id:guid}", async (Guid id, SafetyService service, CancellationToken ct) => Results.Ok(await service.GetMineAsync(id, ct)))
            .Produces<SafetyCaseSummaryDto>();

        safety.MapGet("/alerts/pending", async (SafetyService service, CancellationToken ct) => JsonOrNull(await service.PendingAlertAsync(ct)))
            .Produces<SafetyAlertDto>();
        safety.MapPost("/alerts/{id:guid}/respond", async (Guid id, AlertRespondRequest request, SafetyService service, CancellationToken ct) =>
                Results.Ok(await service.RespondAlertAsync(id, request, ct)))
            .Produces<SafetyAlertDto>()
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);
    }

    private static void MapChat(IEndpointRouteBuilder api, string prefix, string policy, TripMessageSender role, string tag)
    {
        var trips = api.MapGroup(prefix).WithTags(tag).RequireAuthorization(policy);

        trips.MapGet("/{id:guid}/messages", async (Guid id, Guid? after, TripChatService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.ListAsync(id, role, after, http.GetLanguage(), ct)))
            .Produces<List<TripMessageDto>>();
        trips.MapPost("/{id:guid}/messages", async (Guid id, SendMessageRequest request, TripChatService service, HttpContext http, CancellationToken ct) =>
            {
                var message = await service.SendAsync(id, role, request, http.GetLanguage(), ct);
                return Results.Created($"/api/v1{prefix}/{id}/messages", message);
            })
            .Produces<TripMessageDto>(StatusCodes.Status201Created)
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);
        trips.MapPost("/{id:guid}/messages/read", async (Guid id, MarkMessagesReadRequest request, TripChatService service, CancellationToken ct) =>
            {
                await service.MarkReadAsync(id, role, request, ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);
        trips.MapPost("/{id:guid}/call", async (Guid id, TripChatService service, CancellationToken ct) => Results.Ok(await service.CallAsync(id, role, ct)))
            .Produces<MaskedCallDto>();
    }

    private static void MapLostItems(IEndpointRouteBuilder api)
    {
        var passenger = api.MapGroup("/passenger").WithTags("Passenger").RequireAuthorization(Policies.Passenger);
        passenger.MapPost("/trips/{id:guid}/lost-items", async (Guid id, LostItemRequest request, LostItemService service, CancellationToken ct) =>
            {
                var report = await service.ReportAsync(id, request, ct);
                return Results.Created($"/api/v1/passenger/lost-items/{report.Id}", report);
            })
            .Produces<LostItemDto>(StatusCodes.Status201Created)
            .Produces<ErrorEnvelope>(StatusCodes.Status422UnprocessableEntity);
        passenger.MapGet("/lost-items", async (int? page, int? pageSize, LostItemService service, CancellationToken ct) =>
                Results.Ok(await service.ListMineAsync(Paging.From(page, pageSize), ct)))
            .Produces<PagedResult<LostItemDto>>();

        var driver = api.MapGroup("/driver/lost-items").WithTags("Driver").RequireAuthorization(Policies.Driver);
        driver.MapGet("/", async (string? status, int? page, int? pageSize, LostItemService service, CancellationToken ct) =>
                Results.Ok(await service.ListForDriverAsync(QueryEnum.Parse<LostItemStatus>(status, "status"), Paging.From(page, pageSize), ct)))
            .Produces<PagedResult<DriverLostItemDto>>();
        driver.MapPost("/{id:guid}/respond", async (Guid id, LostItemRespondRequest request, LostItemService service, CancellationToken ct) =>
                Results.Ok(await service.RespondAsync(id, request, ct)))
            .Produces<DriverLostItemDto>()
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);
    }

    private static void MapAdmin(IEndpointRouteBuilder api)
    {
        var admin = api.MapGroup("/admin").WithTags("Admin safety").RequireAuthorization(Policies.Admin);
        var safety = admin.MapGroup("/safety").RequirePermission(Permissions.SafetyManage);

        safety.MapGet("/summary", async (AdminSafetyService service, CancellationToken ct) => Results.Ok(await service.SummaryAsync(ct)))
            .Produces<SafetySummaryDto>();
        safety.MapGet("/cases", async (string? status, string? priority, string? type, string? assignedTo, DateOnly? from, DateOnly? to, string? search, int? page, int? pageSize,
                AdminSafetyService service, CancellationToken ct) =>
                Results.Ok(await service.ListAsync(QueryEnum.Parse<SafetyCaseStatus>(status, "status"), QueryEnum.Parse<SafetyPriority>(priority, "priority"),
                    QueryEnum.Parse<SafetyCaseType>(type, "type"), assignedTo, from, to, search, Paging.From(page, pageSize), ct)))
            .Produces<PagedResult<AdminSafetyCaseListItemDto>>();
        safety.MapPost("/cases", async (AdminCreateCaseRequest request, AdminSafetyService service, HttpContext http, CancellationToken ct) =>
            {
                var created = await service.CreateAsync(request, http.GetLanguage(), ct);
                return Results.Created($"/api/v1/admin/safety/cases/{created.Id}", created);
            })
            .Produces<AdminSafetyCaseDetailDto>(StatusCodes.Status201Created);
        safety.MapGet("/cases/{id:guid}", async (Guid id, AdminSafetyService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.GetAsync(id, http.GetLanguage(), ct)))
            .Produces<AdminSafetyCaseDetailDto>();
        safety.MapPost("/cases/{id:guid}/assign", async (Guid id, AssignCaseRequest? request, AdminSafetyService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.AssignAsync(id, request ?? new AssignCaseRequest(null), http.GetLanguage(), ct)))
            .Produces<AdminSafetyCaseDetailDto>();
        safety.MapPost("/cases/{id:guid}/status", async (Guid id, CaseStatusRequest request, AdminSafetyService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.SetStatusAsync(id, request, http.GetLanguage(), ct)))
            .Produces<AdminSafetyCaseDetailDto>();
        safety.MapPost("/cases/{id:guid}/notes", async (Guid id, CaseNoteRequest request, AdminSafetyService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.AddNoteAsync(id, request, http.GetLanguage(), ct)))
            .Produces<AdminSafetyCaseDetailDto>();
        safety.MapPost("/cases/{id:guid}/resolve", async (Guid id, ResolveCaseRequest request, AdminSafetyService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.ResolveAsync(id, request, http.GetLanguage(), ct)))
            .Produces<AdminSafetyCaseDetailDto>();
        safety.MapGet("/alerts", async (string? status, string? type, Guid? tripId, DateOnly? from, DateOnly? to, string? search, int? page, int? pageSize,
                AdminSafetyService service, CancellationToken ct) =>
                Results.Ok(await service.AlertsAsync(QueryEnum.Parse<SafetyAlertStatus>(status, "status"), QueryEnum.Parse<SafetyAlertType>(type, "type"), tripId, from, to, search,
                    Paging.From(page, pageSize), ct)))
            .Produces<PagedResult<AdminSafetyAlertDto>>();
        safety.MapPost("/alerts/{id:guid}/dismiss", async (Guid id, DismissAlertRequest request, AdminSafetyService service, CancellationToken ct) =>
                Results.Ok(await service.DismissAlertAsync(id, request, ct)))
            .Produces<AdminSafetyAlertDto>();

        admin.MapGet("/trips/{id:guid}/messages", async (Guid id, TripChatService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.ListForAdminAsync(id, http.GetLanguage(), ct)))
            .RequirePermission(Permissions.SafetyManage)
            .Produces<List<AdminTripMessageDto>>();
        admin.MapGet("/trips/{id:guid}/shares", async (Guid id, AdminSafetyService service, CancellationToken ct) => Results.Ok(await service.TripSharesAsync(id, ct)))
            .RequirePermission(Permissions.SafetyManage)
            .Produces<List<TripShareDto>>();
        admin.MapGet("/users/{userId:guid}/trusted-contacts", async (Guid userId, AdminSafetyService service, CancellationToken ct) =>
                Results.Ok(await service.TrustedContactsAsync(userId, ct)))
            .RequirePermission(Permissions.SafetyManage)
            .Produces<List<TrustedContactDto>>();

        admin.MapGet("/lost-items", async (string? status, string? search, int? page, int? pageSize, LostItemService service, CancellationToken ct) =>
                Results.Ok(await service.AdminListAsync(QueryEnum.Parse<LostItemStatus>(status, "status"), search, Paging.From(page, pageSize), ct)))
            .RequireAnyPermission(Permissions.SafetyManage, Permissions.SupportManage)
            .Produces<PagedResult<AdminLostItemDto>>();
        admin.MapPatch("/lost-items/{id:guid}", async (Guid id, LostItemUpdateRequest request, LostItemService service, CancellationToken ct) =>
                Results.Ok(await service.AdminUpdateAsync(id, request, ct)))
            .RequireAnyPermission(Permissions.SafetyManage, Permissions.SupportManage)
            .Produces<AdminLostItemDto>();
    }

    /// <summary><c>Results.Ok(null)</c> sends an empty body; the contract requires a JSON <c>null</c> literal.</summary>
    private static IResult JsonOrNull<T>(T? value) where T : class =>
        value is null ? Results.Content("null", "application/json; charset=utf-8") : Results.Ok(value);
}
