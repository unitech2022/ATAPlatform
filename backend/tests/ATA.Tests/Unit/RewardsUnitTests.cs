using ATA.Api.Modules.Incentives;
using ATA.Api.Modules.Promotions;
using ATA.Domain.Drivers;
using ATA.Domain.Incentives;
using ATA.Domain.Promotions;
using ATA.Domain.Ratings;

namespace ATA.Tests.Unit;

public class RewardsUnitTests
{
    private static DiscountCandidate Promo(decimal amount, bool stackable) => new(DiscountSources.Promotion, "ATA10", amount, stackable);

    private static DiscountCandidate Favorite(decimal amount, bool stackable) => new(DiscountSources.FavoriteDriver, null, amount, stackable);

    [Fact]
    public void Discount_engine_applies_a_single_promotion_and_rounds_the_rest_to_half_a_riyal()
    {
        var outcome = DiscountEngine.Compute(51.02m, Promo(5m, false), null, 0m);
        Assert.Equal(5m, outcome.Discount);
        Assert.Equal(46.0m, outcome.Total);
        Assert.False(outcome.PromotionDropped);
        Assert.Equal(DiscountSources.Promotion, Assert.Single(outcome.Applied).Source);
    }

    [Fact]
    public void Discount_engine_keeps_only_the_larger_discount_unless_both_are_stackable()
    {
        // Not stackable: the larger one wins; a tie goes to the favourite driver and the promotion is dropped (released as not_stacked).
        var promoWins = DiscountEngine.Compute(50m, Promo(8m, false), Favorite(5m, true), 0m);
        Assert.Equal(8m, promoWins.Discount);
        Assert.False(promoWins.PromotionDropped);
        var favoriteWins = DiscountEngine.Compute(50m, Promo(4m, true), Favorite(5m, false), 0m);
        Assert.Equal(5m, favoriteWins.Discount);
        Assert.True(favoriteWins.PromotionDropped);
        Assert.Equal(DiscountSources.FavoriteDriver, Assert.Single(favoriteWins.Applied).Source);
        var tie = DiscountEngine.Compute(50m, Promo(5m, false), Favorite(5m, true), 0m);
        Assert.True(tie.PromotionDropped);
        Assert.Equal(DiscountSources.FavoriteDriver, Assert.Single(tie.Applied).Source);

        // Both stackable: the sum, split per source.
        var stacked = DiscountEngine.Compute(50m, Promo(5m, true), Favorite(4m, true), 0m);
        Assert.Equal(9m, stacked.Discount);
        Assert.Equal(41m, stacked.Total);
        Assert.Equal(5m, stacked.AmountOf(DiscountSources.Promotion));
        Assert.Equal(4m, stacked.AmountOf(DiscountSources.FavoriteDriver));
    }

    [Fact]
    public void Discount_never_takes_the_fare_below_the_minimum_payable_fare()
    {
        Assert.Equal(0m, DiscountEngine.Compute(30m, Promo(500m, false), null, 0m).Total);
        var capped = DiscountEngine.Compute(30m, Promo(20m, true), Favorite(15m, true), 5m);
        Assert.Equal(25m, capped.Discount);
        Assert.Equal(5m, capped.Total);
        Assert.Equal(20m, capped.AmountOf(DiscountSources.Promotion));
        Assert.Equal(5m, capped.AmountOf(DiscountSources.FavoriteDriver));
    }

    [Fact]
    public void Promotion_amounts_follow_the_type()
    {
        var percent = new Promotion { Code = "P", NameAr = "p", NameEn = "p", Type = PromotionType.Percent, Value = 20m, MaxDiscount = 15m };
        Assert.Equal(10.20m, percent.AmountFor(51m, 2m));
        Assert.Equal(15m, percent.AmountFor(200m, 2m));
        var fixedAmount = new Promotion { Code = "F", NameAr = "f", NameEn = "f", Type = PromotionType.Fixed, Value = 10m };
        Assert.Equal(10m, fixedAmount.AmountFor(51m, 2m));
        Assert.Equal(8m, fixedAmount.AmountFor(8m, 2m));
        var booking = new Promotion { Code = "B", NameAr = "b", NameEn = "b", Type = PromotionType.FreeBookingFee };
        Assert.Equal(2m, booking.AmountFor(51m, 2m));
        Assert.True(Promotion.IsValidCode(Promotion.Normalize(" ata10 ")));
        Assert.False(Promotion.IsValidCode("AB1"));
        Assert.False(Promotion.IsValidCode("ATA-10"));
    }

    [Fact]
    public void Weighted_rating_average_favours_recent_ratings_and_defaults_to_five()
    {
        Assert.Equal(5.00m, RatingMath.WeightedAverage([], 0.5m));
        Assert.Equal(3.47m, RatingMath.WeightedAverage([2, 4, 5], 0.5m));
        Assert.Equal(4.43m, RatingMath.WeightedAverage([4, 5], 0.5m));
        // Equal weights (minWeight = 1) is the plain mean.
        Assert.Equal(3.67m, RatingMath.WeightedAverage([2, 4, 5], 1m));
    }

    [Fact]
    public void Tier_evaluation_is_inclusive_and_the_commission_discount_is_a_share_of_the_commission()
    {
        DriverTierRule Rule(DriverTier tier, int trips, decimal rating, decimal acceptance, decimal cancellation, int order) => new()
        {
            Tier = tier, MinCompletedTrips = trips, MinRatingAvg = rating, MinAcceptanceRate = acceptance, MaxCancellationRate = cancellation, SortOrder = order,
        };
        var rules = new[]
        {
            Rule(DriverTier.Bronze, 0, 0m, 0m, 1m, 1), Rule(DriverTier.Silver, 60, 4.70m, 0.80m, 0.08m, 2),
            Rule(DriverTier.Gold, 150, 4.80m, 0.85m, 0.05m, 3), Rule(DriverTier.Platinum, 250, 4.90m, 0.90m, 0.03m, 4),
        };
        Assert.Equal(DriverTier.Silver, TierService.Evaluate(rules, new TierMetrics(60, 4.70m, 0.80m, 0.08m)));
        Assert.Equal(DriverTier.Bronze, TierService.Evaluate(rules, new TierMetrics(60, 4.69m, 0.80m, 0.08m)));
        Assert.Equal(DriverTier.Silver, TierService.Evaluate(rules, new TierMetrics(149, 4.95m, 0.95m, 0.01m)));
        Assert.Equal(DriverTier.Platinum, TierService.Evaluate(rules, new TierMetrics(250, 4.90m, 0.90m, 0.03m)));
        Assert.Equal(DriverTier.Gold, TierService.Evaluate(rules, new TierMetrics(300, 4.90m, 0.90m, 0.04m)));
        Assert.Equal(82m, TierMath.EffectiveSharePercent(80m, 10m));
        Assert.Equal(80m, TierMath.EffectiveSharePercent(80m, 0m));
    }

    [Fact]
    public void Incentive_periods_follow_riyadh_days_and_weeks_and_the_daily_window()
    {
        var weekly = new DriverIncentive
        {
            NameAr = "w", NameEn = "w", Type = IncentiveType.Weekly, StartsAt = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), EndsAt = new DateTime(2026, 12, 1, 0, 0, 0, DateTimeKind.Utc),
            DaysOfWeek = "[4,5]", DailyFrom = new TimeOnly(16, 0), DailyTo = new TimeOnly(23, 59),
        };
        // Thursday 2026-10-01 20:00 Riyadh = 17:00 UTC → week Sunday 2026-09-27 00:00 Riyadh (= 26 Sep 21:00 UTC).
        var thursdayEvening = new DateTime(2026, 10, 1, 17, 0, 0, DateTimeKind.Utc);
        var period = weekly.PeriodAt(thursdayEvening, 180)!.Value;
        Assert.Equal(new DateTime(2026, 9, 26, 21, 0, 0, DateTimeKind.Utc), period.Start);
        Assert.Equal(new DateTime(2026, 10, 3, 21, 0, 0, DateTimeKind.Utc), period.End);
        Assert.True(weekly.IsInWindow(thursdayEvening, 180));
        Assert.True(weekly.IsInWindow(new DateTime(2026, 10, 1, 20, 59, 30, DateTimeKind.Utc), 180));
        Assert.False(weekly.IsInWindow(new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc), 180));
        Assert.False(weekly.IsInWindow(new DateTime(2026, 9, 30, 17, 0, 0, DateTimeKind.Utc), 180));
        Assert.Null(weekly.PeriodAt(new DateTime(2026, 12, 2, 0, 0, 0, DateTimeKind.Utc), 180));

        var daily = new DriverIncentive
        {
            NameAr = "d", NameEn = "d", Type = IncentiveType.Daily, StartsAt = new DateTime(2026, 9, 28, 9, 0, 0, DateTimeKind.Utc), EndsAt = new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc),
            DailyFrom = new TimeOnly(22, 0), DailyTo = new TimeOnly(2, 0),
        };
        var first = daily.PeriodAt(new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc), 180)!.Value;
        Assert.Equal(new DateTime(2026, 9, 28, 9, 0, 0, DateTimeKind.Utc), first.Start);
        Assert.Equal(new DateTime(2026, 9, 28, 21, 0, 0, DateTimeKind.Utc), first.End);
        Assert.True(daily.IsInWindow(new DateTime(2026, 9, 28, 22, 30, 0, DateTimeKind.Utc), 180));
        Assert.False(daily.IsInWindow(new DateTime(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc), 180));
    }
}
