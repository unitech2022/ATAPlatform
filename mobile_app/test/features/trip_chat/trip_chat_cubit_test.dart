import 'dart:async';

import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip_chat/domain/entities/trip_message.dart';
import 'package:ata_app/features/trip_chat/domain/usecases/get_quick_replies.dart';
import 'package:ata_app/features/trip_chat/domain/usecases/mark_messages_read.dart';
import 'package:ata_app/features/trip_chat/domain/usecases/send_trip_message.dart';
import 'package:ata_app/features/trip_chat/domain/usecases/watch_trip_messages.dart';
import 'package:ata_app/features/trip_chat/presentation/cubit/trip_chat_cubit.dart';
import 'package:ata_app/features/trip_chat/presentation/cubit/trip_chat_state.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:mocktail/mocktail.dart';

class _MockWatch extends Mock implements WatchTripMessages {}

class _MockSend extends Mock implements SendTripMessage {}

class _MockRead extends Mock implements MarkMessagesRead {}

class _MockReplies extends Mock implements GetQuickReplies {}

void main() {
  late _MockWatch watch;
  late _MockSend send;
  late _MockRead read;
  late _MockReplies replies;
  late StreamController<List<TripMessage>> feed;
  final DateTime t0 = DateTime.utc(2026, 9, 28, 12);

  const ChatTarget target = ChatTarget(
    tripId: 't1',
    actor: TripActor.passenger,
  );
  const QuickReply comingNow = QuickReply(
    code: 'coming_now',
    text: 'قادم الآن',
  );

  TripMessage incoming(String id, int minute) => TripMessage(
    id: id,
    tripId: 't1',
    body: 'وصلت إلى نقطة الالتقاط',
    senderRole: 'driver',
    kind: 'quick_reply',
    createdAt: t0.add(Duration(minutes: minute)),
  );

  setUpAll(() {
    registerFallbackValue(target);
    registerFallbackValue(const SendMessageParams(target: target));
    registerFallbackValue(const MarkReadParams(target: target, upToId: ''));
  });

  setUp(() {
    watch = _MockWatch();
    send = _MockSend();
    read = _MockRead();
    replies = _MockReplies();
    feed = StreamController<List<TripMessage>>.broadcast();
    when(() => watch(target)).thenAnswer((_) => feed.stream);
    when(() => replies(TripActor.passenger)).thenAnswer(
      (_) async =>
          const Right<Failure, List<QuickReply>>(<QuickReply>[comingNow]),
    );
    when(
      () => read(any()),
    ).thenAnswer((_) async => const Right<Failure, Unit>(unit));
  });

  tearDown(() => feed.close());

  TripChatCubit build() => TripChatCubit(
    watchMessages: watch,
    send: send,
    markRead: read,
    getQuickReplies: replies,
    now: () => t0.add(const Duration(minutes: 10)),
  );

  Future<void> push(List<TripMessage> batch) async {
    feed.add(batch);
    await Future<void>.delayed(Duration.zero);
  }

  blocTest<TripChatCubit, TripChatState>(
    'counts unread messages until the chat is opened, then marks them read',
    build: build,
    act: (TripChatCubit cubit) async {
      await cubit.bind(target);
      await push(<TripMessage>[incoming('m1', 1)]);
      await push(<TripMessage>[incoming('m1', 1), incoming('m2', 2)]);
      expect(cubit.state.unreadCount, 2);
      expect(cubit.state.quickReplies, <QuickReply>[comingNow]);
      cubit.open();
      expect(cubit.state.unreadCount, 0);
      await push(<TripMessage>[incoming('m3', 3)]);
    },
    verify: (TripChatCubit cubit) {
      expect(cubit.state.unreadCount, 0);
      expect(cubit.state.messages.map((TripMessage m) => m.id), <String>[
        'm1',
        'm2',
        'm3',
      ]);
      final List<dynamic> calls = verify(() => read(captureAny())).captured;
      expect(calls.map((dynamic p) => (p as MarkReadParams).upToId), <String>[
        'm2',
        'm3',
      ]);
    },
  );

  blocTest<TripChatCubit, TripChatState>(
    'sends optimistically and replaces the pending bubble with the server copy',
    build: build,
    setUp: () => when(() => send(any())).thenAnswer(
      (_) async => Right<Failure, TripMessage>(
        TripMessage(
          id: 'srv1',
          tripId: 't1',
          body: 'أنا عند البوابة',
          senderRole: 'passenger',
          isMine: true,
          createdAt: t0.add(const Duration(minutes: 10)),
        ),
      ),
    ),
    act: (TripChatCubit cubit) async {
      await cubit.bind(target);
      cubit.draftChanged('  أنا عند البوابة ');
      final Future<void> sending = cubit.send();
      expect(cubit.state.draft, isEmpty);
      expect(cubit.state.draftVersion, 1);
      expect(cubit.state.messages.single.delivery, MessageDelivery.pending);
      await sending;
    },
    verify: (TripChatCubit cubit) {
      expect(cubit.state.messages.single.id, 'srv1');
      final SendMessageParams p =
          verify(() => send(captureAny())).captured.single as SendMessageParams;
      expect(p.body, 'أنا عند البوابة');
      expect(p.quickReplyCode, isNull);
    },
  );

  blocTest<TripChatCubit, TripChatState>(
    'chat_closed marks the bubble failed and the chat read-only',
    build: build,
    setUp: () => when(() => send(any())).thenAnswer(
      (_) async => const Left<Failure, TripMessage>(
        ServerFailure(code: 'chat_closed', message: '', statusCode: 409),
      ),
    ),
    act: (TripChatCubit cubit) async {
      await cubit.bind(target);
      await cubit.sendQuickReply(comingNow);
    },
    verify: (TripChatCubit cubit) {
      expect(cubit.state.closed, isTrue);
      expect(cubit.state.messages.single.delivery, MessageDelivery.failed);
      expect(cubit.state.messages.single.quickReplyCode, 'coming_now');
    },
  );

  blocTest<TripChatCubit, TripChatState>(
    'unbind clears the chat when the trip ends',
    build: build,
    act: (TripChatCubit cubit) async {
      await cubit.bind(target);
      await push(<TripMessage>[incoming('m1', 1)]);
      await cubit.unbind();
    },
    verify: (TripChatCubit cubit) => expect(cubit.state, const TripChatState()),
  );
}
