import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/payments/data/models/receipt_model.dart';
import 'package:ata_app/features/payments/data/models/saved_card_model.dart';
import 'package:ata_app/features/payments/domain/entities/receipt.dart';
import 'package:ata_app/features/payments/domain/usecases/get_trip_receipt.dart';
import 'package:ata_app/features/payments/presentation/cubit/receipt_cubit.dart';
import 'package:ata_app/features/payments/presentation/cubit/receipt_state.dart';
import 'package:ata_app/features/wallet/data/models/wallet_transaction_model.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:mocktail/mocktail.dart';

import '../../helpers/payments_fakes.dart';

class _MockGetReceipt extends Mock implements GetTripReceipt {}

const Map<String, dynamic> receiptJson = <String, dynamic>{
  'tripId': 't1',
  'tripNumber': 'T-20260928-00042',
  'status': 'completed',
  'issuedAt': '2026-09-28T10:00:00Z',
  'currency': 'SAR',
  'driverName': 'محمد',
  'rideCategory': 'اقتصادي',
  'pickup': <String, dynamic>{'name': 'المنزل'},
  'dropoff': <String, dynamic>{'name': 'العمل'},
  'distanceMeters': 14200,
  'durationSeconds': 1380,
  'lines': <Map<String, dynamic>>[
    <String, dynamic>{'code': 'base_fare', 'label': 'الأجرة', 'amount': 8.0},
    <String, dynamic>{
      'code': 'discount',
      'label': 'خصم ATA10',
      'amount': -5.0,
      'source': 'promotion',
      'reference': 'ATA10',
    },
  ],
  'discounts': <Map<String, dynamic>>[
    <String, dynamic>{
      'source': 'promotion',
      'reference': 'ATA10',
      'label': 'خصم ATA10',
      'amount': 5.0,
    },
  ],
  'subtotal': 51.02,
  'discountTotal': 5.0,
  'total': 46.0,
  'vatRate': 15,
  'vatIncluded': 6.0,
  'payment': <String, dynamic>{
    'method': 'card',
    'brand': 'mada',
    'last4': '4201',
    'status': 'captured',
    'paidAmount': 46.0,
    'fallbackToCash': true,
  },
  'refunds': <Map<String, dynamic>>[
    <String, dynamic>{'id': 'r1', 'amount': 10.0, 'status': 'succeeded'},
    <String, dynamic>{'id': 'r2', 'amount': 3.0, 'status': 'failed'},
  ],
  'netPaid': 36.0,
};

void main() {
  test('ReceiptModel maps lines, discount source, payment and refunds', () {
    final Receipt receipt = ReceiptModel.fromJson(receiptJson);
    expect(receipt.tripNumber, 'T-20260928-00042');
    expect(receipt.pickupName, 'المنزل');
    expect(receipt.lines.last.isDiscount, isTrue);
    expect(receipt.lines.last.source, ReceiptDiscount.promotion);
    expect(receipt.discounts.single.reference, 'ATA10');
    expect(receipt.total, 46);
    expect(receipt.vatIncluded, 6);
    expect(receipt.payment.last4, '4201');
    expect(receipt.payment.fallbackToCash, isTrue);
    expect(receipt.refundedTotal, 10);
    expect(receipt.netPaid, 36);
  });

  test('add-card and top-up results read the 202 action', () {
    final result = PaymentActionModel.addResultFromJson(<String, dynamic>{
      'paymentMethod': <String, dynamic>{
        'id': 'pm1',
        'brand': 'visa',
        'last4': '3220',
        'expiryMonth': 1,
        'expiryYear': 2030,
        'status': 'pending_verification',
      },
      'action': <String, dynamic>{'type': 'redirect', 'url': 'https://x'},
    });
    expect(result.card.isPending, isTrue);
    expect(result.action?.url, 'https://x');

    final topUp = TopUpResultModel.fromJson(const <String, dynamic>{
      'paymentId': 'p1',
      'status': 'initiated',
      'action': <String, dynamic>{'type': 'redirect', 'url': 'https://y'},
    });
    expect(topUp.requiresAction, isTrue);
    expect(topUp.status, 'initiated');
  });

  group('ReceiptCubit', () {
    late _MockGetReceipt getReceipt;

    setUp(() => getReceipt = _MockGetReceipt());

    blocTest<ReceiptCubit, ReceiptState>(
      'loads the receipt of the trip',
      build: () => ReceiptCubit(getTripReceipt: getReceipt, tripId: 't1'),
      setUp: () => when(() => getReceipt('t1')).thenAnswer(
        (_) async =>
            Right<Failure, Receipt>(ReceiptModel.fromJson(receiptJson)),
      ),
      act: (ReceiptCubit cubit) => cubit.load(),
      verify: (ReceiptCubit cubit) {
        expect(cubit.state.receipt?.tripId, 't1');
        expect(cubit.state.loading, isFalse);
      },
    );

    blocTest<ReceiptCubit, ReceiptState>(
      '409 conflict means no receipt for this trip',
      build: () => ReceiptCubit(
        getTripReceipt: GetTripReceipt(FakePaymentsRepository()),
        tripId: 't2',
      ),
      act: (ReceiptCubit cubit) => cubit.load(),
      verify: (ReceiptCubit cubit) => expect(cubit.state.unavailable, isTrue),
    );
  });
}
