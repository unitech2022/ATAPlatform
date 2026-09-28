import 'package:ata_app/features/payments/domain/entities/saved_card.dart';
import 'package:equatable/equatable.dart';

/// A customer action required by the gateway (3-D Secure redirect).
class PaymentAction extends Equatable {
  const PaymentAction({required this.type, required this.url, this.expiresAt});

  /// `redirect`.
  final String type;
  final String url;
  final DateTime? expiresAt;

  @override
  List<Object?> get props => <Object?>[type, url, expiresAt];
}

/// `POST /passenger/payment-methods` result: the card, plus the 3-D Secure
/// action when the gateway answered `202`.
class AddCardResult extends Equatable {
  const AddCardResult({required this.card, this.action});

  final SavedCard card;
  final PaymentAction? action;

  @override
  List<Object?> get props => <Object?>[card, action];
}
