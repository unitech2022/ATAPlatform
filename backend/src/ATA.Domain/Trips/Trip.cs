using ATA.Domain.Common;

namespace ATA.Domain.Trips;

/// <summary>Row of the <c>trips</c> table with the state machine of feature F8.</summary>
public class Trip : AuditableEntity
{
    public const int PinLength = 4;
    public const int DefaultPinMaxAttempts = 5;

    public required string TripNumber { get; set; }
    public Guid PassengerId { get; set; }
    public Guid? DriverId { get; set; }
    public Guid? VehicleId { get; set; }
    public Guid RideCategoryId { get; set; }
    public TripStatus Status { get; set; } = TripStatus.Requested;
    public BookingType BookingType { get; set; } = BookingType.Now;
    public DateTime? ScheduledAt { get; set; }
    public required string PickupName { get; set; }
    public required string PickupAddress { get; set; }
    public decimal PickupLat { get; set; }
    public decimal PickupLng { get; set; }
    public required string DropoffName { get; set; }
    public required string DropoffAddress { get; set; }
    public decimal DropoffLat { get; set; }
    public decimal DropoffLng { get; set; }
    public bool PreferFemaleDriver { get; set; }
    public PaymentMethodKind PaymentMethod { get; set; } = PaymentMethodKind.Cash;
    /// <summary>Saved card used for a <c>card</c> trip (plain column).</summary>
    public Guid? PaymentMethodId { get; set; }
    public PricingMode PricingMode { get; set; } = PricingMode.Fixed;
    public decimal? OfferedPrice { get; set; }
    public int EstimatedDistanceM { get; set; }
    public int EstimatedDurationS { get; set; }
    public decimal EstimatedFare { get; set; }
    public int? FinalDistanceM { get; set; }
    public int? FinalDurationS { get; set; }
    public decimal? FinalFare { get; set; }
    /// <summary>Driver's net share of <see cref="FinalFare"/>, fixed when the trip completes.</summary>
    public decimal? DriverEarnings { get; set; }
    public int WaitingSeconds { get; set; }
    /// <summary>Final fare breakdown (F10 breakdown + <c>discounts</c>) stored at completion, JSON.</summary>
    public string? FareBreakdown { get; set; }
    /// <summary>Sum of discounts borne by the platform (F15/F16); the passenger pays <c>final_fare</c>, the driver share is computed before discounts.</summary>
    public decimal DiscountTotal { get; set; }
    /// <summary>F15: the driver tier's commission discount applied to <see cref="DriverEarnings"/> at completion (0 for bronze).</summary>
    public decimal TierCommissionDiscountPercent { get; set; }
    /// <summary>F15: when <c>rating.reminder</c> was sent to the parties that had not rated yet (once per trip).</summary>
    public DateTime? RatingRemindedAt { get; set; }
    /// <summary>F16: the favourite driver the passenger asked for (exclusive first offer), fixed at request time.</summary>
    public Guid? FavoriteDriverId { get; set; }
    public FavoriteStatus? FavoriteStatus { get; set; }
    /// <summary>F16: the <c>favorite_driver_discount_rules</c> row pinned when the favourite driver accepted (null = no discount).</summary>
    public Guid? FavoriteDiscountRuleId { get; set; }
    /// <summary>F17: the driver holding a reservation on a <c>scheduled</c> trip (set atomically by the reservation; <see cref="DriverId"/> stays empty until the final confirmation).</summary>
    public Guid? ReservedDriverId { get; set; }
    /// <summary>F17 airport trips: the airport whose geofence contains the pickup (<c>pickup</c>) or the dropoff (<c>dropoff</c>).</summary>
    public Guid? AirportId { get; set; }
    public Airports.AirportDirection? AirportDirection { get; set; }
    /// <summary>The pickup zone chosen for an airport pickup; its coordinates replace the requested pickup.</summary>
    public Guid? AirportZoneId { get; set; }
    public string? TerminalCode { get; set; }
    /// <summary>Stored only (no flight-data integration in v1), uppercase without spaces.</summary>
    public string? FlightNumber { get; set; }
    /// <summary>JSON <see cref="WaitingPolicy"/> fixed at creation (airport pickups).</summary>
    public string? WaitingPolicy { get; set; }
    public required string PinCodeHash { get; set; }
    /// <summary>The PIN protected at rest so it can be shown to the passenger; verification uses <see cref="PinCodeHash"/>.</summary>
    public required string PinCodeProtected { get; set; }
    public int PinAttempts { get; set; }
    public CancelledBy? CancelledBy { get; set; }
    public string? CancellationReason { get; set; }
    public string? RiderNote { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? AssignedAt { get; set; }
    public DateTime? ArrivedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    /// <summary>F12: planned route as a JSON array <c>[[lat,lng],…]</c> (straight segments pickup → stops → dropoff until a maps provider exists).</summary>
    public string? PlannedRoute { get; set; }
    public Safety.PlannedRouteSource PlannedRouteSource { get; set; } = Safety.PlannedRouteSource.Straight;

    public ICollection<TripStop> Stops { get; set; } = [];
    public ICollection<TripEvent> Events { get; set; } = [];

    public static readonly TripStatus[] TerminalStatuses = [TripStatus.Completed, TripStatus.Cancelled, TripStatus.NoDrivers];

    /// <summary>The passenger's / driver's active trip statuses: every non-terminal status except <see cref="TripStatus.Scheduled"/> (a booked trip is not an active trip).</summary>
    public static readonly TripStatus[] ActiveStatuses =
        Enum.GetValues<TripStatus>().Where(s => !TerminalStatuses.Contains(s) && s != TripStatus.Scheduled).ToArray();

    /// <summary>Every non-terminal status including <see cref="TripStatus.Scheduled"/> (resources such as a saved card stay in use).</summary>
    public static readonly TripStatus[] OpenStatuses =
        Enum.GetValues<TripStatus>().Where(s => !TerminalStatuses.Contains(s)).ToArray();

    public bool IsTerminal => TerminalStatuses.Contains(Status);

    public bool HasDriver => DriverId is not null && Status is not TripStatus.Requested and not TripStatus.Searching and not TripStatus.NoDrivers;

    public bool IsScheduledBooking => BookingType == BookingType.Scheduled;

    public bool CanBeCancelled => Status is not TripStatus.InTrip && !IsTerminal;

    public void StartSearching()
    {
        Transition(TripStatus.Searching, TripStatus.Requested);
    }

    /// <summary>F17: <c>scheduled → searching</c> when the normal search window opens (or a reserved driver failed the final confirmation).</summary>
    public void StartScheduledSearch()
    {
        Transition(TripStatus.Searching, TripStatus.Scheduled);
    }

    /// <summary>
    /// F17: a scheduled trip whose driver cancelled or did not show up goes back to matching instead of being cancelled (doc 11 §F17.1): the driver, vehicle,
    /// assignment time, arrival and reservation are cleared. Only from <c>driver_assigned</c> / <c>driver_en_route</c>.
    /// </summary>
    public void Reassign()
    {
        if (!IsScheduledBooking)
        {
            throw new DomainException(ErrorCodes.Conflict, new { status = Status, bookingType = BookingType });
        }

        Transition(TripStatus.Searching, TripStatus.DriverAssigned, TripStatus.DriverEnRoute);
        DriverId = null;
        VehicleId = null;
        AssignedAt = null;
        ArrivedAt = null;
        ReservedDriverId = null;
        PinAttempts = 0;
    }

    public void MarkNoDrivers(DateTime now)
    {
        Transition(TripStatus.NoDrivers, TripStatus.Searching);
        CancelledBy = Trips.CancelledBy.System;
        CancellationReason = "no_drivers";
        CancelledAt = now;
    }

    public void Assign(Guid driverId, Guid? vehicleId, DateTime now)
    {
        Transition(TripStatus.DriverAssigned, TripStatus.Searching, TripStatus.Scheduled);
        DriverId = driverId;
        VehicleId = vehicleId;
        AssignedAt = now;
    }

    public void MarkEnRoute()
    {
        Transition(TripStatus.DriverEnRoute, TripStatus.DriverAssigned);
    }

    /// <summary>driver_en_route → driver_arrived → waiting: the free waiting timer starts immediately.</summary>
    public void MarkArrived(DateTime now)
    {
        Transition(TripStatus.DriverArrived, TripStatus.DriverEnRoute, TripStatus.DriverAssigned);
        ArrivedAt = now;
        Status = TripStatus.Waiting;
    }

    public int PinAttemptsLeft(int maxAttempts) => Math.Max(0, maxAttempts - PinAttempts);

    /// <summary>Applies one PIN attempt. Throws pin_locked / pin_invalid; on success computes billable waiting seconds.</summary>
    public void VerifyPin(bool hashMatches, int maxAttempts, int freeWaitingSeconds, DateTime now)
    {
        EnsureStatus(TripStatus.Waiting);
        if (PinAttempts >= maxAttempts)
        {
            throw new DomainException(ErrorCodes.PinLocked, new { attemptsLeft = 0 });
        }

        PinAttempts++;
        if (!hashMatches)
        {
            var left = PinAttemptsLeft(maxAttempts);
            throw new DomainException(left == 0 ? ErrorCodes.PinLocked : ErrorCodes.PinInvalid, new { attemptsLeft = left });
        }

        var waited = ArrivedAt is null ? 0 : (int)Math.Max(0, (now - ArrivedAt.Value).TotalSeconds);
        WaitingSeconds = Math.Max(0, waited - freeWaitingSeconds);
        Status = TripStatus.PinVerified;
    }

    public void Start(DateTime now)
    {
        Transition(TripStatus.InTrip, TripStatus.PinVerified);
        StartedAt = now;
    }

    public void Complete(int finalDistanceM, int finalDurationS, decimal finalFare, decimal driverEarnings, DateTime now)
    {
        Transition(TripStatus.Completed, TripStatus.InTrip);
        FinalDistanceM = finalDistanceM;
        FinalDurationS = finalDurationS;
        FinalFare = finalFare;
        DriverEarnings = driverEarnings;
        CompletedAt = now;
    }

    public void Cancel(CancelledBy by, string reason, DateTime now)
    {
        if (!CanBeCancelled)
        {
            throw new DomainException(ErrorCodes.Conflict, new { status = Status });
        }

        Status = TripStatus.Cancelled;
        CancelledBy = by;
        CancellationReason = reason;
        CancelledAt = now;
    }

    public void EnsureStatus(params TripStatus[] allowed)
    {
        if (!allowed.Contains(Status))
        {
            throw new DomainException(ErrorCodes.Conflict, new { status = Status, allowed });
        }
    }

    private void Transition(TripStatus target, params TripStatus[] allowedFrom)
    {
        if (!allowedFrom.Contains(Status))
        {
            throw new DomainException(ErrorCodes.Conflict, new { status = Status, target });
        }

        Status = target;
    }
}
