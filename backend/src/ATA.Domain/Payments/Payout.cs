using ATA.Domain.Common;

namespace ATA.Domain.Payments;

/// <summary>A driver withdrawal to a bank account (<c>payouts</c>); the wallet is debited when requested.</summary>
public class Payout : AuditableEntity
{
    public required string PayoutNumber { get; set; }
    public Guid DriverId { get; set; }
    public Guid WalletId { get; set; }
    public decimal Amount { get; set; }
    public required string IbanMasked { get; set; }
    /// <summary>IBAN snapshot protected with ASP.NET Data Protection.</summary>
    public required string IbanEncrypted { get; set; }
    public required string AccountHolderName { get; set; }
    public PayoutStatus Status { get; set; } = PayoutStatus.Requested;
    public Guid? BatchId { get; set; }
    public required string IdempotencyKey { get; set; }
    public DateTime RequestedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? PaidBy { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? BankReference { get; set; }
    public Guid? RejectedBy { get; set; }
    public string? RejectedReason { get; set; }
    public DateTime? CancelledAt { get; set; }

    public void Approve(Guid adminUserId, DateTime now)
    {
        Ensure(PayoutStatus.Requested);
        Status = PayoutStatus.Approved;
        ApprovedBy = adminUserId;
        ApprovedAt = now;
    }

    public void Reject(Guid adminUserId, string reason)
    {
        Ensure(PayoutStatus.Requested, PayoutStatus.Approved);
        Status = PayoutStatus.Rejected;
        RejectedBy = adminUserId;
        RejectedReason = reason;
    }

    public void Cancel(DateTime now)
    {
        Ensure(PayoutStatus.Requested);
        Status = PayoutStatus.Cancelled;
        CancelledAt = now;
    }

    public void MarkPaid(Guid adminUserId, string bankReference, DateTime paidAt)
    {
        Ensure(PayoutStatus.Approved);
        Status = PayoutStatus.Paid;
        PaidBy = adminUserId;
        PaidAt = paidAt;
        BankReference = bankReference;
    }

    private void Ensure(params PayoutStatus[] allowed)
    {
        if (!allowed.Contains(Status))
        {
            throw new DomainException(ErrorCodes.Conflict, new { status = Status });
        }
    }
}

public class PayoutBatch : AuditableEntity
{
    public required string BatchNumber { get; set; }
    public PayoutBatchStatus Status { get; set; } = PayoutBatchStatus.Open;
    public int PayoutsCount { get; set; }
    public decimal TotalAmount { get; set; }
    public Guid? ExportFileId { get; set; }
    public Guid? ExportedBy { get; set; }
    public DateTime? ExportedAt { get; set; }
    public string? BankReference { get; set; }
    public Guid? PaidBy { get; set; }
    public DateTime? PaidAt { get; set; }
    public Guid CreatedBy { get; set; }
}
