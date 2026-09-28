using ATA.Domain.Common;

namespace ATA.Domain.Payments;

/// <summary>A refund to the original card or the passenger wallet (<c>refunds</c>), approved under the four-eyes rule above the limit.</summary>
public class Refund : AuditableEntity
{
    public required string RefundNumber { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? TripId { get; set; }
    public Guid UserId { get; set; }
    public decimal Amount { get; set; }
    public RefundType Type { get; set; }
    public RefundDestination Destination { get; set; }
    public RefundReasonCode ReasonCode { get; set; }
    public required string Reason { get; set; }
    public RefundStatus Status { get; set; } = RefundStatus.PendingApproval;
    public Guid RequestedBy { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? RejectedBy { get; set; }
    public string? RejectedReason { get; set; }
    public string? GatewayRefundId { get; set; }
    public string? FailureMessage { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public Guid? DisputeId { get; set; }

    /// <summary>Refunds still counting against the refundable amount (not rejected or failed).</summary>
    public static readonly RefundStatus[] OpenStatuses = [RefundStatus.PendingApproval, RefundStatus.Approved, RefundStatus.Processing, RefundStatus.Succeeded];

    public void Approve(Guid approverUserId, DateTime now)
    {
        if (Status != RefundStatus.PendingApproval)
        {
            throw new DomainException(ErrorCodes.Conflict, new { status = Status });
        }

        if (approverUserId == RequestedBy)
        {
            throw new DomainException(ErrorCodes.FourEyesRequired, new { requestedBy = RequestedBy });
        }

        Status = RefundStatus.Approved;
        ApprovedBy = approverUserId;
        ApprovedAt = now;
    }

    public void Reject(Guid userId, string reason)
    {
        if (Status is not (RefundStatus.PendingApproval or RefundStatus.Failed))
        {
            throw new DomainException(ErrorCodes.Conflict, new { status = Status });
        }

        Status = RefundStatus.Rejected;
        RejectedBy = userId;
        RejectedReason = reason;
    }
}
