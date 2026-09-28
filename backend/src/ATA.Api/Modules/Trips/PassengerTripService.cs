using System.Text.Json;
using ATA.Api.Common;
using ATA.Api.Modules.Notifications;
using ATA.Api.Modules.Passengers;
using ATA.Api.Modules.Trips.Matching;
using ATA.Domain.Common;
using ATA.Domain.Notifications;
using ATA.Domain.Passengers;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Trips;

/// <summary>Passenger side of the trip lifecycle: estimates, requests, active/detail views, cancellation and history.</summary>
public sealed class PassengerTripService(
    AtaDbContext db,
    ICurrentUser currentUser,
    IClock clock,
    IPricingService pricing,
    IMatcher matcher,
    TripReadService reads,
    TripEventRecorder events,
    TripPinService pins,
    TripNumberGenerator numbers,
    NotificationService notifications)
{
    private const int TripNumberRetries = 3;

    public async Task<EstimateResponse> EstimateAsync(EstimateRequest request, Language lang, CancellationToken ct)
    {
        var now = clock.UtcNow;
        new Validator().Route(request.Pickup, request.Dropoff, request.Stops).Booking(request.BookingType, request.ScheduledAt, now).ThrowIfInvalid();
        await LoadPassengerAsync(ct);

        var categories = await db.RideCategories.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync(ct);
        if (request.RideCategoryId is { } requestedId)
        {
            new Validator().Rule(nameof(request.RideCategoryId), categories.Any(c => c.Id == requestedId), "unknown or inactive ride category").ThrowIfInvalid();
        }

        var pickup = request.Pickup!.Point();
        var route = pricing.EstimateRoute(pickup, (request.Stops ?? []).Select(s => s.Point()).ToList(), request.Dropoff!.Point());
        var items = new List<EstimateCategoryDto>(categories.Count);
        foreach (var category in categories)
        {
            var quote = pricing.Quote(category, route.DistanceMeters, route.DurationSeconds);
            var nearest = (await matcher.FindCandidatesAsync(new MatchCriteria(pickup.Lat, pickup.Lng, category.Id, false, []), ct)).FirstOrDefault();
            int? eta = nearest is null ? null : Math.Max(1, (int)Math.Ceiling(nearest.EtaSeconds / 60d));
            items.Add(new EstimateCategoryDto(category.Id, category.Code, lang.Pick(category.NameAr, category.NameEn), eta, quote.Fare, quote.DriverNetEarnings));
        }

        return new EstimateResponse(route.DistanceMeters, route.DurationSeconds, items);
    }

    public async Task<TripDto> CreateAsync(CreateTripRequest request, Language lang, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var v = new Validator()
            .Route(request.Pickup, request.Dropoff, request.Stops)
            .Booking(request.BookingType, request.ScheduledAt, now)
            .Require(nameof(request.RideCategoryId), request.RideCategoryId)
            .Rule(nameof(request.PricingMode), request.PricingMode is null or PricingMode.Fixed or PricingMode.Offer or PricingMode.Saver, "must be fixed|saver|offer")
            .Rule(nameof(request.OfferedPrice), request.PricingMode != PricingMode.Offer || request.OfferedPrice is > 0, "required and positive for pricingMode=offer")
            .Rule(nameof(request.OfferedPrice), request.OfferedPrice is null || decimal.Round(request.OfferedPrice.Value, 2) == request.OfferedPrice.Value, "at most 2 decimal places")
            .Rule(nameof(request.RiderNote), request.RiderNote is null || request.RiderNote.Length <= 500, "max_length:500");
        v.ThrowIfInvalid();

        var category = await db.RideCategories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.RideCategoryId && c.IsActive, ct);
        v.Rule(nameof(request.RideCategoryId), category is not null, "unknown or inactive ride category");
        v.Rule(nameof(request.Stops), category is null || (request.Stops?.Count ?? 0) <= category.MaxStops, $"must be at most {category?.MaxStops} for this category");
        v.ThrowIfInvalid();

        var passenger = await LoadPassengerAsync(ct);
        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Id == passenger.UserId, ct);
        user.EnsureActive();

        var active = await db.Trips.AsNoTracking()
            .Where(t => t.PassengerId == passenger.Id && Trip.ActiveStatuses.Contains(t.Status))
            .Select(t => new { t.Id, t.TripNumber, t.Status }).FirstOrDefaultAsync(ct);
        if (active is not null)
        {
            throw new DomainException(ErrorCodes.TripActiveExists, new { activeTripId = active.Id, active.TripNumber, status = active.Status });
        }

        var pickup = request.Pickup!.Point();
        var stops = request.Stops ?? [];
        var route = pricing.EstimateRoute(pickup, stops.Select(s => s.Point()).ToList(), request.Dropoff!.Point());
        var quote = pricing.Quote(category!, route.DistanceMeters, route.DurationSeconds);
        var pricingMode = request.PricingMode ?? PricingMode.Fixed;
        var offered = pricingMode == PricingMode.Offer ? request.OfferedPrice : null;

        var trip = new Trip
        {
            TripNumber = string.Empty,
            PassengerId = passenger.Id,
            RideCategoryId = category!.Id,
            BookingType = request.BookingType ?? BookingType.Now,
            ScheduledAt = request.BookingType == BookingType.Scheduled ? request.ScheduledAt!.Value.ToUniversalTime() : null,
            PickupName = request.Pickup!.Name!.Trim(),
            PickupAddress = request.Pickup.Address!.Trim(),
            PickupLat = pickup.Lat,
            PickupLng = pickup.Lng,
            DropoffName = request.Dropoff!.Name!.Trim(),
            DropoffAddress = request.Dropoff.Address!.Trim(),
            DropoffLat = request.Dropoff.Lat!.Value,
            DropoffLng = request.Dropoff.Lng!.Value,
            PreferFemaleDriver = request.PreferFemaleDriver ?? passenger.PreferFemaleDriver,
            PaymentMethod = request.PaymentMethod ?? passenger.DefaultPaymentMethod,
            PricingMode = pricingMode,
            OfferedPrice = offered,
            EstimatedDistanceM = route.DistanceMeters,
            EstimatedDurationS = route.DurationSeconds,
            EstimatedFare = offered ?? quote.Fare,
            RiderNote = string.IsNullOrWhiteSpace(request.RiderNote) ? null : request.RiderNote.Trim(),
            RequestedAt = now,
            PinCodeHash = string.Empty,
            PinCodeProtected = string.Empty,
        };
        var pin = pins.Create(trip.Id);
        trip.PinCodeHash = pin.Hash;
        trip.PinCodeProtected = pin.Protected;
        for (var i = 0; i < stops.Count; i++)
        {
            trip.Stops.Add(new TripStop { TripId = trip.Id, Sequence = (byte)(i + 1), Name = stops[i].Name!.Trim(), Address = stops[i].Address!.Trim(), Lat = stops[i].Lat!.Value, Lng = stops[i].Lng!.Value });
        }

        db.Trips.Add(trip);
        events.Add(trip.Id, TripEventTypes.Requested, TripActor.Passenger, passenger.UserId, pickup.Lat, pickup.Lng,
            new { trip.PaymentMethod, trip.PricingMode, trip.OfferedPrice, trip.EstimatedFare, trip.PreferFemaleDriver });
        trip.StartSearching();
        events.Add(trip.Id, TripEventTypes.SearchStarted, TripActor.System);

        for (var attempt = 0; ; attempt++)
        {
            trip.TripNumber = await numbers.NextAsync(now, attempt, ct);
            try
            {
                await db.SaveChangesAsync(ct);
                break;
            }
            catch (DbUpdateException) when (attempt < TripNumberRetries)
            {
                // Another request took the same sequence number: retry with the next one.
            }
        }

        return await reads.PublishAsync(trip, TripViewer.Passenger, lang, ct);
    }

    public async Task<TripDto?> GetActiveAsync(Language lang, CancellationToken ct)
    {
        var passenger = await LoadPassengerAsync(ct);
        var trip = await db.Trips.AsNoTracking().Include(t => t.Stops)
            .Where(t => t.PassengerId == passenger.Id && Trip.ActiveStatuses.Contains(t.Status))
            .OrderByDescending(t => t.RequestedAt).FirstOrDefaultAsync(ct);
        return trip is null ? null : await reads.BuildAsync(trip, TripViewer.Passenger, lang, ct);
    }

    public async Task<TripDto> GetAsync(Guid tripId, Language lang, CancellationToken ct)
    {
        var (trip, _) = await LoadOwnAsync(tripId, ct);
        return await reads.BuildAsync(trip, TripViewer.Passenger, lang, ct);
    }

    public async Task<TripDto> CancelAsync(Guid tripId, CancelTripRequest request, Language lang, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.ReasonCode), request.ReasonCode, 60)
            .Rule(nameof(request.Note), request.Note is null || request.Note.Length <= 500, "max_length:500")
            .ThrowIfInvalid();

        var (trip, passenger) = await LoadOwnAsync(tripId, ct);
        var now = clock.UtcNow;
        var hadDriver = trip.HasDriver;
        var participants = await reads.ParticipantsAsync(trip, ct);
        trip.Cancel(CancelledBy.Passenger, request.ReasonCode!.Trim(), now);
        await reads.ReleaseDriverAsync(trip, now, ct);
        events.Add(trip.Id, TripEventTypes.Cancelled, TripActor.Passenger, passenger.UserId,
            data: new { reasonCode = trip.CancellationReason, note = request.Note?.Trim(), hadDriver });
        if (hadDriver && participants.DriverUserId is { } driverUserId)
        {
            notifications.Add(driverUserId, NotificationTypes.TripCancelled,
                ("تم إلغاء الرحلة", "Trip cancelled"),
                ($"ألغى الراكب الرحلة {trip.TripNumber}.", $"The passenger cancelled trip {trip.TripNumber}."),
                new { tripId = trip.Id, trip.TripNumber, cancelledBy = trip.CancelledBy });
        }

        await db.SaveChangesAsync(ct);
        return await reads.PublishAsync(trip, TripViewer.Passenger, lang, ct);
    }

    public async Task<PagedResult<TripSummaryDto>> ListAsync(string? status, Paging paging, Language lang, CancellationToken ct)
    {
        var statuses = TripReadService.StatusesFor(status);
        var passenger = await LoadPassengerAsync(ct);
        var query = from t in db.Trips.AsNoTracking()
                    join c in db.RideCategories.AsNoTracking() on t.RideCategoryId equals c.Id
                    where t.PassengerId == passenger.Id && statuses.Contains(t.Status)
                    select new { Trip = t, Category = c };
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.Trip.RequestedAt).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        var items = rows.Select(x => new TripSummaryDto(
            x.Trip.Id, x.Trip.DropoffName, x.Trip.PickupName, x.Trip.ScheduledAt, x.Trip.CompletedAt,
            JsonNamingPolicy.SnakeCaseLower.ConvertName(x.Trip.Status.ToString()), x.Trip.FinalFare ?? x.Trip.EstimatedFare,
            lang.Pick(x.Category.NameAr, x.Category.NameEn))).ToList();
        return paging.Result(items, total);
    }

    private async Task<PassengerProfile> LoadPassengerAsync(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        return await db.Passengers.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId, ct) ?? throw new DomainException(ErrorCodes.Forbidden);
    }

    private async Task<(Trip Trip, PassengerProfile Passenger)> LoadOwnAsync(Guid tripId, CancellationToken ct)
    {
        var passenger = await LoadPassengerAsync(ct);
        var trip = await reads.FindAsync(tripId, ct);
        if (trip is null || trip.PassengerId != passenger.Id)
        {
            throw new DomainException(ErrorCodes.NotFound);
        }

        return (trip, passenger);
    }
}
