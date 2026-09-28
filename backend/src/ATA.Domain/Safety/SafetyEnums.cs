namespace ATA.Domain.Safety;

public enum TripShareChannel { Link, Sms, Auto }

public enum SafetyCaseType { Sos, UnexpectedStop, RouteDeviation, TripOverrun, SafetyReport }

public enum SafetyCaseSource { RiderSos, DriverSos, Alert, Report, Support, Admin }

public enum SafetyPriority { Critical, High, Medium, Low }

public enum SafetyCaseStatus { Open, InProgress, Escalated, Resolved }

public enum SafetyReporterRole { Passenger, Driver, System, Admin }

public enum SafetyReportCategory { UnsafeDriving, Harassment, VehicleMismatch, DriverMismatch, PassengerMisconduct, Other }

public enum SafetyEscalationTarget { Police, Ambulance, CivilDefense, Management, Other }

public enum SafetyResolutionCode { FalseAlarm, ResolvedContacted, EscalatedAuthorities, ActionTakenDriver, ActionTakenPassenger, NoAction, Other }

public enum SafetyNoteKind { Note, StatusChange, Assignment, ContactAttempt, System }

public enum SafetyAlertType { UnexpectedStop, RouteDeviation, TripOverrun }

public enum SafetyAlertStatus { PendingRider, ResolvedOk, Escalated, NoResponse, Dismissed }

public enum SafetyAlertResponse { Ok, NeedHelp }

public enum TripMessageSender { Passenger, Driver, System }

public enum TripMessageKind { Text, QuickReply, System }

public enum LostItemCategory { Phone, Wallet, Bag, Keys, Documents, Other }

public enum LostItemStatus { Open, DriverContacted, Found, Returned, NotFound, Closed }

public enum LostItemDriverResponse { Found, NotFound }

public enum PlannedRouteSource { Straight, Maps }

public static class SafetyPriorities
{
    /// <summary>Sort rank (critical first).</summary>
    public static int Rank(SafetyPriority priority) => priority switch
    {
        SafetyPriority.Critical => 0,
        SafetyPriority.High => 1,
        SafetyPriority.Medium => 2,
        _ => 3,
    };
}
