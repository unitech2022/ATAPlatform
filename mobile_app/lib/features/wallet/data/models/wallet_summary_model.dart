import 'package:ata_app/features/wallet/domain/entities/wallet_summary.dart';

/// JSON mapping for [WalletSummary].
class WalletSummaryModel extends WalletSummary {
  const WalletSummaryModel({
    required super.id,
    required super.kind,
    required super.currency,
    required super.balance,
    required super.paymentMethods,
  });

  factory WalletSummaryModel.fromJson(Map<String, dynamic> json) =>
      WalletSummaryModel(
        id: json['id'] as String,
        kind: json['kind'] as String? ?? 'passenger',
        currency: json['currency'] as String? ?? 'SAR',
        balance: (json['balance'] as num?)?.toDouble() ?? 0,
        paymentMethods:
            (json['paymentMethods'] as List<dynamic>? ?? const <dynamic>[])
                .map((dynamic e) => _method(e as Map<String, dynamic>))
                .toList(growable: false),
      );

  static PaymentMethod _method(Map<String, dynamic> json) => PaymentMethod(
    type: json['type'] as String? ?? 'cash',
    label: json['label'] as String? ?? '',
    isDefault: json['isDefault'] as bool? ?? false,
  );
}
