import 'package:ata_app/features/support/domain/entities/fare_dispute.dart';
import 'package:ata_app/features/support/domain/entities/ticket_enums.dart';
import 'package:equatable/equatable.dart';

/// Body of `POST /support/tickets`.
class NewTicketRequest extends Equatable {
  const NewTicketRequest({
    required this.type,
    required this.subject,
    required this.message,
    this.tripId,
    this.fileIds = const <String>[],
    this.dispute,
  });

  final TicketType type;
  final String subject;
  final String message;
  final String? tripId;
  final List<String> fileIds;
  final DisputeDraft? dispute;

  @override
  List<Object?> get props => <Object?>[
    type,
    subject,
    message,
    tripId,
    fileIds,
    dispute,
  ];
}

/// Body of `POST /support/tickets/{id}/messages`.
class TicketReplyRequest extends Equatable {
  const TicketReplyRequest({
    required this.ticketId,
    required this.body,
    this.fileIds = const <String>[],
  });

  final String ticketId;
  final String body;
  final List<String> fileIds;

  @override
  List<Object?> get props => <Object?>[ticketId, body, fileIds];
}

/// Body of `POST /support/tickets/{id}/csat`.
class TicketRatingRequest extends Equatable {
  const TicketRatingRequest({
    required this.ticketId,
    required this.score,
    this.comment,
  });

  final String ticketId;
  final int score;
  final String? comment;

  @override
  List<Object?> get props => <Object?>[ticketId, score, comment];
}

/// Query of `GET /support/tickets`.
class TicketsQuery extends Equatable {
  const TicketsQuery({this.filter = TicketFilter.open, this.page = 1});

  final TicketFilter filter;
  final int page;

  @override
  List<Object?> get props => <Object?>[filter, page];
}

/// Body of `POST /help/articles/{id}/feedback`.
class ArticleFeedback extends Equatable {
  const ArticleFeedback({required this.articleId, required this.helpful});

  final String articleId;
  final bool helpful;

  @override
  List<Object?> get props => <Object?>[articleId, helpful];
}
