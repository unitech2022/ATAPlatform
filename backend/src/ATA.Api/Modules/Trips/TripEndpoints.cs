using ATA.Api.Common;
using ATA.Api.Modules.Drivers;
using ATA.Api.Modules.Passengers;
using ATA.Api.Modules.Pricing;
using ATA.Domain.Trips;

namespace ATA.Api.Modules.Trips;

public static class TripEndpoints
{
    public static void Map(IEndpointRouteBuilder api)
    {
        MapPassenger(api);
        MapDriver(api);
        MapAdmin(api);
    }

    private static void MapPassenger(IEndpointRouteBuilder api)
    {
        var trips = api.MapGroup("/passenger/trips").WithTags("Passenger").RequireAuthorization(Policies.Passenger);

        trips.MapGet("/", async (string? status, int? page, int? pageSize, PassengerTripService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.ListAsync(status, Paging.From(page, pageSize), http.GetLanguage(), ct)))
            .Produces<PagedResult<TripSummaryDto>>();

        // Alias of POST /pricing/quote kept from F8; returns the same payload.
        trips.MapPost("/estimate", async (EstimateRequest request, PassengerTripService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.QuoteAsync(request, http.GetLanguage(), ct)))
            .Produces<QuoteResponse>();

        trips.MapPost("/", async (CreateTripRequest request, PassengerTripService service, HttpContext http, CancellationToken ct) =>
            {
                var trip = await service.CreateAsync(request, http.GetLanguage(), ct);
                return Results.Created($"/api/v1/passenger/trips/{trip.Id}", trip);
            })
            .Produces<TripDto>(StatusCodes.Status201Created)
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict)
            .Produces<ErrorEnvelope>(StatusCodes.Status422UnprocessableEntity);

        trips.MapGet("/active", async (PassengerTripService service, HttpContext http, CancellationToken ct) =>
                JsonOrNull(await service.GetActiveAsync(http.GetLanguage(), ct)))
            .Produces<TripDto>();

        trips.MapGet("/{id:guid}", async (Guid id, PassengerTripService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.GetAsync(id, http.GetLanguage(), ct)))
            .Produces<TripDto>();

        trips.MapPost("/{id:guid}/cancel", async (Guid id, CancelTripRequest request, PassengerTripService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.CancelAsync(id, request, http.GetLanguage(), ct)))
            .Produces<TripDto>();
    }

    private static void MapDriver(IEndpointRouteBuilder api)
    {
        var driver = api.MapGroup("/driver").WithTags("Driver").RequireAuthorization(Policies.Driver);

        driver.MapPut("/location", async (DriverLocationRequest request, DriverTripService service, CancellationToken ct) =>
            {
                await service.UpdateLocationAsync(request, ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);

        driver.MapGet("/offers/active", async (DriverTripService service, CancellationToken ct) => JsonOrNull(await service.GetActiveOfferAsync(ct)))
            .Produces<OfferDto>();

        driver.MapPost("/offers/{id:guid}/accept", async (Guid id, DriverTripService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.AcceptOfferAsync(id, http.GetLanguage(), ct)))
            .Produces<TripDto>()
            .Produces<ErrorEnvelope>(StatusCodes.Status409Conflict);

        driver.MapPost("/offers/{id:guid}/reject", async (Guid id, RejectOfferRequest? request, DriverTripService service, CancellationToken ct) =>
            {
                await service.RejectOfferAsync(id, request, ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);

        var trips = driver.MapGroup("/trips");
        trips.MapGet("/", async (string? status, int? page, int? pageSize, DriverTripService service, CancellationToken ct) =>
                Results.Ok(await service.ListAsync(status, Paging.From(page, pageSize), ct)))
            .Produces<PagedResult<DriverTripDto>>();

        trips.MapGet("/active", async (DriverTripService service, HttpContext http, CancellationToken ct) =>
                JsonOrNull(await service.GetActiveTripAsync(http.GetLanguage(), ct)))
            .Produces<TripDto>();

        trips.MapPost("/{id:guid}/en-route", async (Guid id, DriverTripService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.EnRouteAsync(id, http.GetLanguage(), ct)))
            .Produces<TripDto>();

        trips.MapPost("/{id:guid}/arrived", async (Guid id, DriverTripService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.ArrivedAsync(id, http.GetLanguage(), ct)))
            .Produces<TripDto>();

        trips.MapPost("/{id:guid}/verify-pin", async (Guid id, VerifyPinRequest request, DriverTripService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.VerifyPinAsync(id, request, http.GetLanguage(), ct)))
            .Produces<TripDto>()
            .Produces<ErrorEnvelope>(StatusCodes.Status400BadRequest)
            .Produces<ErrorEnvelope>(StatusCodes.Status429TooManyRequests);

        trips.MapPost("/{id:guid}/start", async (Guid id, DriverTripService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.StartAsync(id, http.GetLanguage(), ct)))
            .Produces<TripDto>();

        trips.MapPost("/{id:guid}/complete", async (Guid id, CompleteTripRequest? request, DriverTripService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.CompleteAsync(id, request, http.GetLanguage(), ct)))
            .Produces<TripDto>();

        trips.MapPost("/{id:guid}/cancel", async (Guid id, CancelTripRequest request, DriverTripService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.CancelAsync(id, request, http.GetLanguage(), ct)))
            .Produces<TripDto>();
    }

    private static void MapAdmin(IEndpointRouteBuilder api)
    {
        var admin = api.MapGroup("/admin").WithTags("Admin").RequireAuthorization(Policies.Admin);

        admin.MapGet("/trips", async (string? status, DateOnly? from, DateOnly? to, string? search, int? page, int? pageSize, AdminTripService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.ListAsync(QueryEnum.Parse<TripStatus>(status, "status"), from, to, search, Paging.From(page, pageSize), http.GetLanguage(), ct)))
            .Produces<PagedResult<AdminTripListItemDto>>();

        admin.MapGet("/trips/{id:guid}", async (Guid id, AdminTripService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.GetAsync(id, http.GetLanguage(), ct)))
            .Produces<AdminTripDetailDto>();

        admin.MapPost("/trips/{id:guid}/cancel", async (Guid id, AdminCancelTripRequest request, AdminTripService service, HttpContext http, CancellationToken ct) =>
                Results.Ok(await service.CancelAsync(id, request, http.GetLanguage(), ct)))
            .Produces<AdminTripDetailDto>();

        admin.MapGet("/live", async (AdminTripService service, CancellationToken ct) => Results.Ok(await service.GetLiveAsync(ct)))
            .Produces<LiveSnapshotDto>();
    }

    /// <summary><c>Results.Ok(null)</c> sends an empty body; the contract requires a JSON <c>null</c> literal.</summary>
    private static IResult JsonOrNull<T>(T? value) where T : class =>
        value is null ? Results.Content("null", "application/json; charset=utf-8") : Results.Ok(value);
}
