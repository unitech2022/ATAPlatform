using ATA.Api.Modules.Notifications;
using ATA.Api.Modules.Pricing;
using ATA.Api.Modules.Trips.Realtime;
using ATA.Domain.Common;
using ATA.Domain.Matching;
using ATA.Domain.Notifications;
using ATA.Api.Modules.Payments;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Trips.Matching;

/// <summary>Bookkeeping of <c>matching_attempts</c> / <c>matching_candidates</c> shared by the matcher loop and the offer endpoints.</summary>
public sealed class MatchingRecorder(AtaDbContext db)
{
    public Task<MatchingAttempt?> OpenAttemptAsync(Guid tripId, CancellationToken ct) =>
        db.MatchingAttempts.Include(a => a.Candidates).Where(a => a.TripId == tripId && a.FinishedAt == null).OrderByDescending(a => a.Round).FirstOrDefaultAsync(ct);

    public Task<MatchingAttempt?> LatestAttemptAsync(Guid tripId, CancellationToken ct) =>
        db.MatchingAttempts.Include(a => a.Candidates).Where(a => a.TripId == tripId).OrderByDescending(a => a.Round).FirstOrDefaultAsync(ct);

    /// <summary>Records the driver's answer on the candidate row of the open round (no-op when the offer pre-dates the F9 tables).</summary>
    public async Task RecordResponseAsync(Guid tripId, Guid driverId, CandidateResponse response, CancellationToken ct)
    {
        var attempt = await OpenAttemptAsync(tripId, ct);
        var candidate = attempt?.Candidates.FirstOrDefault(c => c.DriverId == driverId && c.Offered && c.Response == null);
        if (candidate is not null)
        {
            candidate.Response = response;
        }
    }

    /// <summary>Closes the open round (if any) with the given outcome; changes are saved by the caller.</summary>
    public async Task CloseOpenAttemptAsync(Guid tripId, MatchingOutcome outcome, DateTime now, CancellationToken ct)
    {
        var attempt = await OpenAttemptAsync(tripId, ct);
        attempt?.Finish(outcome, now);
    }
}

/// <summary>
/// One matching pass over every <c>searching</c> trip: expires timed-out offers, offers the trip to the next ranked candidate of the
/// open round, opens wider rounds (<c>radius_step</c> up to <c>max_radius</c>) when a round is exhausted, and marks trips
/// <c>no_drivers</c> once <c>search_timeout_seconds</c> elapsed. Every round and candidate is recorded for the admin "matching" view.
/// F16: a trip requested with a favourite driver first gets round 0 (<c>mode = favorite</c>) with an exclusive offer to that driver
/// (<c>Favorites:ExclusiveOfferTimeoutSeconds</c>); a rejection or expiry falls back to normal round 1 without re-offering the driver, and the total search timeout only
/// starts when the exclusive round ended.
/// </summary>
public sealed class MatchingService(
    AtaDbContext db,
    IMatcher matcher,
    IPricingService pricing,
    ZoneResolver zones,
    MatchingSettingsProvider settingsProvider,
    MatchingRecorder recorder,
    TripReadService reads,
    TripEventRecorder events,
    ITripNotifier notifier,
    INotificationDispatcher notifications,
    CardTripPaymentService cardPayments,
    IOptions<TripOptions> tripOptions,
    IClock clock,
    ILogger<MatchingService> logger,
    Incentives.TierRuleProvider tierRules,
    Favorites.FavoriteMatchingService favoriteMatching)
{
    private readonly TripOptions _trips = tripOptions.Value;

    public async Task<int> RunOnceAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;
        var searchStartsBefore = now.AddMinutes(_trips.ScheduledLeadMinutes);
        var trips = await db.Trips.Include(t => t.Stops)
            .Where(t => t.Status == TripStatus.Searching && (t.ScheduledAt == null || t.ScheduledAt <= searchStartsBefore))
            .OrderBy(t => t.RequestedAt)
            .ToListAsync(ct);

        var processed = 0;
        foreach (var trip in trips)
        {
            try
            {
                await ProcessAsync(trip, now, ct);
                processed++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Matching failed for trip {TripNumber}", trip.TripNumber);
            }
        }

        return processed;
    }

    private async Task ProcessAsync(Trip trip, DateTime now, CancellationToken ct)
    {
        var zone = await zones.ResolveAsync(trip.PickupLat, trip.PickupLng, now, ct);
        var settings = await settingsProvider.ResolveAsync(zone?.Id, trip.RideCategoryId, ct);

        var open = await db.TripOffers.FirstOrDefaultAsync(o => o.TripId == trip.Id && o.Status == OfferStatus.Sent, ct);
        if (open is not null)
        {
            if (open.ExpiresAt > now)
            {
                return;
            }

            open.Expire(now);
            await recorder.RecordResponseAsync(trip.Id, open.DriverId, CandidateResponse.Expired, ct);
            events.Add(trip.Id, TripEventTypes.OfferExpired, TripActor.System, data: new { offerId = open.Id, driverId = open.DriverId });
            await db.SaveChangesAsync(ct);
            var expiredDriverUserId = await db.Drivers.AsNoTracking().Where(d => d.Id == open.DriverId).Select(d => d.UserId).FirstAsync(ct);
            await notifier.OfferExpiredAsync(expiredDriverUserId, open.Id, ct);
        }

        // F16: the exclusive favourite round is over (the driver rejected, or the offer expired above) → normal matching.
        var attempt = await recorder.LatestAttemptAsync(trip.Id, ct);
        if (attempt is { Mode: MatchingMode.Favorite, IsOpen: true })
        {
            await EndFavoriteRoundAsync(trip, attempt, now, ct);
        }

        var searchStartedAt = trip.ScheduledAt is { } scheduledAt
            ? Max(trip.RequestedAt, scheduledAt.AddMinutes(-_trips.ScheduledLeadMinutes))
            : trip.RequestedAt;
        if (trip.FavoriteDriverId is not null
            && await db.MatchingAttempts.AsNoTracking().Where(a => a.TripId == trip.Id && a.Mode == MatchingMode.Favorite).Select(a => a.FinishedAt).FirstOrDefaultAsync(ct) is { } favoriteEndedAt)
        {
            searchStartedAt = Max(searchStartedAt, favoriteEndedAt);
        }

        if ((now - searchStartedAt).TotalSeconds >= settings.SearchTimeoutSeconds)
        {
            await MarkNoDriversAsync(trip, now, ct);
            return;
        }

        // F16 hook for scheduled trips (F17): the exclusive round runs when the search starts (T − ScheduledLeadMinutes); F17's `favorite_exclusive_minutes` window
        // (reservation before the marketplace opens) plugs in here without changing the round.
        if (attempt is null && trip.FavoriteDriverId is { } favoriteId && trip.FavoriteStatus == FavoriteStatus.Requested
            && await TryStartFavoriteRoundAsync(trip, favoriteId, settings, now, ct))
        {
            return;
        }

        if (attempt is { IsOpen: true })
        {
            while (attempt.Candidates.Where(c => !c.Offered).OrderBy(c => c.Rank).FirstOrDefault() is { } next)
            {
                var driver = await db.Drivers.AsNoTracking().Where(d => d.Id == next.DriverId)
                    .Select(d => new { d.UserId, d.IsOnline, d.CurrentTripId }).FirstOrDefaultAsync(ct);
                next.Offered = true;
                if (driver is null || !driver.IsOnline || driver.CurrentTripId is not null)
                {
                    // The candidate became unavailable since the round was scored: its turn lapses like an unanswered offer.
                    next.Response = CandidateResponse.Expired;
                    continue;
                }

                await SendOfferAsync(trip, attempt, next.DriverId, driver.UserId, next.DistanceM, next.EtaS, settings.OfferTimeoutSeconds, now, ct);
                return;
            }

            attempt.Finish(MatchingOutcome.Exhausted, now);
        }

        var round = (attempt?.Round ?? 0) + 1;
        var radius = attempt is null or { Mode: MatchingMode.Favorite } ? settings.RadiusMeters : Math.Min(attempt.RadiusMeters + settings.RadiusStepMeters, settings.MaxRadiusMeters);
        if (attempt is { IsOpen: false, CandidatesCount: 0 } && attempt.RadiusMeters >= settings.MaxRadiusMeters
            && attempt.FinishedAt is { } finishedAt && finishedAt.AddSeconds(settings.OfferTimeoutSeconds) > now)
        {
            // Already at the widest radius and nobody was found: re-scan at most once per offer timeout until the search times out.
            await db.SaveChangesAsync(ct);
            return;
        }

        var offered = await db.TripOffers.AsNoTracking().Where(o => o.TripId == trip.Id).Select(o => o.DriverId).ToListAsync(ct);
        while (true)
        {
            var criteria = new MatchCriteria(trip.PickupLat, trip.PickupLng, trip.RideCategoryId, trip.PreferFemaleDriver, offered, radius, trip.PassengerId, trip.Id);
            var ranked = (await matcher.FindCandidatesAsync(criteria, ct)).Take(settings.MaxCandidates).ToList();
            var next = new MatchingAttempt { TripId = trip.Id, Round = round, RadiusMeters = radius, CandidatesCount = ranked.Count, StartedAt = now };
            for (var i = 0; i < ranked.Count; i++)
            {
                next.Candidates.Add(new MatchingCandidate
                {
                    AttemptId = next.Id, DriverId = ranked[i].DriverId, DistanceM = ranked[i].DistanceMeters, EtaS = ranked[i].EtaSeconds, Score = ranked[i].Score, Rank = i + 1,
                });
            }

            db.MatchingAttempts.Add(next);
            if (ranked.Count > 0)
            {
                var best = ranked[0];
                next.Candidates.First().Offered = true;
                await SendOfferAsync(trip, next, best.DriverId, best.UserId, best.DistanceMeters, best.EtaSeconds, settings.OfferTimeoutSeconds, now, ct);
                return;
            }

            next.Finish(MatchingOutcome.Exhausted, now);
            if (radius >= settings.MaxRadiusMeters)
            {
                await db.SaveChangesAsync(ct);
                return;
            }

            round++;
            radius = Math.Min(radius + settings.RadiusStepMeters, settings.MaxRadiusMeters);
        }
    }

    /// <summary>
    /// Round 0: an exclusive offer to the favourite driver when the zone/category prefers favourites and the driver is eligible now. Otherwise the request becomes
    /// <c>unavailable</c> and the caller goes on with normal round 1 in the same pass.
    /// </summary>
    private async Task<bool> TryStartFavoriteRoundAsync(Trip trip, Guid favoriteDriverId, ResolvedMatchingSettings settings, DateTime now, CancellationToken ct)
    {
        var radius = favoriteMatching.RadiusFor(settings);
        var candidate = settings.PreferFavoriteDriver ? await favoriteMatching.FindExclusiveAsync(trip, favoriteDriverId, radius, ct) : null;
        if (candidate is null)
        {
            trip.FavoriteStatus = FavoriteStatus.Unavailable;
            events.Add(trip.Id, TripEventTypes.FavoriteUnavailable, TripActor.System,
                data: new { driverId = favoriteDriverId, reason = settings.PreferFavoriteDriver ? "not_eligible" : "prefer_favorite_driver_disabled" });
            await db.SaveChangesAsync(ct);
            await reads.PublishAsync(trip, TripViewer.Admin, Language.Ar, ct);
            return false;
        }

        var round = new MatchingAttempt { TripId = trip.Id, Round = 0, Mode = MatchingMode.Favorite, RadiusMeters = radius, CandidatesCount = 1, StartedAt = now };
        round.Candidates.Add(new MatchingCandidate
        {
            AttemptId = round.Id, DriverId = candidate.DriverId, DistanceM = candidate.DistanceMeters, EtaS = candidate.EtaSeconds, Score = candidate.Score, Rank = 1, Offered = true,
        });
        db.MatchingAttempts.Add(round);
        await SendOfferAsync(trip, round, candidate.DriverId, candidate.UserId, candidate.DistanceMeters, candidate.EtaSeconds, favoriteMatching.ExclusiveTimeoutSeconds, now, ct);
        return true;
    }

    /// <summary>The favourite did not take the exclusive offer: <c>rejected</c> / <c>expired</c>, the round is closed and <c>TripUpdated</c> tells the passenger we search on.</summary>
    private async Task EndFavoriteRoundAsync(Trip trip, MatchingAttempt round, DateTime now, CancellationToken ct)
    {
        var rejected = round.Candidates.Any(c => c.Response == CandidateResponse.Rejected);
        trip.FavoriteStatus = rejected ? FavoriteStatus.Rejected : FavoriteStatus.Expired;
        round.Finish(MatchingOutcome.Exhausted, now);
        events.Add(trip.Id, TripEventTypes.FavoriteFallback, TripActor.System, data: new { driverId = trip.FavoriteDriverId, status = trip.FavoriteStatus });
        await db.SaveChangesAsync(ct);
        await reads.PublishAsync(trip, TripViewer.Admin, Language.Ar, ct);
    }

    private async Task SendOfferAsync(Trip trip, MatchingAttempt attempt, Guid driverId, Guid driverUserId, int distanceMeters, int etaSeconds, int timeoutSeconds, DateTime now, CancellationToken ct)
    {
        var isFavorite = trip.FavoriteDriverId == driverId;
        var driverNet = await DriverNetAsync(trip, driverId, ct);
        var offer = new TripOffer
        {
            TripId = trip.Id,
            DriverId = driverId,
            DriverNetEarnings = driverNet,
            DistanceToPickupM = distanceMeters,
            EtaSeconds = etaSeconds,
            SentAt = now,
            ExpiresAt = now.AddSeconds(timeoutSeconds),
        };
        db.TripOffers.Add(offer);
        events.Add(trip.Id, TripEventTypes.OfferSent, TripActor.System,
            data: new { offerId = offer.Id, driverId, round = attempt.Round, distanceMeters, etaSeconds, expiresAt = offer.ExpiresAt, favorite = isFavorite, exclusive = attempt.Mode == MatchingMode.Favorite });
        // offer.received: push only (no inbox row), TTL = offer timeout, collapsed per offer.
        await notifications.DispatchAsync(new NotificationRequest(NotificationTypes.OfferReceived, driverUserId,
            NotificationPlaceholders.Of(("pickupName", trip.PickupName), ("etaMinutes", Math.Max(1, (int)Math.Ceiling(etaSeconds / 60d)))).Money("driverNet", driverNet),
            "trip", trip.Id, new Dictionary<string, object?>
            {
                ["offerId"] = offer.Id, ["tripId"] = trip.Id, ["ttlSeconds"] = timeoutSeconds, ["collapseId"] = offer.Id.ToString(), ["isFavoriteRequest"] = isFavorite,
            }), ct);
        await db.SaveChangesAsync(ct);
        await notifier.OfferReceivedAsync(driverUserId, await reads.BuildOfferAsync(offer, trip, ct), ct);
    }

    /// <summary>
    /// The driver's net from the quote the trip was created with (offer mode: offered price × driver share), else a fresh calculation; F15 raises the share
    /// by the driver's tier commission discount (<c>share + (100 − share) × discount / 100</c>).
    /// </summary>
    private async Task<decimal> DriverNetAsync(Trip trip, Guid driverId, CancellationToken ct)
    {
        var tier = await db.Drivers.AsNoTracking().Where(d => d.Id == driverId).Select(d => d.Tier).FirstOrDefaultAsync(ct);
        var tierDiscount = await tierRules.CommissionDiscountPercentAsync(tier, ct);
        var quote = await db.FareQuotes.AsNoTracking().Where(q => q.UsedTripId == trip.Id).Select(q => new { q.DriverNetEarnings, q.DriverSharePercent }).FirstOrDefaultAsync(ct);
        if (trip.PricingMode == PricingMode.Offer && trip.OfferedPrice is { } offered)
        {
            var share = quote?.DriverSharePercent ?? await db.RideCategories.AsNoTracking().Where(c => c.Id == trip.RideCategoryId).Select(c => c.DriverSharePercent).FirstAsync(ct);
            return PricingMath.Round2(offered * Domain.Incentives.TierMath.EffectiveSharePercent(share, tierDiscount) / 100m);
        }

        if (quote is not null)
        {
            return tierDiscount <= 0 || quote.DriverSharePercent <= 0
                ? quote.DriverNetEarnings
                : PricingMath.Round2(quote.DriverNetEarnings * Domain.Incentives.TierMath.EffectiveSharePercent(quote.DriverSharePercent, tierDiscount) / quote.DriverSharePercent);
        }

        var category = await db.RideCategories.AsNoTracking().FirstAsync(c => c.Id == trip.RideCategoryId, ct);
        var calculation = await pricing.CalculateAsync(new FareRequest(category, new GeoPoint(trip.PickupLat, trip.PickupLng), new GeoPoint(trip.DropoffLat, trip.DropoffLng),
            trip.EstimatedDistanceM, trip.EstimatedDurationS, trip.RequestedAt), ct);
        return tierDiscount <= 0
            ? calculation.DriverNetEarnings
            : PricingMath.Round2(calculation.ShareBase * Domain.Incentives.TierMath.EffectiveSharePercent(calculation.DriverSharePercent, tierDiscount) / 100m);
    }

    private async Task MarkNoDriversAsync(Trip trip, DateTime now, CancellationToken ct)
    {
        trip.MarkNoDrivers(now);
        await recorder.CloseOpenAttemptAsync(trip.Id, MatchingOutcome.Timeout, now, ct);
        events.Add(trip.Id, TripEventTypes.NoDrivers, TripActor.System);
        await Cancellation.SystemCancellation.RecordAsync(db, trip, "no_drivers", ct);
        var participants = await reads.ParticipantsAsync(trip, ct);
        var category = await db.RideCategories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == trip.RideCategoryId, ct);
        await notifications.DispatchAsync(TripNotifications.NoDrivers(trip, participants.PassengerUserId, category), ct);
        await db.SaveChangesAsync(ct);
        await cardPayments.ReleaseAsync(trip.Id, ct);
        await reads.PublishAsync(trip, TripViewer.Admin, Language.Ar, ct);
    }

    private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;
}

/// <summary>Polls <c>searching</c> trips every <c>Matching:PollIntervalSeconds</c>; disabled with <c>Matching:Enabled=false</c> (tests call <see cref="RunOnceAsync"/>).</summary>
public sealed class MatchingBackgroundService(IServiceScopeFactory scopes, IOptions<MatchingOptions> options, ILogger<MatchingBackgroundService> logger) : BackgroundService
{
    private readonly MatchingOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1, _options.PollIntervalSeconds)));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Matching pass failed");
            }
        }
    }

    internal async Task<int> RunOnceAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<MatchingService>().RunOnceAsync(ct);
    }
}
