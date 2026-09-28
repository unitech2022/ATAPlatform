using ATA.Api.Common;
using ATA.Api.Modules.Pricing;
using ATA.Api.Modules.Trips;
using ATA.Domain.Cancellation;
using ATA.Domain.Common;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Cancellation;

/// <summary>Rider/driver side of F14: reasons catalogue, cancel preview, no-show and the reliability summary.</summary>
public sealed class TripCancellationService(
    AtaDbContext db,
    ICurrentUser currentUser,
    IClock clock,
    IPricingService pricing,
    CancellationEngine engine,
    ReliabilityService reliability,
    TripReadService reads,
    TripEventRecorder events,
    IOptions<CancellationOptions> options,
    IOptions<TripOptions> tripOptions)
{
    public async Task<IReadOnlyList<CancellationReasonDto>> ReasonsAsync(CancellationActor? actor, CancellationStage? stage, Language lang, CancellationToken ct)
    {
        var query = db.CancellationReasons.AsNoTracking().Where(r => r.IsActive && r.IsSelectable && r.Actor != CancellationActor.System);
        if (actor is not null) query = query.Where(r => r.Actor == actor);
        var rows = await query.OrderBy(r => r.SortOrder).ThenBy(r => r.Code).ToListAsync(ct);
        return rows.Where(r => stage is null || CancellationEngine.AllowsStage(r, stage.Value))
            .Select(r => new CancellationReasonDto(r.Code, lang.Pick(r.NameAr, r.NameEn), r.RequiresNote, r.IsExcusable, r.IsEmergency, CancellationEngine.StagesOf(r)))
            .ToList();
    }

    public async Task<CancelPreviewDto> PreviewForPassengerAsync(Guid tripId, CancelPreviewRequest? request, Language lang, CancellationToken ct)
    {
        var trip = await LoadPassengerTripAsync(tripId, ct);
        var quote = await engine.QuoteAsync(trip, TripActor.Passenger, Clean(request?.ReasonCode), false, null, false, ct);
        var fee = quote.RequiresReview ? quote.Outcome.Fee : quote.FeeToCharge;
        string message;
        if (quote.RequiresReview)
        {
            message = lang.Pick($"سيُراجع فريقنا سبب الإلغاء؛ قد تُحتسب رسوم {Formats.MoneyAr(fee)} إن لم يُقبل العذر", $"Our team will review this reason; a {Formats.MoneyEn(fee)} fee may apply if it is not accepted");
        }
        else if (fee > 0)
        {
            var why = quote.Stage switch
            {
                CancellationStage.EnRoute => ("لأن الكابتن في الطريق إليك", "because the driver is on the way"),
                CancellationStage.Arrived or CancellationStage.Waiting => ("لأن الكابتن وصل إلى موقعك", "because the driver has arrived"),
                _ => ("لأن الكابتن قبل الرحلة", "because a driver accepted the trip"),
            };
            message = lang.Pick($"سيتم خصم {Formats.MoneyAr(fee)} رسوم إلغاء {why.Item1}", $"A {Formats.MoneyEn(fee)} cancellation fee applies {why.Item2}");
        }
        else if (quote.Outcome.FreeUntil is { } freeUntil)
        {
            message = lang.Pick($"الإلغاء مجاني حتى {Formats.LocalTime(freeUntil)}", $"Cancelling is free until {Formats.LocalTime(freeUntil)}");
        }
        else
        {
            message = lang.Pick("الإلغاء مجاني", "Cancelling is free");
        }

        return new CancelPreviewDto(quote.Stage, trip.BookingType, fee, quote.RequiresReview ? PendingPoints(quote) : quote.PointsToApply, fee == 0m, FreeUntilOf(quote), quote.RequiresReview, message);
    }

    public async Task<CancelPreviewDto> PreviewForDriverAsync(Guid tripId, CancelPreviewRequest? request, Language lang, CancellationToken ct)
    {
        var (trip, _) = await LoadDriverTripAsync(tripId, ct);
        var quote = await engine.QuoteAsync(trip, TripActor.Driver, Clean(request?.ReasonCode), false, null, false, ct);
        var points = quote.RequiresReview ? PendingPoints(quote) : quote.PointsToApply;
        var message = quote.RequiresReview
            ? lang.Pick("سيُراجع فريقنا سبب الإلغاء قبل احتسابه على موثوقيتك", "Our team will review this reason before it affects your reliability")
            : points > 0
                ? lang.Pick($"سيؤثر هذا الإلغاء على نسبة موثوقيتك (+{points} نقاط)", $"This cancellation will affect your reliability (+{points} points)")
                : lang.Pick("سيُحتسب هذا الإلغاء في نسبة إلغائك دون نقاط", "This cancellation counts toward your cancellation rate without points");
        return new CancelPreviewDto(quote.Stage, trip.BookingType, quote.FeeToCharge, points, quote.FeeToCharge == 0m && points == 0, FreeUntilOf(quote), quote.RequiresReview, message);
    }

    /// <summary><c>POST /driver/trips/{id}/no-show</c>: waiting at least <c>Cancellation:NoShowWaitMinutes</c> (and the free waiting time) at the pickup.</summary>
    public async Task<TripDto> NoShowAsync(Guid tripId, NoShowRequest? request, Language lang, CancellationToken ct)
    {
        new Validator()
            .Rule("lat", request?.Lat is null or (>= -90 and <= 90), "out of range")
            .Rule("lng", request?.Lng is null or (>= -180 and <= 180), "out of range")
            .ThrowIfInvalid();
        var (trip, driverUserId) = await LoadDriverTripAsync(tripId, ct);
        trip.EnsureStatus(TripStatus.Waiting);
        var now = clock.UtcNow;
        var category = await db.RideCategories.AsNoTracking().FirstAsync(c => c.Id == trip.RideCategoryId, ct);
        var freeWaitingMinutes = await pricing.FreeWaitingMinutesAsync(category, new GeoPoint(trip.PickupLat, trip.PickupLng), trip.RequestedAt, ct);
        var requiredSeconds = Math.Max(options.Value.NoShowWaitMinutes, freeWaitingMinutes) * 60;
        var waited = trip.ArrivedAt is { } arrived ? (int)(now - arrived).TotalSeconds : 0;
        if (waited < requiredSeconds)
        {
            throw new DomainException(ErrorCodes.NoShowTooEarly, new { secondsRemaining = requiredSeconds - waited });
        }

        var (lat, lng) = request?.Lat is { } la && request.Lng is { } ln
            ? (la, ln)
            : await db.DriverLocations.AsNoTracking().Where(l => l.DriverId == trip.DriverId).Select(l => new ValueTuple<decimal, decimal>(l.Lat, l.Lng)).FirstOrDefaultAsync(ct);
        if (lat != 0m || lng != 0m)
        {
            var distance = (int)Math.Round(Geo.HaversineMeters(lat, lng, trip.PickupLat, trip.PickupLng));
            if (distance > tripOptions.Value.ArrivalRadiusMeters)
            {
                events.Add(trip.Id, TripEventTypes.ArrivalDistanceWarning, TripActor.System, null, lat, lng,
                    new { distanceMeters = distance, allowedMeters = tripOptions.Value.ArrivalRadiusMeters, context = "no_show" });
            }
        }

        await engine.CancelAsync(trip, new CancelCommand(TripActor.Driver, driverUserId, "passenger_no_show", null, NoShow: true), ct);
        return await reads.PublishAsync(trip, TripViewer.Driver, lang, ct);
    }

    public async Task<ReliabilitySummaryDto> SummaryAsync(Role role, Language lang, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var profile = await reliability.RefreshAsync(userId, role, ct) ?? throw new DomainException(ErrorCodes.Forbidden);
        var thresholds = await reliability.ThresholdsAsync(role, ct);
        var snapshot = reliability.Snapshot(profile, thresholds, clock.UtcNow);
        var recent = await RecentEventsAsync(userId, role, clock.UtcNow.AddDays(-profile.WindowDays), 20, lang, ct);
        return new ReliabilitySummaryDto(role, profile.RestrictionLevel, snapshot.RestrictedUntil, profile.WindowDays, profile.TripsAccepted, profile.TripsCompleted,
            profile.CancellationsAtFault, profile.CancellationRate, profile.ReliabilityRate, profile.NoShowCount, profile.PenaltyPoints, reliability.NextLevel(profile, thresholds), recent,
            role == Role.Driver ? profile.OffersReceived : null,
            role == Role.Driver ? profile.OffersAccepted : null,
            role == Role.Driver ? profile.AcceptanceRate : null,
            role == Role.Driver ? new ReliabilityEffectsDto(reliability.MatchingFactor(snapshot), reliability.IncentiveMultiplier(snapshot)) : null);
    }

    /// <summary>At-fault cancellation events of the user in the role (passenger: trips they requested; driver: trips assigned to them).</summary>
    public async Task<IReadOnlyList<ReliabilityEventDto>> RecentEventsAsync(Guid userId, Role role, DateTime since, int take, Language lang, CancellationToken ct)
    {
        var fault = role == Role.Driver ? AtFault.Driver : AtFault.Passenger;
        IQueryable<Guid> tripIds = role == Role.Driver
            ? from t in db.Trips join d in db.Drivers on t.DriverId equals d.Id where d.UserId == userId select t.Id
            : from t in db.Trips join p in db.Passengers on t.PassengerId equals p.Id where p.UserId == userId select t.Id;
        var rows = await (from e in db.CancellationEvents.AsNoTracking()
                          join t in db.Trips.AsNoTracking() on e.TripId equals t.Id
                          join r in db.CancellationReasons.AsNoTracking() on e.ReasonId equals r.Id into reasons
                          from r in reasons.DefaultIfEmpty()
                          where tripIds.Contains(e.TripId) && e.CreatedAt >= since && (e.AtFault == fault || (e.ExcuseStatus == ExcuseStatus.Approved && e.UserId == userId))
                          orderby e.CreatedAt descending
                          select new { e, t.TripNumber, NameAr = r == null ? null : r.NameAr, NameEn = r == null ? null : r.NameEn }).Take(take).ToListAsync(ct);
        return rows.Select(x => new ReliabilityEventDto(x.e.Id, x.e.TripId, x.TripNumber, x.e.Stage, x.e.ReasonCode, lang.PickOptional(x.NameAr, x.NameEn), x.e.AtFault, x.e.FeeCharged,
            x.e.PenaltyPoints, x.e.ExcuseStatus, x.e.CountsTowardRate, x.e.CreatedAt)).ToList();
    }

    private static int PendingPoints(CancellationQuote quote) => quote.AtFault == AtFault.None ? 0 : quote.Outcome.PenaltyPoints;

    private static DateTime? FreeUntilOf(CancellationQuote quote) => quote.Outcome.FreeUntil;

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<Trip> LoadPassengerTripAsync(Guid tripId, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var trip = await reads.FindAsync(tripId, ct);
        var owner = trip is null ? null : await db.Passengers.AsNoTracking().Where(p => p.Id == trip.PassengerId).Select(p => (Guid?)p.UserId).FirstOrDefaultAsync(ct);
        return trip is not null && owner == userId ? trip : throw new DomainException(ErrorCodes.NotFound);
    }

    private async Task<(Trip Trip, Guid DriverUserId)> LoadDriverTripAsync(Guid tripId, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var driverId = await db.Drivers.AsNoTracking().Where(d => d.UserId == userId).Select(d => (Guid?)d.Id).FirstOrDefaultAsync(ct) ?? throw new DomainException(ErrorCodes.Forbidden);
        var trip = await reads.FindAsync(tripId, ct);
        return trip is not null && trip.DriverId == driverId ? (trip, userId) : throw new DomainException(ErrorCodes.NotFound);
    }
}
