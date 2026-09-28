using ATA.Api.Modules.Pricing;
using ATA.Api.Modules.Trips.Matching;
using ATA.Domain.Drivers;
using ATA.Domain.Matching;
using ATA.Domain.Pricing;

namespace ATA.Tests.Unit;

public class PricingUnitTests
{
    [Theory]
    [InlineData(24.70, 46.70, true)]
    [InlineData(24.85, 46.70, false)]
    [InlineData(24.80, 46.80, false)] // on the corner ray: outside
    [InlineData(24.60, 46.60, true)]
    public void GeoPolygon_ray_casting_contains(double lat, double lng, bool expected)
    {
        var ring = GeoPolygon.Parse("[[24.6,46.6],[24.6,46.8],[24.8,46.8],[24.8,46.6],[24.6,46.6]]");
        Assert.Equal(expected, ring.Contains(lat, lng));
    }

    [Fact]
    public void GeoPolygon_rejects_degenerate_rings()
    {
        Assert.False(GeoPolygon.TryParse("[[1,2],[3,4]]", out _));
        Assert.False(GeoPolygon.TryParse("not json", out _));
        Assert.True(GeoPolygon.TryParse("[[1,1],[1,2],[2,2]]", out var open));
        Assert.Equal(3, open!.Points.Count);
    }

    [Theory]
    [InlineData(12.24, 12.0)]
    [InlineData(12.25, 12.5)]
    [InlineData(12.74, 12.5)]
    [InlineData(12.75, 13.0)]
    [InlineData(0.1, 0.0)]
    public void Totals_round_to_the_nearest_half_riyal(decimal input, decimal expected) => Assert.Equal(expected, PricingMath.RoundToHalf(input));

    [Fact]
    public void Time_multiplier_windows_handle_midnight_and_weekday()
    {
        var night = new PricingTimeMultiplier { FromTime = new TimeOnly(22, 0), ToTime = new TimeOnly(5, 0), Multiplier = 1.15m, Label = "night" };
        Assert.True(night.AppliesAt(new DateTime(2026, 9, 28, 23, 30, 0)));
        Assert.True(night.AppliesAt(new DateTime(2026, 9, 29, 0, 30, 0)));
        Assert.False(night.AppliesAt(new DateTime(2026, 9, 29, 5, 0, 0)));
        Assert.False(night.AppliesAt(new DateTime(2026, 9, 28, 12, 0, 0)));

        // Monday (1) 22:00 → Tuesday 05:00 still counts as the Monday window after midnight.
        var mondayNight = new PricingTimeMultiplier { DayOfWeek = 1, FromTime = new TimeOnly(22, 0), ToTime = new TimeOnly(5, 0), Multiplier = 1.3m, Label = "monday_night" };
        Assert.True(mondayNight.AppliesAt(new DateTime(2026, 9, 28, 23, 0, 0))); // 2026-09-28 is a Monday
        Assert.True(mondayNight.AppliesAt(new DateTime(2026, 9, 29, 2, 0, 0)));
        Assert.False(mondayNight.AppliesAt(new DateTime(2026, 9, 29, 23, 0, 0)));

        var rule = new PricingRule { Name = "r", TimeMultipliers = { night, new PricingTimeMultiplier { FromTime = new TimeOnly(0, 0), ToTime = new TimeOnly(5, 0), Multiplier = 1.5m, Label = "deep_night" } } };
        Assert.Equal((1.5m, "deep_night"), rule.TimeMultiplierAt(new DateTime(2026, 9, 29, 1, 0, 0)));
        Assert.Equal((1.15m, "night"), rule.TimeMultiplierAt(new DateTime(2026, 9, 28, 23, 0, 0)));
        Assert.Equal((1m, null), rule.TimeMultiplierAt(new DateTime(2026, 9, 28, 12, 0, 0)));
    }

    [Fact]
    public void Demand_rule_maps_ratio_to_levels()
    {
        var rule = new DemandRule { ThresholdModerate = 0.8m, ThresholdHigh = 1.5m, ThresholdVeryHigh = 2.5m };
        Assert.Equal(DemandLevel.Normal, rule.LevelFor(0.5m));
        Assert.Equal(DemandLevel.Moderate, rule.LevelFor(0.8m));
        Assert.Equal(DemandLevel.High, rule.LevelFor(2m));
        Assert.Equal(DemandLevel.VeryHigh, rule.LevelFor(3m));
    }

    [Fact]
    public void Operating_schedule_parses_and_checks_local_time()
    {
        var schedule = OperatingSchedule.Parse("""[{"day":1,"from":"06:00","to":"23:59"}]""");
        Assert.True(schedule.IsOpenAt(new DateTime(2026, 9, 28, 7, 0, 0)));
        Assert.False(schedule.IsOpenAt(new DateTime(2026, 9, 28, 5, 0, 0)));
        Assert.False(schedule.IsOpenAt(new DateTime(2026, 9, 29, 7, 0, 0)));
        Assert.True(OperatingSchedule.Parse(null).IsAlwaysOpen);
        Assert.True(OperatingSchedule.Parse("[]").IsAlwaysOpen);
    }

    [Fact]
    public void Score_weighs_distance_eta_rating_reliability_tier_and_favourite()
    {
        var w = MatchingWeights.Default;
        var perfect = ScoringMatcher.Score(w, 0, 12000, 0, 5m, new DriverReliability(1m, 0m, false), DriverTier.Platinum, favorite: true);
        Assert.Equal(1m, perfect);
        var closerButWorse = ScoringMatcher.Score(w, 100, 12000, 16, 3.5m, DriverReliability.Neutral, DriverTier.Bronze, favorite: false);
        var fartherButBetter = ScoringMatcher.Score(w, 300, 12000, 47, 5m, DriverReliability.Neutral, DriverTier.Bronze, favorite: false);
        Assert.True(fartherButBetter > closerButWorse);
        var unreliable = ScoringMatcher.Score(w, 300, 12000, 47, 5m, new DriverReliability(0.2m, 0.5m, false), DriverTier.Bronze, favorite: false);
        Assert.True(unreliable < fartherButBetter);
        Assert.InRange(ScoringMatcher.Score(w, 20000, 12000, 2000, 1m, new DriverReliability(0m, 1m, false), DriverTier.Bronze, favorite: false), 0m, 0.02m);

        // Weights are normalised, so a scaled set gives the same score.
        var scaled = new MatchingWeights(3.5m, 2m, 1.5m, 1m, 1m, 0.5m, 0.5m);
        Assert.Equal(fartherButBetter, ScoringMatcher.Score(scaled, 300, 12000, 47, 5m, DriverReliability.Neutral, DriverTier.Bronze, favorite: false));
        Assert.Equal(MatchingWeights.Default, MatchingWeights.Parse(MatchingWeights.Default.ToJson()));
        Assert.Equal(MatchingWeights.Default, MatchingWeights.Parse("{}"));
    }
}
