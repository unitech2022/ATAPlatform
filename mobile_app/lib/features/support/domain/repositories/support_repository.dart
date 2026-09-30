import 'dart:typed_data';

import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/features/support/domain/entities/attachments.dart';
import 'package:ata_app/features/support/domain/entities/help.dart';
import 'package:ata_app/features/support/domain/entities/ticket.dart';
import 'package:ata_app/features/support/domain/entities/ticket_requests.dart';
import 'package:fpdart/fpdart.dart';

/// Help center and support tickets (`docs/11` §F18.3).
abstract interface class SupportRepository {
  Future<Either<Failure, List<HelpCategory>>> getHelpCategories(
    String audience,
  );

  Future<Either<Failure, PageResult<HelpArticleSummary>>> searchHelpArticles(
    HelpQuery query,
  );

  Future<Either<Failure, HelpArticle>> getHelpArticle(String slug);

  Future<Either<Failure, Unit>> sendArticleFeedback(ArticleFeedback feedback);

  Future<Either<Failure, UploadedAttachment>> uploadAttachment(
    PickedAttachment file,
  );

  Future<Either<Failure, TicketDetail>> createTicket(NewTicketRequest request);

  Future<Either<Failure, PageResult<TicketSummary>>> getTickets(
    TicketsQuery query,
  );

  /// Opening a ticket zeroes its unread counter on the server.
  Future<Either<Failure, TicketDetail>> getTicket(String id);

  Future<Either<Failure, TicketMessage>> replyToTicket(
    TicketReplyRequest request,
  );

  Future<Either<Failure, Unit>> rateTicket(TicketRatingRequest request);

  /// Authenticated `GET /files/{id}`.
  Future<Either<Failure, Uint8List>> downloadFile(String fileId);

  /// Ids of the tickets the hub reports as updated
  /// (`SupportTicketUpdated`).
  Stream<String> watchTicketUpdates();
}

/// Native file chooser (jpg / png / pdf), swapped for a fake in tests.
abstract interface class AttachmentPicker {
  /// The files the user chose (empty when cancelled).
  Future<List<PickedAttachment>> pick({required int maxCount});
}
