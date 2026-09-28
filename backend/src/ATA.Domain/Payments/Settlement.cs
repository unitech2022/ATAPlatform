using ATA.Domain.Common;

namespace ATA.Domain.Payments;

/// <summary>A settlement statement run for a half-open period <c>[PeriodStart, PeriodEnd)</c> (<c>settlement_batches</c>).</summary>
public class SettlementBatch : AuditableEntity
{
    public required string BatchNumber { get; set; }
    public Guid? CityId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public SettlementBatchStatus Status { get; set; } = SettlementBatchStatus.Generating;
    public int DriversCount { get; set; }
    public int TotalTrips { get; set; }
    public decimal TotalGrossFares { get; set; }
    public decimal TotalEarnings { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal TotalCashCollected { get; set; }
    public decimal TotalIncentives { get; set; }
    public decimal TotalCompensation { get; set; }
    public decimal TotalAdjustments { get; set; }
    public decimal TotalNet { get; set; }
    public string? Error { get; set; }
    public Guid? GeneratedBy { get; set; }
    public DateTime GeneratedAt { get; set; }
    public Guid? FinalizedBy { get; set; }
    public DateTime? FinalizedAt { get; set; }
}

/// <summary>One driver's statement inside a <see cref="SettlementBatch"/>. Invariant: <c>closing = opening + net + topups − fees − payouts</c>.</summary>
public class Settlement : Entity
{
    public Guid BatchId { get; set; }
    public Guid DriverId { get; set; }
    public int TripsCount { get; set; }
    public decimal GrossFares { get; set; }
    public decimal Earnings { get; set; }
    public decimal Commission { get; set; }
    public decimal CashCollected { get; set; }
    public decimal Incentives { get; set; }
    public decimal CancellationCompensation { get; set; }
    public decimal Adjustments { get; set; }
    public decimal Fees { get; set; }
    public decimal Topups { get; set; }
    public decimal PayoutsInPeriod { get; set; }
    public decimal NetAmount { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public SettlementDirection Direction { get; set; }
    public Guid? PayoutId { get; set; }
    public SettlementStatus Status { get; set; } = SettlementStatus.Open;
}
