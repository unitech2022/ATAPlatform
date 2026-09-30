using ATA.Api.Modules.Scheduling;
using ATA.Domain.Cancellation;
using ATA.Domain.Common;
using ATA.Domain.Scheduling;
using ATA.Domain.Trips;

namespace ATA.Tests.Integration;

/// <summary>Pure rules of F17 scheduled rides: rule selection, the booking window, fees, reservation deadlines and the trip state machine.</summary>
public class SchedulingUnitTests
{
    private static readonly DateTime Now = new(2026, 9, 26, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid City = Guid.CreateVersion7();
    private static readonly Guid Category = Guid.CreateVersion7();

    private static Trip NewTrip(TripStatus status = TripStatus.Scheduled, BookingType type = BookingType.Scheduled) => new()
    {
        TripNumber = "T-1", PinCodeHash = "h", PinCodeProtected = "p", PickupName = "a", PickupAddress = "a", DropoffName = "b", DropoffAddress = "b", Status = status, BookingType = type,
        RequestedAt = Now, ScheduledAt = Now.AddDays(1),
    };

    [Fact]
    public void Rule_selection_prefers_city_and_category_then_city_then_category_then_global_and_ignores_inactive_rows()
    {
        var global = new ScheduledRideRule();
        var byCategory = new ScheduledRideRule { RideCategoryId = Category };
        var byCity = new ScheduledRideRule { CityId = City };
        var both = new ScheduledRideRule { CityId = City, RideCategoryId = Category };
        var other = new ScheduledRideRule { CityId = Guid.CreateVersion7() };
        var rules = new[] { global, byCategory, byCity, both, other };

        Assert.Same(both, ScheduleRuleProvider.Select(rules, City, Category));
        Assert.Same(byCity, ScheduleRuleProvider.Select(rules, City, Guid.CreateVersion7()));
        Assert.Same(byCategory, ScheduleRuleProvider.Select(rules, Guid.CreateVersion7(), Category));
        Assert.Same(global, ScheduleRuleProvider.Select(rules, null, null));

        both.IsActive = false;
        Assert.Same(byCity, ScheduleRuleProvider.Select(rules, City, Category));
        global.IsActive = false;
        Assert.Null(ScheduleRuleProvider.Select([global], null, null));
    }

    [Fact]
    public void Booking_window_is_seven_days_from_booking_time_inclusive_and_at_least_the_minimum_lead()
    {
        var rule = new ScheduledRideRule();
        ScheduleRuleProvider.EnsureWindow(rule, Now.AddDays(7), Now);
        ScheduleRuleProvider.EnsureWindow(rule, Now.AddMinutes(30), Now);

        var far = Assert.Throws<DomainException>(() => ScheduleRuleProvider.EnsureWindow(rule, Now.AddDays(7).AddSeconds(1), Now));
        Assert.Equal(ErrorCodes.ScheduleWindowExceeded, far.Code);
        var near = Assert.Throws<DomainException>(() => ScheduleRuleProvider.EnsureWindow(rule, Now.AddMinutes(29), Now));
        Assert.Equal(ErrorCodes.ScheduleLeadTooShort, near.Code);

        rule.MaxDaysAhead = 2;
        Assert.Equal(Now.AddDays(2), rule.MaxScheduledAt(Now));
        Assert.Throws<DomainException>(() => ScheduleRuleProvider.EnsureWindow(rule, Now.AddDays(3), Now));
    }

    [Fact]
    public void Rule_time_offsets_are_measured_from_the_pickup_time()
    {
        var rule = new ScheduledRideRule();
        var at = Now.AddDays(1);
        Assert.Equal(at.AddMinutes(-60), rule.FreeCancelUntil(at));
        Assert.Equal(at.AddMinutes(-10), rule.SearchStartsAt(at));
        Assert.Equal(at.AddMinutes(-60), rule.FirstConfirmationAt(at));
        Assert.Equal(at.AddMinutes(-15), rule.FinalConfirmationAt(at));
        Assert.Equal(at.AddMinutes(-120), rule.FreeReleaseUntil(at));
    }

    [Fact]
    public void Late_cancel_fee_follows_the_configured_type_and_never_exceeds_the_fare()
    {
        var rule = new ScheduledRideRule { LateCancelFeeType = CancellationFeeType.Fixed, LateCancelFeeAmount = 10m };
        Assert.Equal(10m, rule.LateCancelFee(80m, 99m));
        Assert.Equal(4m, rule.LateCancelFee(4m, 99m));

        rule.LateCancelFeeType = CancellationFeeType.Percent;
        rule.LateCancelFeePercent = 25m;
        Assert.Equal(20m, rule.LateCancelFee(80m, 99m));

        rule.LateCancelFeeType = CancellationFeeType.PricingRule;
        Assert.Equal(7.5m, rule.LateCancelFee(80m, 7.5m));

        rule.LateCancelFeeType = CancellationFeeType.None;
        Assert.Equal(0m, rule.LateCancelFee(80m, 7.5m));
    }

    [Fact]
    public void Reminder_offsets_are_parsed_sorted_distinct_and_tolerate_bad_json()
    {
        Assert.Equal([1440, 60, 15], ScheduledRideRule.ParseOffsets("[15,1440,60,60,0,-5]"));
        Assert.Empty(ScheduledRideRule.ParseOffsets(null));
        Assert.Empty(ScheduledRideRule.ParseOffsets("  "));
        Assert.Empty(ScheduledRideRule.ParseOffsets("not json"));
        Assert.Equal("[60,15]", ScheduledRideRule.SerializeOffsets([15, 60, 15]));
        Assert.Equal([1440, 60, 15], new ScheduledRideRule().RiderOffsets());
        Assert.Equal([1440, 180], new ScheduledRideRule().DriverOffsets());
    }

    [Fact]
    public void Reservation_deadlines_exist_only_once_the_confirmation_was_requested()
    {
        var rule = new ScheduledRideRule();
        var reservation = new ScheduledRideReservation { Status = ReservationStatus.Reserved };
        Assert.Null(reservation.ConfirmDeadline(rule));

        reservation.ConfirmRequestedAt = Now;
        Assert.Equal(Now.AddMinutes(10), reservation.ConfirmDeadline(rule));
        Assert.Null(reservation.FinalConfirmDeadline(rule));

        reservation.Status = ReservationStatus.Confirmed;
        reservation.FinalConfirmRequestedAt = Now;
        Assert.Null(reservation.ConfirmDeadline(rule));
        Assert.Equal(Now.AddMinutes(5), reservation.FinalConfirmDeadline(rule));
        Assert.True(reservation.IsActive);
        Assert.True(reservation.IsPreAssignment);

        reservation.Status = ReservationStatus.Assigned;
        Assert.True(reservation.IsActive);
        Assert.False(reservation.IsPreAssignment);
    }

    [Fact]
    public void Driver_fault_covers_no_shows_late_releases_and_missed_confirmations_only()
    {
        static ScheduledRideReservation R(ReservationStatus s, ReservationReleaseReason? reason = null, bool late = false) =>
            new() { Status = s, ReleaseReason = reason, IsLateRelease = late };

        Assert.True(R(ReservationStatus.NoShow).IsDriverFault);
        Assert.True(R(ReservationStatus.Released, ReservationReleaseReason.DriverReleased, late: true).IsDriverFault);
        Assert.True(R(ReservationStatus.Released, ReservationReleaseReason.ConfirmationMissed).IsDriverFault);
        Assert.True(R(ReservationStatus.Released, ReservationReleaseReason.FinalConfirmationMissed).IsDriverFault);
        Assert.False(R(ReservationStatus.Released, ReservationReleaseReason.DriverReleased).IsDriverFault);
        Assert.False(R(ReservationStatus.Released, ReservationReleaseReason.Admin).IsDriverFault);
        Assert.False(R(ReservationStatus.Cancelled, ReservationReleaseReason.TripCancelled).IsDriverFault);
        Assert.False(R(ReservationStatus.Completed).IsDriverFault);
    }

    [Fact]
    public void Scheduled_status_is_open_but_not_active_and_never_terminal()
    {
        Assert.DoesNotContain(TripStatus.Scheduled, Trip.ActiveStatuses);
        Assert.Contains(TripStatus.Scheduled, Trip.OpenStatuses);
        Assert.DoesNotContain(TripStatus.Scheduled, Trip.TerminalStatuses);
        Assert.Contains(TripStatus.Searching, Trip.ActiveStatuses);
        Assert.True(NewTrip().CanBeCancelled);
    }

    [Fact]
    public void Reassign_clears_the_driver_and_returns_a_scheduled_trip_to_searching()
    {
        var trip = NewTrip(TripStatus.Scheduled);
        trip.Assign(Guid.CreateVersion7(), Guid.CreateVersion7(), Now);
        trip.ReservedDriverId = trip.DriverId;
        Assert.Equal(TripStatus.DriverAssigned, trip.Status);

        trip.MarkEnRoute();
        trip.Reassign();
        Assert.Equal(TripStatus.Searching, trip.Status);
        Assert.Null(trip.DriverId);
        Assert.Null(trip.VehicleId);
        Assert.Null(trip.AssignedAt);
        Assert.Null(trip.ReservedDriverId);
    }

    [Fact]
    public void Reassign_is_only_for_scheduled_bookings_and_scheduled_search_only_starts_from_scheduled()
    {
        var now = NewTrip(TripStatus.DriverAssigned, BookingType.Now);
        now.DriverId = Guid.CreateVersion7();
        Assert.Throws<DomainException>(now.Reassign);

        var searching = NewTrip(TripStatus.Searching);
        Assert.Throws<DomainException>(searching.StartScheduledSearch);

        var scheduled = NewTrip(TripStatus.Scheduled);
        scheduled.StartScheduledSearch();
        Assert.Equal(TripStatus.Searching, scheduled.Status);
    }

    [Fact]
    public void Favorite_exclusive_window_ends_at_booking_plus_minutes_but_never_after_the_search_start()
    {
        var rule = new ScheduledRideRule { FavoriteExclusiveMinutes = 30 };
        var trip = NewTrip();
        trip.RequestedAt = Now;
        trip.ScheduledAt = Now.AddHours(5);
        Assert.Equal(Now.AddMinutes(30), ScheduledRideEngine.ExclusiveUntil(trip, rule));

        trip.ScheduledAt = Now.AddMinutes(35);
        Assert.Equal(Now.AddMinutes(25), ScheduledRideEngine.ExclusiveUntil(trip, rule));
    }

    [Fact]
    public void At_risk_means_pickup_within_ninety_minutes_without_a_confirmed_driver()
    {
        var trip = NewTrip();
        trip.ScheduledAt = Now.AddMinutes(60);
        Assert.True(ScheduledAdminService.IsAtRisk(trip, null, Now));
        Assert.True(ScheduledAdminService.IsAtRisk(trip, ReservationStatus.Reserved, Now));
        Assert.False(ScheduledAdminService.IsAtRisk(trip, ReservationStatus.Confirmed, Now));
        Assert.False(ScheduledAdminService.IsAtRisk(trip, ReservationStatus.Assigned, Now));

        trip.ScheduledAt = Now.AddMinutes(91);
        Assert.False(ScheduledAdminService.IsAtRisk(trip, null, Now));

        trip.ScheduledAt = Now.AddMinutes(60);
        trip.Status = TripStatus.Searching;
        Assert.False(ScheduledAdminService.IsAtRisk(trip, null, Now));
    }
}
