using ATA.Api.Common;
using ATA.Domain.Common;
using ATA.Domain.Passengers;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Passengers;

public sealed record TripSummaryDto(Guid Id, string DestinationName, string PickupName, DateTime? ScheduledAt, DateTime? CompletedAt, string Status, decimal Fare, string CategoryName);

public sealed record SavedPlaceDto(Guid Id, SavedPlaceLabel Label, string Name, string Address, decimal Latitude, decimal Longitude);

public sealed record SavePlaceRequest(string? Name, string? Address, decimal? Latitude, decimal? Longitude);

public sealed record PassengerPreferencesRequest(bool? PreferFemaleDriver, PaymentMethodKind? DefaultPaymentMethod);

public sealed record PassengerPreferencesDto(bool PreferFemaleDriver, PaymentMethodKind DefaultPaymentMethod);

public static class PassengerEndpoints
{
    private static readonly string[] TripStatuses = ["all", "active", "completed", "cancelled"];

    public static void Map(IEndpointRouteBuilder api)
    {
        var group = api.MapGroup("/passenger").WithTags("Passenger").RequireAuthorization(Policies.Passenger);

        group.MapGet("/trips", (string? status, int? page, int? pageSize) =>
            {
                new Validator().Rule("status", status is null || TripStatuses.Contains(status), "must be all|active|completed|cancelled").ThrowIfInvalid();
                return Results.Ok(Paging.From(page, pageSize).Result<TripSummaryDto>([], 0));
            })
            .Produces<PagedResult<TripSummaryDto>>();

        group.MapGet("/saved-places", async (PassengerService service, CancellationToken ct) =>
                Results.Ok(await service.GetSavedPlacesAsync(ct)))
            .Produces<List<SavedPlaceDto>>();

        group.MapPut("/saved-places/{label}", async (string label, SavePlaceRequest request, PassengerService service, CancellationToken ct) =>
                Results.Ok(await service.SavePlaceAsync(ParseLabel(label), request, ct)))
            .Produces<SavedPlaceDto>();

        group.MapDelete("/saved-places/{label}", async (string label, PassengerService service, CancellationToken ct) =>
            {
                await service.DeletePlaceAsync(ParseLabel(label), ct);
                return Results.NoContent();
            })
            .Produces(StatusCodes.Status204NoContent);

        group.MapPatch("/preferences", async (PassengerPreferencesRequest request, PassengerService service, CancellationToken ct) =>
                Results.Ok(await service.UpdatePreferencesAsync(request, ct)))
            .Produces<PassengerPreferencesDto>();
    }

    private static SavedPlaceLabel ParseLabel(string label) =>
        Enum.TryParse<SavedPlaceLabel>(label, ignoreCase: true, out var parsed)
            ? parsed
            : throw new DomainException(ErrorCodes.ValidationFailed, new { label = "must be home|work|other" });
}

public sealed class PassengerService(AtaDbContext db, ICurrentUser currentUser)
{
    public async Task<List<SavedPlaceDto>> GetSavedPlacesAsync(CancellationToken ct)
    {
        var passenger = await LoadAsync(ct);
        return passenger.SavedPlaces.OrderBy(p => p.Label).Select(ToDto).ToList();
    }

    public async Task<SavedPlaceDto> SavePlaceAsync(SavedPlaceLabel label, SavePlaceRequest request, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.Name), request.Name, 120)
            .Require(nameof(request.Address), request.Address, 500)
            .Require(nameof(request.Latitude), request.Latitude)
            .Require(nameof(request.Longitude), request.Longitude)
            .Rule(nameof(request.Latitude), request.Latitude is null or (>= -90 and <= 90), "out of range")
            .Rule(nameof(request.Longitude), request.Longitude is null or (>= -180 and <= 180), "out of range")
            .ThrowIfInvalid();

        var passenger = await LoadAsync(ct);
        var place = passenger.SavedPlaces.FirstOrDefault(p => p.Label == label);
        if (place is null)
        {
            place = new SavedPlace { PassengerId = passenger.Id, Label = label, Name = request.Name!.Trim(), Address = request.Address!.Trim() };
            passenger.SavedPlaces.Add(place);
        }

        place.Name = request.Name!.Trim();
        place.Address = request.Address!.Trim();
        place.Latitude = request.Latitude!.Value;
        place.Longitude = request.Longitude!.Value;
        await db.SaveChangesAsync(ct);
        return ToDto(place);
    }

    public async Task DeletePlaceAsync(SavedPlaceLabel label, CancellationToken ct)
    {
        var passenger = await LoadAsync(ct);
        var place = Guard.NotFound(passenger.SavedPlaces.FirstOrDefault(p => p.Label == label));
        db.SavedPlaces.Remove(place);
        await db.SaveChangesAsync(ct);
    }

    public async Task<PassengerPreferencesDto> UpdatePreferencesAsync(PassengerPreferencesRequest request, CancellationToken ct)
    {
        var passenger = await LoadAsync(ct);
        if (request.PreferFemaleDriver is not null) passenger.PreferFemaleDriver = request.PreferFemaleDriver.Value;
        if (request.DefaultPaymentMethod is not null) passenger.DefaultPaymentMethod = request.DefaultPaymentMethod.Value;
        await db.SaveChangesAsync(ct);
        return new PassengerPreferencesDto(passenger.PreferFemaleDriver, passenger.DefaultPaymentMethod);
    }

    private async Task<PassengerProfile> LoadAsync(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        return await db.Passengers.Include(p => p.SavedPlaces).FirstOrDefaultAsync(p => p.UserId == userId, ct)
            ?? throw new DomainException(ErrorCodes.Forbidden);
    }

    private static SavedPlaceDto ToDto(SavedPlace p) => new(p.Id, p.Label, p.Name, p.Address, p.Latitude, p.Longitude);
}
