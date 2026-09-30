import 'dart:async';

import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/features/support/domain/entities/ticket.dart';
import 'package:ata_app/features/support/domain/entities/ticket_enums.dart';
import 'package:ata_app/features/support/domain/entities/ticket_requests.dart';
import 'package:ata_app/features/support/domain/usecases/get_tickets.dart';
import 'package:ata_app/features/support/domain/usecases/watch_ticket.dart';
import 'package:ata_app/features/support/presentation/cubit/tickets_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// "تذاكري": the user's tickets (open / closed tabs, paging) with unread
/// counters, refreshed by the hub's `SupportTicketUpdated`.
class TicketsCubit extends Cubit<TicketsState> {
  TicketsCubit({required this._getTickets, required this._watch})
    : super(const TicketsState());

  final GetTickets _getTickets;
  final WatchTicket _watch;

  StreamSubscription<String>? _updates;
  int _generation = 0;

  /// Loads the first page and starts following live updates.
  Future<void> start() async {
    _updates ??= _watch().listen((_) => unawaited(refresh(silent: true)));
    await refresh();
  }

  Future<void> refresh({bool silent = false}) async {
    final int generation = ++_generation;
    if (!silent) emit(state.copyWith(loading: true, clearFailure: true));
    final result = await _getTickets(TicketsQuery(filter: state.filter));
    if (isClosed || generation != _generation) return;
    emit(
      result.fold(
        (failure) =>
            silent ? state : state.copyWith(loading: false, failure: failure),
        (PageResult<TicketSummary> p) => state.copyWith(
          loading: false,
          tickets: p.items,
          page: 1,
          hasMore: p.items.isNotEmpty && p.hasMore,
          clearFailure: true,
        ),
      ),
    );
  }

  Future<void> setFilter(TicketFilter filter) async {
    if (filter == state.filter) return;
    emit(state.copyWith(filter: filter, tickets: const <TicketSummary>[]));
    await refresh();
  }

  Future<void> loadMore() async {
    if (state.loading || state.loadingMore || !state.hasMore) return;
    final int generation = ++_generation;
    emit(state.copyWith(loadingMore: true));
    final int next = state.page + 1;
    final result = await _getTickets(
      TicketsQuery(filter: state.filter, page: next),
    );
    if (isClosed || generation != _generation) return;
    emit(
      result.fold(
        (failure) => state.copyWith(loadingMore: false, failure: failure),
        (PageResult<TicketSummary> p) => state.copyWith(
          loadingMore: false,
          tickets: <TicketSummary>[...state.tickets, ...p.items],
          page: next,
          hasMore: p.items.isNotEmpty && p.hasMore,
        ),
      ),
    );
  }

  /// Opening a ticket zeroes its unread counter (the API does the same);
  /// applied right away so the badge does not linger.
  void markRead(String ticketId) {
    emit(
      state.copyWith(
        tickets: state.tickets
            .map(
              (TicketSummary t) =>
                  t.id == ticketId && t.hasUnread ? t.copyWith(unread: 0) : t,
            )
            .toList(growable: false),
      ),
    );
  }

  @override
  Future<void> close() async {
    await _updates?.cancel();
    return super.close();
  }
}
