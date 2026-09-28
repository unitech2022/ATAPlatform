using ATA.Domain.Common;

namespace ATA.Domain.Payments;

/// <summary>
/// A gateway payment (<c>payments</c>). Transitions are one-way and idempotent:
/// <c>initiated → authorized → captured → partially_refunded → refunded</c>, <c>initiated → failed</c>, <c>authorized → voided|failed</c>.
/// A stale or repeated state (e.g. a late webhook) is rejected by <see cref="CanMoveTo"/> and changes nothing.
/// </summary>
public class Payment : AuditableEntity
{
    public const string DefaultCurrency = "SAR";

    public Guid UserId { get; set; }
    public PaymentPurpose Purpose { get; set; }
    /// <summary>Plain column (no FK): the payment of a trip is authorized before the trip row is inserted.</summary>
    public Guid? TripId { get; set; }
    public Guid? WalletId { get; set; }
    public Guid? PaymentMethodId { get; set; }
    public PaymentInstrument Method { get; set; }
    public required string Provider { get; set; }
    public string Currency { get; set; } = DefaultCurrency;
    public decimal Amount { get; set; }
    public decimal? AuthorizedAmount { get; set; }
    public decimal? CapturedAmount { get; set; }
    public decimal RefundedAmount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Initiated;
    public CaptureMode CaptureMode { get; set; }
    public string? GatewayPaymentId { get; set; }
    public string? GatewayStatus { get; set; }
    public string? ActionUrl { get; set; }
    public string? ReturnUrl { get; set; }
    public DateTime? ActionExpiresAt { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureMessage { get; set; }
    public required string IdempotencyKey { get; set; }
    public DateTime? AuthorizedAt { get; set; }
    public DateTime? CapturedAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public DateTime? VoidedAt { get; set; }
    /// <summary>JSON object (e.g. <c>{"capturePending":true,"captureAttempts":2,"amountDue":46.5}</c>).</summary>
    public string? Metadata { get; set; }

    public bool IsTerminal => Status is PaymentStatus.Failed or PaymentStatus.Voided or PaymentStatus.Refunded;

    public decimal Refundable => Math.Max(0m, (CapturedAmount ?? 0m) - RefundedAmount);

    private static int Rank(PaymentStatus status) => status switch
    {
        PaymentStatus.Initiated => 0,
        PaymentStatus.Authorized => 1,
        PaymentStatus.Captured => 2,
        PaymentStatus.PartiallyRefunded => 3,
        PaymentStatus.Refunded => 4,
        _ => 5,
    };

    /// <summary>Whether moving to <paramref name="target"/> is a forward transition (older or repeated states are ignored).</summary>
    public bool CanMoveTo(PaymentStatus target)
    {
        if (target == Status || IsTerminal)
        {
            return false;
        }

        return target switch
        {
            PaymentStatus.Failed => Status is PaymentStatus.Initiated or PaymentStatus.Authorized,
            PaymentStatus.Voided => Status == PaymentStatus.Authorized,
            _ => Rank(target) > Rank(Status),
        };
    }

    public void MarkAuthorized(decimal amount, DateTime now)
    {
        Status = PaymentStatus.Authorized;
        AuthorizedAmount = amount;
        AuthorizedAt = now;
        ActionUrl = null;
        ActionExpiresAt = null;
    }

    public void MarkCaptured(decimal amount, DateTime now)
    {
        Status = PaymentStatus.Captured;
        CapturedAmount = amount;
        AuthorizedAmount ??= amount;
        AuthorizedAt ??= now;
        CapturedAt = now;
        ActionUrl = null;
        ActionExpiresAt = null;
    }

    public void MarkFailed(string? code, string? message, DateTime now)
    {
        Status = PaymentStatus.Failed;
        FailureCode = code;
        FailureMessage = message is { Length: > 500 } ? message[..500] : message;
        FailedAt = now;
        ActionUrl = null;
    }

    public void MarkVoided(DateTime now)
    {
        Status = PaymentStatus.Voided;
        VoidedAt = now;
    }

    public void ApplyRefund(decimal amount)
    {
        RefundedAmount += amount;
        Status = RefundedAmount >= (CapturedAmount ?? 0m) ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;
    }
}

/// <summary>Raw gateway webhook (<c>payment_webhook_events</c>); <c>UNIQUE(provider, event_id)</c> makes processing exactly-once.</summary>
public class PaymentWebhookEvent
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public required string Provider { get; set; }
    public required string EventId { get; set; }
    public required string EventType { get; set; }
    public string? GatewayPaymentId { get; set; }
    public bool SignatureValid { get; set; }
    public required string Payload { get; set; }
    public WebhookProcessingStatus ProcessingStatus { get; set; } = WebhookProcessingStatus.Pending;
    public string? Error { get; set; }
    public int Attempts { get; set; }
    public DateTime ReceivedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
