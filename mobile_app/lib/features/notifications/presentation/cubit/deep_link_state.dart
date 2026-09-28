import 'package:ata_app/features/notifications/domain/entities/deep_link.dart';
import 'package:equatable/equatable.dart';

/// App-wide deep-link state: a [target] waiting to be navigated to, a link
/// parked until the session is ready, and the inbox request.
class DeepLinkState extends Equatable {
  const DeepLinkState({
    this.target,
    this.pendingLink,
    this.actionId,
    this.inboxRequested = false,
  });

  /// Location to open; cleared by `DeepLinkCubit.consumed`.
  final DeepLink? target;

  /// Link received before sign-in / onboarding finished.
  final String? pendingLink;

  /// Notification button that opened the link (e.g. `help`).
  final String? actionId;

  /// The notifications sheet should be shown (`ata://notifications`).
  final bool inboxRequested;

  DeepLinkState copyWith({
    DeepLink? target,
    String? pendingLink,
    String? actionId,
    bool? inboxRequested,
    bool clearTarget = false,
    bool clearPending = false,
  }) => DeepLinkState(
    target: clearTarget ? null : target ?? this.target,
    pendingLink: clearPending ? null : pendingLink ?? this.pendingLink,
    actionId: actionId ?? this.actionId,
    inboxRequested: inboxRequested ?? this.inboxRequested,
  );

  @override
  List<Object?> get props => <Object?>[
    target,
    pendingLink,
    actionId,
    inboxRequested,
  ];
}
