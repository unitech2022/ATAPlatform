import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:equatable/equatable.dart';

/// Which trip chat the app is in, and on which side.
class ChatTarget extends Equatable {
  const ChatTarget({required this.tripId, required this.actor});

  final String tripId;
  final TripActor actor;

  @override
  List<Object?> get props => <Object?>[tripId, actor];
}

/// Delivery state of a message sent from this device.
enum MessageDelivery { sent, pending, failed }

/// `TripMessage` (F12.4); identities are masked (roles only).
class TripMessage extends Equatable {
  const TripMessage({
    required this.id,
    required this.tripId,
    required this.body,
    this.senderRole = 'system',
    this.kind = 'text',
    this.quickReplyCode,
    this.isMine = false,
    this.readAt,
    this.createdAt,
    this.delivery = MessageDelivery.sent,
  });

  /// Prefix of optimistic (not yet acknowledged) messages.
  static const String localPrefix = 'local-';

  final String id;
  final String tripId;

  /// `passenger`, `driver` or `system`.
  final String senderRole;

  /// `text`, `quick_reply` or `system`.
  final String kind;

  /// Phone-like digit runs are already masked (`••••`) by the API.
  final String body;
  final String? quickReplyCode;
  final bool isMine;
  final DateTime? readAt;
  final DateTime? createdAt;
  final MessageDelivery delivery;

  bool get isSystem => kind == 'system' || senderRole == 'system';
  bool get isLocal => id.startsWith(localPrefix);

  TripMessage copyWith({MessageDelivery? delivery, DateTime? readAt}) =>
      TripMessage(
        id: id,
        tripId: tripId,
        body: body,
        senderRole: senderRole,
        kind: kind,
        quickReplyCode: quickReplyCode,
        isMine: isMine,
        readAt: readAt ?? this.readAt,
        createdAt: createdAt,
        delivery: delivery ?? this.delivery,
      );

  @override
  List<Object?> get props => <Object?>[
    id,
    tripId,
    senderRole,
    kind,
    body,
    quickReplyCode,
    isMine,
    readAt,
    createdAt,
    delivery,
  ];
}

/// `GET /catalog/chat-quick-replies?role=` row.
class QuickReply extends Equatable {
  const QuickReply({required this.code, required this.text});

  final String code;
  final String text;

  @override
  List<Object?> get props => <Object?>[code, text];
}

/// `POST …/call`: a proxy number, or `unavailable` (chat instead).
class MaskedCall extends Equatable {
  const MaskedCall({
    required this.mode,
    this.proxyNumber,
    this.pin,
    this.expiresAt,
  });

  static const String proxyMode = 'proxy';

  final String mode;
  final String? proxyNumber;
  final String? pin;
  final DateTime? expiresAt;

  bool get isAvailable => mode == proxyMode && proxyNumber != null;

  @override
  List<Object?> get props => <Object?>[mode, proxyNumber, pin, expiresAt];
}
