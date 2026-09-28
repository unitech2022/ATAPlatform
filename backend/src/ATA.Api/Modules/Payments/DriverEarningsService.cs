using ATA.Api.Common;
using ATA.Domain.Common;
using ATA.Domain.Payments;
using ATA.Domain.Trips;
using ATA.Domain.Wallet;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Payments;

/// <summary>Driver earnings statement (local Riyadh dates, at most 92 days) and per-trip earnings.</summary>
public sealed class DriverEarningsService(AtaDbContext db, ICurrentUser currentUser, IClock clock)
{
    public const int MaxDays = 92;

    public async Task<EarningsStatementDto> StatementAsync(DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(from), from)
            .Require(nameof(to), to)
            .Rule(nameof(to), from is null || to is null || to >= from, "must not be before from")
            .Rule(nameof(to), from is null || to is null || to.Value.DayNumber - from.Value.DayNumber < MaxDays, $"at most {MaxDays} days")
            .ThrowIfInvalid();
        var driver = await LoadDriverAsync(ct);
        var start = Formats.RiyadhMidnightUtc(from!.Value);
        var end = Formats.RiyadhMidnightUtc(to!.Value.AddDays(1));
        var walletId = await db.Wallets.AsNoTracking().Where(w => w.UserId == driver.UserId && w.Kind == WalletKind.Driver).Select(w => (Guid?)w.Id).FirstOrDefaultAsync(ct);
        var movements = walletId is null
            ? []
            : await db.WalletTransactions.AsNoTracking().Where(t => t.WalletId == walletId && t.CreatedAt >= start && t.CreatedAt < end)
                .Select(t => new { t.Type, t.Direction, t.Amount, t.CreatedAt }).ToListAsync(ct);
        var trips = await db.Trips.AsNoTracking()
            .Where(t => t.DriverId == driver.Id && t.Status == TripStatus.Completed && t.CompletedAt >= start && t.CompletedAt < end)
            .Select(t => new { CompletedAt = t.CompletedAt!.Value, Gross = (t.FinalFare ?? 0m) + t.DiscountTotal }).ToListAsync(ct);
        var logs = await db.DriverStatusLogs.AsNoTracking().Where(l => l.DriverId == driver.Id && l.ChangedAt < end).OrderBy(l => l.ChangedAt)
            .Select(l => new { l.IsOnline, l.ChangedAt }).ToListAsync(ct);

        var earnings = movements.Where(m => m.Type == TransactionType.TripEarning).Sum(m => m.Amount);
        var cash = movements.Where(m => m.Type == TransactionType.CashCollection).Sum(m => m.Amount);
        var incentives = movements.Where(m => m.Type == TransactionType.Incentive).Sum(m => m.Amount);
        var compensation = movements.Where(m => m.Type == TransactionType.CancellationCompensation).Sum(m => m.Amount);
        var adjustments = movements.Where(m => m.Type == TransactionType.Adjustment).Sum(m => m.Direction == TransactionDirection.Credit ? m.Amount : -m.Amount);
        var payouts = movements.Where(m => m.Type == TransactionType.Payout).Sum(m => m.Amount) - movements.Where(m => m.Type == TransactionType.PayoutReversal).Sum(m => m.Amount);
        var gross = trips.Sum(t => t.Gross);
        var totals = new EarningsTotalsDto(trips.Count, gross, gross - earnings, earnings, cash, incentives, compensation, adjustments, payouts,
            earnings + incentives + compensation + adjustments - cash);

        var days = new List<EarningsDayDto>();
        for (var day = to.Value; day >= from.Value; day = day.AddDays(-1))
        {
            var dayStart = Formats.RiyadhMidnightUtc(day);
            var dayEnd = dayStart.AddDays(1) < clock.UtcNow ? dayStart.AddDays(1) : clock.UtcNow;
            if (dayEnd <= dayStart) continue;
            var dayMoves = movements.Where(m => m.CreatedAt >= dayStart && m.CreatedAt < dayEnd).ToList();
            var dayTrips = trips.Count(t => t.CompletedAt >= dayStart && t.CompletedAt < dayEnd);
            var online = OnlineHours(logs.Select(l => (l.IsOnline, l.ChangedAt)).ToList(), dayStart, dayEnd);
            if (dayTrips == 0 && dayMoves.Count == 0 && online == 0) continue;
            days.Add(new EarningsDayDto(day, dayTrips,
                dayMoves.Where(m => m.Type == TransactionType.TripEarning).Sum(m => m.Amount),
                dayMoves.Where(m => m.Type == TransactionType.CashCollection).Sum(m => m.Amount),
                dayMoves.Where(m => m.Type == TransactionType.Incentive).Sum(m => m.Amount),
                online));
        }

        return new EarningsStatementDto(from.Value, to.Value, totals, days);
    }

    public async Task<TripEarningsDto> TripAsync(Guid tripId, CancellationToken ct)
    {
        var driver = await LoadDriverAsync(ct);
        var trip = await db.Trips.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tripId && t.DriverId == driver.Id, ct) ?? throw new DomainException(ErrorCodes.NotFound);
        var fare = trip.FinalFare ?? trip.EstimatedFare;
        var gross = fare + trip.DiscountTotal;
        var earnings = trip.DriverEarnings ?? 0m;
        var commission = gross - earnings;
        return new TripEarningsDto(trip.Id, fare, trip.DiscountTotal, gross, commission, gross == 0 ? 0 : decimal.Round(commission / gross * 100m, 1), 0m, earnings,
            System.Text.Json.JsonNamingPolicy.SnakeCaseLower.ConvertName(trip.PaymentMethod.ToString()),
            trip.PaymentMethod == PaymentMethodKind.Cash && trip.Status == TripStatus.Completed ? fare : 0m);
    }

    private static double OnlineHours(IReadOnlyList<(bool IsOnline, DateTime ChangedAt)> logs, DateTime start, DateTime end)
    {
        var online = logs.LastOrDefault(l => l.ChangedAt < start).IsOnline;
        var cursor = start;
        var total = TimeSpan.Zero;
        foreach (var log in logs.Where(l => l.ChangedAt >= start && l.ChangedAt < end))
        {
            if (online) total += log.ChangedAt - cursor;
            online = log.IsOnline;
            cursor = log.ChangedAt;
        }

        if (online) total += end - cursor;
        return Math.Round(total.TotalHours, 2);
    }

    private async Task<Domain.Drivers.DriverProfile> LoadDriverAsync(CancellationToken ct)
    {
        var userId = currentUser.UserId;
        return await db.Drivers.AsNoTracking().FirstOrDefaultAsync(d => d.UserId == userId, ct) ?? throw new DomainException(ErrorCodes.Forbidden);
    }
}
