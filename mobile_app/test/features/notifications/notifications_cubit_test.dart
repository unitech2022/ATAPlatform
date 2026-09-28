import 'dart:async';

import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/push/push_event.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/notifications/domain/entities/notification_item.dart';
import 'package:ata_app/features/notifications/domain/usecases/get_notifications.dart';
import 'package:ata_app/features/notifications/domain/usecases/mark_notifications_read.dart';
import 'package:ata_app/features/notifications/domain/usecases/watch_incoming_notifications.dart';
import 'package:ata_app/features/notifications/presentation/cubit/notifications_cubit.dart';
import 'package:ata_app/features/notifications/presentation/cubit/notifications_state.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:mocktail/mocktail.dart';

class _MockGet extends Mock implements GetNotifications {}

class _MockMarkRead extends Mock implements MarkNotificationsRead {}

class _MockWatch extends Mock implements WatchIncomingNotifications {}

void main() {
  late _MockGet getNotifications;
  late _MockMarkRead markRead;
  late _MockWatch watch;
  late StreamController<PushEvent> incoming;

  final DateTime now = DateTime.utc(2026, 9, 28);
  final NotificationItem unread = NotificationItem(
    id: 'n1',
    type: 'wallet.topup',
    title: 't',
    body: 'b',
    createdAt: now,
  );
  final NotificationsPage page = NotificationsPage(
    items: <NotificationItem>[unread],
    unreadCount: 1,
  );

  setUpAll(() => registerFallbackValue(const NoParams()));

  setUp(() {
    getNotifications = _MockGet();
    markRead = _MockMarkRead();
    watch = _MockWatch();
    incoming = StreamController<PushEvent>.broadcast();
    when(() => watch()).thenAnswer((_) => incoming.stream);
    when(
      () => getNotifications(any()),
    ).thenAnswer((_) async => Right<Failure, NotificationsPage>(page));
  });

  tearDown(() => incoming.close());

  NotificationsCubit build() => NotificationsCubit(
    getNotifications: getNotifications,
    markRead: markRead,
    watchIncoming: watch,
    now: () => now,
  );

  blocTest<NotificationsCubit, NotificationsState>(
    'a foreground push reloads the inbox',
    build: () => build()..watchPushes(),
    act: (_) => incoming.add(const PushEvent(eventCode: 'wallet.topup')),
    wait: Duration.zero,
    expect: () => <NotificationsState>[
      const NotificationsState(loading: true),
      NotificationsState(items: page.items, unreadCount: 1),
    ],
    verify: (_) => verify(() => getNotifications(any())).called(1),
  );

  blocTest<NotificationsCubit, NotificationsState>(
    'markItemRead marks one row read and decrements the badge',
    build: build,
    seed: () => NotificationsState(items: page.items, unreadCount: 1),
    act: (NotificationsCubit cubit) => cubit
      ..markItemRead('n1')
      ..markItemRead('n1'),
    expect: () => <NotificationsState>[
      NotificationsState(
        items: <NotificationItem>[unread.markedRead(now)],
        unreadCount: 0,
      ),
    ],
  );
}
