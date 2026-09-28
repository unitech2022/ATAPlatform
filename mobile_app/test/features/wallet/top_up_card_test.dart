import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/payments/domain/entities/payment_action.dart';
import 'package:ata_app/features/payments/domain/entities/saved_card.dart';
import 'package:ata_app/features/payments/domain/usecases/get_payment_methods.dart';
import 'package:ata_app/features/wallet/domain/entities/top_up_params.dart';
import 'package:ata_app/features/wallet/domain/entities/wallet_transaction.dart';
import 'package:ata_app/features/wallet/domain/usecases/top_up_wallet.dart';
import 'package:ata_app/features/wallet/domain/usecases/top_up_with_method.dart';
import 'package:ata_app/features/wallet/presentation/cubit/top_up_cubit.dart';
import 'package:ata_app/features/wallet/presentation/cubit/top_up_state.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:mocktail/mocktail.dart';

import '../../helpers/fakes.dart';
import '../../helpers/payments_fakes.dart';

class _MockTopUpWallet extends Mock implements TopUpWallet {}

class _MockTopUpWithMethod extends Mock implements TopUpWithMethod {}

class _MockGetCards extends Mock implements GetPaymentMethods {}

void main() {
  late _MockTopUpWallet sandbox;
  late _MockTopUpWithMethod withMethod;
  late _MockGetCards getCards;

  const SavedCard expired = SavedCard(
    id: 'pm3',
    brand: 'visa',
    last4: '0000',
    expiryMonth: 1,
    expiryYear: 2020,
    isExpired: true,
  );

  setUpAll(() {
    registerFallbackValue(const NoParams());
    registerFallbackValue(const TopUpParams(amount: 0));
  });

  setUp(() {
    sandbox = _MockTopUpWallet();
    withMethod = _MockTopUpWithMethod();
    getCards = _MockGetCards();
    when(() => getCards(any())).thenAnswer(
      (_) async => const Right<Failure, List<SavedCard>>(<SavedCard>[
        testMada,
        testVisa,
        expired,
      ]),
    );
  });

  TopUpCubit build({WalletKind kind = WalletKind.passenger}) => TopUpCubit(
    topUpWallet: sandbox,
    topUpWithMethod: withMethod,
    getPaymentMethods: getCards,
    kind: kind,
  );

  blocTest<TopUpCubit, TopUpState>(
    'loadCards keeps usable cards and preselects the default one',
    build: build,
    act: (TopUpCubit cubit) => cubit.loadCards(),
    expect: () => <TopUpState>[
      const TopUpState(cards: <SavedCard>[testMada, testVisa], cardId: 'pm1'),
    ],
  );

  blocTest<TopUpCubit, TopUpState>(
    'a card top-up sends the card id',
    build: build,
    setUp: () => when(() => withMethod(any())).thenAnswer(
      (_) async => const Right<Failure, TopUpResult>(
        TopUpResult(transactionId: 't1', balance: 225),
      ),
    ),
    act: (TopUpCubit cubit) async {
      await cubit.loadCards();
      cubit.selectCard('pm2');
      await cubit.confirm();
    },
    verify: (TopUpCubit cubit) {
      expect(cubit.state.step, TopUpStep.success);
      verify(
        () => withMethod(
          const TopUpParams(
            amount: 100,
            method: TopUpMethod.card,
            paymentMethodId: 'pm2',
          ),
        ),
      ).called(1);
      verifyNever(() => sandbox(any()));
    },
  );

  blocTest<TopUpCubit, TopUpState>(
    'a 202 answer moves to the 3-D Secure step',
    build: build,
    setUp: () => when(() => withMethod(any())).thenAnswer(
      (_) async => const Right<Failure, TopUpResult>(
        TopUpResult(
          transactionId: '',
          balance: 0,
          status: 'initiated',
          action: PaymentAction(type: 'redirect', url: 'https://3ds'),
        ),
      ),
    ),
    act: (TopUpCubit cubit) async {
      await cubit.loadCards();
      await cubit.confirm();
    },
    verify: (TopUpCubit cubit) {
      expect(cubit.state.step, TopUpStep.action);
      expect(cubit.state.action?.url, 'https://3ds');
    },
  );

  blocTest<TopUpCubit, TopUpState>(
    'the driver wallet settles its debt with kind=driver',
    build: () => TopUpCubit(
      topUpWallet: sandbox,
      topUpWithMethod: withMethod,
      kind: WalletKind.driver,
      initialAmount: 320,
    ),
    setUp: () => when(() => withMethod(any())).thenAnswer(
      (_) async => const Right<Failure, TopUpResult>(
        TopUpResult(transactionId: 't2', balance: 0),
      ),
    ),
    act: (TopUpCubit cubit) => cubit.confirm(),
    verify: (_) => verify(
      () => withMethod(const TopUpParams(amount: 320, kind: WalletKind.driver)),
    ).called(1),
  );

  test('TopUpWithMethod requires a card id for card top-ups', () async {
    final TopUpWithMethod useCase = TopUpWithMethod(FakeWalletRepository());
    expect(
      (await useCase(
        const TopUpParams(amount: 50, method: TopUpMethod.card),
      )).isLeft(),
      isTrue,
    );
    expect(
      (await useCase(
        const TopUpParams(
          amount: 50,
          method: TopUpMethod.card,
          paymentMethodId: 'pm1',
        ),
      )).isRight(),
      isTrue,
    );
  });
}
