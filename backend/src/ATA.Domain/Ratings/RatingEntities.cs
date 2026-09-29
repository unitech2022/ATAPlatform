using ATA.Domain.Common;

namespace ATA.Domain.Ratings;

/// <summary>Who rates / is rated (the role in the trip).</summary>
public enum RatingRole { Passenger, Driver }

public enum RatingStatus { Visible, Hidden }

public enum RatingFlagType { LowRating, LowAverage, AbusiveComment }

public enum RatingFlagStatus { Open, Dismissed, Actioned }

public enum RatingFlagAction { Warned, SuspensionReview, None }

/// <summary>
/// Row of <c>ratings</c> (doc 10 §F15.1): one per (trip, rater role). <see cref="Tags"/> is a JSON array of <c>rating_tags.code</c>.
/// <see cref="CommentHidden"/> hides an abusive comment while the stars stay visible.
/// </summary>
public class Rating : AuditableEntity
{
    public Guid TripId { get; set; }
    public Guid RaterUserId { get; set; }
    public RatingRole RaterRole { get; set; }
    public Guid RateeUserId { get; set; }
    public RatingRole RateeRole { get; set; }
    public byte Stars { get; set; }
    public string Tags { get; set; } = "[]";
    public string? Comment { get; set; }
    public bool CommentHidden { get; set; }
    public RatingStatus Status { get; set; } = RatingStatus.Visible;
    public Guid? HiddenBy { get; set; }
    public string? HiddenReason { get; set; }
    public DateTime? HiddenAt { get; set; }
}

/// <summary>Row of <c>rating_tags</c>; <see cref="TargetRole"/> is the role being rated. Codes are unique per target role.</summary>
public class RatingTag : Entity
{
    public required string Code { get; set; }
    public RatingRole TargetRole { get; set; }
    public required string NameAr { get; set; }
    public required string NameEn { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Row of <c>rating_flags</c>: something ops should look at (a low rating, a low driver average, an abusive comment).</summary>
public class RatingFlag : Entity
{
    public Guid UserId { get; set; }
    public RatingRole Role { get; set; }
    public RatingFlagType Type { get; set; }
    public Guid? RatingId { get; set; }
    public decimal? Value { get; set; }
    public RatingFlagStatus Status { get; set; } = RatingFlagStatus.Open;
    public RatingFlagAction? Action { get; set; }
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? Note { get; set; }
}

/// <summary>The rolling weighted average of doc 10 §F15.2.</summary>
public static class RatingMath
{
    public const decimal DefaultAverage = 5.00m;

    /// <summary>
    /// <c>w_i = 1 − (i / N) × (1 − minWeight)</c> with <c>i</c> the 0-based position (newest first) and <c>N</c> the number of ratings in the window;
    /// <c>avg = round(Σ w_i × stars_i / Σ w_i, 2)</c>; no ratings → 5.00.
    /// </summary>
    public static decimal WeightedAverage(IReadOnlyList<byte> newestFirst, decimal minWeight)
    {
        var n = newestFirst.Count;
        if (n == 0)
        {
            return DefaultAverage;
        }

        decimal weighted = 0m, weights = 0m;
        for (var i = 0; i < n; i++)
        {
            var w = 1m - (decimal)i / n * (1m - minWeight);
            weighted += w * newestFirst[i];
            weights += w;
        }

        return decimal.Round(weighted / weights, 2, MidpointRounding.AwayFromZero);
    }
}
