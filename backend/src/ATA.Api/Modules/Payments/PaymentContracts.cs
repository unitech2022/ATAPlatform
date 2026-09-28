using ATA.Domain.Payments;
using ATA.Domain.Wallet;

namespace ATA.Api.Modules.Payments;

public sealed record ApplePayConfigDto(bool Enabled, string? MerchantId);

public sealed record PaymentConfigDto(string Provider, string? PublishableKey, string Currency, IReadOnlyList<string> SupportedBrands, ApplePayConfigDto ApplePay, IReadOnlyList<string>? SandboxTokens);

public sealed record PaymentActionDto(string Type, string Url, DateTime? ExpiresAt);

public sealed record PaymentCardRefDto(string Brand, string Last4);

public sealed record PaymentDto(
    Guid Id, PaymentPurpose Purpose, PaymentStatus Status, PaymentInstrument Method, decimal Amount, decimal? AuthorizedAmount, decimal? CapturedAmount,
    decimal RefundedAmount, string Currency, PaymentCardRefDto? Card, PaymentActionDto? Action, string? FailureCode, string? FailureMessage,
    Guid? TripId, DateTime CreatedAt, DateTime? CapturedAt);

/// <summary><c>PaymentUpdated</c> SignalR event for the payment owner.</summary>
public sealed record PaymentUpdatedEvent(Guid PaymentId, PaymentPurpose Purpose, PaymentStatus Status, Guid? TripId, string? FailureCode);

public sealed record PayoutRequestedEvent(Guid PayoutId, string? DriverName, decimal Amount);

public sealed record SavedCardDto(Guid Id, string Type, string Brand, string Last4, int ExpiryMonth, int ExpiryYear, string? HolderName,
    SavedCardStatus Status, bool IsDefault, bool IsExpired, DateTime CreatedAt);

public sealed record AddCardRequest(string? Token, bool? SetDefault, string? ReturnUrl);

public sealed record AddCardPendingDto(SavedCardDto PaymentMethod, PaymentActionDto Action);

/// <summary><c>trip.payment</c> in the Trip object.</summary>
public sealed record TripPaymentDto(Guid Id, PaymentStatus Status, PaymentInstrument Method, string? Brand, string? Last4, decimal? AuthorizedAmount, decimal? CapturedAmount, PaymentActionDto? Action);

// ----- receipts -----

public sealed record ReceiptPlaceDto(string Name, string Address, decimal Lat, decimal Lng);

public sealed record ReceiptLineDto(string Code, string Label, decimal Amount, string? Source = null, string? Reference = null);

public sealed record DiscountDto(string Source, string? Reference, string Label, decimal Amount);

public sealed record ReceiptPaymentDto(string Method, string? Brand, string? Last4, string? Status, decimal PaidAmount, bool FallbackToCash);

public sealed record ReceiptRefundDto(Guid Id, decimal Amount, RefundStatus Status, RefundDestination Destination, DateTime CreatedAt);

public sealed record ReceiptDto(
    Guid TripId, string TripNumber, string Status, DateTime IssuedAt, string Currency, string? PassengerName, string? DriverName, string? Vehicle, string RideCategory,
    ReceiptPlaceDto Pickup, ReceiptPlaceDto Dropoff, IReadOnlyList<ReceiptPlaceDto> Stops, DateTime? StartedAt, DateTime? CompletedAt,
    int DistanceMeters, int DurationSeconds, int WaitingSeconds, IReadOnlyList<ReceiptLineDto> Lines, IReadOnlyList<DiscountDto> Discounts,
    decimal Subtotal, decimal DiscountTotal, decimal Total, decimal VatRate, decimal VatIncluded, ReceiptPaymentDto Payment,
    IReadOnlyList<ReceiptRefundDto> Refunds, decimal NetPaid);

/// <summary>Stored in <c>trips.fare_breakdown</c>: the F10 breakdown plus <c>discounts</c> and the min-fare adjustment used by receipts.</summary>
public sealed record StoredFareBreakdown(
    decimal BaseFare, decimal DistanceFare, decimal TimeFare, decimal WaitingFare, bool MinFareApplied, decimal TimeMultiplier, string? TimeMultiplierLabel,
    decimal DemandMultiplier, decimal BookingFee, decimal ServiceFee, decimal Discount, IReadOnlyList<DiscountDto> Discounts, decimal MinFareAdjustment, string PricingSource);

// ----- refunds -----

public sealed record CreateRefundRequest(decimal? Amount, RefundReasonCode? ReasonCode, string? Reason, RefundDestination? Destination);

public sealed record RefundDto(
    Guid Id, string RefundNumber, Guid? PaymentId, Guid? TripId, string? TripNumber, Guid UserId, string? UserName, string? UserPhone, decimal Amount, RefundType Type,
    RefundDestination Destination, RefundReasonCode ReasonCode, string Reason, RefundStatus Status, Guid RequestedBy, string? RequestedByName, Guid? ApprovedBy,
    string? ApprovedByName, DateTime? ApprovedAt, Guid? RejectedBy, string? RejectedByName, string? RejectedReason, string? GatewayRefundId, string? FailureMessage,
    DateTime? ProcessedAt, DateTime CreatedAt);

public sealed record ReasonBody(string? Reason);

// ----- payouts -----

public sealed record PayoutRequest(decimal? Amount);

public sealed record PayoutDto(
    Guid Id, string PayoutNumber, decimal Amount, string IbanMasked, PayoutStatus Status, DateTime RequestedAt, DateTime? ApprovedAt, DateTime? PaidAt,
    string? RejectedReason, string? BankReference);

public sealed record AdminPayoutDto(
    Guid Id, string PayoutNumber, Guid DriverId, string? DriverName, string DriverPhone, decimal Amount, string IbanMasked, string AccountHolderName,
    PayoutStatus Status, Guid? BatchId, string? BatchNumber, DateTime RequestedAt, DateTime? ApprovedAt, DateTime? PaidAt, string? RejectedReason, string? BankReference);

public sealed record PayoutSummaryDto(decimal Balance, decimal CashDebt, decimal AvailableForPayout, decimal MinPayoutAmount, PayoutDto? PendingPayout,
    string? IbanMasked, bool CanRequest, string? Reason);

public sealed record MarkPaidRequest(string? BankReference, DateTime? PaidAt);

public sealed record CreatePayoutBatchRequest(List<Guid>? PayoutIds, bool? AllApproved);

public sealed record PayoutBatchDto(Guid Id, string BatchNumber, PayoutBatchStatus Status, int PayoutsCount, decimal TotalAmount, DateTime? ExportedAt,
    string? ExportedByName, string? BankReference, DateTime? PaidAt, string? PaidByName, string? CreatedByName, DateTime CreatedAt, IReadOnlyList<AdminPayoutDto>? Payouts);

// ----- settlements -----

public sealed record GenerateSettlementRequest(DateTime? PeriodStart, DateTime? PeriodEnd, Guid? CityId);

public sealed record SettlementBatchDto(
    Guid Id, string BatchNumber, Guid? CityId, string? CityName, DateTime PeriodStart, DateTime PeriodEnd, SettlementBatchStatus Status, int DriversCount, int TotalTrips,
    decimal TotalGrossFares, decimal TotalEarnings, decimal TotalCommission, decimal TotalCashCollected, decimal TotalIncentives, decimal TotalCompensation,
    decimal TotalAdjustments, decimal TotalNet, string? Error, string? GeneratedByName, DateTime GeneratedAt, string? FinalizedByName, DateTime? FinalizedAt, DateTime CreatedAt);

public sealed record SettlementDto(
    Guid Id, Guid BatchId, string BatchNumber, DateTime PeriodStart, DateTime PeriodEnd, Guid DriverId, string? DriverName, string? DriverPhone,
    int TripsCount, decimal GrossFares, decimal Earnings, decimal Commission, decimal CashCollected, decimal Incentives, decimal CancellationCompensation,
    decimal Adjustments, decimal Fees, decimal Topups, decimal PayoutsInPeriod, decimal NetAmount, decimal OpeningBalance, decimal ClosingBalance,
    SettlementDirection Direction, Guid? PayoutId, SettlementStatus Status, DateTime CreatedAt);

public sealed record DriverSettlementItemDto(Guid Id, string BatchNumber, DateTime PeriodStart, DateTime PeriodEnd, int TripsCount, decimal Earnings,
    decimal CashCollected, decimal NetAmount, decimal ClosingBalance, SettlementDirection Direction, SettlementStatus Status);

// ----- admin payments / wallets / ledger -----

public sealed record AdminPaymentListItemDto(Guid Id, PaymentPurpose Purpose, PaymentStatus Status, PaymentInstrument Method, string Provider, decimal Amount,
    decimal? CapturedAmount, decimal RefundedAmount, string? UserName, string UserPhone, string? TripNumber, string? GatewayPaymentId, DateTime CreatedAt);

public sealed record WebhookEventDto(Guid Id, string Provider, string EventId, string EventType, string? GatewayPaymentId, bool SignatureValid, System.Text.Json.JsonElement? Payload,
    WebhookProcessingStatus ProcessingStatus, string? Error, DateTime ReceivedAt, DateTime? ProcessedAt);

/// <summary>A ledger line tied to a payment; <c>Type</c> is the wallet transaction type or the journal type.</summary>
public sealed record LedgerEntryDto(Guid Id, Guid? TransactionId, Guid? JournalId, string Account, decimal Debit, decimal Credit, string? Type, string? Description, DateTime CreatedAt);

public sealed record AdminPaymentDetailDto(
    Guid Id, Guid UserId, string? UserName, string UserPhone, PaymentPurpose Purpose, PaymentStatus Status, PaymentInstrument Method, string Provider,
    string Currency, decimal Amount, decimal? AuthorizedAmount, decimal? CapturedAmount, decimal RefundedAmount, CaptureMode CaptureMode, Guid? TripId,
    string? TripNumber, Guid? WalletId, Guid? PaymentMethodId, PaymentCardRefDto? Card, string? GatewayPaymentId, string? GatewayStatus, string? ActionUrl,
    DateTime? ActionExpiresAt, string? FailureCode, string? FailureMessage, string IdempotencyKey, DateTime? AuthorizedAt, DateTime? CapturedAt, DateTime? FailedAt,
    DateTime? VoidedAt, DateTime CreatedAt, DateTime UpdatedAt, System.Text.Json.JsonElement? Metadata,
    IReadOnlyList<WebhookEventDto> WebhookEvents, IReadOnlyList<RefundDto> Refunds, IReadOnlyList<LedgerEntryDto> Ledger);

/// <summary>Wallet row; the owner's name/phone are also exposed as <c>name</c>/<c>phoneNumber</c> for the dashboard.</summary>
public sealed record AdminWalletDto(Guid Id, Guid UserId, string? UserName, string Phone, WalletKind Kind, decimal Balance, WalletStatus Status)
{
    public string? Name => UserName;
    public string PhoneNumber => Phone;
}

public sealed record AdminWalletTransactionDto(Guid Id, TransactionType Type, TransactionDirection Direction, decimal Amount, decimal BalanceAfter,
    string? Description, string? ReferenceType, Guid? ReferenceId, string? CreatedByName, DateTime CreatedAt);

public sealed record AdminWalletDetailDto(Guid Id, Guid UserId, string? UserName, string Phone, WalletKind Kind, decimal Balance, WalletStatus Status,
    string Currency, decimal CashDebt, DateTime CreatedAt, IReadOnlyList<AdminWalletTransactionDto> Transactions)
{
    public string? Name => UserName;
    public string PhoneNumber => Phone;
}

public sealed record WalletAdjustmentRequest(TransactionDirection? Direction, decimal? Amount, string? Reason);

public sealed record LedgerBalanceDto(string Account, decimal Debit, decimal Credit, decimal Balance);

// ----- driver earnings -----

public sealed record EarningsTotalsDto(int Trips, decimal GrossFares, decimal Commission, decimal Earnings, decimal CashCollected, decimal Incentives,
    decimal CancellationCompensation, decimal Adjustments, decimal Payouts, decimal Net);

public sealed record EarningsDayDto(DateOnly Date, int Trips, decimal Earnings, decimal CashCollected, decimal Incentives, double OnlineHours);

public sealed record EarningsStatementDto(DateOnly From, DateOnly To, EarningsTotalsDto Totals, IReadOnlyList<EarningsDayDto> Days);

public sealed record TripEarningsDto(Guid TripId, decimal Fare, decimal DiscountTotal, decimal GrossFare, decimal Commission, decimal CommissionPercent,
    decimal TierCommissionDiscountPercent, decimal DriverEarnings, string PaymentMethod, decimal CashCollected);
