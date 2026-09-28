import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/safety/domain/entities/trip_share.dart';
import 'package:ata_app/features/safety/domain/entities/trusted_contact.dart';
import 'package:ata_app/features/safety/domain/usecases/create_trip_share.dart';
import 'package:ata_app/features/safety/domain/usecases/get_trip_shares.dart';
import 'package:ata_app/features/safety/domain/usecases/get_trusted_contacts.dart';
import 'package:ata_app/features/safety/domain/usecases/revoke_trip_share.dart';
import 'package:ata_app/features/safety/presentation/cubit/trip_share_cubit.dart';
import 'package:ata_app/features/safety/presentation/cubit/trip_share_state.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:mocktail/mocktail.dart';

class _MockCreate extends Mock implements CreateTripShare {}

class _MockGetShares extends Mock implements GetTripShares {}

class _MockRevoke extends Mock implements RevokeTripShare {}

class _MockContacts extends Mock implements GetTrustedContacts {}

void main() {
  late _MockCreate create;
  late _MockGetShares getShares;
  late _MockRevoke revoke;
  late _MockContacts contacts;
  final DateTime now = DateTime.utc(2026, 9, 28, 12);

  const TripShare link = TripShare(id: 's1', url: 'https://ata.sa/t/Xy12');
  const TrustedContact mom = TrustedContact(
    id: 'c1',
    name: 'أمي',
    phoneNumber: '+966500000001',
  );

  setUpAll(() {
    registerFallbackValue(const NoParams());
    registerFallbackValue(const CreateTripShareParams(tripId: ''));
  });

  setUp(() {
    create = _MockCreate();
    getShares = _MockGetShares();
    revoke = _MockRevoke();
    contacts = _MockContacts();
  });

  TripShareCubit build() => TripShareCubit(
    createShare: create,
    getShares: getShares,
    revokeShare: revoke,
    getContacts: contacts,
    tripId: 't1',
    now: () => now,
  );

  blocTest<TripShareCubit, TripShareState>(
    'shareLink creates a link and asks the page to share its URL',
    build: build,
    setUp: () => when(() => create(any())).thenAnswer(
      (_) async => const Right<Failure, List<TripShare>>(<TripShare>[link]),
    ),
    act: (TripShareCubit cubit) => cubit.shareLink(),
    expect: () => const <TripShareState>[
      TripShareState(creating: true),
      TripShareState(
        shares: <TripShare>[link],
        linkToShare: 'https://ata.sa/t/Xy12',
        shareRequest: 1,
      ),
    ],
    verify: (_) {
      final CreateTripShareParams p =
          verify(() => create(captureAny())).captured.single
              as CreateTripShareParams;
      expect(p.tripId, 't1');
      expect(p.channel, ShareChannel.link);
    },
  );

  blocTest<TripShareCubit, TripShareState>(
    'sends SMS links to the selected trusted contacts',
    build: build,
    setUp: () {
      when(() => getShares('t1')).thenAnswer(
        (_) async => const Right<Failure, List<TripShare>>(<TripShare>[]),
      );
      when(() => contacts(any())).thenAnswer(
        (_) async =>
            const Right<Failure, List<TrustedContact>>(<TrustedContact>[mom]),
      );
      when(() => create(any())).thenAnswer(
        (_) async => const Right<Failure, List<TripShare>>(<TripShare>[
          TripShare(
            id: 's2',
            url: 'https://ata.sa/t/Ab',
            channel: 'sms',
            trustedContactId: 'c1',
          ),
        ]),
      );
    },
    act: (TripShareCubit cubit) async {
      await cubit.load();
      cubit.toggleContact('c1');
      await cubit.sendToContacts();
    },
    verify: (TripShareCubit cubit) {
      final CreateTripShareParams p =
          verify(() => create(captureAny())).captured.single
              as CreateTripShareParams;
      expect(p.channel, ShareChannel.sms);
      expect(p.contactIds, <String>['c1']);
      expect(cubit.state.smsSent, 1);
      expect(cubit.state.selectedContactIds, isEmpty);
      expect(cubit.state.activeShares, hasLength(1));
    },
  );

  blocTest<TripShareCubit, TripShareState>(
    'revoke marks the link revoked',
    build: build,
    seed: () => const TripShareState(shares: <TripShare>[link]),
    setUp: () => when(
      () => revoke('s1'),
    ).thenAnswer((_) async => const Right<Failure, Unit>(unit)),
    act: (TripShareCubit cubit) => cubit.revoke('s1'),
    verify: (TripShareCubit cubit) {
      expect(cubit.state.shares.single.revokedAt, now);
      expect(cubit.state.activeShares, isEmpty);
    },
  );
}
