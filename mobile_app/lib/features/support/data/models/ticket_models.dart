import 'package:ata_app/features/support/domain/entities/attachments.dart';
import 'package:ata_app/features/support/domain/entities/fare_dispute.dart';
import 'package:ata_app/features/support/domain/entities/ticket.dart';
import 'package:ata_app/features/support/domain/entities/ticket_enums.dart';
import 'package:ata_app/features/support/domain/entities/ticket_requests.dart';
import 'package:ata_app/features/trip/data/models/json_readers.dart';

/// JSON mapping of the `/support` API (`docs/11` §F18.3).
abstract final class TicketModels {
  static TicketSummary summary(Map<String, dynamic> json) => TicketSummary(
    id: JsonReaders.string(json, 'id'),
    ticketNumber: JsonReaders.string(json, 'ticketNumber'),
    subject: JsonReaders.string(json, 'subject'),
    type: TicketType.parse(JsonReaders.optionalString(json, 'type')),
    status: TicketStatus.parse(JsonReaders.optionalString(json, 'status')),
    tripNumber: JsonReaders.optionalString(json, 'tripNumber'),
    lastMessageAt: JsonReaders.date(json, 'lastMessageAt'),
    unread: JsonReaders.integer(json, 'unread'),
    createdAt: JsonReaders.date(json, 'createdAt'),
  );

  static TicketAttachment attachment(Map<String, dynamic> json) =>
      TicketAttachment(
        fileId: JsonReaders.string(json, 'fileId'),
        fileName: JsonReaders.string(json, 'fileName'),
        contentType: JsonReaders.string(json, 'contentType'),
      );

  static TicketMessage message(Map<String, dynamic> json) => TicketMessage(
    id: JsonReaders.string(json, 'id'),
    body: JsonReaders.string(json, 'body'),
    authorRole: JsonReaders.optionalString(json, 'authorRole') ?? 'user',
    authorName: JsonReaders.optionalString(json, 'authorName'),
    attachments: JsonReaders.objects(
      json,
      'attachments',
    ).map(attachment).toList(growable: false),
    createdAt: JsonReaders.date(json, 'createdAt'),
  );

  static FareDispute dispute(Map<String, dynamic> json) => FareDispute(
    reason: DisputeReason.parse(JsonReaders.optionalString(json, 'reason')),
    status: DisputeStatus.parse(JsonReaders.optionalString(json, 'status')),
    chargedAmount: JsonReaders.number(json, 'chargedAmount'),
    requestedRefundAmount: JsonReaders.optionalNumber(
      json,
      'requestedRefundAmount',
    ),
    resolution: JsonReaders.optionalString(json, 'resolution'),
    approvedRefundAmount: JsonReaders.optionalNumber(
      json,
      'approvedRefundAmount',
    ),
  );

  static TicketDetail detail(Map<String, dynamic> json) {
    final Map<String, dynamic>? trip = JsonReaders.object(json, 'trip');
    final Map<String, dynamic>? dispute = JsonReaders.object(json, 'dispute');
    final TicketStatus status = TicketStatus.parse(
      JsonReaders.optionalString(json, 'status'),
    );
    final List<TicketMessage> messages =
        JsonReaders.objects(json, 'messages').map(message).toList()..sort(
          (TicketMessage a, TicketMessage b) =>
              (a.createdAt ?? _epoch).compareTo(b.createdAt ?? _epoch),
        );
    return TicketDetail(
      id: JsonReaders.string(json, 'id'),
      ticketNumber: JsonReaders.string(json, 'ticketNumber'),
      subject: JsonReaders.string(json, 'subject'),
      type: TicketType.parse(JsonReaders.optionalString(json, 'type')),
      status: status,
      priority: JsonReaders.optionalString(json, 'priority') ?? 'normal',
      trip: trip == null
          ? null
          : TicketTripRef(
              id: JsonReaders.string(trip, 'id'),
              tripNumber: JsonReaders.string(trip, 'tripNumber'),
              completedAt: JsonReaders.date(trip, 'completedAt'),
            ),
      messages: List<TicketMessage>.unmodifiable(messages),
      dispute: dispute == null ? null : TicketModels.dispute(dispute),
      canReply: json['canReply'] as bool? ?? !status.isClosed,
      canRate: json['canRate'] as bool? ?? false,
      csatScore: JsonReaders.optionalInteger(json, 'csatScore'),
      createdAt: JsonReaders.date(json, 'createdAt'),
      resolvedAt: JsonReaders.date(json, 'resolvedAt'),
    );
  }

  static final DateTime _epoch = DateTime.fromMillisecondsSinceEpoch(0);

  static UploadedAttachment uploaded(Map<String, dynamic> json) =>
      UploadedAttachment(
        fileId: JsonReaders.string(json, 'fileId'),
        fileName: JsonReaders.string(json, 'fileName'),
        contentType: JsonReaders.string(json, 'contentType'),
        sizeBytes: JsonReaders.integer(json, 'sizeBytes'),
      );

  static Map<String, dynamic> createBody(NewTicketRequest r) =>
      <String, dynamic>{
        'type': r.type.apiValue,
        'tripId': ?r.tripId,
        'subject': r.subject.trim(),
        'message': r.message.trim(),
        if (r.fileIds.isNotEmpty) 'fileIds': r.fileIds,
        if (r.dispute != null)
          'dispute': <String, dynamic>{
            'reason': r.dispute!.reason.apiValue,
            'requestedRefundAmount': ?r.dispute!.requestedRefundAmount,
          },
      };

  static Map<String, dynamic> replyBody(TicketReplyRequest r) =>
      <String, dynamic>{
        'body': r.body.trim(),
        if (r.fileIds.isNotEmpty) 'fileIds': r.fileIds,
      };

  static Map<String, dynamic> ratingBody(TicketRatingRequest r) =>
      <String, dynamic>{
        'score': r.score,
        if (r.comment != null && r.comment!.trim().isNotEmpty)
          'comment': r.comment!.trim(),
      };
}
