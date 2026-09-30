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
    Cancellation.ReliabilityService reliability,
    Promotions.PromotionService promotions,
    Favorites.FavoriteService favorites,
    Favorites.FavoriteMatchingService favoriteMatching,
    Scheduling.ScheduleRuleProvider scheduleRules,
    Scheduling.ScheduledRideService scheduledRides,
    Scheduling.ScheduledRideEngine scheduledEngine,
    Airports.AirportTripService airportTrips,
    Corporate.CorporateRiderService corporateRiders,
    Corporate.CorporateTripPolicyService corporatePolicies,
    Safety.TripShareService shares,
    IOptions<Corporate.CorporateOptions> corporateOptions)
{
    private const int TripNumberRetries = 3;

    /// <summary><c>POST /pricing/quote</c> (and its alias <c>/passenger/trips/estimate</c>): prices every category and stores the quotes.</summary>
    public async Task<QuoteResponse> QuoteAsync(EstimateRequest request, Language lang, CancellationToken ct)
    {
        var passenger = await LoadPassengerAsync(ct);
        if (request.PaymentMethod != PaymentMethodKind.Corporate)
        {
            return await quotes.QuoteAsync(request, passenger.Id, lang, ct);
        }

        // F19: `paymentMethod: "corporate"` adds `corporate { allowed, violations, remainingBudget }` (no promo or favourite discount is priced in).
        new Validator().Rule(nameof(request.TripPurpose), request.TripPurpose is null || request.TripPurpose.Length <= 200, "max_length:200").ThrowIfInvalid();
        var booking = await corporateRiders.ResolveForAppAsync(passenger, ct);
        var response = await quotes.QuoteAsync(request, passenger.Id, lang, ct, corporate: true);
        return response with { Corporate = await corporateRiders.QuoteBlockAsync(booking, request, request.TripPurpose, request.CostCenterId, response, clock.UtcNow, ct) };
    }

    public Task<TripDto> CreateAsync(CreateTripRequest request, Language lang, CancellationToken ct) => CreateCoreAsync(request, lang, null, ct);

    /// <summary>F19: a company admin books in the portal; <paramref name="booking"/> says who pays, who rides (employee, or a guest recorded on the admin's profile) and who booked.</summary>
    public Task<TripDto> CreateForCorporateAsync(CreateTripRequest request, Corporate.CorporateBooking booking, Language lang, CancellationToken ct) => CreateCoreAsync(request, lang, booking, ct);

    private async Task<TripDto> CreateCoreAsync(CreateTripRequest request, Language lang, Corporate.CorporateBooking? portalBooking, CancellationToken ct)
    {
        var now = clock.UtcNow;
        var corporateTrip = portalBooking is not null || request.PaymentMethod == PaymentMethodKind.Corporate;
        var v = new Validator()
            .Route(request.Pickup, request.Dropoff, request.Stops)
            .Booking(request.BookingType, request.ScheduledAt)
            .Require(nameof(request.RideCategoryId), request.RideCategoryId)
            .Rule(nameof(request.PricingMode), request.PricingMode is null or PricingMode.Fixed or PricingMode.Offer or PricingMode.Saver, "must be fixed|saver|offer")
            .Rule(nameof(request.OfferedPrice), request.PricingMode != PricingMode.Offer || request.OfferedPrice is > 0, "required and positive for pricingMode=offer")
            .Rule(nameof(request.OfferedPrice), request.OfferedPrice is null || decimal.Round(request.OfferedPrice.Value, 2) == request.OfferedPrice.Value, "at most 2 decimal places")
            .Rule(nameof(request.RiderNote), request.RiderNote is null || request.RiderNote.Length <= 500, "max_length:500")
            .Rule(nameof(request.PaymentMethodId), request.PaymentMethodId is null || (request.PaymentMethod ?? PaymentMethodKind.Card) == PaymentMethodKind.Card, "only for paymentMethod=card")
            .Rule(nameof(request.PromoCode), request.PromoCode is null || request.PromoCode.Length <= 40, "max_length:40")
            .Rule(nameof(request.TripPurpose), request.TripPurpose is null || request.TripPurpose.Length <= 200, "max_length:200");
        v.ThrowIfInvalid();
        if (corporateTrip && !string.IsNullOrWhiteSpace(request.PromoCode))
        {
            // doc 10 §F15.1: no discounts with corporate payment.
            throw new DomainException(ErrorCodes.PromoNotEligible, new Promotions.PromoReason(Promotions.PromotionService.Reasons.PaymentMethod));
        }

        var category = await db.RideCategories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.RideCategoryId && c.IsActive, ct);
        v.Rule(nameof(request.RideCategoryId), category is not null, "unknown or inactive ride category");
        v.Rule(nameof(request.Stops), category is null || (request.Stops?.Count ?? 0) <= category.MaxStops, $"must be at most {category?.MaxStops} for this category");
        v.ThrowIfInvalid();

        var passenger = portalBooking?.Passenger ?? await LoadPassengerAsync(ct);
        var booking = portalBooking ?? (corporateTrip ? await corporateRiders.ResolveForAppAsync(passenger, ct) : null);
        if (booking is not null)
        {
            await corporateRiders.EnsureCostCenterAsync(booking.Account.Id, request.CostCenterId, ct);
        }

        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Id == passenger.UserId, ct);
        user.EnsureActive();
        // F14: a temporarily restricted or suspended passenger cannot request trips (403 account_restricted); a guest is not the admin's own ride.
        if (booking is not { IsGuest: true })
        {
            await reliability.EnsureNotRestrictedAsync(passenger.UserId, Role.Passenger, ct);
        }

        // F17: airport detection and validation (pickup zone, flight number, terminal); an airport pickup zone replaces the pickup point.
        var airport = await airportTrips.PrepareAsync(request.Pickup!.Lat!.Value, request.Pickup.Lng!.Value, request.Dropoff!.Lat!.Value, request.Dropoff.Lng!.Value,
            new Airports.AirportRequestInput(request.AirportPickupZoneId, request.AirportTerminalCode, request.FlightNumber), requirePickupZone: true, ct);
        if (airport is null && category!.Code == Airports.AirportTripService.AirportCategoryCode)
        {
            throw new DomainException(ErrorCodes.AirportCategoryNotApplicable);
        }

        var bookingType = request.BookingType ?? BookingType.Now;
        var scheduled = bookingType == BookingType.Scheduled;
        var pickup = airport is null ? request.Pickup.Point() : new GeoPoint(airport.PickupLat, airport.PickupLng);
        Domain.Scheduling.ScheduledRideRule? scheduleRule = null;
        if (scheduled)
        {
            // F17: the booking window is measured from the booking time; a scheduled trip is not an active trip, so it neither needs nor blocks one — up to
            // `max_open_per_passenger` scheduled bookings may be open.
            scheduleRule = await scheduleRules.ResolveForPickupAsync(pickup.Lat, pickup.Lng, category!.Id, now, ct);
            Scheduling.ScheduleRuleProvider.EnsureWindow(scheduleRule, request.ScheduledAt!.Value, now);
            if (booking is null)
            {
                await scheduledRides.EnsureCanBookAsync(passenger.Id, scheduleRule, ct);
            }
        }
        else if (booking is { IsGuest: true })
        {
            // F19: the trip_active_exists rule does not apply to guest bookings; a company admin may run up to Corporate:MaxActiveGuestTripsPerAdmin guest trips at once.
            var limit = corporateOptions.Value.MaxActiveGuestTripsPerAdmin;
            var open = await db.Trips.AsNoTracking().CountAsync(t => t.BookedByUserId == booking.BookedByUserId && t.IsGuest && Trip.ActiveStatuses.Contains(t.Status), ct);
            if (open >= limit)
            {
                throw new DomainException(ErrorCodes.TripActiveExists, new { reason = "guest_trips_limit", limit, open });
            }
        }
        else
        {
            var active = await db.Trips.AsNoTracking()
                .Where(t => t.PassengerId == passenger.Id && !t.IsGuest && Trip.ActiveStatuses.Contains(t.Status))
                .Select(t => new { t.Id, t.TripNumber, t.Status }).FirstOrDefaultAsync(ct);
            if (active is not null)
            {
                throw new DomainException(ErrorCodes.TripActiveExists, new { activeTripId = active.Id, active.TripNumber, status = active.Status });
            }
        }

        // A negative passenger balance is a debt (failed card collection, cancellation fees): top up before requesting again (the company pays corporate trips).
        if (booking is null && paymentOptions.Value.BlockOnOutstandingBalance)
        {
            var balance = await db.Wallets.AsNoTracking().Where(w => w.UserId == passenger.UserId && w.Kind == WalletKind.Passenger).Select(w => (decimal?)w.Balance).FirstOrDefaultAsync(ct) ?? 0m;
            if (balance < 0)
            {
                throw new DomainException(ErrorCodes.OutstandingBalance, new { amount = -balance, balance });
            }
        }

        // F16: the requested driver must be one of the passenger's favourites.
        if (request.FavoriteDriverId is { } requestedFavorite && !await favorites.IsFavoriteAsync(passenger.Id, requestedFavorite, ct))
        {
            new Validator().Fail(nameof(request.FavoriteDriverId), "not_favorite").ThrowIfInvalid();
        }

        var stops = request.Stops ?? [];
        var route = pricing.EstimateRoute(pickup, stops.Select(s => s.Point()).ToList(), request.Dropoff!.Point());
        var pricingMode = request.PricingMode ?? PricingMode.Fixed;
        var offered = pricingMode == PricingMode.Offer ? request.OfferedPrice : null;
        var pickupAt = request.BookingType == BookingType.Scheduled ? request.ScheduledAt!.Value.ToUniversalTime() : now;
        var quote = await ResolveQuoteAsync(request.QuoteId, passenger.Id, category!, pickup, request.Dropoff!.Point(), route, pickupAt, now, scheduleRule is { LockDemandNormal: true }, ct);
        if (offered is { } offeredPrice && (offeredPrice < quote.OfferMin || offeredPrice > quote.OfferMax))
        {
            throw new DomainException(ErrorCodes.OfferOutOfRange, new { quote.OfferMin, quote.OfferMax, offeredPrice });
        }

        // F19: policy, monthly budget and credit limit of the company (the estimated fare is the quoted total, or the rider's offered price).
        if (booking is not null)
        {
            var check = await corporatePolicies.CheckAsync(booking.Account, booking.Member,
                new Corporate.CorporateCheckInput(category!.Id, pickup, request.Dropoff!.Point(), pickupAt, offered ?? quote.Total, bookingType, request.TripPurpose, request.CostCenterId, booking.IsGuest), ct);
            check.EnforceOrThrow();
        }

        // F15: a promo code is fully validated before anything is created (no discounts with "offer your price").
        Promotions.PromotionReservationPlan? promo = null;
        if (!string.IsNullOrWhiteSpace(request.PromoCode))
        {
            var promoContext = await promotions.ContextOfQuoteAsync(quote, request.PaymentMethod ?? passenger.DefaultPaymentMethod, request.BookingType ?? BookingType.Now, pricingMode, ct);
            promo = await promotions.PrepareReservationAsync(request.PromoCode, passenger.Id, promoContext, ct);
        }

        var preferFemale = request.PreferFemaleDriver ?? passenger.PreferFemaleDriver;
        (FavoriteStatus Status, string? Reason)? favorite = request.FavoriteDriverId is { } favoriteId
            ? await favoriteMatching.InitialStatusAsync(favoriteId, pickup.Lat, pickup.Lng, category!.Id, preferFemale, bookingType, ct)
            : null;
        var trip = new Trip
        {
            TripNumber = string.Empty,
            PassengerId = passenger.Id,
            RideCategoryId = category!.Id,
            BookingType = bookingType,
            Status = scheduled ? TripStatus.Scheduled : TripStatus.Requested,
            ScheduledAt = scheduled ? request.ScheduledAt!.Value.ToUniversalTime() : null,
            PickupName = airport?.PickupZone is { } pickupZone ? lang.Pick(pickupZone.NameAr, pickupZone.NameEn) : request.Pickup!.Name!.Trim(),
            PickupAddress = request.Pickup.Address!.Trim(),
            PickupLat = pickup.Lat,
            PickupLng = pickup.Lng,
            DropoffName = request.Dropoff!.Name!.Trim(),
            DropoffAddress = request.Dropoff.Address!.Trim(),
            DropoffLat = request.Dropoff.Lat!.Value,
            DropoffLng = request.Dropoff.Lng!.Value,
            PreferFemaleDriver = preferFemale,
            FavoriteDriverId = request.FavoriteDriverId,
            FavoriteStatus = favorite?.Status,
            PaymentMethod = booking is null ? request.PaymentMethod ?? passenger.DefaultPaymentMethod : PaymentMethodKind.Corporate,
            CorporateAccountId = booking?.Account.Id,
            CorporateUserId = booking?.Member?.Id,
            BookedByUserId = booking?.BookedByUserId,
            IsGuest = booking?.IsGuest ?? false,
            GuestName = booking?.GuestName,
            GuestPhone = booking?.GuestPhone,
            TripPurpose = booking is null || string.IsNullOrWhiteSpace(request.TripPurpose) ? null : request.TripPurpose.Trim(),
            CostCenterId = booking is null ? null : request.CostCenterId,
            PricingMode = pricingMode,
            OfferedPrice = offered,
            EstimatedDistanceM = quote.DistanceM,
            EstimatedDurationS = quote.DurationS,
            EstimatedFare = offered ?? promo?.TotalAfter ?? quote.Total,
            RiderNote = string.IsNullOrWhiteSpace(request.RiderNote) ? null : request.RiderNote.Trim(),
            RequestedAt = now,
            PinCodeHash = string.Empty,
            PinCodeProtected = string.Empty,
            PlannedRoute = Safety.PlannedRoutes.Serialize(Safety.PlannedRoutes.Straight(pickup.Lat, pickup.Lng, stops.Select(st => (st.Lat!.Value, st.Lng!.Value)), request.Dropoff!.Lat!.Value, request.Dropoff.Lng!.Value)),
            PlannedRouteSource = Domain.Safety.PlannedRouteSource.Straight,
            AirportId = airport?.Airport.Id,
            AirportDirection = airport?.Direction,
            AirportZoneId = airport?.PickupZone?.Id,
            TerminalCode = airport?.TerminalCode,
            FlightNumber = airport?.FlightNumber,
            WaitingPolicy = airport is null ? null : (await airportTrips.WaitingPolicyAsync(airport, category!, pickupAt, ct))?.ToJson(),
        };
        var pin = pins.Create(trip.Id);
        trip.PinCodeHash = pin.Hash;
        trip.PinCodeProtected = pin.Protected;
        for (var i = 0; i < stops.Count; i++)
        {
            trip.Stops.Add(new TripStop { TripId = trip.Id, Sequence = (byte)(i + 1), Name = stops[i].Name!.Trim(), Address = stops[i].Address!.Trim(), Lat = stops[i].Lat!.Value, Lng = stops[i].Lng!.Value });
        }

        // Card trips are authorized before the trip row exists; an immediate decline rejects the request (422 payment_failed). Scheduled card trips only check the card
        // now: the authorization is made when the search starts or the final confirmation assigns the driver.
        Payment? payment = null;
        if (trip.PaymentMethod == PaymentMethodKind.Card)
        {
            if (scheduled)
            {
                await cardPayments.ResolveCardAsync(trip, passenger.UserId, request.PaymentMethodId, passenger.DefaultPaymentMethodId, ct);
            }
            else
            {
                payment = await cardPayments.AuthorizeForTripAsync(trip, passenger.UserId, request.PaymentMethodId, passenger.DefaultPaymentMethodId, ct);
            }
        }

        db.Trips.Add(trip);
        quote.UsedTripId = trip.Id;
        events.Add(trip.Id, TripEventTypes.Requested, TripActor.Passenger, booking?.BookedByUserId ?? passenger.UserId, pickup.Lat, pickup.Lng,
            new { corporateAccountId = booking?.Account.Id, corporateUserId = booking?.Member?.Id, isGuest = booking?.IsGuest, trip.PaymentMethod, trip.PricingMode, trip.OfferedPrice, trip.EstimatedFare, trip.PreferFemaleDriver, quoteId = quote.Id, quote.DemandLevelCode, quote.PricingRuleId, paymentId = payment?.Id,
                promoCode = promo?.Promotion.Code, promoReservedAmount = promo?.Amount, favoriteDriverId = request.FavoriteDriverId, favoriteStatus = favorite?.Status,
                bookingType, scheduledAt = trip.ScheduledAt, airportId = trip.AirportId, airportDirection = trip.AirportDirection, airportZoneId = trip.AirportZoneId });
        if (favorite is { Status: FavoriteStatus.Unavailable })
        {
            events.Add(trip.Id, TripEventTypes.FavoriteUnavailable, TripActor.System, data: new { driverId = request.FavoriteDriverId, reason = favorite.Value.Reason });
        }

        if (scheduled)
        {
            // F17: the trip stays `scheduled` (not searching) until T − search_start_minutes_before; rider reminders and the booking / favourite-request notifications.
            await scheduledEngine.OnBookedAsync(trip, scheduleRule!, passenger.UserId, user.FullName, ct);
        }
        else if (payment is { Status: PaymentStatus.Initiated })
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

        if (booking is { IsGuest: true })
        {
            await SendGuestTripSmsAsync(trip, booking, lang, ct);
        }

        if (promo is null)
        {
            await SaveNewTripAsync(trip, now, ct);
        }
        else
        {
            try
            {
                // The reservation (atomic usage_count increment + redemption row) commits with the trip or not at all.
                await db.InTransactionAsync(async () =>
                {
                    await promotions.ReserveAsync(promo, trip.Id, passenger.Id, ct);
                    await SaveNewTripAsync(trip, now, ct);
                }, ct);
            }
            catch (DomainException) when (payment is not null)
            {
                // The code was used up concurrently: drop the pending trip and release the card authorization.
                foreach (var entry in db.ChangeTracker.Entries().Where(e => e.State == EntityState.Added && e.Entity is not Payment).ToList())
                {
                    entry.State = EntityState.Detached;
                }

                quote.UsedTripId = null;
                await cardPayments.ReleaseAsync(trip.Id, ct);
                throw;
            }
        }

        await paymentService.PublishPendingAsync(ct);
        return await reads.PublishAsync(trip, portalBooking is null ? TripViewer.Passenger : TripViewer.Corporate, lang, ct);
    }

    /// <summary>
    /// F19 guest booking: a tracking link (F12, kept alive until the trip ends) and the trip PIN go to the guest by SMS (<c>corporate.guest_trip</c>); the guest has no account, the
    /// booking admin's rider profile owns the trip.
    /// </summary>
    private async Task SendGuestTripSmsAsync(Trip trip, Corporate.CorporateBooking booking, Language lang, CancellationToken ct)
    {
        var share = shares.Add(trip.Id, booking.BookedByUserId, null, Domain.Safety.TripShareChannel.Sms);
        var pickupAt = trip.ScheduledAt ?? trip.RequestedAt;
        await notifications.DispatchAsync(new NotificationRequest("corporate.guest_trip", Guid.Empty,
            NotificationPlaceholders.Of(("companyName", booking.Account.DisplayName), ("pickupName", trip.PickupName), ("pickupTime", pickupAt), ("shareUrl", shares.UrlOf(share.Token)), ("pin", pins.Reveal(trip) ?? string.Empty)),
            "trip", trip.Id, new Dictionary<string, object?> { ["tripId"] = trip.Id, ["shareId"] = share.Id }, RecipientPhoneOverride: booking.GuestPhone), ct);
    }

    private async Task SaveNewTripAsync(Trip trip, DateTime now, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            trip.TripNumber = await numbers.NextAsync(now, attempt, ct);
            try
            {
                await db.SaveChangesAsync(ct);
                return;
            }
            catch (DbUpdateException) when (attempt < TripNumberRetries)
            {
                // Another request took the same sequence number: retry with the next one.
            }
        }
    }

    public async Task<TripDto?> GetActiveAsync(Language lang, CancellationToken ct)
    {
        var passenger = await LoadPassengerAsync(ct);
        var trip = await db.Trips.AsNoTracking().Include(t => t.Stops)
            .Where(t => t.PassengerId == passenger.Id && !t.IsGuest && Trip.ActiveStatuses.Contains(t.Status))
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
        var ratings = await reads.RatingStatesAsync(rows.Select(x => x.Trip).ToList(), TripViewer.Passenger, ct);
        var driverIds = rows.Where(x => x.Trip.DriverId != null).Select(x => x.Trip.DriverId!.Value).Distinct().ToList();
        var driverNames = driverIds.Count == 0 ? [] : await (from d in db.Drivers.AsNoTracking() join u in db.Users.AsNoTracking() on d.UserId equals u.Id
                                                             where driverIds.Contains(d.Id) select new { d.Id, u.FullName }).ToDictionaryAsync(x => x.Id, x => x.FullName, ct);
        var items = rows.Select(x =>
        {
            var rating = ratings[x.Trip.Id];
            return new TripSummaryDto(
                x.Trip.Id, x.Trip.DropoffName, x.Trip.PickupName, x.Trip.ScheduledAt, x.Trip.CompletedAt,
                JsonNamingPolicy.SnakeCaseLower.ConvertName(x.Trip.Status.ToString()), x.Trip.FinalFare ?? x.Trip.EstimatedFare,
                lang.Pick(x.Category.NameAr, x.Category.NameEn),
                x.Trip.DriverId is { } driverId && x.Trip.HasDriver ? Ratings.RatingService.FirstName(driverNames.GetValueOrDefault(driverId)) : null,
                rating.MyRating, rating.CanRate, rating.RateUntil);
        }).ToList();
        return paging.Result(items, total);
    }

    /// <summary>
    /// The quote that fixes the trip's price: the referenced <c>fare_quotes</c> row (or its sibling for the chosen category) when it is
    /// still valid, otherwise a fresh calculation stored as an already-used quote so the driver share and demand level stay with the trip.
    /// </summary>
    private async Task<FareQuote> ResolveQuoteAsync(
        Guid? quoteId, Guid passengerId, RideCategory category, GeoPoint pickup, GeoPoint dropoff, RouteEstimate route, DateTime pickupAt, DateTime now, bool lockDemandNormal, CancellationToken ct)
    {
        if (quoteId is null)
        {
            var fresh = await quotes.QuoteForTripAsync(category, pickup, dropoff, route, pickupAt, passengerId, ct, lockDemandNormal ? DemandReading.Neutral() : null);
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
