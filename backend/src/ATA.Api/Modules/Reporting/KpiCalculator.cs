using ATA.Api.Common;
using ATA.Api.Modules.Pricing;
using ATA.Domain.Cancellation;
using ATA.Domain.Common;
using ATA.Domain.Ratings;
using ATA.Domain.Payments;
using ATA.Domain.Scheduling;
using ATA.Domain.Support;
using ATA.Domain.Trips;
using ATA.Domain.Wallet;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Reporting;

/// <summary>A report scope (doc 12 §F20.1 <c>scope_key</c>): <c>all</c>, <c>city:{id}</c>, <c>zone:{id}</c>, <c>cat:{id}</c>, <c>city:{id}|cat:{id}</c>, <c>zone:{id}|cat:{id}</c>.</summary>
public sealed record KpiScope(string Key, Guid? CityId, Guid? ZoneId, Guid? RideCategoryId)
{
    public static readonly KpiScope All = new(Domain.Reporting.ReportSnapshot.AllScope, null, null, null);

    /// <summary>The scope matching a filter: the zone wins over the city (a zone belongs to one city).</summary>
    public static KpiScope For(Guid? cityId, Guid? zoneId, Guid? rideCategoryId)
    {
        var place = zoneId is { } z ? $"zone:{z}" : cityId is { } c ? $"city:{c}" : null;
        var cat = rideCategoryId is { } r ? $"cat:{r}" : null;
        var key = place is null ? cat ?? Domain.Reporting.ReportSnapshot.AllScope : cat is null ? place : $"{place}|{cat}";
        return new KpiScope(key, zoneId is null ? cityId : null, zoneId, rideCategoryId);
    }
}

/// <summary>Accumulated parts of one metric in one bucket and scope.</summary>
public sealed class KpiAccumulator
{
    public decimal Value { get; set; }
    public decimal Numerator { get; set; }
    public decimal Denominator { get; set; }
    /// <summary><c>sum</c> metrics that also count occurrences (the favourite-driver discount keeps the number of trips in <c>numerator</c>).</summary>
    public bool HasNumerator { get; set; }
}

/// <summary>The value of one metric (display-ready parts for the API, raw for <c>report_snapshots</c>).</summary>
public sealed record KpiCell(decimal? Value, decimal? Numerator, decimal? Denominator)
{
    public static KpiCell From(KpiDefinition definition, KpiAccumulator? acc)
    {
        acc ??= new KpiAccumulator();
        return definition.Aggregation switch
        {
            KpiAggregation.Sum => new KpiCell(KpiMath.Round(acc.Value), acc.HasNumerator ? acc.Numerator : null, null),
            KpiAggregation.Distinct when definition.Unit == KpiUnit.Count => new KpiCell(acc.Value, null, null),
            _ => new KpiCell(KpiMath.Ratio(definition, acc.Numerator, acc.Denominator), KpiMath.Round(acc.Numerator), KpiMath.Round(acc.Denominator)),
        };
    }
}

/// <summary>Result of a computation: accumulators per (bucket start, scope key, metric) plus the scope descriptors.</summary>
public sealed class KpiComputation
{
    public Dictionary<(DateOnly Bucket, string Scope), Dictionary<string, KpiAccumulator>> Cells { get; } = [];
    public Dictionary<string, KpiScope> Scopes { get; } = new(StringComparer.Ordinal) { [KpiScope.All.Key] = KpiScope.All };

    public KpiAccumulator? Get(DateOnly bucket, string scope, string metric) =>
        Cells.TryGetValue((bucket, scope), out var metrics) && metrics.TryGetValue(metric, out var acc) ? acc : null;

    public KpiAccumulator Acc(DateOnly bucket, KpiScope scope, string metric)
    {
        Scopes.TryAdd(scope.Key, scope);
        if (!Cells.TryGetValue((bucket, scope.Key), out var metrics))
        {
            metrics = new Dictionary<string, KpiAccumulator>(StringComparer.Ordinal);
            Cells[(bucket, scope.Key)] = metrics;
        }

        if (!metrics.TryGetValue(metric, out var acc))
        {
            acc = new KpiAccumulator();
            metrics[metric] = acc;
        }

        return acc;
    }
}

/// <summary>
/// Computes the KPIs of doc 12 §F20.6 from the operational tables (F8–F19) for a range of Riyadh days, grouped into buckets (a day for the snapshots, a
/// week / month for series, the whole range for the <c>distinct</c> metrics) and scopes (pickup city / zone — the <c>fare_quotes.pickup_zone_id</c> of the
/// trip, else <see cref="ZoneResolver"/> on the pickup point — and ride category). Rows are projected and aggregated in memory (as the other stats of the
/// platform do) so the same code runs on MySQL and SQLite.
/// </summary>
public sealed class KpiCalculator(AtaDbContext db, ZoneResolver zones, IClock clock)
{
    private const int Chunk = 500;
    private static readonly TimeSpan OnlineLookback = TimeSpan.FromDays(7);

    private sealed record TripDim(Guid? CityId, Guid? ZoneId, Guid CategoryId);

    private sealed record TripRow(
        Guid Id, Guid PassengerId, Guid? DriverId, Guid RideCategoryId, TripStatus Status, BookingType BookingType, DateTime? ScheduledAt, DateTime RequestedAt,
        DateTime? AssignedAt, DateTime? ArrivedAt, DateTime? CompletedAt, DateTime? CancelledAt, CancelledBy? CancelledBy, decimal? FinalFare, decimal DiscountTotal,
        decimal? DriverEarnings, FavoriteStatus? FavoriteStatus, decimal PickupLat, decimal PickupLng);

    public async Task<KpiComputation> ComputeAsync(DateOnly from, DateOnly to, Func<DateOnly, DateOnly> bucketOf, IReadOnlySet<string> metrics, CancellationToken ct)
    {
        var result = new KpiComputation();
        var start = Formats.RiyadhMidnightUtc(from);
        var end = Formats.RiyadhMidnightUtc(to.AddDays(1));
        var now = clock.UtcNow;
        bool Need(params string[] codes) => codes.Any(metrics.Contains);
        bool InRange(DateTime? at) => at is { } t && t >= start && t < end;
        var dims = new Dictionary<Guid, TripDim>();
        var zoneCities = await db.Zones.AsNoTracking().Select(z => new { z.Id, z.CityId }).ToDictionaryAsync(z => z.Id, z => z.CityId, ct);
        var activeZones = await zones.AllAsync(ct);

        void Add(DateTime? at, TripDim? dim, string metric, decimal value = 0, decimal numerator = 0, decimal denominator = 0, bool countNumerator = false)
        {
            if (!InRange(at) || !metrics.Contains(metric))
            {
                return;
            }

            var bucket = bucketOf(Formats.RiyadhDate(at!.Value));
            var definition = KpiCodes.ByCode[metric];
            foreach (var scope in ScopesOf(definition.Dimensional ? dim : null))
            {
                var acc = result.Acc(bucket, scope, metric);
                acc.Value += value;
                acc.Numerator += numerator;
                acc.Denominator += denominator;
                acc.HasNumerator |= countNumerator;
            }
        }

        async Task LoadDimsAsync(IEnumerable<TripRow> rows)
        {
            var list = rows.Where(r => !dims.ContainsKey(r.Id)).ToList();
            var places = await PlacesAsync(list.Select(r => new TripPoint(r.Id, r.PickupLat, r.PickupLng, r.RequestedAt)).ToList(), zoneCities, activeZones, ct);
            foreach (var row in list)
            {
                var (cityId, zoneId) = places[row.Id];
                dims[row.Id] = new TripDim(cityId, zoneId, row.RideCategoryId);
            }
        }

        async Task EnsureDimsAsync(IEnumerable<Guid> tripIds)
        {
            var missing = tripIds.Where(id => !dims.ContainsKey(id)).Distinct().ToList();
            foreach (var chunk in missing.Chunk(Chunk))
            {
                var ids = chunk.ToList();
                await LoadDimsAsync(await TripQuery(t => ids.Contains(t.Id)).ToListAsync(ct));
            }
        }

        TripDim? DimOf(Guid? tripId) => tripId is { } id && dims.TryGetValue(id, out var d) ? d : null;

        // ----- trips -----
        var needTrips = Need(KpiCodes.CompletedTrips, KpiCodes.RequestedTrips, KpiCodes.CompletionRate, KpiCodes.TripsPerActiveRider, KpiCodes.AverageEta,
            KpiCodes.AverageTimeToAssign, KpiCodes.NoDriversRate, KpiCodes.DriverCancellationRate, KpiCodes.PassengerCancellationRate, KpiCodes.AverageFare,
            KpiCodes.DriverEarningsPerOnlineHour, KpiCodes.Gmv, KpiCodes.PlatformRevenue, KpiCodes.TakeRate, KpiCodes.ActiveRiders, KpiCodes.ActiveDrivers,
            KpiCodes.NewRiders, KpiCodes.RepeatRate, KpiCodes.DriverReliabilityRate, KpiCodes.PassengerReliabilityRate, KpiCodes.FavoriteDriverBookingRate,
            KpiCodes.FavoriteDriverDiscountUsage, KpiCodes.ScheduledRideCompletionRate, KpiCodes.ScheduledRideCancellationRate);
        var trips = needTrips
            ? await TripQuery(t => (t.RequestedAt >= start && t.RequestedAt < end) || (t.CompletedAt >= start && t.CompletedAt < end)
                                           || (t.CancelledAt >= start && t.CancelledAt < end) || (t.AssignedAt >= start && t.AssignedAt < end)
                                           || (t.ArrivedAt >= start && t.ArrivedAt < end) || (t.ScheduledAt >= start && t.ScheduledAt < end)).ToListAsync(ct)
            : [];
        await LoadDimsAsync(trips);
        var completed = trips.Where(t => t.Status == TripStatus.Completed && InRange(t.CompletedAt)).ToList();

        foreach (var t in trips)
        {
            var dim = dims[t.Id];
            if (t.Status == TripStatus.Completed && InRange(t.CompletedAt))
            {
                var fare = t.FinalFare ?? 0m;
                var earnings = t.DriverEarnings ?? 0m;
                Add(t.CompletedAt, dim, KpiCodes.CompletedTrips, 1);
                Add(t.CompletedAt, dim, KpiCodes.AverageFare, numerator: fare, denominator: 1);
                Add(t.CompletedAt, dim, KpiCodes.Gmv, fare + t.DiscountTotal);
                Add(t.CompletedAt, dim, KpiCodes.PlatformRevenue, fare + t.DiscountTotal - earnings);
                Add(t.CompletedAt, null, KpiCodes.DriverEarningsPerOnlineHour, numerator: earnings);
                Add(t.CompletedAt, dim, KpiCodes.FavoriteDriverBookingRate, numerator: t.FavoriteStatus == FavoriteStatus.Accepted ? 1 : 0, denominator: 1);
            }

            if (t.BookingType == BookingType.Now || (t.ScheduledAt is { } due && due <= now))
            {
                Add(t.RequestedAt, dim, KpiCodes.RequestedTrips, 1);
            }

            // Finished trips: completed at completed_at, cancelled / no_drivers at cancelled_at.
            var finishedAt = t.Status switch
            {
                TripStatus.Completed => t.CompletedAt,
                TripStatus.Cancelled or TripStatus.NoDrivers => t.CancelledAt,
                _ => null,
            };
            Add(finishedAt, dim, KpiCodes.CompletionRate, numerator: t.Status == TripStatus.Completed ? 1 : 0, denominator: 1);
            Add(finishedAt, dim, KpiCodes.NoDriversRate, numerator: t.Status == TripStatus.NoDrivers ? 1 : 0, denominator: 1);

            if (t.ArrivedAt is { } arrived && t.AssignedAt is { } assignedForEta)
            {
                Add(arrived, dim, KpiCodes.AverageEta, numerator: (decimal)Math.Max(0, (arrived - assignedForEta).TotalSeconds), denominator: 1);
            }

            if (t.AssignedAt is { } assigned)
            {
                if (t.BookingType == BookingType.Now)
                {
                    Add(assigned, dim, KpiCodes.AverageTimeToAssign, numerator: (decimal)Math.Max(0, (assigned - t.RequestedAt).TotalSeconds), denominator: 1);
                }

                // Cohort of trips assigned to a driver in the bucket (denominator of the cancellation and reliability rates).
                var done = t.Status == TripStatus.Completed ? 1 : 0;
                Add(assigned, dim, KpiCodes.DriverCancellationRate, denominator: 1);
                Add(assigned, dim, KpiCodes.PassengerCancellationRate, denominator: 1);
                Add(assigned, dim, KpiCodes.DriverReliabilityRate, numerator: done, denominator: 1);
                Add(assigned, dim, KpiCodes.PassengerReliabilityRate, numerator: done, denominator: 1);
            }

            if (t.BookingType == BookingType.Scheduled)
            {
                Add(t.RequestedAt, dim, KpiCodes.ScheduledRideCancellationRate,
                    numerator: t.Status is TripStatus.Cancelled or TripStatus.NoDrivers ? 1 : 0, denominator: 1);
            }
        }

        // Scheduled completion: scheduled trips whose time came in the bucket, except free cancellations by the passenger (doc 11 §F17.5).
        if (Need(KpiCodes.ScheduledRideCompletionRate))
        {
            var due = trips.Where(t => t.BookingType == BookingType.Scheduled && InRange(t.ScheduledAt) && t.ScheduledAt <= now).ToList();
            var passengerCancelled = due.Where(t => t.CancelledBy == CancelledBy.Passenger).Select(t => t.Id).ToList();
            var faultyCancellations = new HashSet<Guid>();
            foreach (var chunk in passengerCancelled.Chunk(Chunk))
            {
                var ids = chunk.ToList();
                faultyCancellations.UnionWith(await db.CancellationEvents.AsNoTracking()
                    .Where(e => ids.Contains(e.TripId) && e.Actor == TripActor.Passenger && e.AtFault != AtFault.None).Select(e => e.TripId).ToListAsync(ct));
            }

            foreach (var t in due.Where(t => t.CancelledBy != CancelledBy.Passenger || faultyCancellations.Contains(t.Id)))
            {
                Add(t.ScheduledAt, dims[t.Id], KpiCodes.ScheduledRideCompletionRate, numerator: t.Status == TripStatus.Completed ? 1 : 0, denominator: 1);
            }
        }

        // First completed trip ever (new riders).
        if (Need(KpiCodes.NewRiders))
        {
            var passengerIds = completed.Select(t => t.PassengerId).Distinct().ToList();
            var firsts = new Dictionary<Guid, DateTime>();
            foreach (var chunk in passengerIds.Chunk(Chunk))
            {
                var ids = chunk.ToList();
                var rows = await db.Trips.AsNoTracking().Where(t => t.Status == TripStatus.Completed && t.CompletedAt != null && ids.Contains(t.PassengerId))
                    .GroupBy(t => t.PassengerId).Select(g => new { PassengerId = g.Key, First = g.Min(t => t.CompletedAt) }).ToListAsync(ct);
                foreach (var row in rows.Where(r => r.First != null))
                {
                    firsts[row.PassengerId] = row.First!.Value;
                }
            }

            foreach (var group in completed.GroupBy(t => t.PassengerId))
            {
                var first = group.OrderBy(t => t.CompletedAt).First();
                if (firsts.TryGetValue(group.Key, out var firstEver) && firstEver >= first.CompletedAt!.Value)
                {
                    Add(first.CompletedAt, dims[first.Id], KpiCodes.NewRiders, 1);
                }
            }
        }

        // Platform-borne discounts (ledger trip_discount journals of the completed trips).
        if (Need(KpiCodes.PlatformRevenue, KpiCodes.TakeRate, KpiCodes.FavoriteDriverDiscountUsage))
        {
            var byId = completed.ToDictionary(t => t.Id);
            foreach (var chunk in completed.Select(t => t.Id).Chunk(Chunk))
            {
                var ids = chunk.ToList();
                var rows = await (from e in db.LedgerEntries.AsNoTracking()
                                  join j in db.LedgerJournals.AsNoTracking() on e.JournalId equals j.Id
                                  where j.Type == JournalType.TripDiscount && j.ReferenceId != null && ids.Contains(j.ReferenceId.Value)
                                        && (e.Account == LedgerAccounts.DiscountPromotion || e.Account == LedgerAccounts.DiscountFavoriteDriver)
                                  select new { TripId = j.ReferenceId!.Value, e.Account, e.Debit }).ToListAsync(ct);
                foreach (var row in rows)
                {
                    var trip = byId[row.TripId];
                    Add(trip.CompletedAt, dims[trip.Id], KpiCodes.PlatformRevenue, -row.Debit);
                    if (row.Account == LedgerAccounts.DiscountFavoriteDriver)
                    {
                        Add(trip.CompletedAt, dims[trip.Id], KpiCodes.FavoriteDriverDiscountUsage, row.Debit, numerator: 1, countNumerator: true);
                    }
                }
            }
        }

        // ----- offers -----
        if (Need(KpiCodes.DriverAcceptanceRate))
        {
            var offers = await db.TripOffers.AsNoTracking()
                .Where(o => o.RespondedAt >= start && o.RespondedAt < end && o.Status != OfferStatus.Sent)
                .Select(o => new { o.TripId, o.Status, o.RespondedAt }).ToListAsync(ct);
            await EnsureDimsAsync(offers.Select(o => o.TripId));
            foreach (var o in offers)
            {
                Add(o.RespondedAt, DimOf(o.TripId), KpiCodes.DriverAcceptanceRate, numerator: o.Status == OfferStatus.Accepted ? 1 : 0, denominator: 1);
            }
        }

        // ----- cancellation events -----
        if (Need(KpiCodes.DriverCancellationRate, KpiCodes.PassengerCancellationRate, KpiCodes.Gmv, KpiCodes.PlatformRevenue, KpiCodes.TakeRate,
                KpiCodes.CancellationFeeRevenue, KpiCodes.RepeatCancellationRate))
        {
            var events = await db.CancellationEvents.AsNoTracking().Where(e => e.CreatedAt >= start && e.CreatedAt < end)
                .Select(e => new { e.TripId, e.UserId, e.AtFault, e.CountsTowardRate, e.FeeCharged, e.FeeStatus, e.CompensationAmount, e.CreatedAt }).ToListAsync(ct);
            await EnsureDimsAsync(events.Select(e => e.TripId));
            var tripUsers = trips.ToDictionary(t => t.Id, t => (t.PassengerId, t.DriverId));
            var missingUsers = events.Where(e => e.UserId == null && !tripUsers.ContainsKey(e.TripId)).Select(e => e.TripId).Distinct().ToList();
            foreach (var chunk in missingUsers.Chunk(Chunk))
            {
                var ids = chunk.ToList();
                foreach (var row in await db.Trips.AsNoTracking().Where(t => ids.Contains(t.Id)).Select(t => new { t.Id, t.PassengerId, t.DriverId }).ToListAsync(ct))
                {
                    tripUsers[row.Id] = (row.PassengerId, row.DriverId);
                }
            }

            var faults = new Dictionary<(DateOnly Bucket, string Scope), Dictionary<string, int>>();
            foreach (var e in events)
            {
                var dim = DimOf(e.TripId);
                var counted = e.CountsTowardRate && e.AtFault != AtFault.None;
                Add(e.CreatedAt, dim, KpiCodes.DriverCancellationRate, numerator: counted && e.AtFault == AtFault.Driver ? 1 : 0);
                Add(e.CreatedAt, dim, KpiCodes.PassengerCancellationRate, numerator: counted && e.AtFault == AtFault.Passenger ? 1 : 0);
                var collected = e.FeeStatus == CancellationFeeStatus.Charged ? e.FeeCharged : 0m;
                Add(e.CreatedAt, dim, KpiCodes.Gmv, collected);
                Add(e.CreatedAt, dim, KpiCodes.CancellationFeeRevenue, collected);
                // Gross fees (refunded ones included: the refund itself is subtracted below with the other refunds) minus driver compensation.
                var gross = e.FeeStatus is CancellationFeeStatus.Charged or CancellationFeeStatus.Refunded ? e.FeeCharged : 0m;
                Add(e.CreatedAt, dim, KpiCodes.PlatformRevenue, gross - e.CompensationAmount);
                if (counted && metrics.Contains(KpiCodes.RepeatCancellationRate))
                {
                    var (passengerId, driverId) = tripUsers.GetValueOrDefault(e.TripId);
                    var user = e.UserId?.ToString() ?? (e.AtFault == AtFault.Passenger ? $"p:{passengerId}" : $"d:{driverId}");
                    var bucket = bucketOf(Formats.RiyadhDate(e.CreatedAt));
                    foreach (var scope in ScopesOf(dim))
                    {
                        result.Scopes.TryAdd(scope.Key, scope);
                        if (!faults.TryGetValue((bucket, scope.Key), out var perUser))
                        {
                            faults[(bucket, scope.Key)] = perUser = [];
                        }

                        perUser[user] = perUser.GetValueOrDefault(user) + 1;
                    }
                }
            }

            foreach (var ((bucket, scopeKey), perUser) in faults)
            {
                var acc = result.Acc(bucket, result.Scopes[scopeKey], KpiCodes.RepeatCancellationRate);
                acc.Numerator = perUser.Count(u => u.Value >= 2);
                acc.Denominator = perUser.Count;
            }
        }

        // ----- scheduled reservations a driver dropped (driver reliability) -----
        if (Need(KpiCodes.DriverReliabilityRate))
        {
            ReservationReleaseReason[] driverFailures =
                [ReservationReleaseReason.DriverReleased, ReservationReleaseReason.ConfirmationMissed, ReservationReleaseReason.FinalConfirmationMissed, ReservationReleaseReason.NoShow];
            var dropped = await db.ScheduledRideReservations.AsNoTracking()
                .Where(r => r.ReleasedAt >= start && r.ReleasedAt < end && r.ReleaseReason != null && driverFailures.Contains(r.ReleaseReason.Value))
                .Select(r => new { r.TripId, r.ReleasedAt }).ToListAsync(ct);
            await EnsureDimsAsync(dropped.Select(r => r.TripId));
            foreach (var r in dropped)
            {
                Add(r.ReleasedAt, DimOf(r.TripId), KpiCodes.DriverReliabilityRate, denominator: 1);
            }
        }

        // ----- ratings of drivers by passengers -----
        if (Need(KpiCodes.CustomerRating))
        {
            var ratings = await db.Ratings.AsNoTracking()
                .Where(r => r.CreatedAt >= start && r.CreatedAt < end && r.RaterRole == RatingRole.Passenger && r.Status == RatingStatus.Visible)
                .Select(r => new { r.TripId, r.Stars, r.CreatedAt }).ToListAsync(ct);
            await EnsureDimsAsync(ratings.Select(r => r.TripId));
            foreach (var r in ratings)
            {
                Add(r.CreatedAt, DimOf(r.TripId), KpiCodes.CustomerRating, numerator: r.Stars, denominator: 1);
            }
        }

        // ----- refunds -----
        if (Need(KpiCodes.RefundsAmount, KpiCodes.PlatformRevenue, KpiCodes.TakeRate))
        {
            var refunds = await db.Refunds.AsNoTracking()
                .Where(r => r.Status == RefundStatus.Succeeded && ((r.ProcessedAt >= start && r.ProcessedAt < end) || (r.ProcessedAt == null && r.UpdatedAt >= start && r.UpdatedAt < end)))
                .Select(r => new { r.TripId, r.Amount, At = r.ProcessedAt ?? r.UpdatedAt }).ToListAsync(ct);
            await EnsureDimsAsync(refunds.Where(r => r.TripId != null).Select(r => r.TripId!.Value));
            foreach (var r in refunds)
            {
                var dim = DimOf(r.TripId);
                Add(r.At, dim, KpiCodes.RefundsAmount, r.Amount);
                Add(r.At, dim, KpiCodes.PlatformRevenue, -r.Amount);
            }
        }

        // ----- incentives (driver wallets) -----
        if (Need(KpiCodes.IncentivesPaid, KpiCodes.DriverEarningsPerOnlineHour))
        {
            var incentives = await db.WalletTransactions.AsNoTracking()
                .Where(t => t.Type == TransactionType.Incentive && t.CreatedAt >= start && t.CreatedAt < end)
                .Select(t => new { t.Amount, t.Direction, t.CreatedAt }).ToListAsync(ct);
            foreach (var i in incentives)
            {
                var amount = i.Direction == TransactionDirection.Credit ? i.Amount : -i.Amount;
                Add(i.CreatedAt, null, KpiCodes.IncentivesPaid, amount);
                Add(i.CreatedAt, null, KpiCodes.DriverEarningsPerOnlineHour, numerator: amount);
            }
        }

        // ----- online hours (driver_status_logs, split per Riyadh day) -----
        if (Need(KpiCodes.OnlineHours, KpiCodes.DriverEarningsPerOnlineHour))
        {
            var lookback = start - OnlineLookback;
            var logs = await db.DriverStatusLogs.AsNoTracking().Where(l => l.ChangedAt >= lookback && l.ChangedAt < end)
                .Select(l => new { l.DriverId, l.IsOnline, l.ChangedAt }).ToListAsync(ct);
            var limit = end < now ? end : now;
            foreach (var driver in logs.GroupBy(l => l.DriverId))
            {
                var ordered = driver.OrderBy(l => l.ChangedAt).ToList();
                for (var i = 0; i < ordered.Count; i++)
                {
                    if (!ordered[i].IsOnline)
                    {
                        continue;
                    }

                    var from0 = ordered[i].ChangedAt < start ? start : ordered[i].ChangedAt;
                    var to0 = i + 1 < ordered.Count ? ordered[i + 1].ChangedAt : limit;
                    if (to0 > limit) to0 = limit;
                    // Split the interval at Riyadh midnights.
                    while (from0 < to0)
                    {
                        var dayEnd = Formats.RiyadhMidnightUtc(Formats.RiyadhDate(from0).AddDays(1));
                        var sliceEnd = dayEnd < to0 ? dayEnd : to0;
                        var hours = (decimal)(sliceEnd - from0).TotalHours;
                        Add(from0, null, KpiCodes.OnlineHours, hours);
                        Add(from0, null, KpiCodes.DriverEarningsPerOnlineHour, denominator: hours);
                        from0 = sliceEnd;
                    }
                }
            }
        }

        // ----- support resolution time (doc 11 §F18.5) -----
        if (Need(KpiCodes.SupportResolutionTime))
        {
            var tickets = await db.SupportTickets.AsNoTracking().Where(t => t.ResolvedAt >= start && t.ResolvedAt < end)
                .Select(t => new { t.CreatedAt, t.ResolvedAt, t.SlaPausedSeconds }).ToListAsync(ct);
            foreach (var t in tickets)
            {
                var hours = (decimal)Math.Max(0, (t.ResolvedAt!.Value - t.CreatedAt).TotalSeconds - t.SlaPausedSeconds) / 3600m;
                Add(t.ResolvedAt, null, KpiCodes.SupportResolutionTime, numerator: hours, denominator: 1);
            }
        }

        // ----- take rate = platform revenue ÷ GMV (per cell) -----
        if (Need(KpiCodes.TakeRate))
        {
            foreach (var ((bucket, scopeKey), cell) in result.Cells.ToList())
            {
                if (cell.TryGetValue(KpiCodes.Gmv, out var gmv) || cell.ContainsKey(KpiCodes.PlatformRevenue))
                {
                    var take = result.Acc(bucket, result.Scopes[scopeKey], KpiCodes.TakeRate);
                    take.Numerator = cell.GetValueOrDefault(KpiCodes.PlatformRevenue)?.Value ?? 0m;
                    take.Denominator = gmv?.Value ?? 0m;
                }
            }
        }

        // ----- distinct riders / drivers -----
        if (Need(KpiCodes.ActiveRiders, KpiCodes.ActiveDrivers, KpiCodes.RepeatRate, KpiCodes.TripsPerActiveRider))
        {
            var groups = new Dictionary<(DateOnly Bucket, string Scope), (Dictionary<Guid, int> Riders, HashSet<Guid> Drivers, int Trips)>();
            foreach (var t in completed)
            {
                var bucket = bucketOf(Formats.RiyadhDate(t.CompletedAt!.Value));
                foreach (var scope in ScopesOf(dims[t.Id]))
                {
                    result.Scopes.TryAdd(scope.Key, scope);
                    if (!groups.TryGetValue((bucket, scope.Key), out var g))
                    {
                        g = ([], [], 0);
                    }

                    g.Riders[t.PassengerId] = g.Riders.GetValueOrDefault(t.PassengerId) + 1;
                    if (t.DriverId is { } driverId) g.Drivers.Add(driverId);
                    groups[(bucket, scope.Key)] = (g.Riders, g.Drivers, g.Trips + 1);
                }
            }

            foreach (var ((bucket, scopeKey), g) in groups)
            {
                var scope = result.Scopes[scopeKey];
                if (metrics.Contains(KpiCodes.ActiveRiders)) result.Acc(bucket, scope, KpiCodes.ActiveRiders).Value = g.Riders.Count;
                if (metrics.Contains(KpiCodes.ActiveDrivers)) result.Acc(bucket, scope, KpiCodes.ActiveDrivers).Value = g.Drivers.Count;
                if (metrics.Contains(KpiCodes.RepeatRate))
                {
                    var repeat = result.Acc(bucket, scope, KpiCodes.RepeatRate);
                    repeat.Numerator = g.Riders.Count(r => r.Value >= 2);
                    repeat.Denominator = g.Riders.Count;
                }

                if (metrics.Contains(KpiCodes.TripsPerActiveRider))
                {
                    var perRider = result.Acc(bucket, scope, KpiCodes.TripsPerActiveRider);
                    perRider.Numerator = g.Trips;
                    perRider.Denominator = g.Riders.Count;
                }
            }
        }

        return result;
    }

    public sealed record TripPoint(Guid Id, decimal PickupLat, decimal PickupLng, DateTime RequestedAt);

    /// <summary>Pickup zone and city of trips: <c>fare_quotes.pickup_zone_id</c> of the quote the trip used, else the zone resolved at the pickup point.</summary>
    public async Task<Dictionary<Guid, (Guid? CityId, Guid? ZoneId)>> PlacesAsync(IReadOnlyCollection<TripPoint> trips, CancellationToken ct)
    {
        var zoneCities = await db.Zones.AsNoTracking().Select(z => new { z.Id, z.CityId }).ToDictionaryAsync(z => z.Id, z => z.CityId, ct);
        return await PlacesAsync(trips, zoneCities, await zones.AllAsync(ct), ct);
    }

    private async Task<Dictionary<Guid, (Guid? CityId, Guid? ZoneId)>> PlacesAsync(
        IReadOnlyCollection<TripPoint> trips, IReadOnlyDictionary<Guid, Guid> zoneCities, IReadOnlyList<ZoneSnapshot> activeZones, CancellationToken ct)
    {
        var result = new Dictionary<Guid, (Guid? CityId, Guid? ZoneId)>();
        foreach (var chunk in trips.Chunk(Chunk))
        {
            var ids = chunk.Select(r => r.Id).ToList();
            var quoted = await db.FareQuotes.AsNoTracking().Where(q => q.UsedTripId != null && ids.Contains(q.UsedTripId.Value) && q.PickupZoneId != null)
                .Select(q => new { TripId = q.UsedTripId!.Value, ZoneId = q.PickupZoneId!.Value }).ToListAsync(ct);
            var byTrip = quoted.GroupBy(q => q.TripId).ToDictionary(g => g.Key, g => g.First().ZoneId);
            foreach (var row in chunk)
            {
                Guid? zoneId = byTrip.TryGetValue(row.Id, out var z) ? z : zones.Resolve(activeZones, row.PickupLat, row.PickupLng, row.RequestedAt)?.Id;
                Guid? cityId = zoneId is { } zid && zoneCities.TryGetValue(zid, out var city) ? city : null;
                result[row.Id] = (cityId, zoneId);
            }
        }

        return result;
    }

    private IQueryable<TripRow> TripQuery(System.Linq.Expressions.Expression<Func<Trip, bool>> filter) =>
        db.Trips.AsNoTracking().Where(filter).Select(t => new TripRow(
            t.Id, t.PassengerId, t.DriverId, t.RideCategoryId, t.Status, t.BookingType, t.ScheduledAt, t.RequestedAt, t.AssignedAt, t.ArrivedAt, t.CompletedAt,
            t.CancelledAt, t.CancelledBy, t.FinalFare, t.DiscountTotal, t.DriverEarnings, t.FavoriteStatus, t.PickupLat, t.PickupLng));

    private static IEnumerable<KpiScope> ScopesOf(TripDim? dim)
    {
        yield return KpiScope.All;
        if (dim is null)
        {
            yield break;
        }

        yield return KpiScope.For(null, null, dim.CategoryId);
        if (dim.CityId is { } city)
        {
            yield return KpiScope.For(city, null, null);
            yield return KpiScope.For(city, null, dim.CategoryId);
        }

        if (dim.ZoneId is { } zone)
        {
            yield return KpiScope.For(null, zone, null);
            yield return KpiScope.For(null, zone, dim.CategoryId);
        }
    }
}
