import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/promotions/domain/entities/promo_validation.dart';
import 'package:ata_app/features/promotions/domain/entities/promotion.dart';
import 'package:ata_app/features/promotions/domain/repositories/promotions_repository.dart';
import 'package:ata_app/features/promotions/domain/usecases/validate_promo_code.dart';
import 'package:ata_app/features/promotions/presentation/cubit/promo_code_cubit.dart';
import 'package:ata_app/features/promotions/presentation/cubit/promo_code_state.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:mocktail/mocktail.dart';

class _MockValidate extends Mock implements ValidatePromoCode {}

class _MockRepository extends Mock implements PromotionsRepository {}

const PromoValidationParams _draft = PromoValidationParams(
  code: '',
  quoteId: 'q1',
  rideCategoryId: 'c1',
  paymentMethod: 'card',
  bookingType: 'now',
);

const PromoValidation _valid = PromoValidation(
  code: 'ATA10',
  name: 'خصم 10',
  type: PromotionType.fixed,
  value: 10,
  discountAmount: 10,
  totalBefore: 51,
  totalAfter: 41,
);

void main() {
  late _MockValidate validate;

  setUpAll(() => registerFallbackValue(_draft));

  setUp(() {
    validate = _MockValidate();
    when(
      () => validate(any()),
    ).thenAnswer((_) async => const Right<Failure, PromoValidation>(_valid));
  });

  PromoCodeCubit build({String? initial}) =>
      PromoCodeCubit(validate: validate, initialCode: initial);

  group('PromoCodeCubit', () {
    test('starts empty, or with a normalized pre-filled code', () {
      expect(build().state, const PromoCodeState());
      expect(build(initial: ' ata 10').state.input, 'ATA10');
    });

    blocTest<PromoCodeCubit, PromoCodeState>(
      'validates the input against the current quote and applies it',
      build: build,
      act: (PromoCodeCubit c) async {
        c.inputChanged('ata10');
        await c.validate(_draft);
      },
      expect: () => <PromoCodeState>[
        const PromoCodeState(input: 'ata10'),
        const PromoCodeState(
          input: 'ata10',
          status: PromoCodeStatus.validating,
        ),
        const PromoCodeState(
          input: 'ATA10',
          status: PromoCodeStatus.applied,
          applied: _valid,
        ),
      ],
      verify: (_) {
        final PromoValidationParams sent =
            verify(() => validate(captureAny())).captured.single
                as PromoValidationParams;
        expect(sent.code, 'ata10');
        expect(sent.quoteId, 'q1');
        expect(sent.rideCategoryId, 'c1');
        expect(sent.paymentMethod, 'card');
      },
    );

    blocTest<PromoCodeCubit, PromoCodeState>(
      'a refused code shows the error and applies nothing',
      setUp: () => when(() => validate(any())).thenAnswer(
        (_) async => const Left<Failure, PromoValidation>(
          ServerFailure(code: 'promo_expired', message: '', statusCode: 422),
        ),
      ),
      build: build,
      act: (PromoCodeCubit c) async {
        c.inputChanged('OLD2020');
        await c.validate(_draft);
      },
      verify: (PromoCodeCubit c) {
        expect(c.state.status, PromoCodeStatus.error);
        expect(c.state.appliedCode, isNull);
        expect(c.state.failure?.code, 'promo_expired');
      },
    );

    blocTest<PromoCodeCubit, PromoCodeState>(
      'remove drops the applied code and resets the field',
      build: build,
      act: (PromoCodeCubit c) async {
        c.inputChanged('ATA10');
        await c.validate(_draft);
        c.remove();
      },
      verify: (PromoCodeCubit c) {
        expect(c.state.appliedCode, isNull);
        expect(c.state.input, isEmpty);
        expect(c.state.status, PromoCodeStatus.empty);
        expect(c.state.inputVersion, 1);
      },
    );

    blocTest<PromoCodeCubit, PromoCodeState>(
      'a quote answering promotion.valid=false removes the applied code',
      build: build,
      act: (PromoCodeCubit c) async {
        c.inputChanged('ATA10');
        await c.validate(_draft);
        c.rejectedByQuote('min_fare');
      },
      verify: (PromoCodeCubit c) {
        expect(c.state.appliedCode, isNull);
        expect(c.state.failure?.code, 'promo_not_eligible');
        expect(c.state.failure?.details, <String, dynamic>{
          'reason': 'min_fare',
        });
      },
    );

    blocTest<PromoCodeCubit, PromoCodeState>(
      'rejectedByQuote is ignored when nothing is applied',
      build: build,
      act: (PromoCodeCubit c) => c.rejectedByQuote('promo_expired'),
      expect: () => const <PromoCodeState>[],
    );

    blocTest<PromoCodeCubit, PromoCodeState>(
      'the trip request refusing the code clears it with that failure',
      build: build,
      act: (PromoCodeCubit c) async {
        c.inputChanged('ATA10');
        await c.validate(_draft);
        c.rejected(
          const ServerFailure(
            code: 'promo_usage_limit_reached',
            message: '',
            details: <String, dynamic>{'scope': 'user'},
          ),
        );
      },
      verify: (PromoCodeCubit c) {
        expect(c.state.appliedCode, isNull);
        expect(c.state.status, PromoCodeStatus.error);
      },
    );

    blocTest<PromoCodeCubit, PromoCodeState>(
      'prefill replaces the input and bumps the field version',
      build: build,
      act: (PromoCodeCubit c) => c.prefill('welcome'),
      expect: () => <PromoCodeState>[
        const PromoCodeState(input: 'WELCOME', inputVersion: 1),
      ],
    );

    test('canValidate needs a well-formed code', () {
      final PromoCodeCubit cubit = build()..inputChanged('AB');
      expect(cubit.state.canValidate, isFalse);
      cubit.inputChanged('ab 12');
      expect(cubit.state.canValidate, isTrue);
      cubit.close();
    });
  });

  group('ValidatePromoCode', () {
    late _MockRepository repository;

    setUp(() {
      repository = _MockRepository();
      when(
        () => repository.validate(any()),
      ).thenAnswer((_) async => const Right<Failure, PromoValidation>(_valid));
    });

    test('normalizes to uppercase without spaces', () async {
      await ValidatePromoCode(repository)(_draft.withCode(' ata 10 '));
      final PromoValidationParams sent =
          verify(() => repository.validate(captureAny())).captured.single
              as PromoValidationParams;
      expect(sent.code, 'ATA10');
      expect(sent.quoteId, 'q1');
    });

    test(
      'a code that cannot exist fails locally with promo_not_found',
      () async {
        for (final String code in <String>['AB', 'ATA-10', 'X' * 21]) {
          final result = await ValidatePromoCode(repository)(
            _draft.withCode(code),
          );
          expect(result.getLeft().toNullable()?.code, 'promo_not_found');
        }
        verifyNever(() => repository.validate(any()));
      },
    );
  });
}
