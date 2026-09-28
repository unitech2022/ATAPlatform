import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/safety/domain/entities/trusted_contact.dart';
import 'package:ata_app/features/safety/domain/usecases/add_trusted_contact.dart';
import 'package:ata_app/features/safety/domain/usecases/delete_trusted_contact.dart';
import 'package:ata_app/features/safety/domain/usecases/get_trusted_contacts.dart';
import 'package:ata_app/features/safety/domain/usecases/update_trusted_contact.dart';
import 'package:ata_app/features/safety/presentation/cubit/trusted_contacts_cubit.dart';
import 'package:ata_app/features/safety/presentation/cubit/trusted_contacts_state.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:mocktail/mocktail.dart';

class _MockGet extends Mock implements GetTrustedContacts {}

class _MockAdd extends Mock implements AddTrustedContact {}

class _MockUpdate extends Mock implements UpdateTrustedContact {}

class _MockDelete extends Mock implements DeleteTrustedContact {}

TrustedContact contact(int i, {bool autoShare = false}) => TrustedContact(
  id: 'c$i',
  name: 'جهة $i',
  phoneNumber: '+96650000000$i',
  autoShare: autoShare,
);

void main() {
  late _MockGet getContacts;
  late _MockAdd add;
  late _MockUpdate update;
  late _MockDelete delete;

  setUpAll(() {
    registerFallbackValue(const NoParams());
    registerFallbackValue(const TrustedContactDraft());
    registerFallbackValue(
      const UpdateTrustedContactParams(id: '', draft: TrustedContactDraft()),
    );
  });

  setUp(() {
    getContacts = _MockGet();
    add = _MockAdd();
    update = _MockUpdate();
    delete = _MockDelete();
  });

  TrustedContactsCubit build() => TrustedContactsCubit(
    getContacts: getContacts,
    addContact: add,
    updateContact: update,
    deleteContact: delete,
  );

  blocTest<TrustedContactsCubit, TrustedContactsState>(
    'validates the form, normalizes the phone to E.164 and adds',
    build: build,
    setUp: () => when(
      () => add(any()),
    ).thenAnswer((_) async => Right<Failure, TrustedContact>(contact(1))),
    act: (TrustedContactsCubit cubit) async {
      cubit
        ..startAdd()
        ..phoneChanged('123');
      await cubit.save();
      expect(cubit.state.form!.errors, <ContactFieldError>{
        ContactFieldError.nameRequired,
        ContactFieldError.phoneInvalid,
      });
      cubit
        ..nameChanged('سارة')
        ..phoneChanged('0551234567')
        ..autoShareChanged(true);
      await cubit.save();
    },
    verify: (TrustedContactsCubit cubit) {
      final TrustedContactDraft draft =
          verify(() => add(captureAny())).captured.single
              as TrustedContactDraft;
      expect(draft.phoneNumber, '+966551234567');
      expect(draft.autoShare, isTrue);
      expect(cubit.state.form, isNull);
      expect(cubit.state.contacts, <TrustedContact>[contact(1)]);
    },
  );

  blocTest<TrustedContactsCubit, TrustedContactsState>(
    'the sixth contact cannot be started (limit 5)',
    build: build,
    setUp: () => when(() => getContacts(any())).thenAnswer(
      (_) async => Right<Failure, List<TrustedContact>>(
        List<TrustedContact>.generate(5, contact),
      ),
    ),
    act: (TrustedContactsCubit cubit) async {
      await cubit.load();
      cubit.startAdd();
    },
    verify: (TrustedContactsCubit cubit) {
      expect(cubit.state.canAdd, isFalse);
      expect(cubit.state.form, isNull);
    },
  );

  blocTest<TrustedContactsCubit, TrustedContactsState>(
    'the API refusing the own number flags the phone field',
    build: build,
    setUp: () => when(() => add(any())).thenAnswer(
      (_) async => const Left<Failure, TrustedContact>(
        ServerFailure(
          code: 'validation_failed',
          message: '',
          details: <String, dynamic>{'phoneNumber': 'self'},
        ),
      ),
    ),
    act: (TrustedContactsCubit cubit) async {
      cubit
        ..startAdd()
        ..nameChanged('أنا')
        ..phoneChanged('512345678');
      await cubit.save();
    },
    verify: (TrustedContactsCubit cubit) => expect(
      cubit.state.form!.errors,
      <ContactFieldError>{ContactFieldError.phoneSelf},
    ),
  );

  blocTest<TrustedContactsCubit, TrustedContactsState>(
    'auto-share flips optimistically and reverts on failure',
    build: build,
    seed: () => TrustedContactsState(contacts: <TrustedContact>[contact(1)]),
    setUp: () => when(() => update(any())).thenAnswer(
      (_) async => const Left<Failure, TrustedContact>(
        NetworkFailure(message: 'offline'),
      ),
    ),
    act: (TrustedContactsCubit cubit) => cubit.toggleAutoShare(contact(1)),
    expect: () => <TrustedContactsState>[
      TrustedContactsState(
        contacts: <TrustedContact>[contact(1, autoShare: true)],
        busyId: 'c1',
      ),
      TrustedContactsState(
        contacts: <TrustedContact>[contact(1)],
        failure: const NetworkFailure(message: 'offline'),
      ),
    ],
  );

  blocTest<TrustedContactsCubit, TrustedContactsState>(
    'delete removes the contact',
    build: build,
    seed: () => TrustedContactsState(
      contacts: <TrustedContact>[contact(1), contact(2)],
    ),
    setUp: () => when(
      () => delete('c1'),
    ).thenAnswer((_) async => const Right<Failure, Unit>(unit)),
    act: (TrustedContactsCubit cubit) => cubit.delete('c1'),
    verify: (TrustedContactsCubit cubit) =>
        expect(cubit.state.contacts, <TrustedContact>[contact(2)]),
  );
}
