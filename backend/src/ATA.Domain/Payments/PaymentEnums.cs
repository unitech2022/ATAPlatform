namespace ATA.Domain.Payments;

public static class PaymentProviders
{
    public const string Sandbox = "sandbox";
    public const string Moyasar = "moyasar";
}

public enum PaymentPurpose { Trip, Topup, CancellationFee }

/// <summary><c>payments.method</c>: the instrument used for the charge.</summary>
public enum PaymentInstrument { Card, ApplePay, Sandbox }

public enum PaymentStatus { Initiated, Authorized, Captured, Failed, Voided, Refunded, PartiallyRefunded }

public enum CaptureMode { Manual, Auto }

public enum SavedCardStatus { PendingVerification, Active, Failed, Removed }

public enum WebhookProcessingStatus { Pending, Processed, Ignored, Failed }

public enum RefundType { Full, Partial }

public enum RefundDestination { OriginalMethod, Wallet }

public enum RefundReasonCode { FareDispute, TripNotTaken, DuplicateCharge, ServiceIssue, CancellationFeeWaived, Goodwill, Other }

public enum RefundStatus { PendingApproval, Approved, Processing, Succeeded, Failed, Rejected }

public enum PayoutStatus { Requested, Approved, Paid, Rejected, Cancelled }

public enum PayoutBatchStatus { Open, Exported, Paid }

public enum SettlementBatchStatus { Generating, Ready, Finalized, Failed }

public enum SettlementDirection { PayableToDriver, DueFromDriver, Zero }

public enum SettlementStatus { Open, Finalized }
