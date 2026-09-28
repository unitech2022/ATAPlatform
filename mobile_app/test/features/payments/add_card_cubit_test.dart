import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/payments/data/tokenizers/sandbox_card_tokenizer.dart';
import 'package:ata_app/features/payments/domain/entities/card_details.dart';
import 'package:ata_app/features/payments/domain/entities/payment_action.dart';
import 'package:ata_app/features/payments/domain/usecases/add_payment_method.dart';
import 'package:ata_app/features/payments/presentation/cubit/add_card_cubit.dart';
import 'package:ata_app/features/payments/presentation/cubit/add_card_state.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/payments_fakes.dart';

void main() {
  late FakePaymentsRepository repository;

  setUp(() => repository = FakePaymentsRepository());

  AddCardCubit build() => AddCardCubit(
    addPaymentMethod: AddPaymentMethod(
      const SandboxCardTokenizer(),
      repository,
    ),
    now: () => DateTime(2026, 9, 28),
  );

  void fill(AddCardCubit cubit, {String number = '4406 4700 0000 0007'}) =>
      cubit
        ..numberChanged(number)
        ..expiryChanged('12/29')
        ..cvcChanged('123');

  blocTest<AddCardCubit, AddCardState>(
    'submitting an invalid form only reveals the errors',
    build: build,
    act: (AddCardCubit cubit) => cubit
      ..numberChanged('4111 1111 1111 1112')
      ..submit(),
    verify: (AddCardCubit cubit) {
      expect(cubit.state.showErrors, isTrue);
      expect(cubit.state.hasError(CardField.number), isTrue);
      expect(cubit.state.hasError(CardField.expiry), isTrue);
      expect(cubit.state.status, AddCardStatus.editing);
      expect(repository.tokens, isEmpty);
    },
  );

  blocTest<AddCardCubit, AddCardState>(
    'a valid card is tokenised and only the token reaches the API',
    build: build,
    act: (AddCardCubit cubit) async {
      fill(cubit);
      await cubit.submit();
    },
    verify: (AddCardCubit cubit) {
      expect(cubit.state.status, AddCardStatus.saved);
      expect(repository.tokens, <String>['tok_sandbox_mada']);
      expect(
        repository.tokens.any((String t) => t.contains('440647')),
        isFalse,
      );
    },
  );

  blocTest<AddCardCubit, AddCardState>(
    'a 3-D Secure card ends in requiresAction with the redirect',
    build: build,
    setUp: () => repository.nextAction = const PaymentAction(
      type: 'redirect',
      url: 'https://sandbox/challenge',
    ),
    act: (AddCardCubit cubit) async {
      fill(cubit, number: '4000 0000 0000 3220');
      await cubit.submit();
    },
    verify: (AddCardCubit cubit) {
      expect(cubit.state.status, AddCardStatus.requiresAction);
      expect(cubit.state.action?.url, 'https://sandbox/challenge');
      expect(repository.tokens, <String>['tok_sandbox_3ds']);
    },
  );

  blocTest<AddCardCubit, AddCardState>(
    'a declined card returns to the form with the failure',
    build: build,
    setUp: () => repository.failure = const ServerFailure(
      code: 'payment_failed',
      message: '',
    ),
    act: (AddCardCubit cubit) async {
      fill(cubit, number: '4000 0000 0000 0002');
      await cubit.submit();
    },
    verify: (AddCardCubit cubit) {
      expect(cubit.state.status, AddCardStatus.editing);
      expect(cubit.state.failure?.code, 'payment_failed');
    },
  );
}
