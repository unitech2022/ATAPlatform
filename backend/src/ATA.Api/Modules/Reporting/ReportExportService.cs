using System.Globalization;
using System.Text;
using System.Text.Json;
using ATA.Api.Common;
using ATA.Api.Modules.Payments;
using ATA.Domain.Cancellation;
using ATA.Domain.Common;
using ATA.Domain.Reporting;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Reporting;

/// <summary>A CSV export ready to stream: header + rows (the row limit is checked before the response starts).</summary>
public sealed record ReportExport(string FileName, IReadOnlyList<string> Header, IReadOnlyList<string?[]> Rows)
{
    public async Task WriteAsync(Stream stream, CancellationToken ct)
    {
        await using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), 64 * 1024, leaveOpen: true);
        await writer.WriteAsync(Csv.Row([.. Header]));
        foreach (var row in Rows)
        {
            ct.ThrowIfCancellationRequested();
            await writer.WriteAsync(Csv.Row(row));
        }

        await writer.FlushAsync(ct);
    }
}

/// <summary>
/// <c>GET /admin/reports/export</c> (doc 12 §F20.7): the datasets and columns of the doc, UTF-8 with BOM, at most <c>Reports:MaxExportRows</c> rows
/// (<c>422 report_range_too_large { maxRows }</c>). Users appear as UUIDs (passenger / driver profile ids), never names or phone numbers. Dates filter the
/// dataset's own timestamp by Riyadh day; city / zone / category filter the trip-based datasets (trips, cancellations, ratings, kpis) and the drivers' city.
/// </summary>
public sealed class ReportExportService(AtaDbContext db, KpiCalculator calculator, ReportService reports, IClock clock, IOptions<ReportsOptions> options)
{
    public static readonly IReadOnlyDictionary<string, string[]> Datasets = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["kpis"] = ["date", "scope", "metric_code", "value", "numerator", "denominator"],
        ["trips"] = ["trip_number", "requested_at", "completed_at", "status", "category", "city", "zone", "passenger_id", "driver_id", "booking_type", "payment_method",
            "distance_km", "duration_min", "estimated_fare", "final_fare", "discount_total", "driver_earnings", "cancelled_by", "cancellation_reason", "corporate_account"],
        ["payments"] = ["payment_id", "created_at", "purpose", "method", "provider", "status", "amount", "captured_amount", "refunded_amount", "trip_number", "gateway_payment_id"],
        ["payouts"] = ["payout_number", "requested_at", "driver", "amount", "status", "approved_at", "paid_at", "bank_reference", "batch_number"],
        ["cancellations"] = ["trip_number", "created_at", "actor", "at_fault", "stage", "reason_code", "fee_amount", "fee_charged", "fee_status", "compensation", "penalty_points", "excuse_status"],
        ["ratings"] = ["trip_number", "created_at", "rater_role", "stars", "tags", "has_comment"],
        ["support_tickets"] = ["ticket_number", "created_at", "type", "priority", "status", "first_response_minutes", "resolution_hours", "sla_met", "csat"],
        ["drivers"] = ["driver_id", "application_number", "status", "tier", "rating_avg", "city", "approved_at", "completed_trips", "online_hours", "reliability_level"],
        ["incentives"] = ["incentive", "driver", "period_start", "completed_trips", "status", "reward_amount", "paid_at"],
    };

    public async Task<ReportExport> ExportAsync(string? dataset, DateOnly? from, DateOnly? to, Guid? cityId, Guid? zoneId, Guid? rideCategoryId, string? format, Language lang, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(dataset), dataset, 40)
            .Rule(nameof(dataset), dataset is null || Datasets.ContainsKey(dataset), "must be one of: " + string.Join('|', Datasets.Keys))
            .Rule(nameof(format), format is null || format == "csv", "must be csv")
            .ThrowIfInvalid();
        var (start, end) = reports.ValidateRange(from, to);
        var scope = await reports.ValidateScopeAsync(cityId, zoneId, rideCategoryId, ct);
        var window = (Formats.RiyadhMidnightUtc(start), Formats.RiyadhMidnightUtc(end.AddDays(1)));
        var rows = dataset switch
        {
            "kpis" => await KpisAsync(start, end, scope, ct),
            "trips" => await TripsAsync(window, scope, lang, ct),
            "payments" => await PaymentsAsync(window, ct),
            "payouts" => await PayoutsAsync(window, ct),
            "cancellations" => await CancellationsAsync(window, scope, ct),
            "ratings" => await RatingsAsync(window, scope, ct),
            "support_tickets" => await TicketsAsync(window, ct),
            "drivers" => await DriversAsync(window, cityId, lang, ct),
            _ => await IncentivesAsync(window, lang, ct),
        };
        return new ReportExport($"{dataset}-{start:yyyyMMdd}-{end:yyyyMMdd}.csv", Datasets[dataset!], rows);
    }

    private void EnsureRowLimit(int count)
    {
        if (count > options.Value.MaxExportRows)
        {
            throw new DomainException(ErrorCodes.ReportRangeTooLarge, new { maxRows = options.Value.MaxExportRows, rows = count });
        }
    }

    private async Task<List<string?[]>> KpisAsync(DateOnly from, DateOnly to, KpiScope scope, CancellationToken ct)
    {
        var query = db.ReportSnapshots.AsNoTracking().Where(s => s.SnapshotDate >= from && s.SnapshotDate <= to);
        if (scope.Key != ReportSnapshot.AllScope)
        {
            query = query.Where(s => s.ScopeKey == scope.Key);
        }

        EnsureRowLimit(await query.CountAsync(ct));
        var rows = await query.OrderBy(s => s.SnapshotDate).ThenBy(s => s.ScopeKey).ThenBy(s => s.MetricCode)
            .Select(s => new { s.SnapshotDate, s.ScopeKey, s.MetricCode, s.Value, s.Numerator, s.Denominator }).ToListAsync(ct);
        return rows.Select(s => new[] { s.SnapshotDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), s.ScopeKey, s.MetricCode, Num(s.Value), Num(s.Numerator), Num(s.Denominator) })
            .Cast<string?[]>().ToList();
    }

    private async Task<List<string?[]>> TripsAsync((DateTime Start, DateTime End) w, KpiScope scope, Language lang, CancellationToken ct)
    {
        var query = db.Trips.AsNoTracking().Where(t => t.RequestedAt >= w.Start && t.RequestedAt < w.End);
        if (scope.RideCategoryId is { } category) query = query.Where(t => t.RideCategoryId == category);
        if (scope.CityId is null && scope.ZoneId is null) EnsureRowLimit(await query.CountAsync(ct));
        var trips = await query.OrderBy(t => t.RequestedAt).Select(t => new
        {
            t.Id, t.TripNumber, t.RequestedAt, t.CompletedAt, t.Status, t.RideCategoryId, t.PickupLat, t.PickupLng, t.PassengerId, t.DriverId, t.BookingType, t.PaymentMethod,
            t.EstimatedDistanceM, t.FinalDistanceM, t.EstimatedDurationS, t.FinalDurationS, t.EstimatedFare, t.FinalFare, t.DiscountTotal, t.DriverEarnings, t.CancelledBy,
            t.CancellationReason, t.CorporateAccountId,
        }).ToListAsync(ct);
        var places = await calculator.PlacesAsync(trips.Select(t => new KpiCalculator.TripPoint(t.Id, t.PickupLat, t.PickupLng, t.RequestedAt)).ToList(), ct);
        var filtered = trips.Where(t => Matches(places[t.Id], scope)).ToList();
        EnsureRowLimit(filtered.Count);
        var categories = await db.RideCategories.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.Code, ct);
        var cities = await db.Cities.AsNoTracking().ToDictionaryAsync(c => c.Id, c => lang.Pick(c.NameAr, c.NameEn), ct);
        var zoneCodes = await db.Zones.AsNoTracking().ToDictionaryAsync(z => z.Id, z => z.Code, ct);
        var accountIds = filtered.Where(t => t.CorporateAccountId != null).Select(t => t.CorporateAccountId!.Value).Distinct().ToList();
        var accounts = await db.CorporateAccounts.AsNoTracking().Where(a => accountIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id, a => a.AccountNumber, ct);
        return filtered.Select(t =>
        {
            var (city, zone) = places[t.Id];
            return new[]
            {
                t.TripNumber, Ts(t.RequestedAt), Ts(t.CompletedAt), Snake(t.Status), categories.GetValueOrDefault(t.RideCategoryId), city is { } c ? cities.GetValueOrDefault(c) : null,
                zone is { } z ? zoneCodes.GetValueOrDefault(z) : null, t.PassengerId.ToString(), t.DriverId?.ToString(), Snake(t.BookingType), Snake(t.PaymentMethod),
                Num(decimal.Round((t.FinalDistanceM ?? t.EstimatedDistanceM) / 1000m, 2)), Num(decimal.Round((t.FinalDurationS ?? t.EstimatedDurationS) / 60m, 1)),
                Num(t.EstimatedFare), Num(t.FinalFare), Num(t.DiscountTotal), Num(t.DriverEarnings), t.CancelledBy is null ? null : Snake(t.CancelledBy.Value), t.CancellationReason,
                t.CorporateAccountId is { } a ? accounts.GetValueOrDefault(a) : null,
            };
        }).Cast<string?[]>().ToList();
    }

    private async Task<List<string?[]>> PaymentsAsync((DateTime Start, DateTime End) w, CancellationToken ct)
    {
        var query = db.Payments.AsNoTracking().Where(p => p.CreatedAt >= w.Start && p.CreatedAt < w.End);
        EnsureRowLimit(await query.CountAsync(ct));
        var rows = await (from p in query
                          join t in db.Trips.AsNoTracking() on p.TripId equals (Guid?)t.Id into tj
                          from t in tj.DefaultIfEmpty()
                          orderby p.CreatedAt
                          select new { p.Id, p.CreatedAt, p.Purpose, p.Method, p.Provider, p.Status, p.Amount, p.CapturedAmount, p.RefundedAmount, TripNumber = t == null ? null : t.TripNumber, p.GatewayPaymentId })
            .ToListAsync(ct);
        return rows.Select(p => new[]
        {
            p.Id.ToString(), Ts(p.CreatedAt), Snake(p.Purpose), Snake(p.Method), p.Provider, Snake(p.Status), Num(p.Amount), Num(p.CapturedAmount), Num(p.RefundedAmount), p.TripNumber, p.GatewayPaymentId,
        }).Cast<string?[]>().ToList();
    }

    private async Task<List<string?[]>> PayoutsAsync((DateTime Start, DateTime End) w, CancellationToken ct)
    {
        var query = db.Payouts.AsNoTracking().Where(p => p.RequestedAt >= w.Start && p.RequestedAt < w.End);
        EnsureRowLimit(await query.CountAsync(ct));
        var rows = await (from p in query
                          join b in db.PayoutBatches.AsNoTracking() on p.BatchId equals (Guid?)b.Id into bj
                          from b in bj.DefaultIfEmpty()
                          orderby p.RequestedAt
                          select new { p.PayoutNumber, p.RequestedAt, p.DriverId, p.Amount, p.Status, p.ApprovedAt, p.PaidAt, p.BankReference, BatchNumber = b == null ? null : b.BatchNumber })
            .ToListAsync(ct);
        return rows.Select(p => new[]
        {
            p.PayoutNumber, Ts(p.RequestedAt), p.DriverId.ToString(), Num(p.Amount), Snake(p.Status), Ts(p.ApprovedAt), Ts(p.PaidAt), p.BankReference, p.BatchNumber,
        }).Cast<string?[]>().ToList();
    }

    private async Task<List<string?[]>> CancellationsAsync((DateTime Start, DateTime End) w, KpiScope scope, CancellationToken ct)
    {
        var events = await (from e in db.CancellationEvents.AsNoTracking()
                            join t in db.Trips.AsNoTracking() on e.TripId equals t.Id
                            where e.CreatedAt >= w.Start && e.CreatedAt < w.End
                            orderby e.CreatedAt
                            select new { e, t.TripNumber, t.RideCategoryId, t.PickupLat, t.PickupLng, t.RequestedAt }).ToListAsync(ct);
        var filtered = await FilterByTripAsync(events, x => (x.e.TripId, x.RideCategoryId, x.PickupLat, x.PickupLng, x.RequestedAt), scope, ct);
        EnsureRowLimit(filtered.Count);
        return filtered.Select(x => new[]
        {
            x.TripNumber, Ts(x.e.CreatedAt), Snake(x.e.Actor), Snake(x.e.AtFault), Snake(x.e.Stage), x.e.ReasonCode, Num(x.e.FeeAmount), Num(x.e.FeeCharged), Snake(x.e.FeeStatus),
            Num(x.e.CompensationAmount), x.e.PenaltyPoints.ToString(CultureInfo.InvariantCulture), Snake(x.e.ExcuseStatus),
        }).Cast<string?[]>().ToList();
    }

    private async Task<List<string?[]>> RatingsAsync((DateTime Start, DateTime End) w, KpiScope scope, CancellationToken ct)
    {
        var ratings = await (from r in db.Ratings.AsNoTracking()
                             join t in db.Trips.AsNoTracking() on r.TripId equals t.Id
                             where r.CreatedAt >= w.Start && r.CreatedAt < w.End
                             orderby r.CreatedAt
                             select new { r.TripId, t.TripNumber, r.CreatedAt, r.RaterRole, r.Stars, r.Tags, r.Comment, t.RideCategoryId, t.PickupLat, t.PickupLng, t.RequestedAt })
            .ToListAsync(ct);
        var filtered = await FilterByTripAsync(ratings, x => (x.TripId, x.RideCategoryId, x.PickupLat, x.PickupLng, x.RequestedAt), scope, ct);
        EnsureRowLimit(filtered.Count);
        return filtered.Select(x => new[]
        {
            x.TripNumber, Ts(x.CreatedAt), Snake(x.RaterRole), x.Stars.ToString(CultureInfo.InvariantCulture), string.Join('|', ParseTags(x.Tags)),
            string.IsNullOrWhiteSpace(x.Comment) ? "false" : "true",
        }).Cast<string?[]>().ToList();
    }

    private async Task<List<string?[]>> TicketsAsync((DateTime Start, DateTime End) w, CancellationToken ct)
    {
        var query = db.SupportTickets.AsNoTracking().Where(t => t.CreatedAt >= w.Start && t.CreatedAt < w.End);
        EnsureRowLimit(await query.CountAsync(ct));
        var tickets = await query.OrderBy(t => t.CreatedAt)
            .Select(t => new { t.TicketNumber, t.CreatedAt, t.Type, t.Priority, t.Status, t.FirstResponseAt, t.ResolvedAt, t.ResolutionDueAt, t.SlaPausedSeconds, t.CsatScore })
            .ToListAsync(ct);
        return tickets.Select(t => new[]
        {
            t.TicketNumber, Ts(t.CreatedAt), Snake(t.Type), Snake(t.Priority), Snake(t.Status),
            t.FirstResponseAt is { } first ? Num(decimal.Round((decimal)(first - t.CreatedAt).TotalMinutes, 1)) : null,
            t.ResolvedAt is { } resolved ? Num(decimal.Round((decimal)Math.Max(0, (resolved - t.CreatedAt).TotalSeconds - t.SlaPausedSeconds) / 3600m, 2)) : null,
            t.ResolvedAt is { } done ? (done <= t.ResolutionDueAt ? "true" : "false") : null,
            t.CsatScore?.ToString(CultureInfo.InvariantCulture),
        }).Cast<string?[]>().ToList();
    }

    private async Task<List<string?[]>> DriversAsync((DateTime Start, DateTime End) w, Guid? cityId, Language lang, CancellationToken ct)
    {
        var query = db.Drivers.AsNoTracking().AsQueryable();
        if (cityId is { } c) query = query.Where(d => d.CityId == c);
        EnsureRowLimit(await query.CountAsync(ct));
        var drivers = await query.OrderBy(d => d.ApplicationNumber)
            .Select(d => new { d.Id, d.UserId, d.ApplicationNumber, d.ApplicationStatus, d.Tier, d.RatingAvg, d.CityId, d.ApprovedAt }).ToListAsync(ct);
        var completed = await db.Trips.AsNoTracking().Where(t => t.Status == TripStatus.Completed && t.CompletedAt >= w.Start && t.CompletedAt < w.End && t.DriverId != null)
            .GroupBy(t => t.DriverId!.Value).Select(g => new { DriverId = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.DriverId, x => x.Count, ct);
        var online = await OnlineHoursAsync(w, ct);
        var levels = await db.ReliabilityProfiles.AsNoTracking().Where(p => p.Role == Role.Driver)
            .Select(p => new { p.UserId, p.RestrictionLevel }).ToDictionaryAsync(p => p.UserId, p => p.RestrictionLevel, ct);
        var cities = await db.Cities.AsNoTracking().ToDictionaryAsync(x => x.Id, x => lang.Pick(x.NameAr, x.NameEn), ct);
        return drivers.Select(d => new[]
        {
            d.Id.ToString(), d.ApplicationNumber, Snake(d.ApplicationStatus), Snake(d.Tier), Num(d.RatingAvg), d.CityId is { } cid ? cities.GetValueOrDefault(cid) : null, Ts(d.ApprovedAt),
            completed.GetValueOrDefault(d.Id).ToString(CultureInfo.InvariantCulture), Num(decimal.Round(online.GetValueOrDefault(d.Id), 2)),
            levels.TryGetValue(d.UserId, out var level) ? Snake(level) : Snake(RestrictionLevel.None),
        }).Cast<string?[]>().ToList();
    }

    private async Task<List<string?[]>> IncentivesAsync((DateTime Start, DateTime End) w, Language lang, CancellationToken ct)
    {
        var query = from p in db.DriverIncentiveProgress.AsNoTracking()
                    join i in db.DriverIncentives.AsNoTracking() on p.IncentiveId equals i.Id
                    where p.PeriodStart >= w.Start && p.PeriodStart < w.End
                    select new { i.NameAr, i.NameEn, p.DriverId, p.PeriodStart, p.CompletedTrips, p.Status, p.RewardAmount, p.PaidAt };
        EnsureRowLimit(await query.CountAsync(ct));
        var rows = await query.OrderBy(x => x.PeriodStart).ToListAsync(ct);
        return rows.Select(x => new[]
        {
            lang.Pick(x.NameAr, x.NameEn), x.DriverId.ToString(), Ts(x.PeriodStart), x.CompletedTrips.ToString(CultureInfo.InvariantCulture), Snake(x.Status), Num(x.RewardAmount), Ts(x.PaidAt),
        }).Cast<string?[]>().ToList();
    }

    private async Task<List<T>> FilterByTripAsync<T>(List<T> rows, Func<T, (Guid TripId, Guid CategoryId, decimal Lat, decimal Lng, DateTime RequestedAt)> trip, KpiScope scope, CancellationToken ct)
    {
        if (scope.RideCategoryId is { } category)
        {
            rows = rows.Where(r => trip(r).CategoryId == category).ToList();
        }

        if (scope.CityId is null && scope.ZoneId is null)
        {
            return rows;
        }

        var points = rows.Select(trip).DistinctBy(t => t.TripId).Select(t => new KpiCalculator.TripPoint(t.TripId, t.Lat, t.Lng, t.RequestedAt)).ToList();
        var places = await calculator.PlacesAsync(points, ct);
        return rows.Where(r => Matches(places[trip(r).TripId], scope)).ToList();
    }

    private static bool Matches((Guid? CityId, Guid? ZoneId) place, KpiScope scope) =>
        (scope.ZoneId is null || place.ZoneId == scope.ZoneId) && (scope.CityId is null || place.CityId == scope.CityId);

    /// <summary>Online hours per driver within the window (<c>driver_status_logs</c>, 7-day look-back for drivers already online at the start).</summary>
    private async Task<Dictionary<Guid, decimal>> OnlineHoursAsync((DateTime Start, DateTime End) w, CancellationToken ct)
    {
        var lookback = w.Start.AddDays(-7);
        var limit = w.End < clock.UtcNow ? w.End : clock.UtcNow;
        var logs = await db.DriverStatusLogs.AsNoTracking().Where(l => l.ChangedAt >= lookback && l.ChangedAt < w.End)
            .Select(l => new { l.DriverId, l.IsOnline, l.ChangedAt }).ToListAsync(ct);
        var result = new Dictionary<Guid, decimal>();
        foreach (var driver in logs.GroupBy(l => l.DriverId))
        {
            var ordered = driver.OrderBy(l => l.ChangedAt).ToList();
            decimal hours = 0;
            for (var i = 0; i < ordered.Count; i++)
            {
                if (!ordered[i].IsOnline) continue;
                var from = ordered[i].ChangedAt < w.Start ? w.Start : ordered[i].ChangedAt;
                var to = i + 1 < ordered.Count ? ordered[i + 1].ChangedAt : limit;
                if (to > limit) to = limit;
                if (to > from) hours += (decimal)(to - from).TotalHours;
            }

            result[driver.Key] = hours;
        }

        return result;
    }

    private static IEnumerable<string> ParseTags(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<string[]>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string? Ts(DateTime? utc) => utc?.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    private static string? Num(decimal? value) => value?.ToString("0.####", CultureInfo.InvariantCulture);

    private static string Snake<TEnum>(TEnum value) where TEnum : struct, Enum => JsonNamingPolicy.SnakeCaseLower.ConvertName(value.ToString());
}
