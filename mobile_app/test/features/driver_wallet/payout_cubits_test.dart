import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/payout.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/payout_summary.dart';
import 'package:ata_app/features/driver_wallet/domain/repositories/driver_wallet_repository.dart';
import 'package:ata_app/features/driver_wallet/domain/usecases/cancel_payout.dart';
import 'package:ata_app/features/driver_wallet/domain/usecases/get_payout_summary.dart';
import 'package:ata_app/features/driver_wallet/domain/usecases/get_payouts.dart';
import 'package:ata_app/features/driver_wallet/domain/usecases/request_payout.dart';
import 'package:ata_app/features/driver_wallet/presentation/cubit/payout_request_cubit.dart';
import 'package:ata_app/features/driver_wallet/presentation/cubit/payout_request_state.dart';
import 'package:ata_app/features/driver_wallet/presentation/cubit/payouts_cubit.dart';
import 'package:ata_app/features/driver_wallet/presentation/cubit/payouts_state.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:mocktail/mocktail.dart';

class _MockSummary extends Mock implements GetPayoutSummary {}

class _MockRequest extends Mock implements RequestPayout {}

class _MockGetPayouts extends Mock implements GetPayouts {}

class _MockCancel extends Mock implements CancelPayout {}

class _FakeParams extends Fake implements PayoutRequestParams {}

class _MockRepository extends Mock implements DriverWalletRepository {}

void main() {
  const PayoutSummary summary = PayoutSummary(
    balance: 640,
    availableForPayout: 640,
    ibanMasked: 'SA03 **** **** 1234',
    canRequest: true,
  );
  const Payout requested = Payout(
    id: 'po1',
    payoutNumber: 'PO-20260928-00012',
    amount: 500,
    status: PayoutStatus.requested,
  );

  setUpAll(() {
    registerFallbackValue(const NoParams());
    registerFallbackValue(_FakeParams());
  });

  group('PayoutRequestCubit', () {
    late _MockSummary getSummary;
    late _MockRequest request;

    setUp(() {
      getSummary = _MockSummary();
      request = _MockRequest();
      when(
        () => getSummary(any()),
      ).thenAnswer((_) async => const Right<Failure, PayoutSummary>(summary));
    });

    PayoutRequestCubit build() =>
        PayoutRequestCubit(getSummary: getSummary, request: request);

    blocTest<PayoutRequestCubit, PayoutRequestState>(
      'load pre-fills the available amount',
      build: build,
      act: (PayoutRequestCubit cubit) => cubit.load(),
      verify: (PayoutRequestCubit cubit) {
        expect(cubit.state.amountText, '640');
        expect(cubit.state.canSubmit, isTrue);
      },
    );

    blocTest<PayoutRequestCubit, PayoutRequestState>(
      'amounts below the minimum or above the balance cannot be sent',
      build: build,
      act: (PayoutRequestCubit cubit) async {
        await cubit.load();
        cubit.amountChanged('50');
        expect(cubit.state.amountError, PayoutAmountError.belowMinimum);
        cubit.amountChanged('700');
        expect(cubit.state.amountError, PayoutAmountError.aboveAvailable);
        cubit.amountChanged('abc');
        expect(cubit.state.amountError, PayoutAmountError.invalid);
        await cubit.submit();
      },
      verify: (PayoutRequestCubit cubit) {
        expect(cubit.state.canSubmit, isFalse);
        verifyNever(() => request(any()));
      },
    );

    blocTest<PayoutRequestCubit, PayoutRequestState>(
      'a valid amount creates the payout',
      build: build,
      setUp: () => when(
        () => request(any()),
      ).thenAnswer((_) async => const Right<Failure, Payout>(requested)),
      act: (PayoutRequestCubit cubit) async {
        await cubit.load();
        cubit.amountChanged('500');
        await cubit.submit();
      },
      verify: (PayoutRequestCubit cubit) {
        expect(cubit.state.payout, requested);
        final PayoutRequestParams sent =
            verify(() => request(captureAny())).captured.single
                as PayoutRequestParams;
        expect(sent.amount, 500);
        expect(sent.minAmount, 100);
      },
    );

    blocTest<PayoutRequestCubit, PayoutRequestState>(
      'a missing IBAN blocks the request',
      build: build,
      setUp: () => when(() => getSummary(any())).thenAnswer(
        (_) async => const Right<Failure, PayoutSummary>(
          PayoutSummary(
            availableForPayout: 640,
            reason: PayoutSummary.reasonIbanMissing,
          ),
        ),
      ),
      act: (PayoutRequestCubit cubit) async {
        await cubit.load();
        cubit.amountChanged('500');
      },
      verify: (PayoutRequestCubit cubit) =>
          expect(cubit.state.canSubmit, isFalse),
    );
  });

  test('RequestPayout validates locally before calling the API', () async {
    final _MockRepository repository = _MockRepository();
    final RequestPayout useCase = RequestPayout(repository);
    final below = await useCase(
      const PayoutRequestParams(amount: 50, minAmount: 100, available: 640),
    );
    expect(below.getLeft().toNullable()?.code, RequestPayout.belowMinimum);
    final above = await useCase(
      const PayoutRequestParams(amount: 900, minAmount: 100, available: 640),
    );
    expect(
      above.getLeft().toNullable()?.code,
      RequestPayout.insufficientBalance,
    );
    verifyZeroInteractions(repository);
  });

  group('PayoutsCubit', () {
    late _MockGetPayouts getPayouts;
    late _MockCancel cancel;

    setUp(() {
      getPayouts = _MockGetPayouts();
      cancel = _MockCancel();
    });

    blocTest<PayoutsCubit, PayoutsState>(
      'load lists the history',
      build: () => PayoutsCubit(getPayouts: getPayouts, cancelPayout: cancel),
      setUp: () => when(() => getPayouts(1)).thenAnswer(
        (_) async => const Right<Failure, PageResult<Payout>>(
          PageResult<Payout>(
            items: <Payout>[requested],
            page: 1,
            pageSize: 20,
            total: 1,
          ),
        ),
      ),
      act: (PayoutsCubit cubit) => cubit.load(),
      expect: () => <PayoutsState>[
        const PayoutsState(loading: true),
        const PayoutsState(payouts: <Payout>[requested]),
      ],
    );

    blocTest<PayoutsCubit, PayoutsState>(
      'cancel replaces the payout with the cancelled one',
      build: () => PayoutsCubit(getPayouts: getPayouts, cancelPayout: cancel),
      seed: () => const PayoutsState(payouts: <Payout>[requested]),
      setUp: () => when(() => cancel('po1')).thenAnswer(
        (_) async => const Right<Failure, Payout>(
          Payout(
            id: 'po1',
            payoutNumber: 'PO-20260928-00012',
            amount: 500,
            status: PayoutStatus.cancelled,
          ),
        ),
      ),
      act: (PayoutsCubit cubit) => cubit.cancel('po1'),
      verify: (PayoutsCubit cubit) {
        expect(cubit.state.payouts.single.status, PayoutStatus.cancelled);
        expect(cubit.state.payouts.single.canCancel, isFalse);
        expect(cubit.state.cancellingId, isNull);
      },
    );
  });
}
