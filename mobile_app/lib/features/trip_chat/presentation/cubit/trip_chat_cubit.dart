import 'dart:async';

import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/features/trip_chat/domain/entities/trip_message.dart';
import 'package:ata_app/features/trip_chat/domain/usecases/get_quick_replies.dart';
import 'package:ata_app/features/trip_chat/domain/usecases/mark_messages_read.dart';
import 'package:ata_app/features/trip_chat/domain/usecases/send_trip_message.dart';
import 'package:ata_app/features/trip_chat/domain/usecases/watch_trip_messages.dart';
import 'package:ata_app/features/trip_chat/presentation/cubit/trip_chat_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// App-wide in-trip chat (F12.4) bound to the active trip: merged hub +
/// polling feed, optimistic sending, quick replies, unread badge and read
/// receipts while the chat screen is open.
class TripChatCubit extends Cubit<TripChatState> {
  TripChatCubit({
    required this._watchMessages,
    required this._send,
    required this._markRead,
    required this._getQuickReplies,
    DateTime Function()? now,
  }) : _now = now ?? DateTime.now,
       super(const TripChatState());

  final WatchTripMessages _watchMessages;
  final SendTripMessage _send;
  final MarkMessagesRead _markRead;
  final GetQuickReplies _getQuickReplies;
  final DateTime Function() _now;

  StreamSubscription<List<TripMessage>>? _feed;
  ChatTarget? _target;
  String? _lastMarkedId;
  int _localSeq = 0;

  /// Follows [target]'s chat. Idempotent for the same target.
  Future<void> bind(ChatTarget target) async {
    if (_target == target) return;
    _target = target;
    await _feed?.cancel();
    _feed = null;
    _lastMarkedId = null;
    if (isClosed || _target != target) return;
    emit(TripChatState(target: target));
    _feed = _watchMessages(target).listen(_merge);
    final replies = await _getQuickReplies(target.actor);
    if (isClosed || state.target != target) return;
    replies.fold(
      (_) {},
      (List<QuickReply> q) => emit(state.copyWith(quickReplies: q)),
    );
  }

  Future<void> unbind() async {
    _target = null;
    await _feed?.cancel();
    _feed = null;
    _lastMarkedId = null;
    if (!isClosed && state.isBound) emit(const TripChatState());
  }

  void open() {
    emit(state.copyWith(isOpen: true));
    _markSeen();
  }

  /// The chat screen was left.
  void leave() => emit(state.copyWith(isOpen: false));

  void draftChanged(String text) =>
      emit(state.copyWith(draft: text, clearFailure: true));

  Future<void> send() async {
    final String text = state.draft.trim();
    if (!state.canSend) return;
    emit(state.copyWith(draft: '', draftVersion: state.draftVersion + 1));
    await _deliver(_local(text), body: text);
  }

  Future<void> sendQuickReply(QuickReply reply) async {
    if (!state.isBound || state.closed) return;
    await _deliver(
      _local(reply.text, quickReplyCode: reply.code),
      quickReplyCode: reply.code,
    );
  }

  /// Re-sends a message that failed.
  Future<void> retry(TripMessage message) async {
    if (message.delivery != MessageDelivery.failed) return;
    await _deliver(
      message.copyWith(delivery: MessageDelivery.pending),
      body: message.quickReplyCode == null ? message.body : null,
      quickReplyCode: message.quickReplyCode,
    );
  }

  Future<void> _deliver(
    TripMessage local, {
    String? body,
    String? quickReplyCode,
  }) async {
    final ChatTarget? target = state.target;
    if (target == null) return;
    _upsert(<TripMessage>[local]);
    final result = await _send(
      SendMessageParams(
        target: target,
        body: body,
        quickReplyCode: quickReplyCode,
      ),
    );
    if (isClosed || state.target != target) return;
    result.fold((Failure f) {
      _upsert(<TripMessage>[local.copyWith(delivery: MessageDelivery.failed)]);
      emit(state.copyWith(failure: f, closed: f.code == ErrorCodes.chatClosed));
    }, (TripMessage sent) => _replace(local.id, sent));
  }

  TripMessage _local(String body, {String? quickReplyCode}) => TripMessage(
    id: '${TripMessage.localPrefix}${_localSeq++}',
    tripId: state.target?.tripId ?? '',
    body: body,
    senderRole: state.target?.actor.name ?? 'passenger',
    kind: quickReplyCode == null ? 'text' : 'quick_reply',
    quickReplyCode: quickReplyCode,
    isMine: true,
    createdAt: _now(),
    delivery: MessageDelivery.pending,
  );

  void _merge(List<TripMessage> batch) {
    if (isClosed || batch.isEmpty) return;
    _upsert(batch);
    if (state.isOpen) _markSeen();
  }

  void _replace(String localId, TripMessage sent) {
    final List<TripMessage> list =
        state.messages
            .where((TripMessage m) => m.id != localId && m.id != sent.id)
            .toList()
          ..add(sent);
    emit(state.copyWith(messages: _sorted(list)));
  }

  void _upsert(List<TripMessage> batch) {
    final Map<String, TripMessage> byId = <String, TripMessage>{
      for (final TripMessage m in state.messages) m.id: m,
    };
    for (final TripMessage m in batch) {
      byId[m.id] = m;
    }
    emit(state.copyWith(messages: _sorted(byId.values.toList())));
  }

  static List<TripMessage> _sorted(List<TripMessage> list) {
    final DateTime epoch = DateTime.fromMillisecondsSinceEpoch(0);
    list.sort(
      (TripMessage a, TripMessage b) =>
          (a.createdAt ?? epoch).compareTo(b.createdAt ?? epoch),
    );
    return List<TripMessage>.unmodifiable(list);
  }

  void _markSeen() {
    final ChatTarget? target = state.target;
    if (target == null) return;
    final List<TripMessage> incoming = state.messages
        .where((TripMessage m) => !m.isMine && !m.isLocal)
        .toList(growable: false);
    if (incoming.isEmpty) return;
    emit(
      state.copyWith(
        seenIds: <String>{
          ...state.seenIds,
          ...incoming.map((TripMessage m) => m.id),
        },
      ),
    );
    final String lastId = incoming.last.id;
    if (lastId == _lastMarkedId) return;
    _lastMarkedId = lastId;
    unawaited(_markRead(MarkReadParams(target: target, upToId: lastId)));
  }

  @override
  Future<void> close() async {
    await _feed?.cancel();
    return super.close();
  }
}
