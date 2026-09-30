import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/support/domain/entities/ticket.dart';
import 'package:ata_app/features/support/domain/entities/ticket_enums.dart';
import 'package:equatable/equatable.dart';

/// State of `TicketsCubit`.
class TicketsState extends Equatable {
  const TicketsState({
    this.filter = TicketFilter.open,
    this.tickets = const <TicketSummary>[],
    this.page = 1,
    this.hasMore = false,
    this.loading = false,
    this.loadingMore = false,
    this.failure,
  });

  final TicketFilter filter;
  final List<TicketSummary> tickets;
  final int page;
  final bool hasMore;
  final bool loading;
  final bool loadingMore;
  final Failure? failure;

  /// Sum of the unread agent replies of the loaded tickets.
  int get unreadTotal =>
      tickets.fold(0, (int sum, TicketSummary t) => sum + t.unread);

  bool get isEmpty => tickets.isEmpty && !loading && failure == null;

  TicketsState copyWith({
    TicketFilter? filter,
    List<TicketSummary>? tickets,
    int? page,
    bool? hasMore,
    bool? loading,
    bool? loadingMore,
    Failure? failure,
    bool clearFailure = false,
  }) => TicketsState(
    filter: filter ?? this.filter,
    tickets: tickets ?? this.tickets,
    page: page ?? this.page,
    hasMore: hasMore ?? this.hasMore,
    loading: loading ?? this.loading,
    loadingMore: loadingMore ?? this.loadingMore,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[
    filter,
    tickets,
    page,
    hasMore,
    loading,
    loadingMore,
    failure,
  ];
}
