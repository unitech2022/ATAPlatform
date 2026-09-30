using ATA.Api.Common;

namespace ATA.Api.Modules.Reporting;

public static class ReportingEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        var reports = api.MapGroup("/admin/reports").WithTags("Admin reports").RequireAuthorization(Policies.Admin);

        reports.MapGet("/definitions", (ReportService service, HttpContext http) => Results.Ok(service.Definitions(http.GetLanguage())))
            .RequirePermission(Permissions.ReportsView)
            .Produces<List<KpiDefinitionDto>>();

        reports.MapGet("/kpis", async (DateOnly? from, DateOnly? to, Guid? cityId, Guid? zoneId, Guid? rideCategoryId, string? compare, string? metrics,
                    ReportService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.KpisAsync(from, to, cityId, zoneId, rideCategoryId, compare, metrics, http.GetLanguage(), ct)))
            .RequirePermission(Permissions.ReportsView)
            .Produces<KpisResponse>()
            .Produces<ErrorEnvelope>(StatusCodes.Status422UnprocessableEntity);

        reports.MapGet("/kpis/{code}/series", async (string code, DateOnly? from, DateOnly? to, string? granularity, Guid? cityId, Guid? zoneId, Guid? rideCategoryId,
                    ReportService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.SeriesAsync(code, from, to, granularity, cityId, zoneId, rideCategoryId, http.GetLanguage(), ct)))
            .RequirePermission(Permissions.ReportsView)
            .Produces<KpiSeriesResponse>();

        reports.MapGet("/breakdown", async (string? metric, DateOnly? from, DateOnly? to, string? groupBy, Guid? cityId, Guid? zoneId, Guid? rideCategoryId,
                    ReportService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.BreakdownAsync(metric, from, to, groupBy, cityId, zoneId, rideCategoryId, http.GetLanguage(), ct)))
            .RequirePermission(Permissions.ReportsView)
            .Produces<KpiBreakdownResponse>();

        // CSV (UTF-8 with BOM) streamed once the row limit has been checked.
        reports.MapGet("/export", async (string? dataset, DateOnly? from, DateOnly? to, Guid? cityId, Guid? zoneId, Guid? rideCategoryId, string? format,
                    ReportExportService service, HttpContext http, CancellationToken ct) =>
                {
                    var export = await service.ExportAsync(dataset, from, to, cityId, zoneId, rideCategoryId, format, http.GetLanguage(), ct);
                    return Results.Stream(stream => export.WriteAsync(stream, ct), "text/csv; charset=utf-8", export.FileName);
                })
            .RequirePermission(Permissions.ReportsExport)
            .Produces(StatusCodes.Status200OK, contentType: "text/csv")
            .Produces<ErrorEnvelope>(StatusCodes.Status422UnprocessableEntity);

        reports.MapPost("/snapshots/rebuild", async (SnapshotRebuildRequest request, ReportSnapshotService service, CancellationToken ct) =>
                Results.Accepted(value: await service.RebuildRequestedAsync(request, ct)))
            .RequirePermission(Permissions.ReportsExport)
            .Produces<SnapshotRebuildResponse>(StatusCodes.Status202Accepted);
    }
}
