using System.Text.Json;
using ATA.Domain.Common;

namespace ATA.Domain.Matching;

/// <summary>Matching engine parameters (<c>matching_settings</c>): resolved zone+category → zone → category → global.</summary>
public class MatchingSettings : AuditableEntity
{
    public Guid? ZoneId { get; set; }
    public Guid? RideCategoryId { get; set; }
    public int RadiusMeters { get; set; } = 5000;
    public int MaxRadiusMeters { get; set; } = 12000;
    public int RadiusStepMeters { get; set; } = 2500;
    public int OfferTimeoutSeconds { get; set; } = 20;
    public int SearchTimeoutSeconds { get; set; } = 120;
    public int MaxCandidates { get; set; } = 8;
    public string Weights { get; set; } = MatchingWeights.Default.ToJson();
    public bool AllowCategoryUpgrade { get; set; }
    public bool PreferFavoriteDriver { get; set; } = true;
    public bool IsActive { get; set; } = true;

    public MatchingWeights ParseWeights() => MatchingWeights.Parse(Weights);
}

/// <summary>Score weights; normalised to sum 1 when applied.</summary>
public sealed record MatchingWeights(decimal Distance, decimal Eta, decimal Rating, decimal Acceptance, decimal Cancellation, decimal Tier, decimal Favorite)
{
    public static readonly MatchingWeights Default = new(0.35m, 0.20m, 0.15m, 0.10m, 0.10m, 0.05m, 0.05m);

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public decimal Sum => Distance + Eta + Rating + Acceptance + Cancellation + Tier + Favorite;

    public string ToJson() => JsonSerializer.Serialize(this, Json);

    public static MatchingWeights Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Default;
        }

        var parsed = JsonSerializer.Deserialize<MatchingWeights>(json, Json);
        return parsed is null || parsed.Sum <= 0 ? Default : parsed;
    }
}

public enum MatchingOutcome { Assigned, Exhausted, Timeout, Cancelled }

public enum CandidateResponse { Accepted, Rejected, Expired }

/// <summary>F16 <c>matching_attempts.mode</c>: the exclusive favourite-driver round (round 0) or a normal round.</summary>
public enum MatchingMode { Normal, Favorite }

/// <summary>One search round for a trip (<c>matching_attempts</c>).</summary>
public class MatchingAttempt
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid TripId { get; set; }
    public int Round { get; set; }
    public int RadiusMeters { get; set; }
    public int CandidatesCount { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    public MatchingOutcome? Outcome { get; set; }
    public MatchingMode Mode { get; set; } = MatchingMode.Normal;

    public ICollection<MatchingCandidate> Candidates { get; set; } = [];

    public bool IsOpen => FinishedAt is null;

    public void Finish(MatchingOutcome outcome, DateTime now)
    {
        Outcome = outcome;
        FinishedAt = now;
    }
}

/// <summary>A scored driver in one round (<c>matching_candidates</c>).</summary>
public class MatchingCandidate
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid AttemptId { get; set; }
    public Guid DriverId { get; set; }
    public int DistanceM { get; set; }
    public int EtaS { get; set; }
    public decimal Score { get; set; }
    public int Rank { get; set; }
    public bool Offered { get; set; }
    public CandidateResponse? Response { get; set; }
}
