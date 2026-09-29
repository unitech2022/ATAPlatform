using ATA.Api.Modules.Favorites;
using ATA.Domain.Common;
using ATA.Domain.Favorites;
using ATA.Domain.Trips;

namespace ATA.Tests.Unit;

public class FavoriteUnitTests
{
    private static readonly Guid Economy = Guid.NewGuid();
    private static readonly Guid Comfort = Guid.NewGuid();
    private static readonly Guid Zone = Guid.NewGuid();

    private static FavoriteDriverDiscountRule Rule(decimal percent = 10m, decimal cap = 10m) => new() { Name = "r", DiscountPercent = percent, MaxDiscountAmount = cap };

    [Theory]
    [InlineData(10, 10, 35.43, 3.54)]
    [InlineData(10, 10, 200, 10)]
    [InlineData(50, 4, 35.43, 4)]
    [InlineData(12.5, 100, 10, 1.25)]
    [InlineData(10, 10, 0, 0)]
    public void Amount_is_the_percentage_of_the_base_fare_capped_by_the_maximum(double percent, double cap, double baseFare, double expected) =>
        Assert.Equal((decimal)expected, Rule((decimal)percent, (decimal)cap).AmountFor((decimal)baseFare));

    [Fact]
    public void Validity_window_is_inclusive_and_open_ended_without_valid_to()
    {
        var from = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var rule = Rule();
        rule.ValidFrom = from;
        Assert.True(rule.IsWithinValidity(from));
        Assert.False(rule.IsWithinValidity(from.AddSeconds(-1)));
        Assert.True(rule.IsWithinValidity(from.AddYears(5)));
        rule.ValidTo = from.AddDays(10);
        Assert.True(rule.IsWithinValidity(from.AddDays(10)));
        Assert.False(rule.IsWithinValidity(from.AddDays(10).AddSeconds(1)));
    }

    [Fact]
    public void Restrictions_apply_only_when_set_and_min_fare_is_skipped_for_an_unknown_base()
    {
        var open = Rule();
        Assert.True(FavoriteDiscountService.Matches(open, Economy, Zone, BookingType.Now, 30m));
        Assert.True(FavoriteDiscountService.Matches(open, Economy, null, BookingType.Scheduled, null));

        var restricted = Rule();
        restricted.RideCategoryIds = $"[\"{Comfort}\"]";
        Assert.False(FavoriteDiscountService.Matches(restricted, Economy, Zone, BookingType.Now, 30m));
        Assert.True(FavoriteDiscountService.Matches(restricted, Comfort, Zone, BookingType.Now, 30m));

        var zoned = Rule();
        zoned.ZoneIds = $"[\"{Zone}\"]";
        Assert.True(FavoriteDiscountService.Matches(zoned, Economy, Zone, BookingType.Now, null));
        Assert.False(FavoriteDiscountService.Matches(zoned, Economy, Guid.NewGuid(), BookingType.Now, null));
        Assert.False(FavoriteDiscountService.Matches(zoned, Economy, null, BookingType.Now, null));

        var scheduledOnly = Rule();
        scheduledOnly.BookingTypes = "[\"scheduled\"]";
        Assert.False(FavoriteDiscountService.Matches(scheduledOnly, Economy, Zone, BookingType.Now, null));
        Assert.True(FavoriteDiscountService.Matches(scheduledOnly, Economy, Zone, BookingType.Scheduled, null));

        var minFare = Rule();
        minFare.MinFare = 30m;
        Assert.False(FavoriteDiscountService.Matches(minFare, Economy, Zone, BookingType.Now, 29.99m));
        Assert.True(FavoriteDiscountService.Matches(minFare, Economy, Zone, BookingType.Now, 30m));
        Assert.True(FavoriteDiscountService.Matches(minFare, Economy, Zone, BookingType.Now, null));
    }
}
