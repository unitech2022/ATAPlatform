using ATA.Api.Modules.Pricing;
using ATA.Domain.Common;
using ATA.Domain.Scheduling;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Scheduling;

/// <summary>
/// Resolves the effective <see cref="ScheduledRideRule"/> (city + category ← city ← category ← global, active rows only; the in-code defaults when nothing matches)
/// and applies the booking window of doc 11 §F17.3, which replaces the F8 limits.
/// </summary>
public sealed class ScheduleRuleProvider(AtaDbContext db, ZoneResolver zones)
{
    public async Task<ScheduledRideRule> ResolveAsync(Guid? cityId, Guid? rideCategoryId, CancellationToken ct)
    {
        var rows = await db.ScheduledRideRules.AsNoTracking().Where(r => r.IsActive).ToListAsync(ct);
        return Select(rows, cityId, rideCategoryId) ?? ScheduledRideRule.Default();
    }

    /// <summary>The most specific active row for the scope: both filters (3), city only (2), category only (1), global (0); the newest row breaks a tie.</summary>
    public static ScheduledRideRule? Select(IEnumerable<ScheduledRideRule> rules, Guid? cityId, Guid? rideCategoryId) =>
        rules
            .Where(r => r.IsActive && (r.CityId == null || r.CityId == cityId) && (r.RideCategoryId == null || r.RideCategoryId == rideCategoryId))
            .OrderByDescending(r => (r.CityId != null ? 2 : 0) + (r.RideCategoryId != null ? 1 : 0))
            .ThenByDescending(r => r.UpdatedAt)
            .FirstOrDefault();

    public async Task<Guid?> CityOfAsync(decimal lat, decimal lng, DateTime at, CancellationToken ct) => (await zones.ResolveAsync(lat, lng, at, ct))?.CityId;

    public async Task<ScheduledRideRule> ResolveForPickupAsync(decimal lat, decimal lng, Guid? rideCategoryId, DateTime at, CancellationToken ct) =>
        await ResolveAsync(await CityOfAsync(lat, lng, at, ct), rideCategoryId, ct);

    public Task<ScheduledRideRule> ResolveForTripAsync(Trip trip, CancellationToken ct) =>
        ResolveForPickupAsync(trip.PickupLat, trip.PickupLng, trip.RideCategoryId, trip.RequestedAt, ct);

    /// <summary>
    /// <c>scheduledAt ≤ now + max_days_ahead × 24 h</c> measured from the booking time (else <c>422 schedule_window_exceeded { maxScheduledAt }</c>) and
    /// <c>scheduledAt ≥ now + min_lead_minutes</c> (else <c>422 schedule_lead_too_short { minScheduledAt }</c>).
    /// </summary>
    public static void EnsureWindow(ScheduledRideRule rule, DateTime scheduledAt, DateTime now)
    {
        var at = scheduledAt.ToUniversalTime();
        var max = rule.MaxScheduledAt(now);
        if (at > max)
        {
            throw new DomainException(ErrorCodes.ScheduleWindowExceeded, new { maxScheduledAt = max });
        }

        var min = rule.MinScheduledAt(now);
        if (at < min)
        {
            throw new DomainException(ErrorCodes.ScheduleLeadTooShort, new { minScheduledAt = min });
        }
    }
}
