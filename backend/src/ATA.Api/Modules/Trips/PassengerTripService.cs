using System.Text.Json;
using ATA.Api.Common;
using ATA.Api.Modules.Notifications;
using ATA.Api.Modules.Passengers;
using ATA.Api.Modules.Payments;
using ATA.Api.Modules.Pricing;
using ATA.Domain.Catalog;
using ATA.Domain.Common;
using ATA.Domain.Payments;
using ATA.Domain.Passengers;
using ATA.Domain.Pricing;
using ATA.Domain.Trips;
using ATA.Domain.Wallet;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Trips;

/// <summary>Passenger side of the trip lifecycle: estimates, requests, active/detail views, cancellation and history.</summary>
public sealed class PassengerTripService(
    AtaDbContext db,
    ICurrentUser currentUser,
    IClock clock,
    IPricingService pricing,
    QuoteService quotes,
    TripReadService reads,
    TripEventRecorder events,
    TripPinService pins,
    TripNumberGenerator numbers,
    INotificationDispatcher notifications,
    CardTripPaymentService cardPayments,
    PaymentService paymentService,
    IOptions<PaymentsOptions> paymentOptions,
    Cancellation.CancellationEngine cancellations,
    Cancellation.ReliabilityService reliability)
{
    private const int TripNumberRetries = 3;

    /// <summary><c>POST /pricing/quote</c> (and its alias <c>/passenger/trips/estimate</c>): prices every category and stores the quotes.</summary>
    public async Task<QuoteResponse> QuoteAsync(EstimateRequest request, Language lang, CancellationToken ct)
    {
        var passenger = await LoadPassengerAsync(ct);
        return await quotes.QuoteAsync(request, passenger.Id, lang, ct);
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
            .Rule(nameof(request.RiderNote), request.RiderNote is null || request.RiderNote.Length <= 500, "max_length:500")
            .Rule(nameof(request.PaymentMethodId), request.PaymentMethodId is null || (request.PaymentMethod ?? PaymentMethodKind.Card) == PaymentMethodKind.Card, "only for paymentMethod=card");
        v.ThrowIfInvalid();

        var category = await db.RideCategories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.RideCategoryId && c.IsActive, ct);
        v.Rule(nameof(request.RideCategoryId), category is not null, "unknown or inactive ride category");
        v.Rule(nameof(request.Stops), category is null || (request.Stops?.Count ?? 0) <= category.MaxStops, $"must be at most {category?.MaxStops} for this category");
        v.ThrowIfInvalid();

        var passenger = await LoadPassengerAsync(ct);
        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Id == passenger.UserId, ct);
        user.EnsureActive();
        // F14: a temporarily restricted or suspended passenger cannot request trips (403 account_restricted).
        await reliability.EnsureNotRestrictedAsync(passenger.UserId, Role.Passenger, ct);

        var active = await db.Trips.AsNoTracking()
            .Where(t => t.PassengerId == passenger.Id && Trip.ActiveStatuses.Contains(t.Status))
            .Select(t => new { t.Id, t.TripNumber, t.Status }).FirstOrDefaultAsync(ct);
        if (active is not null)
        {
            throw new DomainException(ErrorCodes.TripActiveExists, new { activeTripId = active.Id, active.TripNumber, status = active.Status });
        }

        // A negative passenger balance is a debt (failed card collection, cancellation fees): top up before requesting again.
        if (paymentOptions.Value.BlockOnOutstandingBalance)
        {
            var balance = await db.Wallets.AsNoTracking().Where(w => w.UserId == passenger.UserId && w.Kind == WalletKind.Passenger).Select(w => (decimal?)w.Balance).FirstOrDefaultAsync(ct) ?? 0m;
            if (balance < 0)
            {
                throw new DomainException(ErrorCodes.OutstandingBalance, new { amount = -balance, balance });
            }
        }

        var pickup = request.Pickup!.Point();
        var stops = request.Stops ?? [];
        var route = pricing.EstimateRoute(pickup, stops.Select(s => s.Point()).ToList(), request.Dropoff!.Point());
        var pricingMode = request.PricingMode ?? PricingMode.Fixed;
        var offered = pricingMode == PricingMode.Offer ? request.OfferedPrice : null;
        var pickupAt = request.BookingType == BookingType.Scheduled ? request.ScheduledAt!.Value.ToUniversalTime() : now;
        var quote = await ResolveQuoteAsync(request.QuoteId, passenger.Id, category!, pickup, request.Dropoff!.Point(), route, pickupAt, now, ct);
        if (offered is { } offeredPrice && (offeredPrice < quote.OfferMin || offeredPrice > quote.OfferMax))
        {
            throw new DomainException(ErrorCodes.OfferOutOfRange, new { quote.OfferMin, quote.OfferMax, offeredPrice });
        }

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
            EstimatedDistanceM = quote.DistanceM,
            EstimatedDurationS = quote.DurationS,
            EstimatedFare = offered ?? quote.Total,
            RiderNote = string.IsNullOrWhiteSpace(request.RiderNote) ? null : request.RiderNote.Trim(),
            RequestedAt = now,
            PinCodeHash = string.Empty,
            PinCodeProtected = string.Empty,
            PlannedRoute = Safety.PlannedRoutes.Serialize(Safety.PlannedRoutes.Straight(pickup.Lat, pickup.Lng, stops.Select(st => (st.Lat!.Value, st.Lng!.Value)), request.Dropoff!.Lat!.Value, request.Dropoff.Lng!.Value)),
            PlannedRouteSource = Domain.Safety.PlannedRouteSource.Straight,
        };
        var pin = pins.Create(trip.Id);
        trip.PinCodeHash = pin.Hash;
        trip.PinCodeProtected = pin.Protected;
        for (var i = 0; i < stops.Count; i++)
        {
            trip.Stops.Add(new TripStop { TripId = trip.Id, Sequence = (byte)(i + 1), Name = stops[i].Name!.Trim(), Address = stops[i].Address!.Trim(), Lat = stops[i].Lat!.Value, Lng = stops[i].Lng!.Value });
        }

        // Card trips are authorized before the trip row exists; an immediate decline rejects the request (422 payment_failed).
        Payment? payment = null;
        if (trip.PaymentMethod == PaymentMethodKind.Card)
        {
            payment = await cardPayments.AuthorizeForTripAsync(trip, passenger.UserId, request.PaymentMethodId, passenger.DefaultPaymentMethodId, ct);
        }

        db.Trips.Add(trip);
        quote.UsedTripId = trip.Id;
        events.Add(trip.Id, TripEventTypes.Requested, TripActor.Passenger, passenger.UserId, pickup.Lat, pickup.Lng,
            new { trip.PaymentMethod, trip.PricingMode, trip.OfferedPrice, trip.EstimatedFare, trip.PreferFemaleDriver, quoteId = quote.Id, quote.DemandLevelCode, quote.PricingRuleId, paymentId = payment?.Id });
        if (payment is { Status: PaymentStatus.Initiated })
        {
            // 3-D Secure: the trip stays `requested` (out of matching) until the payment is authorized or the action expires.
            events.Add(trip.Id, TripEventTypes.PaymentActionRequired, TripActor.System, data: new { paymentId = payment.Id, payment.Amount, payment.ActionExpiresAt });
            await notifications.DispatchAsync(TripNotifications.PaymentActionRequired(trip, passenger.UserId, payment.Amount, payment.Id), ct);
        }
        else
        {
            if (payment is { Status: PaymentStatus.Authorized })
            {
                events.Add(trip.Id, TripEventTypes.PaymentAuthorized, TripActor.System, data: new { paymentId = payment.Id, amount = payment.AuthorizedAmount });
            }

            trip.StartSearching();
            events.Add(trip.Id, TripEventTypes.SearchStarted, TripActor.System);
        }

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

        await paymentService.PublishPendingAsync(ct);
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

    /// <summary>F14: goes through the cancellation engine (reason catalogue, stage fee / free window, <c>expectedFee</c> guard).</summary>
    public async Task<TripDto> CancelAsync(Guid tripId, CancelTripRequest request, Language lang, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.ReasonCode), request.ReasonCode, 60)
            .Rule(nameof(request.Note), request.Note is null || request.Note.Length <= 500, "max_length:500")
            .Rule(nameof(request.ExpectedFee), request.ExpectedFee is null or >= 0, "must be positive")
            .ThrowIfInvalid();

        var (trip, passenger) = await LoadOwnAsync(tripId, ct);
        await cancellations.CancelAsync(trip, new Cancellation.CancelCommand(TripActor.Passenger, passenger.UserId, request.ReasonCode!.Trim(), request.Note, request.ExpectedFee), ct);
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

    /// <summary>
    /// The quote that fixes the trip's price: the referenced <c>fare_quotes</c> row (or its sibling for the chosen category) when it is
    /// still valid, otherwise a fresh calculation stored as an already-used quote so the driver share and demand level stay with the trip.
    /// </summary>
    private async Task<FareQuote> ResolveQuoteAsync(Guid? quoteId, Guid passengerId, RideCategory category, GeoPoint pickup, GeoPoint dropoff, RouteEstimate route, DateTime pickupAt, DateTime now, CancellationToken ct)
    {
        if (quoteId is null)
        {
            var fresh = await quotes.QuoteForTripAsync(category, pickup, dropoff, route, pickupAt, passengerId, ct);
            db.FareQuotes.Add(fresh);
            return fresh;
        }

        var referenced = await db.FareQuotes.FirstOrDefaultAsync(q => q.Id == quoteId && q.PassengerId == passengerId, ct);
        new Validator().Rule("quoteId", referenced is not null, "unknown quote").ThrowIfInvalid();
        var quote = referenced!.RideCategoryId == category.Id
            ? referenced
            : await db.FareQuotes.FirstOrDefaultAsync(q => q.GroupId == referenced.GroupId && q.RideCategoryId == category.Id, ct);
        new Validator().Rule("quoteId", quote is not null, "the quote has no price for this ride category").ThrowIfInvalid();
        if (!quote!.IsUsableAt(now))
        {
            throw new DomainException(ErrorCodes.QuoteExpired, new { quoteId = quote.Id, quote.ExpiresAt, used = quote.UsedTripId is not null });
        }

        return quote;
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
