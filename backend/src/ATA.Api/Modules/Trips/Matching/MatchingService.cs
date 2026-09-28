using ATA.Api.Modules.Notifications;
using ATA.Api.Modules.Trips.Realtime;
using ATA.Domain.Common;
using ATA.Domain.Notifications;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Trips.Matching;

/// <summary>
/// One matching pass over every <c>searching</c> trip: expires timed-out offers, sends the next offer to the nearest eligible
/// driver, and marks trips <c>no_drivers</c> once <c>Matching:SearchTimeoutSeconds</c> elapsed.
/// </summary>
public sealed class MatchingService(
    AtaDbContext db,
    IMatcher matcher,
    IPricingService pricing,
    TripReadService reads,
    TripEventRecorder events,
    ITripNotifier notifier,
    NotificationService notifications,
    IOptions<MatchingOptions> matchingOptions,
    IOptions<TripOptions> tripOptions,
    IClock clock,
    ILogger<MatchingService> logger)
{
    private readonly MatchingOptions _matching = matchingOptions.Value;
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
        var open = await db.TripOffers.FirstOrDefaultAsync(o => o.TripId == trip.Id && o.Status == OfferStatus.Sent, ct);
        if (open is not null)
        {
            if (open.ExpiresAt > now)
            {
                return;
            }

            open.Expire(now);
            events.Add(trip.Id, TripEventTypes.OfferExpired, TripActor.System, data: new { offerId = open.Id, driverId = open.DriverId });
            await db.SaveChangesAsync(ct);
            var expiredDriverUserId = await db.Drivers.AsNoTracking().Where(d => d.Id == open.DriverId).Select(d => d.UserId).FirstAsync(ct);
            await notifier.OfferExpiredAsync(expiredDriverUserId, open.Id, ct);
        }

        var searchStartedAt = trip.ScheduledAt is { } scheduledAt
            ? Max(trip.RequestedAt, scheduledAt.AddMinutes(-_trips.ScheduledLeadMinutes))
            : trip.RequestedAt;
        if ((now - searchStartedAt).TotalSeconds >= _matching.SearchTimeoutSeconds)
        {
            await MarkNoDriversAsync(trip, now, ct);
            return;
        }

        var offered = await db.TripOffers.AsNoTracking().Where(o => o.TripId == trip.Id).Select(o => o.DriverId).ToListAsync(ct);
        var candidates = await matcher.FindCandidatesAsync(new MatchCriteria(trip.PickupLat, trip.PickupLng, trip.RideCategoryId, trip.PreferFemaleDriver, offered), ct);
        var best = candidates.FirstOrDefault();
        if (best is null)
        {
            return;
        }

        var category = await db.RideCategories.AsNoTracking().FirstAsync(c => c.Id == trip.RideCategoryId, ct);
        var quote = pricing.Quote(category, trip.EstimatedDistanceM, trip.EstimatedDurationS);
        var driverNet = trip.PricingMode == PricingMode.Offer && trip.OfferedPrice is { } offered_
            ? decimal.Round(offered_ * category.DriverSharePercent / 100m, 2, MidpointRounding.AwayFromZero)
            : quote.DriverNetEarnings;
        var offer = new TripOffer
        {
            TripId = trip.Id,
            DriverId = best.DriverId,
            DriverNetEarnings = driverNet,
            DistanceToPickupM = best.DistanceMeters,
            EtaSeconds = best.EtaSeconds,
            SentAt = now,
            ExpiresAt = now.AddSeconds(_matching.OfferTimeoutSeconds),
        };
        db.TripOffers.Add(offer);
        events.Add(trip.Id, TripEventTypes.OfferSent, TripActor.System,
            data: new { offerId = offer.Id, driverId = best.DriverId, distanceMeters = best.DistanceMeters, etaSeconds = best.EtaSeconds, expiresAt = offer.ExpiresAt });
        await db.SaveChangesAsync(ct);
        await notifier.OfferReceivedAsync(best.UserId, await reads.BuildOfferAsync(offer, trip, ct), ct);
    }

    private async Task MarkNoDriversAsync(Trip trip, DateTime now, CancellationToken ct)
    {
        trip.MarkNoDrivers(now);
        events.Add(trip.Id, TripEventTypes.NoDrivers, TripActor.System);
        var participants = await reads.ParticipantsAsync(trip, ct);
        notifications.Add(participants.PassengerUserId, NotificationTypes.TripNoDrivers,
            ("لم نجد سائقاً", "No drivers available"),
            ($"عذراً، لم نجد سائقاً متاحاً للرحلة {trip.TripNumber}. حاول مرة أخرى.", $"Sorry, no driver was available for trip {trip.TripNumber}. Please try again."),
            new { tripId = trip.Id, trip.TripNumber, status = trip.Status });
        await db.SaveChangesAsync(ct);
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
