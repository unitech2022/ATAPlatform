import 'package:ata_app/features/driver_wallet/domain/entities/payout.dart';
import 'package:equatable/equatable.dart';

/// `GET /driver/payouts/summary`.
class PayoutSummary extends Equatable {
  const PayoutSummary({
    this.balance = 0,
    this.cashDebt = 0,
    this.availableForPayout = 0,
    this.minPayoutAmount = defaultMinPayout,
    this.cashDebtLimit = defaultCashDebtLimit,
    this.pendingPayout,
    this.ibanMasked,
    this.canRequest = false,
    this.reason,
  });

  /// `Payouts:MinAmount` / `Payouts:MaxCashDebt` defaults.
  static const double defaultMinPayout = 100;
  static const double defaultCashDebtLimit = 500;

  static const String reasonIbanMissing = 'iban_missing';
  static const String reasonPendingExists = 'payout_pending_exists';
  static const String reasonCashDebt = 'cash_debt_outstanding';
  static const String reasonBelowMinimum = 'below_minimum';

  final double balance;
  final double cashDebt;
  final double availableForPayout;
  final double minPayoutAmount;

  /// Going online is refused above this debt.
  final double cashDebtLimit;
  final Payout? pendingPayout;
  final String? ibanMasked;
  final bool canRequest;
  final String? reason;

  bool get hasCashDebt => cashDebt > 0;
  bool get debtLimitReached => cashDebt >= cashDebtLimit;

  /// 0..1 share of the cash-debt limit used.
  double get debtRatio =>
      cashDebtLimit <= 0 ? 0 : (cashDebt / cashDebtLimit).clamp(0, 1);

  @override
  List<Object?> get props => <Object?>[
    balance,
    cashDebt,
    availableForPayout,
    minPayoutAmount,
    cashDebtLimit,
    pendingPayout,
    ibanMasked,
    canRequest,
    reason,
  ];
}
