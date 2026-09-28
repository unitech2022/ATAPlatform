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

    public static readonly TripStatus[] ActiveStatuses =
        Enum.GetValues<TripStatus>().Where(s => !TerminalStatuses.Contains(s)).ToArray();

    public bool IsTerminal => TerminalStatuses.Contains(Status);

    public bool HasDriver => DriverId is not null && Status is not TripStatus.Requested and not TripStatus.Searching and not TripStatus.NoDrivers;

    public bool CanBeCancelled => Status is not TripStatus.InTrip && !IsTerminal;

    public void StartSearching()
    {
        Transition(TripStatus.Searching, TripStatus.Requested);
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
        Transition(TripStatus.DriverAssigned, TripStatus.Searching);
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
