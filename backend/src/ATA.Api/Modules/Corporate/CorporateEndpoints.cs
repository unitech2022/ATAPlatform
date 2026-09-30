using ATA.Api.Common;
using ATA.Api.Modules.Cancellation;
using ATA.Api.Modules.Payments;
using ATA.Api.Modules.Pricing;
using ATA.Api.Modules.Trips;
using ATA.Domain.Common;
using ATA.Domain.Corporate;
using ATA.Domain.Trips;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Corporate;

/// <summary>F19 endpoints (doc 12 §F19.4): the rider's <c>/passenger/corporate</c>, the company portal <c>/corporate</c> and the platform admin <c>/admin/corporate</c>.</summary>
public static class CorporateEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        MapRider(api);
        MapPortal(api);
        MapAdmin(api);
    }

    // ----- rider (employee) -----

    private static void MapRider(IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/passenger/corporate").WithTags("Passenger corporate").RequireAuthorization(Policies.Passenger);

        group.MapGet("/", async (CorporateMemberService service, CancellationToken ct) => JsonOrNull(await service.GetAsync(ct)))
            .Produces<CorporateMembershipResponse>();

        group.MapGet("/invitations", async (CorporateMemberService service, CancellationToken ct) => Results.Ok(await service.InvitationsAsync(ct)))
            .Produces<IReadOnlyList<CorporateInvitationDto>>();

        group.MapPost("/invitations/{id:guid}/accept", async (Guid id, CorporateMemberService service, CancellationToken ct) =>
            {
                await service.AcceptAsync(id, ct);
                return Results.Ok();
            })
            .Produces(StatusCodes.Status200OK)
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict)
            .Produces<ErrorEnvelope>(StatusCodes.Status410Gone);

        group.MapPost("/invitations/{id:guid}/decline", async (Guid id, CorporateMemberService service, CancellationToken ct) =>
            {
                await service.DeclineAsync(id, ct);
                return Results.Ok();
            })
            .Produces(StatusCodes.Status200OK)
            .Produces<ErrorEnvelope>(StatusCodes.Status410Gone);
    }

    // ----- company portal -----

    private static void MapPortal(IEndpointRouteBuilder api)
    {
        var corporate = api.MapGroup("/corporate").WithTags("Corporate").RequireAuthorization(Policies.CorporateAdmin);

        corporate.MapGet("/account", async (CorporateContext context, CorporateAccountService service, CancellationToken ct) =>
                Results.Ok(await service.GetAsync((await context.RequireAsync(ct)).Account, ct)))
            .Produces<CorporateAccountDto>();
        corporate.MapPut("/account", async (UpdateCorporateAccountRequest request, CorporateContext context, CorporateAccountService service, CancellationToken ct) =>
                Results.Ok(await service.UpdateAsync((await context.RequireAsync(ct)).Account, request, CorporateMembershipService.AdminActor, ct)))
            .Produces<CorporateAccountDto>();
        corporate.MapGet("/dashboard", async (CorporateContext context, CorporateAccountService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.DashboardAsync((await context.RequireAsync(ct)).Account, http.GetLanguage(), ct)))
            .Produces<CorporateDashboardDto>();

        // employees
        corporate.MapGet("/employees", async (string? status, string? department, Guid? costCenterId, string? search, int? page, int? pageSize, CorporateContext context, CorporateEmployeeService service, CancellationToken ct) =>
                Results.Ok(await service.ListAsync((await context.RequireAsync(ct)).Account, QueryEnum.Parse<CorporateUserStatus>(status, "status"), department, costCenterId, search, Paging.From(page, pageSize), ct)))
            .Produces<PagedResult<CorporateEmployeeDto>>();
        corporate.MapPost("/employees", async (InviteEmployeeRequest request, CorporateContext context, CorporateEmployeeService service, CancellationToken ct) =>
            {
                var scope = await context.RequireAsync(ct);
                var created = await service.InviteAsync(scope.Account, request, scope.UserId, CorporateMembershipService.AdminActor, ct);
                return Results.Created($"/api/v1/corporate/employees/{created.Id}", created);
            })
            .Produces<CorporateEmployeeDto>(StatusCodes.Status201Created)
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);
        corporate.MapPost("/employees/import", async (HttpRequest request, CorporateContext context, CorporateEmployeeService service, CancellationToken ct) =>
            {
                if (!request.HasFormContentType)
                {
                    throw new DomainException(ErrorCodes.ValidationFailed, new { body = "multipart/form-data expected" });
                }

                var scope = await context.RequireAsync(ct);
                IFormCollection form;
                try
                {
                    form = await request.ReadFormAsync(ct);
                }
                catch (Exception ex) when (ex is InvalidDataException or BadHttpRequestException or IOException)
                {
                    throw new DomainException(ErrorCodes.ValidationFailed, new { file = "required" });
                }

                var file = form.Files.GetFile("file") ?? throw new DomainException(ErrorCodes.ValidationFailed, new { file = "required" });
                await using var stream = file.OpenReadStream();
                return Results.Ok(await service.ImportAsync(scope.Account, stream, scope.UserId, ct));
            })
            .DisableAntiforgery()
            .Accepts<IFormFile>("multipart/form-data")
            .Produces<ImportResultDto>();
        corporate.MapGet("/employees/{id:guid}", async (Guid id, CorporateContext context, CorporateEmployeeService service, CancellationToken ct) =>
                Results.Ok(await service.GetAsync((await context.RequireAsync(ct)).Account, id, ct)))
            .Produces<CorporateEmployeeDto>();
        corporate.MapPut("/employees/{id:guid}", async (Guid id, UpdateEmployeeRequest request, CorporateContext context, CorporateEmployeeService service, CancellationToken ct) =>
                Results.Ok(await service.UpdateAsync((await context.RequireAsync(ct)).Account, id, request, CorporateMembershipService.AdminActor, ct)))
            .Produces<CorporateEmployeeDto>();
        corporate.MapPost("/employees/{id:guid}/disable", async (Guid id, CorporateContext context, CorporateEmployeeService service, CancellationToken ct) =>
                Results.Ok(await service.DisableAsync((await context.RequireAsync(ct)).Account, id, CorporateMembershipService.AdminActor, ct)))
            .Produces<CorporateEmployeeDto>()
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);
        corporate.MapPost("/employees/{id:guid}/enable", async (Guid id, CorporateContext context, CorporateEmployeeService service, CancellationToken ct) =>
                Results.Ok(await service.EnableAsync((await context.RequireAsync(ct)).Account, id, CorporateMembershipService.AdminActor, ct)))
            .Produces<CorporateEmployeeDto>();
        corporate.MapPost("/employees/{id:guid}/resend-invitation", async (Guid id, CorporateContext context, CorporateEmployeeService service, CancellationToken ct) =>
                Results.Ok(await service.ResendAsync((await context.RequireAsync(ct)).Account, id, CorporateMembershipService.AdminActor, ct)))
            .Produces<CorporateEmployeeDto>();
        corporate.MapDelete("/employees/{id:guid}", async (Guid id, CorporateContext context, CorporateEmployeeService service, CancellationToken ct) =>
            {
                await service.RevokeInvitationAsync((await context.RequireAsync(ct)).Account, id, CorporateMembershipService.AdminActor, ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);

        // policies and cost centres
        corporate.MapGet("/policies", async (CorporateContext context, CorporatePolicyService service, CancellationToken ct) =>
                Results.Ok(await service.ListPoliciesAsync((await context.RequireAsync(ct)).Account.Id, ct)))
            .Produces<IReadOnlyList<CorporatePolicyDto>>();
        corporate.MapPost("/policies", async (CorporatePolicyRequest request, CorporateContext context, CorporatePolicyService service, CancellationToken ct) =>
            {
                var created = await service.CreatePolicyAsync((await context.RequireAsync(ct)).Account, request, CorporateMembershipService.AdminActor, ct);
                return Results.Created($"/api/v1/corporate/policies/{created.Id}", created);
            })
            .Produces<CorporatePolicyDto>(StatusCodes.Status201Created);
        corporate.MapPut("/policies/{id:guid}", async (Guid id, CorporatePolicyRequest request, CorporateContext context, CorporatePolicyService service, CancellationToken ct) =>
                Results.Ok(await service.UpdatePolicyAsync((await context.RequireAsync(ct)).Account.Id, id, request, CorporateMembershipService.AdminActor, ct)))
            .Produces<CorporatePolicyDto>();
        corporate.MapDelete("/policies/{id:guid}", async (Guid id, CorporateContext context, CorporatePolicyService service, CancellationToken ct) =>
            {
                await service.DeletePolicyAsync((await context.RequireAsync(ct)).Account.Id, id, CorporateMembershipService.AdminActor, ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);
        corporate.MapPost("/policies/{id:guid}/default", async (Guid id, CorporateContext context, CorporatePolicyService service, CancellationToken ct) =>
                Results.Ok(await service.SetDefaultAsync((await context.RequireAsync(ct)).Account, id, CorporateMembershipService.AdminActor, ct)))
            .Produces<CorporatePolicyDto>();

        corporate.MapGet("/cost-centers", async (CorporateContext context, CorporatePolicyService service, CancellationToken ct) =>
                Results.Ok(await service.ListCostCentersAsync((await context.RequireAsync(ct)).Account.Id, ct)))
            .Produces<IReadOnlyList<CostCenterDto>>();
        corporate.MapPost("/cost-centers", async (CostCenterRequest request, CorporateContext context, CorporatePolicyService service, CancellationToken ct) =>
            {
                var created = await service.CreateCostCenterAsync((await context.RequireAsync(ct)).Account.Id, request, CorporateMembershipService.AdminActor, ct);
                return Results.Created($"/api/v1/corporate/cost-centers/{created.Id}", created);
            })
            .Produces<CostCenterDto>(StatusCodes.Status201Created);
        corporate.MapPut("/cost-centers/{id:guid}", async (Guid id, CostCenterRequest request, CorporateContext context, CorporatePolicyService service, CancellationToken ct) =>
                Results.Ok(await service.UpdateCostCenterAsync((await context.RequireAsync(ct)).Account.Id, id, request, CorporateMembershipService.AdminActor, ct)))
            .Produces<CostCenterDto>();
        corporate.MapDelete("/cost-centers/{id:guid}", async (Guid id, CorporateContext context, CorporatePolicyService service, CancellationToken ct) =>
            {
                await service.DeleteCostCenterAsync((await context.RequireAsync(ct)).Account.Id, id, CorporateMembershipService.AdminActor, ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);

        // bookings
        corporate.MapPost("/bookings/quote", async (CorporateQuoteRequest request, CorporateContext context, CorporateBookingService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.QuoteAsync(await context.RequireAsync(ct), request, http.GetLanguage(), ct)))
            .Produces<QuoteResponse>();
        corporate.MapPost("/bookings", async (CorporateBookingRequest request, CorporateContext context, CorporateBookingService service, HttpContext http, CancellationToken ct) =>
            {
                var trip = await service.BookAsync(await context.RequireAsync(ct), request, http.GetLanguage(), ct);
                return Results.Created($"/api/v1/corporate/bookings/{trip.Id}", trip);
            })
            .Produces<TripDto>(StatusCodes.Status201Created)
            .Produces<ErrorEnvelope>(StatusCodes.Status422UnprocessableEntity);
        corporate.MapGet("/bookings", async (string? status, DateOnly? from, DateOnly? to, Guid? employeeId, bool? isGuest, int? page, int? pageSize, CorporateContext context, CorporateBookingService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.ListAsync((await context.RequireAsync(ct)).Account, status, from, to, employeeId, isGuest, Paging.From(page, pageSize), http.GetLanguage(), ct)))
            .Produces<PagedResult<CorporateTripSummaryDto>>();
        corporate.MapGet("/bookings/{tripId:guid}", async (Guid tripId, CorporateContext context, CorporateBookingService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.GetAsync((await context.RequireAsync(ct)).Account, tripId, http.GetLanguage(), ct)))
            .Produces<TripDto>();
        corporate.MapPost("/bookings/{tripId:guid}/cancel/preview", async (Guid tripId, CancelPreviewRequest? request, CorporateContext context, CorporateBookingService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.CancelPreviewAsync((await context.RequireAsync(ct)).Account, tripId, request, http.GetLanguage(), ct)))
            .Produces<CancelPreviewDto>();
        corporate.MapPost("/bookings/{tripId:guid}/cancel", async (Guid tripId, CorporateCancelRequest request, CorporateContext context, CorporateBookingService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.CancelAsync(await context.RequireAsync(ct), tripId, request, http.GetLanguage(), ct)))
            .Produces<TripDto>();

        // invoices
        corporate.MapGet("/invoices", async (string? status, int? page, int? pageSize, CorporateContext context, CorporateInvoiceService service, CancellationToken ct) =>
                Results.Ok(await service.ListAsync((await context.RequireAsync(ct)).Account.Id, QueryEnum.Parse<CorporateInvoiceStatus>(status, "status"), null, null, false, Paging.From(page, pageSize), ct)))
            .Produces<PagedResult<CorporateInvoiceDto>>();
        corporate.MapGet("/invoices/{id:guid}", async (Guid id, int? page, int? pageSize, CorporateContext context, CorporateInvoiceService service, CancellationToken ct) =>
                Results.Ok(await service.GetAsync(id, (await context.RequireAsync(ct)).Account.Id, false, Paging.From(page, pageSize ?? 50), ct)))
            .Produces<CorporateInvoiceDetailDto>();
        corporate.MapGet("/invoices/{id:guid}/pdf", async (Guid id, CorporateContext context, CorporateInvoiceService service, CancellationToken ct) =>
            {
                var file = await service.GetPdfAsync(id, (await context.RequireAsync(ct)).Account.Id, false, ct);
                return Results.File(file.Content, file.ContentType, file.FileName);
            })
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf");
        corporate.MapGet("/invoices/{id:guid}/export", async (Guid id, string? format, CorporateContext context, CorporateInvoiceService service, CancellationToken ct) =>
            {
                EnsureCsv(format);
                var file = await service.ExportCsvAsync(id, (await context.RequireAsync(ct)).Account.Id, false, ct);
                return Results.File(file.Content, file.ContentType, file.FileName);
            })
            .Produces(StatusCodes.Status200OK, contentType: "text/csv");

        // reports
        corporate.MapGet("/reports/summary", async (DateOnly? from, DateOnly? to, string? groupBy, CorporateContext context, CorporateReportService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.SummaryAsync((await context.RequireAsync(ct)).Account.Id, from, to, QueryEnum.Parse<ReportGroupBy>(groupBy, "groupBy") ?? ReportGroupBy.Employee, http.GetLanguage(), ct)))
            .Produces<ReportSummaryDto>();
        corporate.MapGet("/reports/trips", async (DateOnly? from, DateOnly? to, Guid? employeeId, string? department, Guid? costCenterId, int? page, int? pageSize, CorporateContext context, CorporateReportService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.TripsAsync((await context.RequireAsync(ct)).Account.Id, from, to, employeeId, department, costCenterId, Paging.From(page, pageSize), http.GetLanguage(), ct)))
            .Produces<PagedResult<ReportTripRowDto>>();
        corporate.MapGet("/reports/trips/export", async (string? format, DateOnly? from, DateOnly? to, Guid? employeeId, string? department, Guid? costCenterId, CorporateContext context, CorporateReportService service, HttpContext http, CancellationToken ct) =>
            {
                EnsureCsv(format);
                var file = await service.ExportTripsAsync((await context.RequireAsync(ct)).Account.Id, from, to, employeeId, department, costCenterId, http.GetLanguage(), ct);
                return Results.File(file.Content, "text/csv; charset=utf-8", file.FileName);
            })
            .Produces(StatusCodes.Status200OK, contentType: "text/csv");

        // API keys (Corporate:ApiKeysEnabled)
        corporate.MapGet("/api-keys", async (CorporateContext context, CorporateApiKeyService service, CancellationToken ct) =>
                Results.Ok(await service.ListAsync((await context.RequireAsync(ct)).Account.Id, ct)))
            .Produces<IReadOnlyList<CorporateApiKeyDto>>();
        corporate.MapPost("/api-keys", async (CreateApiKeyRequest request, CorporateContext context, CorporateApiKeyService service, CancellationToken ct) =>
            {
                var scope = await context.RequireAsync(ct);
                var created = await service.CreateAsync(scope.Account.Id, scope.UserId, request, ct);
                return Results.Created($"/api/v1/corporate/api-keys/{created.Id}", created);
            })
            .Produces<CorporateApiKeyDto>(StatusCodes.Status201Created);
        corporate.MapDelete("/api-keys/{id:guid}", async (Guid id, CorporateContext context, CorporateApiKeyService service, CancellationToken ct) =>
            {
                await service.RevokeAsync((await context.RequireAsync(ct)).Account.Id, id, ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);
    }

    // ----- platform admin -----

    private static void MapAdmin(IEndpointRouteBuilder api)
    {
        var admin = api.MapGroup("/admin/corporate").WithTags("Admin corporate").RequireAuthorization(Policies.Admin).RequirePermission(Permissions.CorporateManage);

        admin.MapGet("/accounts", async (string? status, Guid? cityId, string? search, int? page, int? pageSize, CorporateAdminService service, CancellationToken ct) =>
                Results.Ok(await service.ListAsync(QueryEnum.Parse<CorporateAccountStatus>(status, "status"), cityId, search, Paging.From(page, pageSize), ct)))
            .Produces<PagedResult<AdminCorporateAccountListItemDto>>();
        admin.MapPost("/accounts", async (CorporateAccountInput input, CorporateAdminService service, CancellationToken ct) =>
            {
                var created = await service.CreateAsync(input, ct);
                return Results.Created($"/api/v1/admin/corporate/accounts/{created.Id}", created);
            })
            .Produces<AdminCorporateAccountDto>(StatusCodes.Status201Created)
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);
        admin.MapGet("/accounts/{id:guid}", async (Guid id, CorporateAdminService service, CancellationToken ct) => Results.Ok(await service.GetAsync(id, ct)))
            .Produces<AdminCorporateAccountDto>();
        admin.MapPut("/accounts/{id:guid}", async (Guid id, CorporateAccountInput input, CorporateAdminService service, CancellationToken ct) => Results.Ok(await service.UpdateAsync(id, input, ct)))
            .Produces<AdminCorporateAccountDto>();
        admin.MapPost("/accounts/{id:guid}/activate", async (Guid id, CorporateAdminService service, CancellationToken ct) => Results.Ok(await service.ActivateAsync(id, ct)))
            .Produces<AdminCorporateAccountDto>()
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);
        admin.MapPost("/accounts/{id:guid}/suspend", async (Guid id, ReasonRequest request, CorporateAdminService service, CancellationToken ct) => Results.Ok(await service.SuspendAsync(id, request.Reason, ct)))
            .Produces<AdminCorporateAccountDto>();
        admin.MapPost("/accounts/{id:guid}/close", async (Guid id, ReasonRequest request, CorporateAdminService service, CancellationToken ct) => Results.Ok(await service.CloseAsync(id, request.Reason, ct)))
            .Produces<AdminCorporateAccountDto>();

        admin.MapPost("/accounts/{id:guid}/admins", async (Guid id, InviteAdminRequest request, CorporateAdminService accounts, CorporateEmployeeService employees, ICurrentUser user, CancellationToken ct) =>
            {
                var created = await employees.InviteAdminAsync(await accounts.RequireAccountAsync(id, ct), request, user.UserId, ct);
                return Results.Created($"/api/v1/admin/corporate/accounts/{id}/employees", created);
            })
            .Produces<CorporateEmployeeDto>(StatusCodes.Status201Created);
        admin.MapGet("/accounts/{id:guid}/employees", async (Guid id, string? status, string? department, string? search, int? page, int? pageSize, CorporateAdminService accounts, CorporateEmployeeService employees, CancellationToken ct) =>
                Results.Ok(await employees.ListAsync(await accounts.RequireAccountAsync(id, ct), QueryEnum.Parse<CorporateUserStatus>(status, "status"), department, null, search, Paging.From(page, pageSize), ct)))
            .Produces<PagedResult<CorporateEmployeeDto>>();
        admin.MapPost("/accounts/{id:guid}/employees/{employeeId:guid}/disable", async (Guid id, Guid employeeId, CorporateAdminService accounts, CorporateEmployeeService employees, CancellationToken ct) =>
                Results.Ok(await employees.DisableAsync(await accounts.RequireAccountAsync(id, ct), employeeId, null, ct)))
            .Produces<CorporateEmployeeDto>();
        admin.MapPost("/accounts/{id:guid}/employees/{employeeId:guid}/enable", async (Guid id, Guid employeeId, CorporateAdminService accounts, CorporateEmployeeService employees, CancellationToken ct) =>
                Results.Ok(await employees.EnableAsync(await accounts.RequireAccountAsync(id, ct), employeeId, null, ct)))
            .Produces<CorporateEmployeeDto>();
        admin.MapPost("/accounts/{id:guid}/employees/{employeeId:guid}/resend-invitation", async (Guid id, Guid employeeId, CorporateAdminService accounts, CorporateEmployeeService employees, CancellationToken ct) =>
                Results.Ok(await employees.ResendAsync(await accounts.RequireAccountAsync(id, ct), employeeId, null, ct)))
            .Produces<CorporateEmployeeDto>();
        admin.MapDelete("/accounts/{id:guid}/employees/{employeeId:guid}", async (Guid id, Guid employeeId, CorporateAdminService accounts, CorporateEmployeeService employees, CancellationToken ct) =>
            {
                await employees.RevokeInvitationAsync(await accounts.RequireAccountAsync(id, ct), employeeId, null, ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);

        // read-only views of the company's configuration and trips
        admin.MapGet("/accounts/{id:guid}/policies", async (Guid id, CorporateAdminService accounts, CorporatePolicyService service, CancellationToken ct) =>
                Results.Ok(await service.ListPoliciesAsync((await accounts.RequireAccountAsync(id, ct)).Id, ct)))
            .Produces<IReadOnlyList<CorporatePolicyDto>>();
        admin.MapGet("/accounts/{id:guid}/cost-centers", async (Guid id, CorporateAdminService accounts, CorporatePolicyService service, CancellationToken ct) =>
                Results.Ok(await service.ListCostCentersAsync((await accounts.RequireAccountAsync(id, ct)).Id, ct)))
            .Produces<IReadOnlyList<CostCenterDto>>();
        admin.MapGet("/accounts/{id:guid}/trips", async (Guid id, string? status, bool? isGuest, DateOnly? from, DateOnly? to, string? search, int? page, int? pageSize, CorporateAdminService accounts, CorporateReportService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.AdminTripsAsync((await accounts.RequireAccountAsync(id, ct)).Id, QueryEnum.Parse<TripStatus>(status, "status"), isGuest, from, to, search, Paging.From(page, pageSize), http.GetLanguage(), ct)))
            .Produces<PagedResult<AdminCorporateTripRowDto>>();
        admin.MapGet("/accounts/{id:guid}/trips/export", async (Guid id, string? format, string? status, bool? isGuest, DateOnly? from, DateOnly? to, string? search, CorporateAdminService accounts, CorporateReportService service, HttpContext http, CancellationToken ct) =>
            {
                EnsureCsv(format);
                var file = await service.ExportAdminTripsAsync((await accounts.RequireAccountAsync(id, ct)).Id, QueryEnum.Parse<TripStatus>(status, "status"), isGuest, from, to, search, http.GetLanguage(), ct);
                return Results.File(file.Content, "text/csv; charset=utf-8", file.FileName);
            })
            .Produces(StatusCodes.Status200OK, contentType: "text/csv");

        admin.MapGet("/accounts/{id:guid}/adjustments", async (Guid id, CorporateAdminService accounts, CorporateInvoiceService service, CancellationToken ct) =>
                Results.Ok(await service.ListAdjustmentsAsync((await accounts.RequireAccountAsync(id, ct)).Id, ct)))
            .Produces<IReadOnlyList<AdjustmentDto>>();
        admin.MapPost("/accounts/{id:guid}/adjustments", async (Guid id, AdjustmentRequest request, CorporateInvoiceService service, ICurrentUser user, CancellationToken ct) =>
            {
                var created = await service.AddAdjustmentAsync(id, request, user.UserId, ct);
                return Results.Created($"/api/v1/admin/corporate/accounts/{id}/adjustments", created);
            })
            .Produces<AdjustmentDto>(StatusCodes.Status201Created);

        // invoices
        admin.MapGet("/invoices", async (Guid? accountId, string? status, DateOnly? from, DateOnly? to, int? page, int? pageSize, CorporateInvoiceService service, CancellationToken ct) =>
                Results.Ok(await service.ListAsync(accountId, QueryEnum.Parse<CorporateInvoiceStatus>(status, "status"), from, to, true, Paging.From(page, pageSize), ct)))
            .Produces<PagedResult<CorporateInvoiceDto>>();
        admin.MapGet("/invoices/{id:guid}", async (Guid id, int? page, int? pageSize, CorporateInvoiceService service, CancellationToken ct) =>
                Results.Ok(await service.GetAsync(id, null, true, Paging.From(page, pageSize ?? 50), ct)))
            .Produces<CorporateInvoiceDetailDto>();
        admin.MapPost("/accounts/{id:guid}/invoices/generate", async (Guid id, GenerateInvoiceRequest request, CorporateInvoiceService service, ICurrentUser user, CancellationToken ct) =>
            {
                new Validator().Require(nameof(request.PeriodStart), request.PeriodStart).ThrowIfInvalid();
                var invoice = await service.GenerateAsync(id, request.PeriodStart!.Value, user.UserId, ct);
                return Results.Created($"/api/v1/admin/corporate/invoices/{invoice.Id}", await service.GetDtoAsync(invoice.Id, ct));
            })
            .Produces<CorporateInvoiceDto>(StatusCodes.Status201Created)
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);
        admin.MapPost("/invoices/{id:guid}/issue", async (Guid id, CorporateInvoiceService service, ICurrentUser user, CancellationToken ct) =>
            {
                await service.IssueAsync(id, user.UserId, systemActor: false, ct);
                return Results.Ok(await service.GetDtoAsync(id, ct));
            })
            .Produces<CorporateInvoiceDto>()
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);
        admin.MapPost("/invoices/{id:guid}/mark-paid", async (Guid id, MarkInvoicePaidRequest request, CorporateInvoiceService service, CancellationToken ct) =>
            {
                await service.MarkPaidAsync(id, request.Amount, request.Reference, request.PaidAt, ct);
                return Results.Ok(await service.GetDtoAsync(id, ct));
            })
            .Produces<CorporateInvoiceDto>()
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);
        admin.MapPost("/invoices/{id:guid}/void", async (Guid id, ReasonRequest request, CorporateInvoiceService service, CancellationToken ct) =>
            {
                await service.VoidAsync(id, request.Reason, ct);
                return Results.Ok(await service.GetDtoAsync(id, ct));
            })
            .Produces<CorporateInvoiceDto>()
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);
        admin.MapGet("/invoices/{id:guid}/pdf", async (Guid id, CorporateInvoiceService service, CancellationToken ct) =>
            {
                var file = await service.GetPdfAsync(id, null, true, ct);
                return Results.File(file.Content, file.ContentType, file.FileName);
            })
            .Produces(StatusCodes.Status200OK, contentType: "application/pdf");

        // receivables need payments.view too
        admin.MapGet("/receivables", async (CorporateAdminService service, CancellationToken ct) => Results.Ok(await service.ReceivablesAsync(ct)))
            .RequirePermission(Permissions.PaymentsView)
            .Produces<IReadOnlyList<CorporateReceivableDto>>();
    }

    private static void EnsureCsv(string? format) => new Validator().Rule(nameof(format), format is null or "csv", "must be csv").ThrowIfInvalid();

    /// <summary><c>Results.Ok(null)</c> sends an empty body; the contract requires a JSON <c>null</c> literal.</summary>
    private static IResult JsonOrNull<T>(T? value) where T : class =>
        value is null ? Results.Content("null", "application/json; charset=utf-8") : Results.Ok(value);
}
