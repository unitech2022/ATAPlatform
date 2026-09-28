import 'package:ata_app/features/payments/domain/entities/payment_action.dart';
import 'package:equatable/equatable.dart';

/// One ledger movement.
class WalletTransaction extends Equatable {
  const WalletTransaction({
    required this.id,
    required this.type,
    required this.direction,
    required this.amount,
    required this.balanceAfter,
    required this.description,
    required this.createdAt,
  });

  final String id;
  final String type;

  /// `credit` or `debit`.
  final String direction;
  final double amount;
  final double balanceAfter;
  final String description;
  final DateTime createdAt;

  @override
  List<Object?> get props => <Object?>[
    id,
    type,
    direction,
    amount,
    balanceAfter,
    description,
    createdAt,
  ];
}

/// `POST /wallet/topups` response.
/// `201` carries the new balance; `202` a 3-D Secure [action] (the wallet
/// is credited once the payment is captured).
class TopUpResult extends Equatable {
  const TopUpResult({
    required this.transactionId,
    required this.balance,
    this.paymentId,
    this.status = captured,
    this.action,
  });

  static const String captured = 'captured';

  final String transactionId;
  final double balance;
  final String? paymentId;
  final String status;
  final PaymentAction? action;

  bool get requiresAction => action != null;

  @override
  List<Object?> get props => <Object?>[
    transactionId,
    balance,
    paymentId,
    status,
    action,
  ];
}
