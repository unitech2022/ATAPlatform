using ATA.Domain.Ratings;

namespace ATA.Api.Modules.Ratings;

public sealed class RatingsOptions
{
    public const string Section = "Ratings";
    /// <summary>Hours after <c>completed_at</c> during which both parties may rate.</summary>
    public int WindowHours { get; set; } = 72;
    /// <summary>Most recent visible ratings in the weighted average.</summary>
    public int WindowSize { get; set; } = 500;
    /// <summary>Weight of the oldest rating in the window (the newest weighs 1).</summary>
    public decimal MinWeight { get; set; } = 0.5m;
    /// <summary>Stars at or below this raise a <c>low_rating</c> flag.</summary>
    public int LowRatingThreshold { get; set; } = 2;
    public decimal DriverMinAverage { get; set; } = 4.30m;
    public int MinCountForAverageFlag { get; set; } = 50;
    public int ReminderAfterMinutes { get; set; } = 30;
    /// <summary>A comment containing one of these (case-insensitive) is hidden and flagged <c>abusive_comment</c>.</summary>
    public string[] AbusiveWords { get; set; } = [];
    /// <summary>Runs <c>RatingReminderJob</c> (every 5 min) and <c>LowAverageFlagJob</c> (daily) — <c>false</c> in tests.</summary>
    public bool JobsEnabled { get; set; } = true;
    public int LowAverageHourLocal { get; set; } = 4;
}

public sealed record RatingTagDto(string Code, string Name);

public sealed record SubmitRatingRequest(int? Stars, List<string>? Tags, string? Comment);

public sealed record RatingDto(Guid Id, Guid TripId, int Stars, IReadOnlyList<string> Tags, string? Comment, DateTime CreatedAt);

public sealed record PendingRatingDto(Guid TripId, string TripNumber, string? CounterpartName, DateTime CompletedAt, DateTime RateUntil);

public sealed record RatingTagCountDto(string Code, string Name, int Count, bool Positive);

public sealed record RatingCommentDto(int Stars, string Comment, string Week);

public sealed record RatingSummaryDto(decimal RatingAvg, int RatingCount, IReadOnlyDictionary<string, int> Distribution, IReadOnlyList<RatingTagCountDto> TopTags,
    IReadOnlyList<RatingCommentDto> RecentComments);

/// <summary><c>Trip.myRating</c>.</summary>
public sealed record MyRatingDto(int Stars, IReadOnlyList<string> Tags);

// ----- admin -----

public sealed record AdminRatingDto(
    Guid Id, string TripNumber, string? RaterName, RatingRole RaterRole, string? RateeName, int Stars, IReadOnlyList<string> Tags, string? Comment, RatingStatus Status,
    DateTime CreatedAt, Guid TripId, Guid RaterUserId, Guid RateeUserId, RatingRole RateeRole, bool CommentHidden, string? HiddenReason, string? HiddenByName,
    DateTime? HiddenAt, bool Flagged);

/// <summary>Admin <c>Trip.ratings[]</c> (both directions).</summary>
public sealed record TripRatingDto(Guid Id, RatingRole RaterRole, int Stars, IReadOnlyList<string> Tags, string? Comment, RatingStatus Status, bool CommentHidden, DateTime CreatedAt);

public sealed record HideRatingRequest(string? Reason);

public sealed record RatingFlagRatingDto(int Stars, string? Comment, IReadOnlyList<string> Tags, string? TripNumber, Guid? TripId);

public sealed record RatingFlagDto(
    Guid Id, Guid UserId, RatingRole Role, RatingFlagType Type, Guid? RatingId, decimal? Value, RatingFlagStatus Status, RatingFlagAction? Action, DateTime? ReviewedAt,
    string? Note, DateTime CreatedAt, string? UserName, Guid? DriverId, string? ReviewedByName, int? RatingCount, RatingFlagRatingDto? Rating);

public sealed record ReviewRatingFlagRequest(string? Action, string? Note);
