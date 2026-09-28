using System.Globalization;
using System.Text;
using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Api.Modules.Notifications;
using ATA.Domain.Common;
using ATA.Domain.Notifications;
using ATA.Domain.Payments;
using ATA.Domain.Trips;
using ATA.Domain.Wallet;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ATA.Api.Modules.Payments;

/// <summary>
/// Settlement statements (doc 08 §F11.4) for a half-open period <c>[start, end)</c>. A statement moves no money (netting happens in the wallet
/// in real time): per driver <c>net = earnings + incentives + compensation + adjustments − cash_collected</c> and
/// <c>closing = opening + net + topups − fees − payouts</c>, where opening/closing are the wallet balance at <c>start</c>/<c>end</c>.
/// </summary>
public sealed class SettlementService(
    AtaDbContext db,
    PayoutService payouts,
    INotificationDispatcher notifications,
    AuditService audit,
    ICurrentUser currentUser,
    IClock clock,
    IOptions<SettlementsOptions> options)
{
    public const string EntityType = "settlement_batch";
    private readonly SettlementsOptions _options = options.Value;

    public async Task<SettlementBatchDto> GenerateAsync(GenerateSettlementRequest request, CancellationToken ct)
    {
        new Validator()
            .Require(nameof(request.PeriodStart), request.PeriodStart)
            .Require(nameof(request.PeriodEnd), request.PeriodEnd)
            .Rule(nameof(request.PeriodEnd), request.PeriodStart is null || request.PeriodEnd is null || request.PeriodEnd > request.PeriodStart, "must be after periodStart")
            .Rule(nameof(request.PeriodEnd), request.PeriodEnd is null || request.PeriodEnd.Value.ToUniversalTime() <= clock.UtcNow.AddDays(1), "must not be in the future")
            .ThrowIfInvalid();
        var start = request.PeriodStart!.Value.ToUniversalTime();
        var end = request.PeriodEnd!.Value.ToUniversalTime();
        if (request.CityId is { } cityId && !await db.Cities.AnyAsync(c => c.Id == cityId, ct))
        {
            throw new DomainException(ErrorCodes.ValidationFailed, new Dictionary<string, string> { ["cityId"] = "unknown city" });
        }

        var overlap = await db.SettlementBatches.AnyAsync(b => b.Status != SettlementBatchStatus.Failed
            && (b.CityId == null || request.CityId == null || b.CityId == request.CityId)
            && b.PeriodStart < end && b.PeriodEnd > start, ct);
        if (overlap)
        {
            throw new DomainException(ErrorCodes.SettlementPeriodOverlap);
        }

        var local = Formats.RiyadhDate(start);
        var number = $"SB-{local:yyyyMMdd}";
        var taken = await db.SettlementBatches.CountAsync(b => b.BatchNumber.StartsWith(number), ct);
        var batch = new SettlementBatch
        {
            BatchNumber = taken == 0 ? number : $"{number}-{taken + 1}",
            CityId = request.CityId,
            PeriodStart = start,
            PeriodEnd = end,
            GeneratedBy = currentUser.IsAuthenticated ? currentUser.UserId : null,
            GeneratedAt = clock.UtcNow,
        };
        db.SettlementBatches.Add(batch);
        await db.InTransactionAsync(async () =>
        {
            await ComputeAsync(batch, ct);
            audit.Log("settlement_batch.generate", EntityType, batch.Id, null, new { batch.BatchNumber, batch.PeriodStart, batch.PeriodEnd, batch.CityId, batch.DriversCount });
            await db.SaveChangesAsync(ct);
        }, ct);
        return await ToDtoAsync(batch, ct);
    }

    public async Task<SettlementBatchDto> RegenerateAsync(Guid id, CancellationToken ct)
    {
        var batch = Guard.NotFound(await db.SettlementBatches.FirstOrDefaultAsync(b => b.Id == id, ct));
        EnsureOpen(batch);
        await db.InTransactionAsync(async () =>
        {
            await db.Settlements.Where(s => s.BatchId == id).ExecuteDeleteAsync(ct);
            batch.GeneratedAt = clock.UtcNow;
            batch.GeneratedBy = currentUser.IsAuthenticated ? currentUser.UserId : batch.GeneratedBy;
            await ComputeAsync(batch, ct);
            audit.Log("settlement_batch.generate", EntityType, batch.Id, null, new { batch.BatchNumber, regenerated = true, batch.DriversCount });
            await db.SaveChangesAsync(ct);
        }, ct);
        return await ToDtoAsync(batch, ct);
    }

    public async Task<SettlementBatchDto> FinalizeAsync(Guid id, CancellationToken ct)
    {
        var batch = Guard.NotFound(await db.SettlementBatches.FirstOrDefaultAsync(b => b.Id == id, ct));
        EnsureOpen(batch);
        var rows = await db.Settlements.Where(s => s.BatchId == id).ToListAsync(ct);
        var driverIds = rows.Select(r => r.DriverId).ToList();
        var drivers = await db.Drivers.Where(d => driverIds.Contains(d.Id)).ToDictionaryAsync(d => d.Id, ct);
        var userIds = drivers.Values.Select(d => d.UserId).ToList();
        var names = await db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName ?? u.PhoneNumber, ct);
        var label = $"{Formats.RiyadhDate(batch.PeriodStart):dd/MM} – {Formats.RiyadhDate(batch.PeriodEnd.AddTicks(-1)):dd/MM}";
        await db.InTransactionAsync(async () =>
        {
            batch.Status = SettlementBatchStatus.Finalized;
            batch.FinalizedAt = clock.UtcNow;
            batch.FinalizedBy = currentUser.IsAuthenticated ? currentUser.UserId : null;
            foreach (var row in rows)
            {
                row.Status = SettlementStatus.Finalized;
                var driver = drivers[row.DriverId];
                if (_options.AutoCreatePayouts && row.Direction == SettlementDirection.PayableToDriver)
                {
                    var payout = await payouts.CreateApprovedAsync(driver, names.GetValueOrDefault(driver.UserId) ?? string.Empty, row.ClosingBalance, $"settlement:{row.Id}", ct);
                    row.PayoutId = payout?.Id;
                }

                await notifications.DispatchAsync(new NotificationRequest(NotificationTypes.SettlementReady, driver.UserId,
                    NotificationPlaceholders.Of(("periodLabel", label)).Money("netAmount", row.NetAmount), "settlement", row.Id), ct);
            }

            audit.Log("settlement_batch.finalize", EntityType, batch.Id, null, new { batch.BatchNumber, drivers = rows.Count });
            await db.SaveChangesAsync(ct);
        }, ct);
        return await ToDtoAsync(batch, ct);
    }

    /// <summary><c>SettlementWeeklyJob</c>: generates the week that just ended (Sunday 00:00 Riyadh) for every city when not generated yet.</summary>
    public async Task<int> GenerateWeeklyAsync(CancellationToken ct)
    {
        var today = Formats.RiyadhDate(clock.UtcNow);
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var end = Formats.RiyadhMidnightUtc(weekStart);
        var start = end.AddDays(-Math.Max(1, _options.PeriodDays));
        var cities = await db.Cities.AsNoTracking().Where(c => c.IsActive).Select(c => c.Id).ToListAsync(ct);
        var created = 0;
        foreach (var cityId in cities)
        {
            var exists = await db.SettlementBatches.AnyAsync(b => (b.CityId == cityId || b.CityId == null) && b.PeriodStart < end && b.PeriodEnd > start && b.Status != SettlementBatchStatus.Failed, ct);
            if (exists) continue;
            await GenerateAsync(new GenerateSettlementRequest(start, end, cityId), ct);
            created++;
        }

        return created;
    }

    private async Task ComputeAsync(SettlementBatch batch, CancellationToken ct)
    {
        var start = batch.PeriodStart;
        var end = batch.PeriodEnd;
        var driverWallets = from w in db.Wallets.AsNoTracking()
                            join d in db.Drivers.AsNoTracking() on w.UserId equals d.UserId
                            where w.Kind == WalletKind.Driver && (batch.CityId == null || d.CityId == batch.CityId)
                            select new { WalletId = w.Id, w.Balance, DriverId = d.Id };
        var wallets = await driverWallets.ToListAsync(ct);
        var walletIds = wallets.Select(w => w.WalletId).ToList();
        var movements = await db.WalletTransactions.AsNoTracking()
            .Where(t => walletIds.Contains(t.WalletId) && t.CreatedAt >= start)
            .Select(t => new { t.WalletId, t.Type, t.Direction, t.Amount, t.CreatedAt })
            .ToListAsync(ct);
        var driverIds = wallets.Select(w => w.DriverId).ToList();
        var trips = await db.Trips.AsNoTracking()
            .Where(t => t.DriverId != null && driverIds.Contains(t.DriverId.Value) && t.Status == TripStatus.Completed && t.CompletedAt >= start && t.CompletedAt < end)
            .Select(t => new { DriverId = t.DriverId!.Value, Gross = (t.FinalFare ?? 0m) + t.DiscountTotal })
            .ToListAsync(ct);

        var totals = new SettlementBatch { BatchNumber = batch.BatchNumber };
        var count = 0;
        foreach (var wallet in wallets)
        {
            var mine = movements.Where(m => m.WalletId == wallet.WalletId).ToList();
            var inPeriod = mine.Where(m => m.CreatedAt < end).ToList();
            var driverTrips = trips.Where(t => t.DriverId == wallet.DriverId).ToList();
            if (inPeriod.Count == 0 && driverTrips.Count == 0)
            {
                continue;
            }

            decimal Signed(TransactionDirection direction, decimal amount) => direction == TransactionDirection.Credit ? amount : -amount;
            decimal Sum(TransactionType type) => inPeriod.Where(m => m.Type == type).Sum(m => m.Amount);
            var closing = wallet.Balance - mine.Where(m => m.CreatedAt >= end).Sum(m => Signed(m.Direction, m.Amount));
            var opening = wallet.Balance - mine.Sum(m => Signed(m.Direction, m.Amount));
            var earnings = Sum(TransactionType.TripEarning);
            var gross = driverTrips.Sum(t => t.Gross);
            var cash = Sum(TransactionType.CashCollection);
            var incentives = Sum(TransactionType.Incentive);
            var compensation = Sum(TransactionType.CancellationCompensation);
            var adjustments = inPeriod.Where(m => m.Type == TransactionType.Adjustment).Sum(m => Signed(m.Direction, m.Amount));
            var fees = Sum(TransactionType.CancellationFee);
            var topups = Sum(TransactionType.Topup);
            var payoutsInPeriod = Sum(TransactionType.Payout) - Sum(TransactionType.PayoutReversal);
            // Other movements (refunds, trip payments on a driver wallet) are folded into adjustments so the invariant always holds.
            var other = inPeriod.Where(m => m.Type is TransactionType.Refund or TransactionType.TripPayment).Sum(m => Signed(m.Direction, m.Amount));
            adjustments += other;
            var net = earnings + incentives + compensation + adjustments - cash;
            var row = new Settlement
            {
                BatchId = batch.Id,
                DriverId = wallet.DriverId,
                TripsCount = driverTrips.Count,
                GrossFares = gross,
                Earnings = earnings,
                Commission = gross - earnings,
                CashCollected = cash,
                Incentives = incentives,
                CancellationCompensation = compensation,
                Adjustments = adjustments,
                Fees = fees,
                Topups = topups,
                PayoutsInPeriod = payoutsInPeriod,
                NetAmount = net,
                OpeningBalance = opening,
                ClosingBalance = closing,
                Direction = closing > 0 ? SettlementDirection.PayableToDriver : closing < 0 ? SettlementDirection.DueFromDriver : SettlementDirection.Zero,
            };
            db.Settlements.Add(row);
            count++;
            totals.TotalTrips += row.TripsCount;
            totals.TotalGrossFares += row.GrossFares;
            totals.TotalEarnings += row.Earnings;
            totals.TotalCommission += row.Commission;
            totals.TotalCashCollected += row.CashCollected;
            totals.TotalIncentives += row.Incentives;
            totals.TotalCompensation += row.CancellationCompensation;
            totals.TotalAdjustments += row.Adjustments;
            totals.TotalNet += row.NetAmount;
        }

        batch.DriversCount = count;
        batch.TotalTrips = totals.TotalTrips;
        batch.TotalGrossFares = totals.TotalGrossFares;
        batch.TotalEarnings = totals.TotalEarnings;
        batch.TotalCommission = totals.TotalCommission;
        batch.TotalCashCollected = totals.TotalCashCollected;
        batch.TotalIncentives = totals.TotalIncentives;
        batch.TotalCompensation = totals.TotalCompensation;
        batch.TotalAdjustments = totals.TotalAdjustments;
        batch.TotalNet = totals.TotalNet;
        batch.Status = SettlementBatchStatus.Ready;
        batch.Error = null;
    }

    public async Task<PagedResult<SettlementBatchDto>> ListBatchesAsync(Paging paging, CancellationToken ct)
    {
        var total = await db.SettlementBatches.CountAsync(ct);
        var rows = await db.SettlementBatches.AsNoTracking().OrderByDescending(b => b.PeriodStart).ThenByDescending(b => b.CreatedAt).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        return paging.Result(await ToDtosAsync(rows, ct), total);
    }

    public async Task<SettlementBatchDto> GetBatchAsync(Guid id, CancellationToken ct) =>
        await ToDtoAsync(Guard.NotFound(await db.SettlementBatches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, ct)), ct);

    public async Task<PagedResult<SettlementDto>> ListSettlementsAsync(Guid batchId, SettlementDirection? direction, string? search, Paging paging, CancellationToken ct)
    {
        var query = Rows().Where(x => x.Settlement.BatchId == batchId);
        if (direction is not null) query = query.Where(x => x.Settlement.Direction == direction);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var phone = PhoneNumber.TryNormalize(term, out var normalized) ? normalized : term;
            query = query.Where(x => x.Phone.Contains(phone) || (x.Name != null && x.Name.Contains(term)));
        }

        var total = await query.CountAsync(ct);
        var rows = await query.OrderBy(x => x.Name).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        return paging.Result(rows.Select(ToDto).ToList(), total);
    }

    public async Task<SettlementDto> GetSettlementAsync(Guid id, Guid? driverId, CancellationToken ct)
    {
        var row = await Rows().Where(x => x.Settlement.Id == id && (driverId == null || x.Settlement.DriverId == driverId)).FirstOrDefaultAsync(ct)
                  ?? throw new DomainException(ErrorCodes.NotFound);
        return ToDto(row);
    }

    public async Task<PagedResult<DriverSettlementItemDto>> ListForDriverAsync(Guid driverId, Paging paging, CancellationToken ct)
    {
        var query = Rows().Where(x => x.Settlement.DriverId == driverId);
        var total = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.Batch.PeriodStart).Skip(paging.Skip).Take(paging.PageSize).ToListAsync(ct);
        return paging.Result(rows.Select(x => new DriverSettlementItemDto(x.Settlement.Id, x.Batch.BatchNumber, x.Batch.PeriodStart, x.Batch.PeriodEnd,
            x.Settlement.TripsCount, x.Settlement.Earnings, x.Settlement.CashCollected, x.Settlement.NetAmount, x.Settlement.ClosingBalance,
            x.Settlement.Direction, x.Settlement.Status)).ToList(), total);
    }

    /// <summary>UTF-8 CSV with BOM (for Excel) of every statement in the batch.</summary>
    public async Task<CsvFile> ExportAsync(Guid batchId, CancellationToken ct)
    {
        var batch = Guard.NotFound(await db.SettlementBatches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == batchId, ct));
        var rows = await Rows().Where(x => x.Settlement.BatchId == batchId).OrderBy(x => x.Name).ToListAsync(ct);
        var csv = new StringBuilder("driver_name,phone,trips,gross_fares,earnings,commission,cash_collected,incentives,compensation,adjustments,fees,topups,payouts,net,opening_balance,closing_balance,direction\n");
        foreach (var x in rows)
        {
            var s = x.Settlement;
            csv.Append(Csv.Row(x.Name, x.Phone, s.TripsCount.ToString(CultureInfo.InvariantCulture), M(s.GrossFares), M(s.Earnings), M(s.Commission), M(s.CashCollected),
                M(s.Incentives), M(s.CancellationCompensation), M(s.Adjustments), M(s.Fees), M(s.Topups), M(s.PayoutsInPeriod), M(s.NetAmount), M(s.OpeningBalance),
                M(s.ClosingBalance), System.Text.Json.JsonNamingPolicy.SnakeCaseLower.ConvertName(s.Direction.ToString())));
        }

        return new CsvFile($"{batch.BatchNumber}.csv", Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray());

        static string M(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
    }

    private sealed class Row
    {
        public required Settlement Settlement { get; init; }
        public required SettlementBatch Batch { get; init; }
        public string? Name { get; init; }
        public required string Phone { get; init; }
    }

    private IQueryable<Row> Rows() =>
        from s in db.Settlements.AsNoTracking()
        join b in db.SettlementBatches.AsNoTracking() on s.BatchId equals b.Id
        join d in db.Drivers.AsNoTracking() on s.DriverId equals d.Id
        join u in db.Users.AsNoTracking() on d.UserId equals u.Id
        select new Row { Settlement = s, Batch = b, Name = u.FullName, Phone = u.PhoneNumber };

    private static SettlementDto ToDto(Row x)
    {
        var s = x.Settlement;
        return new SettlementDto(s.Id, s.BatchId, x.Batch.BatchNumber, x.Batch.PeriodStart, x.Batch.PeriodEnd, s.DriverId, x.Name, x.Phone, s.TripsCount, s.GrossFares,
            s.Earnings, s.Commission, s.CashCollected, s.Incentives, s.CancellationCompensation, s.Adjustments, s.Fees, s.Topups, s.PayoutsInPeriod, s.NetAmount,
            s.OpeningBalance, s.ClosingBalance, s.Direction, s.PayoutId, s.Status, s.CreatedAt);
    }

    private async Task<SettlementBatchDto> ToDtoAsync(SettlementBatch b, CancellationToken ct) => (await ToDtosAsync([b], ct))[0];

    private async Task<IReadOnlyList<SettlementBatchDto>> ToDtosAsync(IReadOnlyList<SettlementBatch> batches, CancellationToken ct)
    {
        var userIds = batches.SelectMany(b => new[] { b.GeneratedBy, b.FinalizedBy }).Where(i => i != null).Select(i => i!.Value).Distinct().ToList();
        var cityIds = batches.Where(b => b.CityId != null).Select(b => b.CityId!.Value).Distinct().ToList();
        var names = await db.Users.AsNoTracking().Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName, ct);
        var cities = await db.Cities.AsNoTracking().Where(c => cityIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.NameAr, ct);
        string? Name(Guid? id) => id is { } value ? names.GetValueOrDefault(value) : null;
        return batches.Select(b => new SettlementBatchDto(b.Id, b.BatchNumber, b.CityId, b.CityId is { } cityId ? cities.GetValueOrDefault(cityId) : null,
            b.PeriodStart, b.PeriodEnd, b.Status, b.DriversCount, b.TotalTrips, b.TotalGrossFares, b.TotalEarnings, b.TotalCommission, b.TotalCashCollected,
            b.TotalIncentives, b.TotalCompensation, b.TotalAdjustments, b.TotalNet, b.Error, Name(b.GeneratedBy), b.GeneratedAt, Name(b.FinalizedBy), b.FinalizedAt,
            b.CreatedAt)).ToList();
    }

    private static void EnsureOpen(SettlementBatch batch)
    {
        if (batch.Status == SettlementBatchStatus.Finalized)
        {
            throw new DomainException(ErrorCodes.Conflict, new { status = batch.Status });
        }
    }
}
