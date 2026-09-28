import 'dart:async';

import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/push/push_event.dart';
import 'package:ata_app/features/auth/domain/entities/user_role.dart';
import 'package:ata_app/features/auth/presentation/cubit/session_state.dart';
import 'package:ata_app/features/notifications/domain/entities/deep_link.dart';
import 'package:ata_app/features/notifications/domain/usecases/mark_notification_opened.dart';
import 'package:ata_app/features/notifications/domain/usecases/watch_opened_notifications.dart';
import 'package:ata_app/features/notifications/presentation/cubit/deep_link_cubit.dart';
import 'package:ata_app/features/notifications/presentation/cubit/deep_link_state.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:mocktail/mocktail.dart';

import '../../helpers/fakes.dart';

class _MockWatchOpened extends Mock implements WatchOpenedNotifications {}

class _MockMarkOpened extends Mock implements MarkNotificationOpened {}

void main() {
  late _MockWatchOpened watchOpened;
  late _MockMarkOpened markOpened;
  late StreamController<PushEvent> pushes;

  // Onboarded rider (terms accepted), so links are not parked.
  final SessionState rider = SessionState.authenticated(
    testSession.copyWith(
      user: testUser.copyWith(termsAcceptedAt: DateTime.utc(2026)),
    ),
  );
  final SessionState driver = SessionState.authenticated(
    testSession.copyWith(activeRole: UserRole.driver),
  );

  setUp(() {
    watchOpened = _MockWatchOpened();
    markOpened = _MockMarkOpened();
    pushes = StreamController<PushEvent>.broadcast();
    when(() => watchOpened()).thenAnswer((_) => pushes.stream);
    when(
      () => markOpened(any()),
    ).thenAnswer((_) async => const Right<Failure, Unit>(unit));
  });

  tearDown(() => pushes.close());

  DeepLinkCubit build(SessionState session) =>
      DeepLinkCubit(watchOpened: watchOpened, markOpened: markOpened)
        ..sessionChanged(session);

  blocTest<DeepLinkCubit, DeepLinkState>(
    'a link opened while signed in becomes the navigation target',
    build: () => build(rider),
    act: (DeepLinkCubit cubit) => cubit.open('ata://rides/t1/receipt'),
    expect: () => <DeepLinkState>[
      const DeepLinkState(target: DeepLink(route: '/rides/t1/receipt')),
    ],
  );

  blocTest<DeepLinkCubit, DeepLinkState>(
    'a link waits for the session, then resolves for the role',
    build: () => build(const SessionState.unknown()),
    act: (DeepLinkCubit cubit) async {
      await cubit.open('ata://driver/payouts', actionId: 'ok');
      cubit.sessionChanged(driver);
    },
    expect: () => <DeepLinkState>[
      const DeepLinkState(pendingLink: 'ata://driver/payouts', actionId: 'ok'),
      const DeepLinkState(
        target: DeepLink(route: '/driver/payouts'),
        actionId: 'ok',
      ),
    ],
  );

  blocTest<DeepLinkCubit, DeepLinkState>(
    'a tapped push opens its link and is reported as opened',
    build: () => build(rider)..start(),
    act: (_) => pushes.add(
      const PushEvent(deepLink: 'ata://wallet', notificationId: 'n1'),
    ),
    expect: () => <DeepLinkState>[
      const DeepLinkState(target: DeepLink(route: '/wallet')),
    ],
    verify: (_) => verify(() => markOpened('n1')).called(1),
  );

  blocTest<DeepLinkCubit, DeepLinkState>(
    'consuming an inbox link raises the inbox request',
    build: () => build(rider),
    act: (DeepLinkCubit cubit) async {
      await cubit.open('ata://notifications');
      cubit
        ..consumed()
        ..inboxShown();
    },
    expect: () => <DeepLinkState>[
      const DeepLinkState(target: DeepLink(route: '/home', opensInbox: true)),
      const DeepLinkState(inboxRequested: true),
      const DeepLinkState(),
    ],
  );

  blocTest<DeepLinkCubit, DeepLinkState>(
    'a push without a link is only reported as opened',
    build: () => build(rider),
    act: (DeepLinkCubit cubit) => cubit.open(null, notificationId: 'n2'),
    expect: () => <DeepLinkState>[],
    verify: (_) => verify(() => markOpened('n2')).called(1),
  );
}
