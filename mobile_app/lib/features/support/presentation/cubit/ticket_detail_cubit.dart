import 'dart:async';

import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/utils/countdown.dart';
import 'package:ata_app/features/support/domain/entities/ticket.dart';
import 'package:ata_app/features/support/domain/entities/ticket_enums.dart';
import 'package:ata_app/features/support/domain/entities/ticket_requests.dart';
import 'package:ata_app/features/support/domain/usecases/get_ticket.dart';
import 'package:ata_app/features/support/domain/usecases/reply_to_ticket.dart';
import 'package:ata_app/features/support/domain/usecases/watch_ticket.dart';
import 'package:ata_app/features/support/presentation/cubit/ticket_detail_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// One ticket thread (`GET /support/tickets/{id}`): loading it marks it read
/// on the server, the hub's `SupportTicketUpdated` and a 15 s poll keep it
/// fresh while it is open, replies go through `POST …/messages` and a
/// `409 ticket_closed` turns the thread read-only.
class TicketDetailCubit extends Cubit<TicketDetailState> {
  TicketDetailCubit({
    required this._getTicket,
    required this._reply,
    required this._watch,
    required this.ticketId,
    this._ticker = periodicTicker,
  }) : super(const TicketDetailState());

  static const Duration pollInterval = Duration(seconds: 15);

  final GetTicket _getTicket;
  final ReplyToTicket _reply;
  final WatchTicket _watch;
  final String ticketId;
  final Ticker _ticker;

  StreamSubscription<String>? _updates;
  StreamSubscription<void>? _poll;

  /// Loads the thread and follows it. Idempotent.
  Future<void> start() async {
    _updates ??= _watch().listen((String id) {
      if (id == ticketId) unawaited(refresh(silent: true));
    });
    _poll ??= _ticker(
      pollInterval,
    ).listen((_) => unawaited(refresh(silent: true)));
    await refresh();
  }

  Future<void> refresh({bool silent = false}) async {
    if (!silent) emit(state.copyWith(loading: true, clearFailure: true));
    final result = await _getTicket(ticketId);
    if (isClosed) return;
    emit(
      result.fold(
        (Failure f) =>
            silent ? state : state.copyWith(loading: false, failure: f),
        (TicketDetail d) => state.copyWith(
          loading: false,
          detail: d,
          clearFailure: true,
          closed: d.isClosed,
        ),
      ),
    );
  }

  void draftChanged(String text) =>
      emit(state.copyWith(draft: text, clearSendFailure: true));

  /// Sends the draft with the already uploaded [fileIds]. Returns whether
  /// the message went through (the page then clears its attachments).
  Future<bool> send({List<String> fileIds = const <String>[]}) async {
    final TicketDetail? detail = state.detail;
    if (detail == null || !state.canSend) return false;
    emit(state.copyWith(sending: true, clearSendFailure: true));
    final result = await _reply(
      TicketReplyRequest(
        ticketId: ticketId,
        body: state.draft.trim(),
        fileIds: fileIds,
      ),
    );
    if (isClosed) return false;
    return result.fold(
      (Failure f) {
        final bool nowClosed = f.code == ErrorCodes.ticketClosed;
        emit(
          state.copyWith(
            sending: false,
            sendFailure: f,
            closed: nowClosed ? true : null,
            detail: nowClosed
                ? detail.copyWith(status: TicketStatus.closed, canReply: false)
                : null,
          ),
        );
        return false;
      },
      (TicketMessage m) {
        emit(
          state.copyWith(
            sending: false,
            draft: '',
            draftVersion: state.draftVersion + 1,
            detail: _withMessage(state.detail ?? detail, m),
          ),
        );
        // The API reopens a resolved ticket on a user reply.
        unawaited(refresh(silent: true));
        return true;
      },
    );
  }

  /// The rating was sent: no more prompt.
  void rated(int score) {
    final TicketDetail? detail = state.detail;
    if (detail == null) return;
    emit(state.copyWith(detail: detail.copyWith(csatScore: score)));
  }

  TicketDetail _withMessage(TicketDetail detail, TicketMessage m) {
    if (detail.messages.any((TicketMessage e) => e.id == m.id)) return detail;
    return detail.copyWith(
      messages: <TicketMessage>[...detail.messages, m],
      status: detail.status.isFinished ? TicketStatus.open : null,
    );
  }

  @override
  Future<void> close() async {
    await Future.wait(<Future<void>>[?_poll?.cancel(), ?_updates?.cancel()]);
    return super.close();
  }
}
