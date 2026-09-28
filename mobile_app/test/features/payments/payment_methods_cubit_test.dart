import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/payments/domain/entities/saved_card.dart';
import 'package:ata_app/features/payments/domain/usecases/get_payment_methods.dart';
import 'package:ata_app/features/payments/domain/usecases/remove_payment_method.dart';
import 'package:ata_app/features/payments/domain/usecases/set_default_payment_method.dart';
import 'package:ata_app/features/payments/presentation/cubit/payment_methods_cubit.dart';
import 'package:ata_app/features/payments/presentation/cubit/payment_methods_state.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:mocktail/mocktail.dart';

import '../../helpers/payments_fakes.dart';

class _MockGet extends Mock implements GetPaymentMethods {}

class _MockSetDefault extends Mock implements SetDefaultPaymentMethod {}

class _MockRemove extends Mock implements RemovePaymentMethod {}

void main() {
  late _MockGet getCards;
  late _MockSetDefault setDefault;
  late _MockRemove remove;

  const List<SavedCard> cards = <SavedCard>[testMada, testVisa];

  setUpAll(() => registerFallbackValue(const NoParams()));

  setUp(() {
    getCards = _MockGet();
    setDefault = _MockSetDefault();
    remove = _MockRemove();
  });

  PaymentMethodsCubit build() => PaymentMethodsCubit(
    getPaymentMethods: getCards,
    setDefault: setDefault,
    remove: remove,
  );

  blocTest<PaymentMethodsCubit, PaymentMethodsState>(
    'load lists the saved cards',
    build: build,
    setUp: () => when(
      () => getCards(any()),
    ).thenAnswer((_) async => const Right<Failure, List<SavedCard>>(cards)),
    act: (PaymentMethodsCubit cubit) => cubit.load(),
    expect: () => <PaymentMethodsState>[
      const PaymentMethodsState(loading: true),
      const PaymentMethodsState(cards: cards),
    ],
  );

  blocTest<PaymentMethodsCubit, PaymentMethodsState>(
    'setDefault moves the default flag to the chosen card',
    build: build,
    seed: () => const PaymentMethodsState(cards: cards),
    setUp: () => when(() => setDefault('pm2')).thenAnswer(
      (_) async =>
          Right<Failure, SavedCard>(testVisa.copyWith(isDefault: true)),
    ),
    act: (PaymentMethodsCubit cubit) => cubit.setDefault('pm2'),
    expect: () => <PaymentMethodsState>[
      const PaymentMethodsState(cards: cards, busyId: 'pm2'),
      PaymentMethodsState(
        cards: <SavedCard>[
          testMada.copyWith(isDefault: false),
          testVisa.copyWith(isDefault: true),
        ],
      ),
    ],
  );

  blocTest<PaymentMethodsCubit, PaymentMethodsState>(
    'remove drops the card',
    build: build,
    seed: () => const PaymentMethodsState(cards: cards),
    setUp: () => when(
      () => remove('pm1'),
    ).thenAnswer((_) async => const Right<Failure, Unit>(unit)),
    act: (PaymentMethodsCubit cubit) => cubit.remove('pm1'),
    verify: (PaymentMethodsCubit cubit) =>
        expect(cubit.state.cards, <SavedCard>[testVisa]),
  );

  blocTest<PaymentMethodsCubit, PaymentMethodsState>(
    'a card in use is kept and the failure is shown',
    build: build,
    seed: () => const PaymentMethodsState(cards: cards),
    setUp: () => when(() => remove('pm1')).thenAnswer(
      (_) async => const Left<Failure, Unit>(
        ServerFailure(code: 'payment_method_in_use', message: ''),
      ),
    ),
    act: (PaymentMethodsCubit cubit) => cubit.remove('pm1'),
    verify: (PaymentMethodsCubit cubit) {
      expect(cubit.state.cards, cards);
      expect(cubit.state.busyId, isNull);
      expect(cubit.state.actionFailure?.code, 'payment_method_in_use');
    },
  );
}
