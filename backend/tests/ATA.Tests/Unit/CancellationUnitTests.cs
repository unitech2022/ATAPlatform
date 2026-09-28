using ATA.Api.Modules.Cancellation;
using ATA.Api.Modules.Safety;
using ATA.Domain.Cancellation;
using ATA.Domain.Common;
using ATA.Domain.Trips;

namespace ATA.Tests.Unit;

public class CancellationUnitTests
{
    private static readonly Guid Economy = Guid.NewGuid();
    private static readonly Guid Zone = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 9, 28, 12, 0, 0, DateTimeKind.Utc);

    private static CancellationRule Rule(string name, int priority = 0, Guid? category = null, Guid? zone = null, BookingType? booking = null, DateTime? created = null) => new()
    {
        Name = name, Actor = CancellationActor.Passenger, Stage = CancellationStage.AfterAccept, RideCategoryId = category, ZoneId = zone, BookingType = booking,
        Priority = priority, FeeType = CancellationFeeType.Fixed, FeeAmount = 5m, IsActive = true, CreatedAt = created ?? Now,
    };

    [Fact]
    public void Most_specific_rule_wins_then_priority_then_newest()
    {
        var general = Rule("general");
        var category = Rule("category", priority: 9, category: Economy);
        var zoneAndCategory = Rule("zone+category", category: Economy, zone: Zone);
        var otherZone = Rule("other zone", priority: 50, category: Economy, zone: Guid.NewGuid());
        var inactive = Rule("inactive", priority: 99, category: Economy, zone: Zone);
        inactive.IsActive = false;
        CancellationRule[] rules = [general, category, zoneAndCategory, otherZone, inactive];

        Assert.Same(zoneAndCategory, CancellationMath.SelectRule(rules, CancellationActor.Passenger, CancellationStage.AfterAccept, BookingType.Now, Economy, Zone));
        Assert.Same(category, CancellationMath.SelectRule(rules, CancellationActor.Passenger, CancellationStage.AfterAccept, BookingType.Now, Economy, null));
        Assert.Same(general, CancellationMath.SelectRule(rules, CancellationActor.Passenger, CancellationStage.AfterAccept, BookingType.Now, Guid.NewGuid(), Zone));
        Assert.Null(CancellationMath.SelectRule(rules, CancellationActor.Driver, CancellationStage.AfterAccept, BookingType.Now, Economy, Zone));

        var tieOld = Rule("tie old", priority: 1, category: Economy, created: Now.AddDays(-1));
        var tieNew = Rule("tie new", priority: 1, category: Economy, created: Now);
        var tieHigh = Rule("tie high", priority: 2, category: Economy, created: Now.AddDays(-5));
        Assert.Same(tieHigh, CancellationMath.SelectRule([tieOld, tieNew, tieHigh], CancellationActor.Passenger, CancellationStage.AfterAccept, BookingType.Now, Economy, null));
        Assert.Same(tieNew, CancellationMath.SelectRule([tieOld, tieNew], CancellationActor.Passenger, CancellationStage.AfterAccept, BookingType.Now, Economy, null));
    }

    [Fact]
    public void Fee_respects_free_window_type_clamps_and_the_estimated_fare()
    {
        var fixedRule = new CancellationRule { Name = "f", FeeType = CancellationFeeType.Fixed, FeeAmount = 5m, FreeWindowSeconds = 120, PenaltyPoints = 1, DriverCompensationPercent = 50m };
        var inside = CancellationMath.Calculate(fixedRule, Now.AddSeconds(-119), Now, 30m, 0m);
        Assert.True(inside.WithinFreeWindow);
        Assert.True(inside.IsFree);
        Assert.Equal(Now.AddSeconds(1), inside.FreeUntil);
        var after = CancellationMath.Calculate(fixedRule, Now.AddSeconds(-120), Now, 30m, 0m);
        Assert.Equal(5m, after.Fee);
        Assert.Equal(1, after.PenaltyPoints);
        Assert.Equal(2.5m, CancellationMath.Compensation(after.Fee, fixedRule));

        var percent = new CancellationRule { Name = "p", FeeType = CancellationFeeType.Percent, FeePercent = 33.333m, MinFee = 4m, MaxFee = 9m };
        Assert.Equal(6.67m, CancellationMath.Calculate(percent, Now, Now, 20m, 0m).Fee);
        Assert.Equal(4m, CancellationMath.Calculate(percent, Now, Now, 3m * 3m, 0m).Fee);
        Assert.Equal(9m, CancellationMath.Calculate(percent, Now, Now, 100m, 0m).Fee);

        var pricing = new CancellationRule { Name = "pr", FeeType = CancellationFeeType.PricingRule, MinFee = 10m, PenaltyPoints = 4 };
        Assert.Equal(10m, CancellationMath.Calculate(pricing, Now, Now, 25m, 0m).Fee);
        Assert.Equal(12m, CancellationMath.Calculate(pricing, Now, Now, 25m, 12m).Fee);
        Assert.Equal(8m, CancellationMath.Calculate(pricing, Now, Now, 8m, 0m).Fee); // never above the estimated fare

        var none = CancellationMath.Calculate(null, Now.AddHours(-1), Now, 30m, 0m);
        Assert.Equal(0m, none.Fee);
        Assert.Equal(0, none.PenaltyPoints);
    }

    [Fact]
    public void Stage_follows_the_trip_status_and_free_waiting()
    {
        var trip = new Trip { TripNumber = "T", PickupName = "a", PickupAddress = "a", DropoffName = "b", DropoffAddress = "b", PinCodeHash = "x", PinCodeProtected = "x", RequestedAt = Now.AddMinutes(-20) };
        trip.Status = TripStatus.Searching;
        Assert.Equal(CancellationStage.BeforeAccept, CancellationMath.StageOf(trip, 180, Now));
        trip.Status = TripStatus.DriverAssigned;
        Assert.Equal(CancellationStage.AfterAccept, CancellationMath.StageOf(trip, 180, Now));
        trip.Status = TripStatus.DriverEnRoute;
        Assert.Equal(CancellationStage.EnRoute, CancellationMath.StageOf(trip, 180, Now));
        trip.Status = TripStatus.Waiting;
        trip.ArrivedAt = Now.AddSeconds(-180);
        Assert.Equal(CancellationStage.Arrived, CancellationMath.StageOf(trip, 180, Now));
        trip.ArrivedAt = Now.AddSeconds(-181);
        Assert.Equal(CancellationStage.Waiting, CancellationMath.StageOf(trip, 180, Now));
        trip.Status = TripStatus.InTrip;
        Assert.Equal(ErrorCodes.Conflict, Assert.Throws<DomainException>(() => CancellationMath.StageOf(trip, 180, Now)).Code);
    }

    [Fact]
    public void Thresholds_need_the_minimum_sample_for_rates_and_incentives_are_reduced_from_incentives_reduced()
    {
        var restricted = new ReliabilityThreshold { Role = Role.Driver, Level = RestrictionLevel.TemporarilyRestricted, MinPenaltyPoints = 18, MinCancellationRate = 0.30m, MinTripsForRate = 10 };
        Assert.False(restricted.IsMetBy(points: 0, tripsAccepted: 3, cancellationRate: 1m));
        Assert.True(restricted.IsMetBy(points: 0, tripsAccepted: 10, cancellationRate: 0.30m));
        Assert.True(restricted.IsMetBy(points: 18, tripsAccepted: 0, cancellationRate: 0m));

        var service = new ReliabilityService(null!, null!, null!, null!, Microsoft.Extensions.Options.Options.Create(new ReliabilityOptions()));
        var reduced = new ReliabilitySnapshot(Guid.NewGuid(), Role.Driver, RestrictionLevel.IncentivesReduced, null, 0.2m, 12, 0.6m, 50m, false);
        Assert.Equal(0.5m, service.IncentiveMultiplier(reduced));
        Assert.Equal(0.6m, service.MatchingFactor(reduced));
        var warning = reduced with { Level = RestrictionLevel.Warning };
        Assert.Equal(1m, service.IncentiveMultiplier(warning));
        Assert.Equal(1m, service.MatchingFactor(warning));
    }

    [Fact]
    public void Chat_masks_phone_like_numbers_and_route_distance_uses_the_nearest_segment()
    {
        Assert.Equal("رقمي ••••", TripChatService.MaskNumbers("رقمي 0551234567"));
        Assert.Equal("call •••• now", TripChatService.MaskNumbers("call +966 55-123-4567 now"));
        Assert.Equal("اللوحة 1234 والطابق 7", TripChatService.MaskNumbers("اللوحة 1234 والطابق 7"));
        Assert.Equal("••••", TripChatService.MaskNumbers("٠٥٥١٢٣٤٥٦٧"));

        List<decimal[]> route = [[24.70m, 46.60m], [24.70m, 46.70m]];
        Assert.InRange(PlannedRoutes.DistanceToRouteMeters(24.70, 46.65, route), 0, 1);
        Assert.InRange(PlannedRoutes.DistanceToRouteMeters(24.72, 46.65, route), 2200, 2250);
        Assert.InRange(PlannedRoutes.DistanceToRouteMeters(24.70, 46.50, route), 10000, 10200);
        var many = Enumerable.Range(0, 1000).Select(i => new[] { (decimal)i, 0m }).ToList();
        var sampled = PlannedRoutes.Downsample(many, 300);
        Assert.Equal(300, sampled.Count);
        Assert.Equal(999m, sampled[^1][0]);
    }
}
