using ATA.Api.Common;
using ATA.Api.Modules.Trips;
using ATA.Domain.Common;

namespace ATA.Api.Modules.Pricing;

public static class PricingEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        var pricing = api.MapGroup("/pricing").WithTags("Pricing");

        pricing.MapPost("/quote", async (EstimateRequest request, PassengerTripService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.QuoteAsync(request, http.GetLanguage(), ct)))
            .RequireAuthorization(Policies.Passenger)
            .Produces<QuoteResponse>();

        pricing.MapGet("/demand", async (decimal? lat, decimal? lng, ZoneResolver zones, DemandService demand, IClock clock, HttpContext http, CancellationToken ct) =>
            {
                new Validator().Require("lat", lat).Require("lng", lng)
                    .Rule("lat", lat is null or (>= -90 and <= 90), "out of range").Rule("lng", lng is null or (>= -180 and <= 180), "out of range").ThrowIfInvalid();
                var lang = http.GetLanguage();
                var now = clock.UtcNow;
                var zone = await zones.ResolveAsync(lat!.Value, lng!.Value, now, ct);
                var reading = await demand.ReadAsync(zone, null, now, ct);
                return Results.Ok(new DemandAtLocationDto(QuoteService.ToDto(zone, lang), QuoteService.ToDto(reading, lang), reading.ComputedAt));
            })
            .RequireAuthorization(Policies.Authenticated)
            .Produces<DemandAtLocationDto>();

        MapAdmin(api);
    }

    private static void MapAdmin(IEndpointRouteBuilder api)
    {
        var admin = api.MapGroup("/admin").WithTags("Admin").RequireAuthorization(Policies.Admin);

        var zones = admin.MapGroup("/zones");
        zones.MapGet("/", async (PricingAdminService service, CancellationToken ct) => Results.Ok(await service.ListZonesAsync(ct))).Produces<List<ZoneDto>>();
        zones.MapGet("/{id:guid}", async (Guid id, PricingAdminService service, CancellationToken ct) => Results.Ok(await service.GetZoneAsync(id, ct))).Produces<ZoneDto>();
        zones.MapPost("/", async (ZoneUpsertRequest request, PricingAdminService service, CancellationToken ct) =>
            {
                var created = await service.CreateZoneAsync(request, ct);
                return Results.Created($"/api/v1/admin/zones/{created.Id}", created);
            })
            .Produces<ZoneDto>(StatusCodes.Status201Created);
        zones.MapPut("/{id:guid}", async (Guid id, ZoneUpsertRequest request, PricingAdminService service, CancellationToken ct) =>
            Results.Ok(await service.UpdateZoneAsync(id, request, ct))).Produces<ZoneDto>();
        zones.MapDelete("/{id:guid}", async (Guid id, PricingAdminService service, CancellationToken ct) =>
            {
                await service.DeleteZoneAsync(id, ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);

        var rules = admin.MapGroup("/pricing-rules");
        rules.MapGet("/", async (Guid? rideCategoryId, Guid? zoneId, PricingAdminService service, CancellationToken ct) =>
            Results.Ok(await service.ListPricingRulesAsync(rideCategoryId, zoneId, ct))).Produces<List<PricingRuleDto>>();
        rules.MapGet("/{id:guid}", async (Guid id, PricingAdminService service, CancellationToken ct) => Results.Ok(await service.GetPricingRuleAsync(id, ct))).Produces<PricingRuleDto>();
        rules.MapPost("/", async (PricingRuleUpsertRequest request, PricingAdminService service, CancellationToken ct) =>
            {
                var created = await service.CreatePricingRuleAsync(request, ct);
                return Results.Created($"/api/v1/admin/pricing-rules/{created.Id}", created);
            })
            .Produces<PricingRuleDto>(StatusCodes.Status201Created);
        rules.MapPut("/{id:guid}", async (Guid id, PricingRuleUpsertRequest request, PricingAdminService service, CancellationToken ct) =>
            Results.Ok(await service.UpdatePricingRuleAsync(id, request, ct))).Produces<PricingRuleDto>();
        rules.MapDelete("/{id:guid}", async (Guid id, PricingAdminService service, CancellationToken ct) =>
            {
                await service.DeletePricingRuleAsync(id, ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);

        admin.MapPost("/pricing/simulate", async (SimulateRequest request, QuoteService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.SimulateAsync(request, http.GetLanguage(), ct)))
            .Produces<QuoteResponse>();

        var levels = admin.MapGroup("/demand-levels");
        levels.MapGet("/", async (PricingAdminService service, CancellationToken ct) => Results.Ok(await service.ListDemandLevelsAsync(ct))).Produces<List<DemandLevelDto>>();
        levels.MapPut("/{id:guid}", async (Guid id, DemandLevelUpdateRequest request, PricingAdminService service, CancellationToken ct) =>
            Results.Ok(await service.UpdateDemandLevelAsync(id, request, ct))).Produces<DemandLevelDto>();

        var demandRules = admin.MapGroup("/demand-rules");
        demandRules.MapGet("/", async (PricingAdminService service, CancellationToken ct) => Results.Ok(await service.ListDemandRulesAsync(ct))).Produces<List<DemandRuleDto>>();
        demandRules.MapPost("/", async (DemandRuleUpsertRequest request, PricingAdminService service, CancellationToken ct) =>
            {
                var created = await service.CreateDemandRuleAsync(request, ct);
                return Results.Created($"/api/v1/admin/demand-rules/{created.Id}", created);
            })
            .Produces<DemandRuleDto>(StatusCodes.Status201Created);
        demandRules.MapPut("/{id:guid}", async (Guid id, DemandRuleUpsertRequest request, PricingAdminService service, CancellationToken ct) =>
            Results.Ok(await service.UpdateDemandRuleAsync(id, request, ct))).Produces<DemandRuleDto>();
        demandRules.MapDelete("/{id:guid}", async (Guid id, PricingAdminService service, CancellationToken ct) =>
            {
                await service.DeleteDemandRuleAsync(id, ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);

        var overrides = admin.MapGroup("/demand-overrides");
        overrides.MapGet("/", async (bool? active, PricingAdminService service, CancellationToken ct) =>
            Results.Ok(await service.ListDemandOverridesAsync(active ?? false, ct))).Produces<List<DemandOverrideDto>>();
        overrides.MapPost("/", async (DemandOverrideUpsertRequest request, PricingAdminService service, CancellationToken ct) =>
            {
                var created = await service.CreateDemandOverrideAsync(request, ct);
                return Results.Created($"/api/v1/admin/demand-overrides/{created.Id}", created);
            })
            .Produces<DemandOverrideDto>(StatusCodes.Status201Created);
        overrides.MapPut("/{id:guid}", async (Guid id, DemandOverrideUpsertRequest request, PricingAdminService service, CancellationToken ct) =>
            Results.Ok(await service.UpdateDemandOverrideAsync(id, request, ct))).Produces<DemandOverrideDto>();
        overrides.MapDelete("/{id:guid}", async (Guid id, PricingAdminService service, CancellationToken ct) =>
            {
                await service.DeleteDemandOverrideAsync(id, ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);

        admin.MapGet("/demand/current", async (PricingAdminService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.CurrentDemandAsync(http.GetLanguage(), ct)))
            .Produces<List<DemandCurrentZoneDto>>();
    }
}
