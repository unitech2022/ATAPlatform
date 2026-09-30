import 'dart:async';

import 'package:ata_app/features/support/domain/entities/ticket_enums.dart';
import 'package:ata_app/features/support/domain/usecases/get_ticket.dart';
import 'package:ata_app/features/support/domain/usecases/rate_ticket.dart';
import 'package:ata_app/features/support/domain/usecases/reply_to_ticket.dart';
import 'package:ata_app/features/support/domain/usecases/watch_ticket.dart';
import 'package:ata_app/features/support/presentation/cubit/csat_cubit.dart';
import 'package:ata_app/features/support/presentation/cubit/ticket_detail_cubit.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/support_fakes.dart';

void main() {
  late FakeSupportRepository repo;
  late StreamController<void> ticks;
  late TicketDetailCubit cubit;

  setUp(() {
    repo = FakeSupportRepository();
    ticks = StreamController<void>.broadcast();
    cubit = TicketDetailCubit(
      getTicket: GetTicket(repo),
      reply: ReplyToTicket(repo),
      watch: WatchTicket(repo),
      ticketId: 't1',
      ticker: (Duration _) => ticks.stream,
    );
  });
  tearDown(() async {
    await cubit.close();
    await ticks.close();
  });

  Future<void> settle() async {
    await Future<void>.delayed(Duration.zero);
    await Future<void>.delayed(Duration.zero);
  }

  group('loading', () {
    test('loads the thread (opening marks it read on the server)', () async {
      await cubit.start();
      expect(repo.opened, <String>['t1']);
      expect(cubit.state.detail?.messages, hasLength(2));
      expect(cubit.state.detail?.messages.last.isAgent, isTrue);
      expect(cubit.state.canReply, isTrue);
      expect(cubit.state.loading, isFalse);
    });

    test('a failure is shown and a retry recovers', () async {
      repo.ticketFailure = apiFailure('http_404');
      await cubit.start();
      expect(cubit.state.detail, isNull);
      expect(cubit.state.failure?.code, 'http_404');

      repo.ticketFailure = null;
      await cubit.refresh();
      expect(cubit.state.detail, isNotNull);
      expect(cubit.state.failure, isNull);
    });

    test('the hub refreshes only its own ticket', () async {
      await cubit.start();
      repo.details['t1'] = fakeTicketDetail(
        't1',
        status: TicketStatus.pendingUser,
      );
      repo.updates.add('other');
      await settle();
      expect(cubit.state.detail?.status, TicketStatus.open);

      repo.updates.add('t1');
      await settle();
      expect(cubit.state.detail?.status, TicketStatus.pendingUser);
    });

    test('a poll tick reloads silently every 15 seconds', () async {
      expect(TicketDetailCubit.pollInterval, const Duration(seconds: 15));
      await cubit.start();
      repo.details['t1'] = fakeTicketDetail(
        't1',
        status: TicketStatus.resolved,
      );
      ticks.add(null);
      await settle();
      expect(cubit.state.detail?.status, TicketStatus.resolved);
      expect(repo.opened, hasLength(2));
      expect(cubit.state.loading, isFalse);
    });

    test('a failing poll keeps the thread on screen', () async {
      await cubit.start();
      repo.ticketFailure = apiFailure('boom');
      ticks.add(null);
      await settle();
      expect(cubit.state.detail, isNotNull);
      expect(cubit.state.failure, isNull);
    });
  });

  group('replying', () {
    test('the reply is sent, appended and the draft cleared', () async {
      await cubit.start();
      cubit.draftChanged('  شكراً لكم  ');
      expect(cubit.state.canSend, isTrue);

      final bool ok = await cubit.send(fileIds: <String>['f1']);
      expect(ok, isTrue);
      expect(repo.replies.single.body, 'شكراً لكم');
      expect(repo.replies.single.fileIds, <String>['f1']);
      expect(cubit.state.draft, isEmpty);
      expect(cubit.state.draftVersion, 1);
      expect(cubit.state.detail?.messages.last.body, 'شكراً لكم');
      expect(cubit.state.detail?.messages.last.isMine, isTrue);
    });

    test('an empty draft cannot be sent', () async {
      await cubit.start();
      cubit.draftChanged('   ');
      expect(cubit.state.canSend, isFalse);
      expect(await cubit.send(), isFalse);
      expect(repo.replies, isEmpty);
    });

    test('nothing is sent before the thread loaded', () async {
      cubit.draftChanged('hi');
      expect(await cubit.send(), isFalse);
      expect(repo.replies, isEmpty);
    });

    test('a resolved ticket reopens on a reply', () async {
      repo.details['t1'] = fakeTicketDetail(
        't1',
        status: TicketStatus.resolved,
      );
      await cubit.start();
      cubit.draftChanged('لم تُحل');
      await cubit.send();
      expect(cubit.state.detail?.status, TicketStatus.open);
    });

    test('a failure keeps the draft', () async {
      repo.replyFailure = apiFailure('boom');
      await cubit.start();
      cubit.draftChanged('نص');
      expect(await cubit.send(), isFalse);
      expect(cubit.state.draft, 'نص');
      expect(cubit.state.sendFailure?.code, 'boom');
      expect(cubit.state.canReply, isTrue);

      cubit.draftChanged('نص 2');
      expect(cubit.state.sendFailure, isNull);
    });
  });

  group('closed tickets', () {
    test('a closed ticket has no reply box', () async {
      repo.details['t1'] = fakeTicketDetail(
        't1',
        status: TicketStatus.closed,
        canReply: false,
      );
      await cubit.start();
      expect(cubit.state.closed, isTrue);
      expect(cubit.state.canReply, isFalse);
      cubit.draftChanged('hi');
      expect(cubit.state.canSend, isFalse);
    });

    test('409 ticket_closed turns the thread read-only', () async {
      repo.replyFailure = apiFailure('ticket_closed');
      await cubit.start();
      cubit.draftChanged('hi');
      expect(await cubit.send(), isFalse);
      expect(cubit.state.closed, isTrue);
      expect(cubit.state.canReply, isFalse);
      expect(cubit.state.detail?.status, TicketStatus.closed);
      expect(cubit.state.sendFailure?.code, 'ticket_closed');
    });

    test(
      'a resolved ticket can still be answered unless the API says no',
      () async {
        repo.details['t1'] = fakeTicketDetail(
          't1',
          status: TicketStatus.resolved,
          canReply: false,
        );
        await cubit.start();
        expect(cubit.state.canReply, isFalse);
      },
    );
  });

  group('rating', () {
    test('rated stores the score and removes the prompt', () async {
      repo.details['t1'] = fakeTicketDetail(
        't1',
        status: TicketStatus.resolved,
        canRate: true,
      );
      await cubit.start();
      expect(cubit.state.detail?.ratable, isTrue);
      cubit.rated(4);
      expect(cubit.state.detail?.csatScore, 4);
      expect(cubit.state.detail?.ratable, isFalse);
    });
  });

  group('CsatCubit', () {
    CsatCubit csat() => CsatCubit(rate: RateTicket(repo), ticketId: 't1');

    test('needs a score of 1 to 5 before sending', () async {
      final CsatCubit c = csat();
      expect(c.state.canSubmit, isFalse);
      await c.submit();
      expect(repo.ratings, isEmpty);
      c.selectScore(9);
      expect(c.state.score, 5);
      c.selectScore(0);
      expect(c.state.score, 1);
      expect(c.state.canSubmit, isTrue);
      await c.close();
    });

    test('sends the score with the comment once', () async {
      final CsatCubit c = csat();
      c
        ..selectScore(4)
        ..commentChanged('سريعون');
      await c.submit();
      expect(repo.ratings.single.score, 4);
      expect(repo.ratings.single.comment, 'سريعون');
      expect(repo.ratings.single.ticketId, 't1');
      expect(c.state.submitted, isTrue);

      await c.submit();
      c.selectScore(1);
      expect(repo.ratings, hasLength(1));
      expect(c.state.score, 4);
      await c.close();
    });

    test('409 conflict means it was rated already', () async {
      repo.rateFailure = apiFailure('conflict');
      final CsatCubit c = csat()..selectScore(5);
      await c.submit();
      expect(c.state.submitted, isTrue);
      expect(c.state.failure, isNull);
      await c.close();
    });

    test('another failure allows another try', () async {
      repo.rateFailure = apiFailure('boom');
      final CsatCubit c = csat()..selectScore(5);
      await c.submit();
      expect(c.state.submitted, isFalse);
      expect(c.state.failure?.code, 'boom');

      repo.rateFailure = null;
      await c.submit();
      expect(c.state.submitted, isTrue);
      expect(c.state.failure, isNull);
      await c.close();
    });
  });
}
