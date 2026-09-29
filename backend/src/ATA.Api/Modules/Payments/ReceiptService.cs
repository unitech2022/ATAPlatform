using System.Globalization;
using System.Text.Json;
using ATA.Api.Common;
using ATA.Api.Modules.Pricing;
using ATA.Domain.Catalog;
using ATA.Domain.Common;
using ATA.Domain.Payments;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Payments;

/// <summary>
/// Trip receipts (doc 08 §F11.6). Prices include 15 % VAT (<c>vatIncluded = round(total × 15/115, 2)</c>); zero lines are dropped except
/// <c>base_fare</c>; the <c>rounding</c> line makes the lines add up to <c>total</c>.
/// </summary>
public sealed class ReceiptService(AtaDbContext db, ICurrentUser currentUser, IClock clock)
{
    public const decimal VatRate = 15m;

    /// <summary>The breakdown stored in <c>trips.fare_breakdown</c> at completion (with the min-fare adjustment the receipt needs).</summary>
    public static async Task<StoredFareBreakdown> StoredBreakdownAsync(AtaDbContext db, FareCalculation calculation, RideCategory category, decimal discountTotal, CancellationToken ct,
        IReadOnlyList<DiscountDto>? discounts = null)
    {
        var b = calculation.Breakdown;
        var raw = b.BaseFare + b.DistanceFare + b.TimeFare + b.WaitingFare;
        var minAdjustment = 0m;
        if (b.MinFareApplied)
        {
            if (calculation.PricingRuleId is { } ruleId)
            {
                var minFare = await db.PricingRules.AsNoTracking().Where(r => r.Id == ruleId).Select(r => r.MinFare).FirstOrDefaultAsync(ct);
                minAdjustment = Math.Max(0m, minFare - raw);
            }
            else
            {
                minAdjustment = Math.Max(0m, category.MinFare - (raw + b.BookingFee));
            }
        }

        return new StoredFareBreakdown(b.BaseFare, b.DistanceFare, b.TimeFare, b.WaitingFare, b.MinFareApplied, b.TimeMultiplier, b.TimeMultiplierLabel,
            b.DemandMultiplier, b.BookingFee, b.ServiceFee, discountTotal, discounts ?? [], minAdjustment, calculation.Source);
    }

    public async Task<ReceiptDto> ForPassengerAsync(Guid tripId, Language lang, CancellationToken ct)
    {
        var userId = currentUser.UserId;
        var passengerId = await db.Passengers.AsNoTracking().Where(p => p.UserId == userId).Select(p => (Guid?)p.Id).FirstOrDefaultAsync(ct) ?? throw new DomainException(ErrorCodes.Forbidden);
        var trip = await db.Trips.AsNoTracking().Include(t => t.Stops).FirstOrDefaultAsync(t => t.Id == tripId && t.PassengerId == passengerId, ct)
                   ?? throw new DomainException(ErrorCodes.NotFound);
        return await BuildAsync(trip, lang, ct);
    }

    public async Task<ReceiptDto> ForAdminAsync(Guid tripId, Language lang, CancellationToken ct)
    {
        var trip = await db.Trips.AsNoTracking().Include(t => t.Stops).FirstOrDefaultAsync(t => t.Id == tripId, ct) ?? throw new DomainException(ErrorCodes.NotFound);
        return await BuildAsync(trip, lang, ct);
    }

    private async Task<ReceiptDto> BuildAsync(Trip trip, Language lang, CancellationToken ct)
    {
        if (trip.Status != TripStatus.Completed || trip.FinalFare is not { } total)
        {
            // Cancellation fees arrive with F14; until then only completed trips have a receipt.
            throw new DomainException(ErrorCodes.Conflict, new { status = trip.Status });
        }

        var category = await db.RideCategories.AsNoTracking().FirstAsync(c => c.Id == trip.RideCategoryId, ct);
        var passengerName = await (from p in db.Passengers.AsNoTracking() join u in db.Users.AsNoTracking() on p.UserId equals u.Id where p.Id == trip.PassengerId select u.FullName).FirstOrDefaultAsync(ct);
        var driverName = trip.DriverId is { } driverId
            ? await (from d in db.Drivers.AsNoTracking() join u in db.Users.AsNoTracking() on d.UserId equals u.Id where d.Id == driverId select u.FullName).FirstOrDefaultAsync(ct)
            : null;
        var vehicle = trip.VehicleId is { } vehicleId
            ? await db.Vehicles.AsNoTracking().Where(v => v.Id == vehicleId).Select(v => v.Make + " " + v.Model + " · " + v.PlateNumber).FirstOrDefaultAsync(ct)
            : null;

        var breakdown = trip.FareBreakdown is null ? null : JsonSerializer.Deserialize<StoredFareBreakdown>(trip.FareBreakdown, JsonDefaults.Options);
        var lines = new List<ReceiptLineDto>();
        // Discount labels are rendered in the viewer's language from (source, reference).
        var discounts = (breakdown?.Discounts ?? []).Select(d => d with { Label = Promotions.DiscountEngine.Label(d.Source, d.Reference, lang) }).ToList();
        var distanceMeters = trip.FinalDistanceM ?? trip.EstimatedDistanceM;
        var durationSeconds = trip.FinalDurationS ?? trip.EstimatedDurationS;
        if (breakdown is null)
        {
            lines.Add(new ReceiptLineDto("base_fare", lang.Pick("الأجرة", "Fare"), total));
        }
        else
        {
            var km = (distanceMeters / 1000m).ToString("0.0", CultureInfo.InvariantCulture);
            var minutes = (int)Math.Round(durationSeconds / 60d);
            var core = breakdown.BaseFare + breakdown.DistanceFare + breakdown.TimeFare + breakdown.WaitingFare + breakdown.MinFareAdjustment;
            var timeLine = PricingMath.Round2(core * (breakdown.TimeMultiplier - 1m));
            var demandLine = PricingMath.Round2(core * breakdown.TimeMultiplier * (breakdown.DemandMultiplier - 1m));
            lines.Add(new("base_fare", lang.Pick("الأجرة الأساسية", "Base fare"), breakdown.BaseFare));
            lines.Add(new("distance_fare", lang.Pick($"المسافة ({km} كم)", $"Distance ({km} km)"), breakdown.DistanceFare));
            lines.Add(new("time_fare", lang.Pick($"الوقت ({minutes} د)", $"Time ({minutes} min)"), breakdown.TimeFare));
            lines.Add(new("waiting_fare", lang.Pick("الانتظار", "Waiting"), breakdown.WaitingFare));
            lines.Add(new("min_fare_adjustment", lang.Pick("فرق الحد الأدنى", "Minimum fare adjustment"), breakdown.MinFareAdjustment));
            lines.Add(new("time_multiplier", lang.Pick("تعرفة الوقت", "Time-of-day rate") + (breakdown.TimeMultiplierLabel is { } label ? $" ({label})" : string.Empty), timeLine));
            var multiplier = breakdown.DemandMultiplier.ToString("0.##", CultureInfo.InvariantCulture);
            lines.Add(new("demand_multiplier", lang.Pick($"الطلب المرتفع ×{multiplier}", $"High demand ×{multiplier}"), demandLine));
            lines.Add(new("booking_fee", lang.Pick("رسوم الحجز", "Booking fee"), breakdown.BookingFee));
            lines.Add(new("service_fee", lang.Pick("رسوم الخدمة", "Service fee"), breakdown.ServiceFee));
            if (trip.PricingMode == PricingMode.Offer && trip.OfferedPrice is { } offered)
            {
                var computed = lines.Sum(l => l.Amount) - trip.DiscountTotal;
                lines.Add(new("offer_adjustment", lang.Pick("فرق السعر المقترح", "Offered price adjustment"), PricingMath.Round2(offered - computed)));
            }
        }

        var subtotal = lines.Sum(l => l.Amount);
        foreach (var discount in discounts)
        {
            lines.Add(new("discount", discount.Label, -discount.Amount, discount.Source, discount.Reference));
        }

        lines.Add(new("cancellation_fee", lang.Pick("رسوم الإلغاء", "Cancellation fee"), 0m));
        lines.Add(new("rounding", lang.Pick("التقريب", "Rounding"), PricingMath.Round2(total - lines.Sum(l => l.Amount))));
        lines = lines.Where(l => l.Code == "base_fare" || l.Amount != 0m).ToList();

        var payment = await db.Payments.AsNoTracking().Where(p => p.TripId == trip.Id && p.Purpose == PaymentPurpose.Trip)
            .OrderByDescending(p => p.CreatedAt).FirstOrDefaultAsync(ct);
        var card = payment?.PaymentMethodId is { } methodId
            ? await db.PaymentMethods.AsNoTracking().Where(m => m.Id == methodId).Select(m => new { m.Brand, m.Last4 }).FirstOrDefaultAsync(ct)
            : null;
        var fallback = await db.TripEvents.AsNoTracking().AnyAsync(e => e.TripId == trip.Id && e.Type == TripEventTypes.PaymentFallbackCash, ct);
        var isCard = trip.PaymentMethod == PaymentMethodKind.Card;
        var paymentDto = new ReceiptPaymentDto(
            JsonNamingPolicy.SnakeCaseLower.ConvertName(trip.PaymentMethod.ToString()),
            isCard ? card?.Brand : null, isCard ? card?.Last4 : null,
            isCard && payment is not null ? JsonNamingPolicy.SnakeCaseLower.ConvertName(payment.Status.ToString()) : null,
            total, fallback);

        var refunds = await db.Refunds.AsNoTracking().Where(r => r.TripId == trip.Id && r.Status != RefundStatus.Rejected).OrderBy(r => r.CreatedAt)
            .Select(r => new ReceiptRefundDto(r.Id, r.Amount, r.Status, r.Destination, r.CreatedAt)).ToListAsync(ct);
        var refunded = refunds.Where(r => r.Status == RefundStatus.Succeeded).Sum(r => r.Amount);

        return new ReceiptDto(
            trip.Id, trip.TripNumber, JsonNamingPolicy.SnakeCaseLower.ConvertName(trip.Status.ToString()), trip.CompletedAt ?? clock.UtcNow, Payment.DefaultCurrency,
            passengerName, driverName, vehicle, lang.Pick(category.NameAr, category.NameEn),
            new ReceiptPlaceDto(trip.PickupName, trip.PickupAddress, trip.PickupLat, trip.PickupLng),
            new ReceiptPlaceDto(trip.DropoffName, trip.DropoffAddress, trip.DropoffLat, trip.DropoffLng),
            trip.Stops.OrderBy(s => s.Sequence).Select(s => new ReceiptPlaceDto(s.Name, s.Address, s.Lat, s.Lng)).ToList(),
            trip.StartedAt, trip.CompletedAt, distanceMeters, durationSeconds, trip.WaitingSeconds,
            lines, discounts, subtotal, trip.DiscountTotal, total, VatRate, PricingMath.Round2(total * VatRate / (100m + VatRate)),
            paymentDto, refunds, total - refunded);
    }
}
