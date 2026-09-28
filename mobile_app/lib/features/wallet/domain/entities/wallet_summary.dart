import 'package:equatable/equatable.dart';

/// A payment method attached to the wallet.
class PaymentMethod extends Equatable {
  const PaymentMethod({
    required this.type,
    required this.label,
    this.isDefault = false,
    this.id,
    this.brand,
    this.last4,
  });

  /// `wallet`, `cash`, `card`.
  final String type;
  final String label;
  final bool isDefault;

  /// Saved card fields (`type == card`).
  final String? id;
  final String? brand;
  final String? last4;

  bool get isCard => type == 'card';

  @override
  List<Object?> get props => <Object?>[
    type,
    label,
    isDefault,
    id,
    brand,
    last4,
  ];
}

/// `GET /wallet`.
class WalletSummary extends Equatable {
  const WalletSummary({
    required this.id,
    required this.kind,
    required this.currency,
    required this.balance,
    required this.paymentMethods,
    this.cashDebt = 0,
  });

  final String id;
  final String kind;
  final String currency;
  final double balance;
  final List<PaymentMethod> paymentMethods;

  /// Driver wallets: cash fares still owed to the platform (≥ 0).
  final double cashDebt;

  /// A negative passenger balance blocks new trips until topped up.
  bool get hasOutstandingBalance => balance < 0;

  WalletSummary copyWith({double? balance}) => WalletSummary(
    id: id,
    kind: kind,
    currency: currency,
    balance: balance ?? this.balance,
    paymentMethods: paymentMethods,
    cashDebt: cashDebt,
  );

  @override
  List<Object?> get props => <Object?>[
    id,
    kind,
    currency,
    balance,
    paymentMethods,
    cashDebt,
  ];
}
