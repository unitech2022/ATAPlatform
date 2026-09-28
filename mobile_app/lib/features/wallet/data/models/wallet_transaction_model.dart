import 'package:ata_app/features/payments/data/models/saved_card_model.dart';
import 'package:ata_app/features/trip/data/models/json_readers.dart';
import 'package:ata_app/features/wallet/domain/entities/wallet_transaction.dart';

/// JSON mapping for [WalletTransaction].
class WalletTransactionModel extends WalletTransaction {
  const WalletTransactionModel({
    required super.id,
    required super.type,
    required super.direction,
    required super.amount,
    required super.balanceAfter,
    required super.description,
    required super.createdAt,
  });

  factory WalletTransactionModel.fromJson(Map<String, dynamic> json) =>
      WalletTransactionModel(
        id: json['id'] as String,
        type: json['type'] as String? ?? '',
        direction: json['direction'] as String? ?? 'credit',
        amount: (json['amount'] as num?)?.toDouble() ?? 0,
        balanceAfter: (json['balanceAfter'] as num?)?.toDouble() ?? 0,
        description: json['description'] as String? ?? '',
        createdAt:
            DateTime.tryParse(json['createdAt'] as String? ?? '') ??
            DateTime.now(),
      );
}

/// JSON mapping for [TopUpResult].
class TopUpResultModel extends TopUpResult {
  const TopUpResultModel({
    required super.transactionId,
    required super.balance,
    super.paymentId,
    super.status,
    super.action,
  });

  factory TopUpResultModel.fromJson(Map<String, dynamic> json) =>
      TopUpResultModel(
        transactionId: JsonReaders.string(json, 'transactionId'),
        balance: JsonReaders.number(json, 'balance'),
        paymentId: JsonReaders.optionalString(json, 'paymentId'),
        status:
            JsonReaders.optionalString(json, 'status') ?? TopUpResult.captured,
        action: PaymentActionModel.fromJson(JsonReaders.object(json, 'action')),
      );
}
