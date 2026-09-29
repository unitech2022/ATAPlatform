namespace ATA.Domain.Trips;

public enum TripStatus
{
    Requested,
    Searching,
    DriverAssigned,
    DriverEnRoute,
    DriverArrived,
    Waiting,
    PinVerified,
    InTrip,
    Completed,
    Cancelled,
    NoDrivers,
}

public enum BookingType { Now, Scheduled }

public enum PricingMode { Fixed, Saver, Offer }

public enum OfferStatus { Sent, Accepted, Rejected, Expired }

/// <summary>F16 <c>trips.favorite_status</c>: how the passenger's favourite-driver request went (null = no favourite requested).</summary>
public enum FavoriteStatus { Requested, Accepted, Unavailable, Rejected, Expired }

public enum TripActor { Passenger, Driver, System, Admin }

public enum CancelledBy { Passenger, Driver, System, Admin }

/// <summary>Event type names written to <c>trip_events.type</c>.</summary>
public static class TripEventTypes
{
    public const string Requested = "requested";
    public const string SearchStarted = "search_started";
    public const string OfferSent = "offer_sent";
    public const string OfferAccepted = "offer_accepted";
    public const string OfferRejected = "offer_rejected";
    public const string OfferExpired = "offer_expired";
    public const string DriverAssigned = "driver_assigned";
    public const string DriverEnRoute = "driver_en_route";
    public const string DriverArrived = "driver_arrived";
    public const string WaitingStarted = "waiting_started";
    public const string ArrivalDistanceWarning = "arrival_distance_warning";
    public const string PinFailed = "pin_failed";
    public const string PinVerified = "pin_verified";
    public const string Started = "started";
    public const string Completed = "completed";
    public const string PaymentRecorded = "payment_recorded";
    public const string PaymentFallbackCash = "payment_fallback_cash";
    public const string PaymentAuthorized = "payment_authorized";
    public const string PaymentActionRequired = "payment_action_required";
    public const string PaymentFailed = "payment_failed";
    public const string PaymentCapturePending = "payment_capture_pending";
    public const string Cancelled = "cancelled";
    public const string NoDrivers = "no_drivers";
    public const string FavoriteAccepted = "favorite_accepted";
    public const string FavoriteUnavailable = "favorite_unavailable";
    public const string FavoriteFallback = "favorite_fallback";
    public const string PassengerNoShow = "passenger_no_show";
    public const string CancellationFeeCharged = "cancellation_fee_charged";
}
