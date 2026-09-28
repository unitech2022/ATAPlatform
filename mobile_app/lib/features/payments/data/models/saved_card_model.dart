import 'package:ata_app/features/payments/domain/entities/payment_action.dart';
import 'package:ata_app/features/payments/domain/entities/saved_card.dart';
import 'package:ata_app/features/trip/data/models/json_readers.dart';

/// JSON mapping for [SavedCard].
abstract final class SavedCardModel {
  static SavedCard fromJson(Map<String, dynamic> json) => SavedCard(
    id: JsonReaders.string(json, 'id'),
    brand: JsonReaders.optionalString(json, 'brand') ?? 'card',
    last4: JsonReaders.string(json, 'last4'),
    expiryMonth: JsonReaders.integer(json, 'expiryMonth'),
    expiryYear: JsonReaders.integer(json, 'expiryYear'),
    holderName: JsonReaders.optionalString(json, 'holderName'),
    status: JsonReaders.optionalString(json, 'status') ?? SavedCard.active,
    isDefault: json['isDefault'] == true,
    isExpired: json['isExpired'] == true,
  );

  static List<SavedCard> listFromJson(dynamic body) => body is List<dynamic>
      ? body
            .whereType<Map<String, dynamic>>()
            .map(fromJson)
            .toList(growable: false)
      : const <SavedCard>[];
}

/// JSON mapping for [PaymentAction] and [AddCardResult].
abstract final class PaymentActionModel {
  static PaymentAction? fromJson(Map<String, dynamic>? json) {
    if (json == null) return null;
    final String url = JsonReaders.string(json, 'url');
    if (url.isEmpty) return null;
    return PaymentAction(
      type: JsonReaders.optionalString(json, 'type') ?? 'redirect',
      url: url,
      expiresAt: JsonReaders.date(json, 'expiresAt'),
    );
  }

  /// `201 PaymentMethod` or `202 { paymentMethod, action }`.
  static AddCardResult addResultFromJson(Map<String, dynamic> json) {
    final Map<String, dynamic>? wrapped = JsonReaders.object(
      json,
      'paymentMethod',
    );
    return AddCardResult(
      card: SavedCardModel.fromJson(wrapped ?? json),
      action: fromJson(JsonReaders.object(json, 'action')),
    );
  }
}
