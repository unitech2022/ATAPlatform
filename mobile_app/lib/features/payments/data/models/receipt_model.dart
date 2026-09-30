import 'package:ata_app/features/corporate/data/models/corporate_check_model.dart';
import 'package:ata_app/features/payments/domain/entities/receipt.dart';
import 'package:ata_app/features/trip/data/models/json_readers.dart';

/// JSON mapping for [Receipt] (`docs/08` §F11.6).
abstract final class ReceiptModel {
  static const String _refundSucceeded = 'succeeded';

  static Receipt fromJson(Map<String, dynamic> json) {
    final double refunded = JsonReaders.objects(json, 'refunds')
        .where(
          (Map<String, dynamic> r) =>
              JsonReaders.optionalString(r, 'status') == _refundSucceeded,
        )
        .fold(0, (double sum, Map<String, dynamic> r) {
          return sum + JsonReaders.number(r, 'amount');
        });
    return Receipt(
      tripId: JsonReaders.string(json, 'tripId'),
      tripNumber: JsonReaders.string(json, 'tripNumber'),
      status: JsonReaders.string(json, 'status'),
      issuedAt: JsonReaders.date(json, 'issuedAt'),
      currency: JsonReaders.optionalString(json, 'currency') ?? 'SAR',
      driverName: JsonReaders.optionalString(json, 'driverName'),
      vehicle: JsonReaders.optionalString(json, 'vehicle'),
      rideCategory: JsonReaders.optionalString(json, 'rideCategory'),
      pickupName: _placeName(json, 'pickup'),
      dropoffName: _placeName(json, 'dropoff'),
      distanceMeters: JsonReaders.integer(json, 'distanceMeters'),
      durationSeconds: JsonReaders.integer(json, 'durationSeconds'),
      lines: JsonReaders.objects(json, 'lines').map(_line).toList(),
      discounts: JsonReaders.objects(json, 'discounts').map(_discount).toList(),
      subtotal: JsonReaders.number(json, 'subtotal'),
      discountTotal: JsonReaders.number(json, 'discountTotal'),
      total: JsonReaders.number(json, 'total'),
      vatRate: JsonReaders.number(json, 'vatRate'),
      vatIncluded: JsonReaders.number(json, 'vatIncluded'),
      payment: _payment(
        JsonReaders.object(json, 'payment') ?? const <String, dynamic>{},
      ),
      refundedTotal: refunded,
      netPaid: JsonReaders.optionalNumber(json, 'netPaid'),
      corporate: CorporateCheckModel.trip(json),
    );
  }

  static String? _placeName(Map<String, dynamic> json, String key) {
    final Map<String, dynamic>? place = JsonReaders.object(json, key);
    return place == null ? null : JsonReaders.optionalString(place, 'name');
  }

  static ReceiptLine _line(Map<String, dynamic> json) => ReceiptLine(
    code: JsonReaders.string(json, 'code'),
    label: JsonReaders.string(json, 'label'),
    amount: JsonReaders.number(json, 'amount'),
    source: JsonReaders.optionalString(json, 'source'),
    reference: JsonReaders.optionalString(json, 'reference'),
  );

  static ReceiptDiscount _discount(Map<String, dynamic> json) =>
      ReceiptDiscount(
        source: JsonReaders.string(json, 'source'),
        label: JsonReaders.string(json, 'label'),
        amount: JsonReaders.number(json, 'amount'),
        reference: JsonReaders.optionalString(json, 'reference'),
      );

  static ReceiptPayment _payment(Map<String, dynamic> json) => ReceiptPayment(
    method: JsonReaders.optionalString(json, 'method') ?? 'cash',
    status: JsonReaders.string(json, 'status'),
    paidAmount: JsonReaders.number(json, 'paidAmount'),
    brand: JsonReaders.optionalString(json, 'brand'),
    last4: JsonReaders.optionalString(json, 'last4'),
    fallbackToCash: json['fallbackToCash'] == true,
  );
}
