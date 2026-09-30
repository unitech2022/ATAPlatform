import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/support/domain/entities/ticket.dart';
import 'package:equatable/equatable.dart';

/// State of `TicketDetailCubit`.
class TicketDetailState extends Equatable {
  const TicketDetailState({
    this.detail,
    this.loading = false,
    this.failure,
    this.draft = '',
    this.draftVersion = 0,
    this.sending = false,
    this.sendFailure,
    this.closed = false,
  });

  static const int maxLength = 4000;

  final TicketDetail? detail;
  final bool loading;
  final Failure? failure;
  final String draft;

  /// Bumped when a sent draft is cleared (rebuilds the input empty).
  final int draftVersion;
  final bool sending;
  final Failure? sendFailure;

  /// `409 ticket_closed` came back: the thread is read-only.
  final bool closed;

  /// The reply box accepts messages (`canReply`, not closed).
  bool get canReply => !closed && (detail?.replyEnabled ?? false);

  bool get canSend => canReply && !sending && draft.trim().isNotEmpty;

  TicketDetailState copyWith({
    TicketDetail? detail,
    bool? loading,
    Failure? failure,
    bool clearFailure = false,
    String? draft,
    int? draftVersion,
    bool? sending,
    Failure? sendFailure,
    bool clearSendFailure = false,
    bool? closed,
  }) => TicketDetailState(
    detail: detail ?? this.detail,
    loading: loading ?? this.loading,
    failure: clearFailure ? null : failure ?? this.failure,
    draft: draft ?? this.draft,
    draftVersion: draftVersion ?? this.draftVersion,
    sending: sending ?? this.sending,
    sendFailure: clearSendFailure ? null : sendFailure ?? this.sendFailure,
    closed: closed ?? this.closed,
  );

  @override
  List<Object?> get props => <Object?>[
    detail,
    loading,
    failure,
    draft,
    draftVersion,
    sending,
    sendFailure,
    closed,
  ];
}
