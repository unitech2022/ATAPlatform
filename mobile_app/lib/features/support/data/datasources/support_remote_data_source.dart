import 'dart:typed_data';

import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/core/network/api_client.dart';
import 'package:ata_app/features/support/data/models/help_models.dart';
import 'package:ata_app/features/support/data/models/ticket_models.dart';
import 'package:ata_app/features/support/domain/entities/attachments.dart';
import 'package:ata_app/features/support/domain/entities/help.dart';
import 'package:ata_app/features/support/domain/entities/ticket.dart';
import 'package:ata_app/features/support/domain/entities/ticket_requests.dart';

/// `/help/*` (public), `/support/*` and `/files/{id}` (`docs/11` §F18.3).
class SupportRemoteDataSource {
  const SupportRemoteDataSource(this._api);

  final ApiClient _api;

  static const String helpPath = '/help';
  static const String supportPath = '/support';

  Future<List<HelpCategory>> categories(String audience) async {
    final Object? body = await _api.get(
      '$helpPath/categories',
      query: <String, dynamic>{'audience': audience},
    );
    return PageResult<HelpCategory>.fromAny(body, HelpModels.category).items;
  }

  Future<PageResult<HelpArticleSummary>> articles(HelpQuery query) async =>
      PageResult<HelpArticleSummary>.fromAny(
        await _api.get(
          '$helpPath/articles',
          query: <String, dynamic>{
            'audience': query.audience,
            if (query.categoryId != null) 'categoryId': query.categoryId,
            if (query.text.trim().isNotEmpty) 'q': query.text.trim(),
            'page': query.page,
          },
        ),
        HelpModels.summary,
      );

  Future<HelpArticle> article(String slug) async => HelpModels.article(
    await _api.get('$helpPath/articles/${Uri.encodeComponent(slug)}')
        as Map<String, dynamic>,
  );

  Future<void> feedback(ArticleFeedback feedback) => _api.post(
    '$helpPath/articles/${feedback.articleId}/feedback',
    body: <String, dynamic>{'helpful': feedback.helpful},
  );

  Future<UploadedAttachment> upload(PickedAttachment file) async =>
      TicketModels.uploaded(
        await _api.uploadFile(
              '$supportPath/attachments',
              field: 'file',
              fileName: file.name,
              filePath: file.path,
              bytes: file.bytes,
              contentType: file.contentType,
            )
            as Map<String, dynamic>,
      );

  Future<TicketDetail> create(NewTicketRequest request) async =>
      TicketModels.detail(
        await _api.post(
              '$supportPath/tickets',
              body: TicketModels.createBody(request),
            )
            as Map<String, dynamic>,
      );

  Future<PageResult<TicketSummary>> tickets(TicketsQuery query) async =>
      PageResult<TicketSummary>.fromAny(
        await _api.get(
          '$supportPath/tickets',
          query: <String, dynamic>{
            'status': query.filter.apiValue,
            'page': query.page,
          },
        ),
        TicketModels.summary,
      );

  Future<TicketDetail> ticket(String id) async => TicketModels.detail(
    await _api.get('$supportPath/tickets/$id') as Map<String, dynamic>,
  );

  Future<TicketMessage> reply(TicketReplyRequest request) async =>
      TicketModels.message(
        await _api.post(
              '$supportPath/tickets/${request.ticketId}/messages',
              body: TicketModels.replyBody(request),
            )
            as Map<String, dynamic>,
      );

  Future<void> rate(TicketRatingRequest request) => _api.post(
    '$supportPath/tickets/${request.ticketId}/csat',
    body: TicketModels.ratingBody(request),
  );

  Future<Uint8List> file(String fileId) => _api.getBytes('/files/$fileId');
}
