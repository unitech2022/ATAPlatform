import 'package:ata_app/core/network/api_client.dart';
import 'package:ata_app/features/payments/data/models/receipt_model.dart';
import 'package:ata_app/features/payments/data/models/saved_card_model.dart';
import 'package:ata_app/features/payments/domain/entities/payment_action.dart';
import 'package:ata_app/features/payments/domain/entities/receipt.dart';
import 'package:ata_app/features/payments/domain/entities/saved_card.dart';

/// Passenger payment endpoints of `docs/08` §F11.5.
class PaymentsRemoteDataSource {
  const PaymentsRemoteDataSource(this._api);

  final ApiClient _api;

  static const String _methodsPath = '/passenger/payment-methods';
  static const String _tripsPath = '/passenger/trips';

  /// Where the gateway sends the customer back after 3-D Secure.
  static const String returnUrl = 'ata://payments/return';

  Future<List<SavedCard>> paymentMethods() async =>
      SavedCardModel.listFromJson(await _api.get(_methodsPath));

  Future<AddCardResult> addPaymentMethod({
    required String token,
    required bool setDefault,
  }) async => PaymentActionModel.addResultFromJson(
    await _api.post(
          _methodsPath,
          body: <String, dynamic>{
            'token': token,
            'setDefault': setDefault,
            'returnUrl': returnUrl,
          },
        )
        as Map<String, dynamic>,
  );

  Future<SavedCard> setDefault(String id) async => SavedCardModel.fromJson(
    await _api.post('$_methodsPath/$id/default') as Map<String, dynamic>,
  );

  Future<void> remove(String id) => _api.delete('$_methodsPath/$id');

  Future<Receipt> receipt(String tripId) async => ReceiptModel.fromJson(
    await _api.get('$_tripsPath/$tripId/receipt') as Map<String, dynamic>,
  );
}
