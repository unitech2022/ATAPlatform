import 'package:ata_app/core/errors/app_exception.dart';
import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/promotions/data/models/promotion_models.dart';
import 'package:ata_app/features/promotions/domain/entities/promo_validation.dart';
import 'package:ata_app/features/promotions/domain/entities/promotion.dart';
import 'package:ata_app/features/promotions/domain/usecases/get_promotions.dart';
import 'package:ata_app/features/promotions/presentation/cubit/promotions_cubit.dart';
import 'package:ata_app/features/promotions/presentation/cubit/promotions_state.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:mocktail/mocktail.dart';

class _MockGet extends Mock implements GetPromotions {}

final DateTime _now = DateTime.utc(2026, 9, 29);

final Promotion _welcome = Promotion(
  code: 'WELCOME',
  name: 'مرحباً',
  type: PromotionType.percent,
  value: 20,
  maxDiscount: 15,
  firstTripOnly: true,
  validTo: DateTime.utc(2026, 12, 31),
);

final Promotion _stale = Promotion(
  code: 'SUMMER',
  validTo: DateTime.utc(2026, 9, 1),
);

const Promotion _used = Promotion(code: 'ATA10', status: PromotionStatus.used);

void main() {
  group('models', () {
    test('parses a public promotion', () {
      final Promotion p = PromotionModels.promotion(<String, dynamic>{
        'code': 'WELCOME',
        'name': 'مرحباً',
        'description': 'لأول رحلة',
        'type': 'percent',
        'value': 20,
        'maxDiscount': 15,
        'minFare': null,
        'validTo': '2026-12-31T20:59:59Z',
        'firstTripOnly': true,
        'rideCategoryCodes': <String>['economy'],
        'paymentMethods': null,
      });
      expect(p.type, PromotionType.percent);
      expect(p.maxDiscount, 15);
      expect(p.firstTripOnly, isTrue);
      expect(p.rideCategoryCodes, <String>['economy']);
      expect(p.paymentMethods, isNull);
      expect(p.status, PromotionStatus.available);
      expect(p.statusAt(DateTime.utc(2027)), PromotionStatus.expired);
    });

    test('parses a validation with the quote discount', () {
      final PromoValidation v = PromotionModels.validation(<String, dynamic>{
        'valid': true,
        'promotion': <String, dynamic>{
          'code': 'ATA10',
          'name': 'خصم 10',
          'type': 'fixed',
          'value': 10,
          'maxDiscount': null,
          'isStackable': false,
        },
        'discountAmount': 5.0,
        'totalBefore': 51.0,
        'totalAfter': 46.0,
      });
      expect(v.code, 'ATA10');
      expect(v.type, PromotionType.fixed);
      expect(v.discountAmount, 5);
      expect(v.totalAfter, 46);
    });

    test('valid:false becomes the matching error', () {
      expect(
        () => PromotionModels.validation(<String, dynamic>{
          'valid': false,
          'reason': 'min_fare',
        }),
        throwsA(
          isA<AppException>()
              .having((AppException e) => e.code, 'code', 'promo_not_eligible')
              .having(
                (AppException e) => e.details,
                'details',
                <String, dynamic>{'reason': 'min_fare'},
              ),
        ),
      );
    });

    test('validation body sends only the known fields', () {
      expect(
        PromotionModels.validationBody(
          const PromoValidationParams(code: 'ATA10', quoteId: 'q1'),
        ),
        <String, dynamic>{'code': 'ATA10', 'quoteId': 'q1'},
      );
    });
  });

  group('PromotionsCubit', () {
    late _MockGet get;

    setUpAll(() => registerFallbackValue(PromotionStatus.available));

    setUp(() {
      get = _MockGet();
      when(() => get(PromotionStatus.available)).thenAnswer(
        (_) async =>
            Right<Failure, List<Promotion>>(<Promotion>[_welcome, _stale]),
      );
      when(() => get(PromotionStatus.used)).thenAnswer(
        (_) async => const Right<Failure, List<Promotion>>(<Promotion>[_used]),
      );
    });

    PromotionsCubit build() =>
        PromotionsCubit(getPromotions: get, now: () => _now);

    blocTest<PromotionsCubit, PromotionsState>(
      'loads the available tab and hides promotions past validTo',
      build: build,
      act: (PromotionsCubit c) => c.load(),
      verify: (PromotionsCubit c) =>
          expect(c.state.current, <Promotion>[_welcome]),
    );

    blocTest<PromotionsCubit, PromotionsState>(
      'switching tabs loads each tab once',
      build: build,
      act: (PromotionsCubit c) async {
        await c.load();
        await c.selectTab(PromotionStatus.used);
        await c.selectTab(PromotionStatus.available);
        await c.selectTab(PromotionStatus.used);
      },
      verify: (PromotionsCubit c) {
        expect(c.state.current, <Promotion>[_used]);
        verify(() => get(PromotionStatus.used)).called(1);
        verify(() => get(PromotionStatus.available)).called(1);
      },
    );

    blocTest<PromotionsCubit, PromotionsState>(
      'a failure is shown and the tab stays unloaded',
      setUp: () => when(() => get(PromotionStatus.available)).thenAnswer(
        (_) async => const Left<Failure, List<Promotion>>(
          NetworkFailure(message: 'offline'),
        ),
      ),
      build: build,
      act: (PromotionsCubit c) => c.load(),
      verify: (PromotionsCubit c) {
        expect(c.state.failure, isA<NetworkFailure>());
        expect(c.state.isLoaded, isFalse);
      },
    );
  });
}
