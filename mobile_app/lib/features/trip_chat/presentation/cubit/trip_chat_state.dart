import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/trip_chat/domain/entities/trip_message.dart';
import 'package:equatable/equatable.dart';

/// State of `TripChatCubit`.
class TripChatState extends Equatable {
  const TripChatState({
    this.target,
    this.messages = const <TripMessage>[],
    this.quickReplies = const <QuickReply>[],
    this.draft = '',
    this.draftVersion = 0,
    this.isOpen = false,
    this.seenIds = const <String>{},
    this.closed = false,
    this.failure,
  });

  static const int maxLength = 500;

  /// The chat currently bound (active trip), or `null`.
  final ChatTarget? target;

  /// Oldest first.
  final List<TripMessage> messages;
  final List<QuickReply> quickReplies;
  final String draft;

  /// Bumped when a sent draft is cleared (rebuilds the input empty).
  final int draftVersion;

  /// The chat screen is visible (incoming messages are marked read).
  final bool isOpen;

  /// Incoming messages already seen on this device.
  final Set<String> seenIds;

  /// `409 chat_closed`: the trip ended, the chat is read-only.
  final bool closed;
  final Failure? failure;

  bool get isBound => target != null;

  /// Badge of the chat button.
  int get unreadCount => messages
      .where(
        (TripMessage m) =>
            !m.isMine &&
            !m.isSystem &&
            m.readAt == null &&
            !seenIds.contains(m.id),
      )
      .length;

  bool get canSend => isBound && !closed && draft.trim().isNotEmpty;

  TripChatState copyWith({
    List<TripMessage>? messages,
    List<QuickReply>? quickReplies,
    String? draft,
    int? draftVersion,
    bool? isOpen,
    Set<String>? seenIds,
    bool? closed,
    Failure? failure,
    bool clearFailure = false,
  }) => TripChatState(
    target: target,
    messages: messages ?? this.messages,
    quickReplies: quickReplies ?? this.quickReplies,
    draft: draft ?? this.draft,
    draftVersion: draftVersion ?? this.draftVersion,
    isOpen: isOpen ?? this.isOpen,
    seenIds: seenIds ?? this.seenIds,
    closed: closed ?? this.closed,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[
    target,
    messages,
    quickReplies,
    draft,
    draftVersion,
    isOpen,
    seenIds,
    closed,
    failure,
  ];
}
