import 'package:equatable/equatable.dart';

/// Source of a top-up (`method` of `POST /wallet/topups`).
enum TopUpMethod {
  sandbox('sandbox'),
  card('card');

  const TopUpMethod(this.apiValue);

  final String apiValue;
}

/// Which wallet is topped up (`?kind=driver` settles the driver's cash debt).
enum WalletKind {
  passenger('passenger'),
  driver('driver');

  const WalletKind(this.apiValue);

  final String apiValue;
}

/// Input of `TopUpWithMethod`.
class TopUpParams extends Equatable {
  const TopUpParams({
    required this.amount,
    this.method = TopUpMethod.sandbox,
    this.paymentMethodId,
    this.kind = WalletKind.passenger,
  });

  final double amount;
  final TopUpMethod method;

  /// Saved card id, required for [TopUpMethod.card].
  final String? paymentMethodId;
  final WalletKind kind;

  @override
  List<Object?> get props => <Object?>[amount, method, paymentMethodId, kind];
}
