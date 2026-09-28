import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/wallet/domain/entities/wallet_transaction.dart';
import 'package:ata_app/features/wallet/domain/usecases/top_up_wallet.dart';
import 'package:ata_app/features/wallet/presentation/cubit/top_up_cubit.dart';
import 'package:ata_app/features/wallet/presentation/cubit/top_up_state.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:mocktail/mocktail.dart';

import '../../helpers/fakes.dart';

class _MockTopUpWallet extends Mock implements TopUpWallet {}

void main() {
  late _MockTopUpWallet topUp;

  setUp(() => topUp = _MockTopUpWallet());

  blocTest<TopUpCubit, TopUpState>(
    'selecting a preset updates the amount',
    build: () => TopUpCubit(topUpWallet: topUp),
    act: (TopUpCubit cubit) => cubit.selectAmount(200),
    expect: () => <TopUpState>[const TopUpState(amount: 200)],
  );

  blocTest<TopUpCubit, TopUpState>(
    'confirm moves to the success step with the new balance',
    build: () => TopUpCubit(topUpWallet: topUp),
    setUp: () => when(() => topUp(100)).thenAnswer(
      (_) async => const Right<Failure, TopUpResult>(
        TopUpResult(transactionId: 't1', balance: 225),
      ),
    ),
    act: (TopUpCubit cubit) => cubit.confirm(),
    expect: () => <TopUpState>[
      const TopUpState(submitting: true),
      const TopUpState(step: TopUpStep.success, newBalance: 225),
    ],
  );

  blocTest<TopUpCubit, TopUpState>(
    'a failed top-up stays on the chooser with the failure',
    build: () => TopUpCubit(topUpWallet: topUp),
    setUp: () => when(() => topUp(any())).thenAnswer(
      (_) async =>
          const Left<Failure, TopUpResult>(NetworkFailure(message: '')),
    ),
    act: (TopUpCubit cubit) => cubit.confirm(),
    verify: (TopUpCubit cubit) {
      expect(cubit.state.step, TopUpStep.choose);
      expect(cubit.state.failure, isA<NetworkFailure>());
    },
  );

  test('TopUpWallet rejects amounts outside the API limits', () async {
    final TopUpWallet useCase = TopUpWallet(FakeWalletRepository());
    expect((await useCase(5)).isLeft(), isTrue);
    expect((await useCase(50)).isRight(), isTrue);
  });
}
