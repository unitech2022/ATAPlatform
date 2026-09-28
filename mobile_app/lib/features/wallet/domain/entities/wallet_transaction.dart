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
class TopUpResult extends Equatable {
  const TopUpResult({required this.transactionId, required this.balance});

  final String transactionId;
  final double balance;

  @override
  List<Object?> get props => <Object?>[transactionId, balance];
}
