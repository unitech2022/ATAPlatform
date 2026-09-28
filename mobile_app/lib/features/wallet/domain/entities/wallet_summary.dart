import 'package:equatable/equatable.dart';

/// A payment method attached to the wallet.
class PaymentMethod extends Equatable {
  const PaymentMethod({
    required this.type,
    required this.label,
    this.isDefault = false,
  });

  /// `wallet`, `cash`, `card`.
  final String type;
  final String label;
  final bool isDefault;

  @override
  List<Object?> get props => <Object?>[type, label, isDefault];
}

/// `GET /wallet`.
class WalletSummary extends Equatable {
  const WalletSummary({
    required this.id,
    required this.kind,
    required this.currency,
    required this.balance,
    required this.paymentMethods,
  });

  final String id;
  final String kind;
  final String currency;
  final double balance;
  final List<PaymentMethod> paymentMethods;

  WalletSummary copyWith({double? balance}) => WalletSummary(
    id: id,
    kind: kind,
    currency: currency,
    balance: balance ?? this.balance,
    paymentMethods: paymentMethods,
  );

  @override
  List<Object?> get props => <Object?>[
    id,
    kind,
    currency,
    balance,
    paymentMethods,
  ];
}
