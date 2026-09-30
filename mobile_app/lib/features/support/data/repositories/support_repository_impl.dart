import 'dart:typed_data';

import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/features/support/data/datasources/support_remote_data_source.dart';
import 'package:ata_app/features/support/domain/entities/attachments.dart';
import 'package:ata_app/features/support/domain/entities/help.dart';
import 'package:ata_app/features/support/domain/entities/ticket.dart';
import 'package:ata_app/features/support/domain/entities/ticket_requests.dart';
import 'package:ata_app/features/support/domain/repositories/support_repository.dart';
import 'package:ata_app/features/trip/data/datasources/trip_realtime_data_source.dart';
import 'package:fpdart/fpdart.dart';

/// [SupportRepository] over the REST API and the trips hub.
class SupportRepositoryImpl implements SupportRepository {
  const SupportRepositoryImpl({required this._remote, required this._realtime});

  final SupportRemoteDataSource _remote;
  final TripRealtimeDataSource _realtime;

  @override
  Future<Either<Failure, List<HelpCategory>>> getHelpCategories(
    String audience,
  ) => guard(() => _remote.categories(audience));

  @override
  Future<Either<Failure, PageResult<HelpArticleSummary>>> searchHelpArticles(
    HelpQuery query,
  ) => guard(() => _remote.articles(query));

  @override
  Future<Either<Failure, HelpArticle>> getHelpArticle(String slug) =>
      guard(() => _remote.article(slug));

  @override
  Future<Either<Failure, Unit>> sendArticleFeedback(ArticleFeedback feedback) =>
      guard(() async {
        await _remote.feedback(feedback);
        return unit;
      });

  @override
  Future<Either<Failure, UploadedAttachment>> uploadAttachment(
    PickedAttachment file,
  ) => guard(() => _remote.upload(file));

  @override
  Future<Either<Failure, TicketDetail>> createTicket(
    NewTicketRequest request,
  ) => guard(() => _remote.create(request));

  @override
  Future<Either<Failure, PageResult<TicketSummary>>> getTickets(
    TicketsQuery query,
  ) => guard(() => _remote.tickets(query));

  @override
  Future<Either<Failure, TicketDetail>> getTicket(String id) =>
      guard(() => _remote.ticket(id));

  @override
  Future<Either<Failure, TicketMessage>> replyToTicket(
    TicketReplyRequest request,
  ) => guard(() => _remote.reply(request));

  @override
  Future<Either<Failure, Unit>> rateTicket(TicketRatingRequest request) =>
      guard(() async {
        await _remote.rate(request);
        return unit;
      });

  @override
  Future<Either<Failure, Uint8List>> downloadFile(String fileId) =>
      guard(() => _remote.file(fileId));

  @override
  Stream<String> watchTicketUpdates() async* {
    // The hub connection is reference counted; hold it while listening.
    await _realtime.acquire();
    try {
      yield* _realtime.supportTicketUpdated
          .map((Map<String, dynamic> e) => e['ticketId']?.toString() ?? '')
          .where((String id) => id.isNotEmpty);
    } finally {
      await _realtime.release();
    }
  }
}
