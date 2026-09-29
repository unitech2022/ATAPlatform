using System.Text.Json;
using ATA.Api.Common;
using ATA.Api.Modules.Admin;
using ATA.Domain.Common;
using ATA.Domain.Matching;
using ATA.Domain.Trips;
using ATA.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ATA.Api.Modules.Trips.Matching;

public sealed record MatchingWeightsDto(decimal Distance, decimal Eta, decimal Rating, decimal Acceptance, decimal Cancellation, decimal Tier, decimal Favorite);

public sealed record MatchingSettingsDto(
    Guid Id, Guid? ZoneId, string? ZoneCode, Guid? RideCategoryId, string? CategoryCode, int RadiusMeters, int MaxRadiusMeters, int RadiusStepMeters,
    int OfferTimeoutSeconds, int SearchTimeoutSeconds, int MaxCandidates, MatchingWeightsDto Weights, bool AllowCategoryUpgrade, bool PreferFavoriteDriver, bool IsActive,
    DateTime CreatedAt, DateTime UpdatedAt);

public sealed record MatchingSettingsUpsertRequest(
    Guid? ZoneId, Guid? RideCategoryId, int? RadiusMeters, int? MaxRadiusMeters, int? RadiusStepMeters, int? OfferTimeoutSeconds, int? SearchTimeoutSeconds,
    int? MaxCandidates, MatchingWeightsDto? Weights, bool? AllowCategoryUpgrade, bool? PreferFavoriteDriver, bool? IsActive);

public sealed record MatchingCandidateDto(Guid DriverId, string? DriverName, decimal RatingAvg, int DistanceMeters, int EtaSeconds, decimal Score, int Rank, bool Offered, CandidateResponse? Response);

public sealed record MatchingAttemptDto(Guid Id, int Round, int RadiusMeters, int CandidatesCount, DateTime StartedAt, DateTime? FinishedAt, MatchingOutcome? Outcome, IReadOnlyList<MatchingCandidateDto> Candidates, MatchingMode Mode = MatchingMode.Normal);

/// <summary>"Why was no driver assigned": every round with its scored candidates and their answers.</summary>
public sealed record TripMatchingDto(Guid TripId, string TripNumber, TripStatus Status, DateTime RequestedAt, DateTime? AssignedAt, int? AssignmentSeconds, IReadOnlyList<MatchingAttemptDto> Attempts);

public sealed record MatchingStatsDto(
    DateOnly From, DateOnly To, int Trips, int Assigned, int NoDrivers, int Cancelled, int StillSearching, decimal NoDriversRate, decimal AssignmentRate,
    int? AverageAssignmentSeconds, int? MedianAssignmentSeconds, int OffersSent, int OffersAccepted, int OffersRejected, int OffersExpired, decimal OfferAcceptanceRate,
    int Rounds, decimal AverageRoundsPerTrip);

/// <summary>Admin CRUD for <c>matching_settings</c> (audited) plus the per-trip matching log and aggregate matching statistics.</summary>
public sealed class MatchingAdminService(AtaDbContext db, MatchingSettingsCache cache, AuditService audit)
{
    public async Task<List<MatchingSettingsDto>> ListSettingsAsync(CancellationToken ct)
    {
        var rows = await db.MatchingSettings.AsNoTracking().OrderByDescending(m => m.ZoneId != null).ThenByDescending(m => m.RideCategoryId != null).ThenBy(m => m.CreatedAt).ToListAsync(ct);
        var (codes, zoneCodes) = await CodesAsync(ct);
        return rows.Select(r => ToDto(r, codes, zoneCodes)).ToList();
    }

    public async Task<MatchingSettingsDto> GetSettingsAsync(Guid id, CancellationToken ct)
    {
        var (codes, zoneCodes) = await CodesAsync(ct);
        return ToDto(Guard.NotFound(await db.MatchingSettings.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, ct)), codes, zoneCodes);
    }

    public async Task<MatchingSettingsDto> CreateSettingsAsync(MatchingSettingsUpsertRequest request, CancellationToken ct)
    {
        Validate(request);
        await EnsureReferencesAsync(request, ct);
        if (await db.MatchingSettings.AnyAsync(m => m.ZoneId == request.ZoneId && m.RideCategoryId == request.RideCategoryId, ct))
        {
            throw new DomainException(ErrorCodes.Conflict, new { zoneId = request.ZoneId, rideCategoryId = request.RideCategoryId, message = "settings for this zone/category already exist" });
        }

        var settings = new MatchingSettings { ZoneId = request.ZoneId, RideCategoryId = request.RideCategoryId };
        Apply(settings, request);
        db.MatchingSettings.Add(settings);
        var (codes, zoneCodes) = await CodesAsync(ct);
        audit.Log("matching_settings.create", "matching_settings", settings.Id, null, ToDto(settings, codes, zoneCodes));
        await db.SaveChangesAsync(ct);
        cache.Invalidate();
        return ToDto(settings, codes, zoneCodes);
    }

    public async Task<MatchingSettingsDto> UpdateSettingsAsync(Guid id, MatchingSettingsUpsertRequest request, CancellationToken ct)
    {
        Validate(request);
        var settings = Guard.NotFound(await db.MatchingSettings.FirstOrDefaultAsync(m => m.Id == id, ct));
        var (codes, zoneCodes) = await CodesAsync(ct);
        var before = ToDto(settings, codes, zoneCodes);
        Apply(settings, request);
        audit.Log("matching_settings.update", "matching_settings", settings.Id, before, ToDto(settings, codes, zoneCodes));
        await db.SaveChangesAsync(ct);
        cache.Invalidate();
        return ToDto(settings, codes, zoneCodes);
    }

    public async Task DeleteSettingsAsync(Guid id, CancellationToken ct)
    {
        var settings = Guard.NotFound(await db.MatchingSettings.FirstOrDefaultAsync(m => m.Id == id, ct));
        db.MatchingSettings.Remove(settings);
        var (codes, zoneCodes) = await CodesAsync(ct);
        audit.Log("matching_settings.delete", "matching_settings", settings.Id, ToDto(settings, codes, zoneCodes), null);
        await db.SaveChangesAsync(ct);
        cache.Invalidate();
    }

    public async Task<TripMatchingDto> GetTripMatchingAsync(Guid tripId, CancellationToken ct)
    {
        var trip = Guard.NotFound(await db.Trips.AsNoTracking().Where(t => t.Id == tripId).Select(t => new { t.Id, t.TripNumber, t.Status, t.RequestedAt, t.AssignedAt }).FirstOrDefaultAsync(ct));
        var attempts = await db.MatchingAttempts.AsNoTracking().Include(a => a.Candidates).Where(a => a.TripId == tripId).OrderBy(a => a.Round).ToListAsync(ct);
        var driverIds = attempts.SelectMany(a => a.Candidates).Select(c => c.DriverId).Distinct().ToList();
        var drivers = await (from d in db.Drivers.AsNoTracking()
                             join u in db.Users.AsNoTracking() on d.UserId equals u.Id
                             where driverIds.Contains(d.Id)
                             select new { d.Id, u.FullName, d.RatingAvg }).ToDictionaryAsync(x => x.Id, ct);
        var dto = attempts.Select(a => new MatchingAttemptDto(
            a.Id, a.Round, a.RadiusMeters, a.CandidatesCount, a.StartedAt, a.FinishedAt, a.Outcome,
            a.Candidates.OrderBy(c => c.Rank).Select(c =>
            {
                var driver = drivers.GetValueOrDefault(c.DriverId);
                return new MatchingCandidateDto(c.DriverId, driver?.FullName, driver?.RatingAvg ?? 0m, c.DistanceM, c.EtaS, c.Score, c.Rank, c.Offered, c.Response);
            }).ToList(), a.Mode)).ToList();
        int? assignmentSeconds = trip.AssignedAt is { } assignedAt ? (int)(assignedAt - trip.RequestedAt).TotalSeconds : null;
        return new TripMatchingDto(trip.Id, trip.TripNumber, trip.Status, trip.RequestedAt, trip.AssignedAt, assignmentSeconds, dto);
    }

    public async Task<MatchingStatsDto> GetStatsAsync(DateOnly? from, DateOnly? to, DateTime now, CancellationToken ct)
    {
        var toDay = to ?? DateOnly.FromDateTime(now);
        var fromDay = from ?? toDay.AddDays(-6);
        new Validator().Rule("from", fromDay <= toDay, "must be on or before 'to'").Rule("to", toDay.DayNumber - fromDay.DayNumber <= 366, "range must be at most one year").ThrowIfInvalid();
        var fromAt = fromDay.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toAt = toDay.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        var trips = await db.Trips.AsNoTracking().Where(t => t.RequestedAt >= fromAt && t.RequestedAt < toAt)
            .Select(t => new { t.Id, t.Status, t.RequestedAt, t.AssignedAt }).ToListAsync(ct);
        var tripIds = trips.Select(t => t.Id).ToList();
        var offers = await db.TripOffers.AsNoTracking().Where(o => o.SentAt >= fromAt && o.SentAt < toAt).GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync(ct);
        var rounds = await db.MatchingAttempts.AsNoTracking().CountAsync(a => a.StartedAt >= fromAt && a.StartedAt < toAt, ct);
        var tripsWithRounds = await db.MatchingAttempts.AsNoTracking().Where(a => a.StartedAt >= fromAt && a.StartedAt < toAt).Select(a => a.TripId).Distinct().CountAsync(ct);

        var assigned = trips.Where(t => t.AssignedAt != null).ToList();
        var durations = assigned.Select(t => (int)(t.AssignedAt!.Value - t.RequestedAt).TotalSeconds).OrderBy(s => s).ToList();
        var noDrivers = trips.Count(t => t.Status == TripStatus.NoDrivers);
        var cancelledBeforeAssignment = trips.Count(t => t.Status == TripStatus.Cancelled && t.AssignedAt == null);
        var searching = trips.Count(t => t.Status is TripStatus.Requested or TripStatus.Searching);
        int Offers(OfferStatus status) => offers.FirstOrDefault(o => o.Status == status)?.Count ?? 0;
        var sent = offers.Sum(o => o.Count);
        var accepted = Offers(OfferStatus.Accepted);
        return new MatchingStatsDto(
            fromDay, toDay, trips.Count, assigned.Count, noDrivers, cancelledBeforeAssignment, searching,
            Rate(noDrivers, trips.Count), Rate(assigned.Count, trips.Count),
            durations.Count == 0 ? null : (int)Math.Round(durations.Average()),
            durations.Count == 0 ? null : durations[durations.Count / 2],
            sent, accepted, Offers(OfferStatus.Rejected), Offers(OfferStatus.Expired), Rate(accepted, sent),
            rounds, tripsWithRounds == 0 ? 0m : decimal.Round((decimal)rounds / tripsWithRounds, 2, MidpointRounding.AwayFromZero));
    }

    private static decimal Rate(int part, int total) => total == 0 ? 0m : decimal.Round((decimal)part / total, 4, MidpointRounding.AwayFromZero);

    private static void Validate(MatchingSettingsUpsertRequest request)
    {
        var v = new Validator()
            .Rule(nameof(request.RadiusMeters), request.RadiusMeters is null or (>= 500 and <= 50000), "must be between 500 and 50000")
            .Rule(nameof(request.MaxRadiusMeters), request.MaxRadiusMeters is null or (>= 500 and <= 100000), "must be between 500 and 100000")
            .Rule(nameof(request.MaxRadiusMeters), request.MaxRadiusMeters is null || request.RadiusMeters is null || request.MaxRadiusMeters >= request.RadiusMeters, "must be at least radiusMeters")
            .Rule(nameof(request.RadiusStepMeters), request.RadiusStepMeters is null or (>= 100 and <= 50000), "must be between 100 and 50000")
            .Rule(nameof(request.OfferTimeoutSeconds), request.OfferTimeoutSeconds is null or (>= 5 and <= 120), "must be between 5 and 120")
            .Rule(nameof(request.SearchTimeoutSeconds), request.SearchTimeoutSeconds is null or (>= 30 and <= 1800), "must be between 30 and 1800")
            .Rule(nameof(request.MaxCandidates), request.MaxCandidates is null or (>= 1 and <= 50), "must be between 1 and 50");
        if (request.Weights is { } w)
        {
            decimal[] all = [w.Distance, w.Eta, w.Rating, w.Acceptance, w.Cancellation, w.Tier, w.Favorite];
            v.Rule(nameof(request.Weights), all.All(x => x >= 0) && all.Sum() > 0, "weights must be non-negative and not all zero");
        }

        v.ThrowIfInvalid();
    }

    private async Task EnsureReferencesAsync(MatchingSettingsUpsertRequest request, CancellationToken ct)
    {
        var v = new Validator();
        if (request.ZoneId is { } z) v.Rule(nameof(request.ZoneId), await db.Zones.AnyAsync(x => x.Id == z, ct), "unknown zone");
        if (request.RideCategoryId is { } c) v.Rule(nameof(request.RideCategoryId), await db.RideCategories.AnyAsync(x => x.Id == c, ct), "unknown ride category");
        v.ThrowIfInvalid();
    }

    private static void Apply(MatchingSettings settings, MatchingSettingsUpsertRequest request)
    {
        if (request.RadiusMeters is not null) settings.RadiusMeters = request.RadiusMeters.Value;
        if (request.MaxRadiusMeters is not null) settings.MaxRadiusMeters = request.MaxRadiusMeters.Value;
        if (request.RadiusStepMeters is not null) settings.RadiusStepMeters = request.RadiusStepMeters.Value;
        if (request.OfferTimeoutSeconds is not null) settings.OfferTimeoutSeconds = request.OfferTimeoutSeconds.Value;
        if (request.SearchTimeoutSeconds is not null) settings.SearchTimeoutSeconds = request.SearchTimeoutSeconds.Value;
        if (request.MaxCandidates is not null) settings.MaxCandidates = request.MaxCandidates.Value;
        if (request.Weights is { } w) settings.Weights = new MatchingWeights(w.Distance, w.Eta, w.Rating, w.Acceptance, w.Cancellation, w.Tier, w.Favorite).ToJson();
        if (request.AllowCategoryUpgrade is not null) settings.AllowCategoryUpgrade = request.AllowCategoryUpgrade.Value;
        if (request.PreferFavoriteDriver is not null) settings.PreferFavoriteDriver = request.PreferFavoriteDriver.Value;
        if (request.IsActive is not null) settings.IsActive = request.IsActive.Value;
        settings.MaxRadiusMeters = Math.Max(settings.MaxRadiusMeters, settings.RadiusMeters);
    }

    private static MatchingSettingsDto ToDto(MatchingSettings m, Dictionary<Guid, string> codes, Dictionary<Guid, string> zoneCodes)
    {
        var w = m.ParseWeights();
        return new MatchingSettingsDto(
            m.Id, m.ZoneId, m.ZoneId is { } z ? zoneCodes.GetValueOrDefault(z) : null, m.RideCategoryId, m.RideCategoryId is { } c ? codes.GetValueOrDefault(c) : null,
            m.RadiusMeters, m.MaxRadiusMeters, m.RadiusStepMeters, m.OfferTimeoutSeconds, m.SearchTimeoutSeconds, m.MaxCandidates,
            new MatchingWeightsDto(w.Distance, w.Eta, w.Rating, w.Acceptance, w.Cancellation, w.Tier, w.Favorite),
            m.AllowCategoryUpgrade, m.PreferFavoriteDriver, m.IsActive, m.CreatedAt, m.UpdatedAt);
    }

    private async Task<(Dictionary<Guid, string> Categories, Dictionary<Guid, string> Zones)> CodesAsync(CancellationToken ct) =>
        (await db.RideCategories.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.Code, ct), await db.Zones.AsNoTracking().ToDictionaryAsync(z => z.Id, z => z.Code, ct));
}
