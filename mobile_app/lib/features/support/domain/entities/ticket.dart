import 'package:ata_app/features/support/domain/entities/fare_dispute.dart';
import 'package:ata_app/features/support/domain/entities/ticket_enums.dart';
import 'package:equatable/equatable.dart';

/// `TicketSummary` row of `GET /support/tickets`.
class TicketSummary extends Equatable {
  const TicketSummary({
    required this.id,
    required this.ticketNumber,
    required this.subject,
    this.type = TicketType.other,
    this.status = TicketStatus.open,
    this.tripNumber,
    this.lastMessageAt,
    this.unread = 0,
    this.createdAt,
  });

  final String id;
  final String ticketNumber;
  final String subject;
  final TicketType type;
  final TicketStatus status;
  final String? tripNumber;
  final DateTime? lastMessageAt;
  final int unread;
  final DateTime? createdAt;

  bool get hasUnread => unread > 0;

  DateTime? get displayDate => lastMessageAt ?? createdAt;

  TicketSummary copyWith({int? unread, TicketStatus? status}) => TicketSummary(
    id: id,
    ticketNumber: ticketNumber,
    subject: subject,
    type: type,
    status: status ?? this.status,
    tripNumber: tripNumber,
    lastMessageAt: lastMessageAt,
    unread: unread ?? this.unread,
    createdAt: createdAt,
  );

  @override
  List<Object?> get props => <Object?>[
    id,
    ticketNumber,
    subject,
    type,
    status,
    tripNumber,
    lastMessageAt,
    unread,
    createdAt,
  ];
}

/// The trip a ticket is about.
class TicketTripRef extends Equatable {
  const TicketTripRef({
    required this.id,
    this.tripNumber = '',
    this.completedAt,
  });

  final String id;
  final String tripNumber;
  final DateTime? completedAt;

  @override
  List<Object?> get props => <Object?>[id, tripNumber, completedAt];
}

/// File attached to a message (fetched with `GET /files/{fileId}`).
class TicketAttachment extends Equatable {
  const TicketAttachment({
    required this.fileId,
    this.fileName = '',
    this.contentType = '',
  });

  final String fileId;
  final String fileName;
  final String contentType;

  bool get isImage => contentType.startsWith('image/');

  @override
  List<Object?> get props => <Object?>[fileId, fileName, contentType];
}

/// One message of the thread (`authorRole`: `user`, `agent` or `system`).
class TicketMessage extends Equatable {
  const TicketMessage({
    required this.id,
    required this.body,
    this.authorRole = 'user',
    this.authorName,
    this.attachments = const <TicketAttachment>[],
    this.createdAt,
  });

  final String id;
  final String body;
  final String authorRole;
  final String? authorName;
  final List<TicketAttachment> attachments;
  final DateTime? createdAt;

  bool get isMine => authorRole == 'user';
  bool get isAgent => authorRole == 'agent';
  bool get isSystem => authorRole == 'system';

  @override
  List<Object?> get props => <Object?>[
    id,
    body,
    authorRole,
    authorName,
    attachments,
    createdAt,
  ];
}

/// `TicketDetail` of `GET /support/tickets/{id}`.
class TicketDetail extends Equatable {
  const TicketDetail({
    required this.id,
    required this.ticketNumber,
    required this.subject,
    this.type = TicketType.other,
    this.status = TicketStatus.open,
    this.priority = 'normal',
    this.trip,
    this.messages = const <TicketMessage>[],
    this.dispute,
    this.canReply = true,
    this.canRate = false,
    this.csatScore,
    this.createdAt,
    this.resolvedAt,
  });

  final String id;
  final String ticketNumber;
  final String subject;
  final TicketType type;
  final TicketStatus status;
  final String priority;
  final TicketTripRef? trip;

  /// Oldest first.
  final List<TicketMessage> messages;
  final FareDispute? dispute;
  final bool canReply;
  final bool canRate;
  final int? csatScore;
  final DateTime? createdAt;
  final DateTime? resolvedAt;

  bool get isClosed => status.isClosed;

  /// The reply box is usable (the API can also say no while resolved).
  bool get replyEnabled => canReply && !isClosed;

  /// A rating is still possible: finished, allowed and not rated yet.
  bool get ratable => status.isFinished && canRate && csatScore == null;

  TicketDetail copyWith({
    List<TicketMessage>? messages,
    TicketStatus? status,
    bool? canReply,
    bool? canRate,
    int? csatScore,
  }) => TicketDetail(
    id: id,
    ticketNumber: ticketNumber,
    subject: subject,
    type: type,
    status: status ?? this.status,
    priority: priority,
    trip: trip,
    messages: messages ?? this.messages,
    dispute: dispute,
    canReply: canReply ?? this.canReply,
    canRate: canRate ?? this.canRate,
    csatScore: csatScore ?? this.csatScore,
    createdAt: createdAt,
    resolvedAt: resolvedAt,
  );

  @override
  List<Object?> get props => <Object?>[
    id,
    ticketNumber,
    subject,
    type,
    status,
    priority,
    trip,
    messages,
    dispute,
    canReply,
    canRate,
    csatScore,
    createdAt,
    resolvedAt,
  ];
}
