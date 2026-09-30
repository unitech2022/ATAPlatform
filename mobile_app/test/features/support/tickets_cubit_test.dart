import 'package:ata_app/features/support/domain/entities/ticket.dart';
import 'package:ata_app/features/support/domain/entities/ticket_enums.dart';
import 'package:ata_app/features/support/domain/usecases/get_tickets.dart';
import 'package:ata_app/features/support/domain/usecases/watch_ticket.dart';
import 'package:ata_app/features/support/presentation/cubit/tickets_cubit.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/support_fakes.dart';

void main() {
  late FakeSupportRepository repo;
  late TicketsCubit cubit;

  setUp(() {
    repo = FakeSupportRepository()
      ..tickets = <TicketSummary>[
        fakeTicketSummary('1', unread: 2),
        fakeTicketSummary('2', status: TicketStatus.pendingUser, unread: 1),
        fakeTicketSummary('3', status: TicketStatus.resolved),
        fakeTicketSummary('4', status: TicketStatus.closed),
      ];
    cubit = TicketsCubit(
      getTickets: GetTickets(repo),
      watch: WatchTicket(repo),
    );
  });
  tearDown(() => cubit.close());

  test('starts with the open tab: everything that is not closed', () async {
    await cubit.start();
    expect(repo.ticketQueries.single.filter, TicketFilter.open);
    expect(cubit.state.tickets.map((TicketSummary t) => t.id), <String>[
      '1',
      '2',
      '3',
    ]);
    expect(cubit.state.loading, isFalse);
    expect(cubit.state.isEmpty, isFalse);
  });

  test('the unread total sums the loaded tickets', () async {
    await cubit.start();
    expect(cubit.state.unreadTotal, 3);
    expect(cubit.state.tickets.first.hasUnread, isTrue);
  });

  test('opening a ticket clears its unread badge', () async {
    await cubit.start();
    cubit.markRead('1');
    expect(cubit.state.tickets.first.unread, 0);
    expect(cubit.state.unreadTotal, 1);
    // Unknown ids and read tickets change nothing.
    cubit.markRead('zzz');
    cubit.markRead('3');
    expect(cubit.state.unreadTotal, 1);
  });

  test('the closed tab lists the closed tickets', () async {
    await cubit.start();
    await cubit.setFilter(TicketFilter.closed);
    expect(cubit.state.filter, TicketFilter.closed);
    expect(cubit.state.tickets.single.id, '4');
    await cubit.setFilter(TicketFilter.closed);
    expect(repo.ticketQueries, hasLength(2));
  });

  test('an empty list is reported as empty, not as a failure', () async {
    repo.tickets = <TicketSummary>[];
    await cubit.start();
    expect(cubit.state.isEmpty, isTrue);
    expect(cubit.state.failure, isNull);
  });

  test('a failure is kept and a refresh recovers', () async {
    repo.ticketsFailure = apiFailure('boom');
    await cubit.start();
    expect(cubit.state.failure?.code, 'boom');
    expect(cubit.state.isEmpty, isFalse);

    repo.ticketsFailure = null;
    await cubit.refresh();
    expect(cubit.state.failure, isNull);
    expect(cubit.state.tickets, hasLength(3));
  });

  test('load more appends the next page', () async {
    repo.pageSize = 2;
    await cubit.start();
    expect(cubit.state.tickets, hasLength(2));
    expect(cubit.state.hasMore, isTrue);

    await cubit.loadMore();
    expect(cubit.state.tickets, hasLength(3));
    expect(cubit.state.hasMore, isFalse);
    expect(repo.ticketQueries.last.page, 2);

    await cubit.loadMore();
    expect(repo.ticketQueries, hasLength(2));
  });

  test('a live update refreshes the list silently', () async {
    await cubit.start();
    repo.tickets = <TicketSummary>[fakeTicketSummary('1', unread: 5)];
    repo.updates.add('1');
    await Future<void>.delayed(Duration.zero);
    await Future<void>.delayed(Duration.zero);
    expect(cubit.state.tickets.single.unread, 5);
    expect(cubit.state.loading, isFalse);
  });

  test('a silent refresh failure keeps what is shown', () async {
    await cubit.start();
    repo.ticketsFailure = apiFailure('boom');
    await cubit.refresh(silent: true);
    expect(cubit.state.failure, isNull);
    expect(cubit.state.tickets, hasLength(3));
  });
}
