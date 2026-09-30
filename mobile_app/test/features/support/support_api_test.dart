import 'dart:async';
import 'dart:convert';
import 'dart:typed_data';

import 'package:ata_app/core/di/data_module.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/di/use_case_module.dart';
import 'package:ata_app/core/errors/app_exception.dart';
import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/core/network/api_client.dart';
import 'package:ata_app/core/network/auth_interceptor.dart';
import 'package:ata_app/core/push/noop_push_service.dart';
import 'package:ata_app/core/storage/preferences_storage.dart';
import 'package:ata_app/core/storage/token_storage.dart';
import 'package:ata_app/features/support/data/datasources/support_remote_data_source.dart';
import 'package:ata_app/features/support/domain/entities/attachments.dart';
import 'package:ata_app/features/support/domain/entities/fare_dispute.dart';
import 'package:ata_app/features/support/domain/entities/help.dart';
import 'package:ata_app/features/support/domain/entities/ticket.dart';
import 'package:ata_app/features/support/domain/entities/ticket_enums.dart';
import 'package:ata_app/features/support/domain/entities/ticket_requests.dart';
import 'package:ata_app/features/support/domain/repositories/support_repository.dart';
import 'package:ata_app/features/support/domain/usecases/create_ticket.dart';
import 'package:ata_app/features/support/domain/usecases/download_support_file.dart';
import 'package:ata_app/features/support/domain/usecases/get_help_article.dart';
import 'package:ata_app/features/support/domain/usecases/get_help_categories.dart';
import 'package:ata_app/features/support/domain/usecases/get_support_trips.dart';
import 'package:ata_app/features/support/domain/usecases/get_ticket.dart';
import 'package:ata_app/features/support/domain/usecases/get_tickets.dart';
import 'package:ata_app/features/support/domain/usecases/pick_support_attachments.dart';
import 'package:ata_app/features/support/domain/usecases/rate_ticket.dart';
import 'package:ata_app/features/support/domain/usecases/reply_to_ticket.dart';
import 'package:ata_app/features/support/domain/usecases/search_help_articles.dart';
import 'package:ata_app/features/support/domain/usecases/send_article_feedback.dart';
import 'package:ata_app/features/support/domain/usecases/upload_support_attachment.dart';
import 'package:ata_app/features/support/domain/usecases/watch_ticket.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../../helpers/fakes.dart';

/// Answers requests from a script and records them.
class _ScriptedAdapter implements HttpClientAdapter {
  _ScriptedAdapter(this.script);

  final List<ResponseBody Function(RequestOptions options)> script;
  final List<RequestOptions> requests = <RequestOptions>[];

  @override
  Future<ResponseBody> fetch(
    RequestOptions options,
    Stream<Uint8List>? requestStream,
    Future<void>? cancelFuture,
  ) async {
    requests.add(options);
    // Consume a multipart body like the real adapter does.
    if (requestStream != null) await requestStream.drain<void>();
    return script[requests.length - 1](options);
  }

  @override
  void close({bool force = false}) {}
}

ResponseBody _json(Object body, int status) => ResponseBody.fromString(
  jsonEncode(body),
  status,
  headers: <String, List<String>>{
    Headers.contentTypeHeader: <String>[Headers.jsonContentType],
  },
);

ResponseBody _bytes(List<int> bytes, int status, {String? type}) =>
    ResponseBody.fromBytes(
      bytes,
      status,
      headers: <String, List<String>>{
        Headers.contentTypeHeader: <String>[type ?? 'image/png'],
      },
    );

ApiClient _client(_ScriptedAdapter adapter, {TokenStorage? tokens}) {
  final ApiClient api = ApiClient(
    baseUrl: 'http://localhost/api/v1',
    tokens: tokens ?? InMemoryTokenStorage(),
    prefs: _prefs,
    onSessionExpired: () {},
  );
  api.dio.httpClientAdapter = adapter;
  return api;
}

late PreferencesStorage _prefs;

void main() {
  setUp(() async {
    SharedPreferences.setMockInitialValues(<String, Object>{});
    _prefs = PreferencesStorage(await SharedPreferences.getInstance());
  });
  tearDown(getIt.reset);

  group('SupportRemoteDataSource', () {
    test('uploads an attachment as multipart under "file"', () async {
      final _ScriptedAdapter adapter = _ScriptedAdapter(
        <ResponseBody Function(RequestOptions)>[
          (_) => _json(<String, dynamic>{
            'fileId': 'f1',
            'fileName': 'proof.png',
            'contentType': 'image/png',
            'sizeBytes': 3,
          }, 201),
        ],
      );
      final SupportRemoteDataSource remote = SupportRemoteDataSource(
        _client(adapter),
      );
      final UploadedAttachment uploaded = await remote.upload(
        PickedAttachment(
          name: 'proof.png',
          sizeBytes: 3,
          bytes: Uint8List.fromList(<int>[1, 2, 3]),
        ),
      );
      expect(uploaded.fileId, 'f1');
      final RequestOptions request = adapter.requests.single;
      expect(request.method, 'POST');
      expect(request.path, '/support/attachments');
      expect(request.contentType, startsWith('multipart/form-data'));
      final FormData form = request.data as FormData;
      expect(form.files.single.key, 'file');
      expect(form.files.single.value.filename, 'proof.png');
      expect(form.files.single.value.contentType?.mimeType, 'image/png');
    });

    test('downloads a file with the bearer token', () async {
      final InMemoryTokenStorage tokens = InMemoryTokenStorage();
      await tokens.save(
        const StoredTokens(accessToken: 'tok', refreshToken: 'r'),
      );
      final _ScriptedAdapter adapter = _ScriptedAdapter(
        <ResponseBody Function(RequestOptions)>[
          (_) => _bytes(<int>[9, 8, 7], 200),
        ],
      );
      final Uint8List bytes = await SupportRemoteDataSource(
        _client(adapter, tokens: tokens),
      ).file('f1');
      expect(bytes, <int>[9, 8, 7]);
      final RequestOptions request = adapter.requests.single;
      expect(request.path, '/files/f1');
      expect(request.responseType, ResponseType.bytes);
      expect(request.headers['Authorization'], 'Bearer tok');
    });

    test('a failed download keeps the API error code', () async {
      final _ScriptedAdapter adapter = _ScriptedAdapter(
        <ResponseBody Function(RequestOptions)>[
          (_) => _bytes(
            utf8.encode(
              jsonEncode(<String, dynamic>{
                'error': <String, dynamic>{
                  'code': 'file_not_found',
                  'message': '',
                },
              }),
            ),
            404,
            type: 'application/json',
          ),
          (_) => _bytes(<int>[1], 500, type: 'text/plain'),
        ],
      );
      final SupportRemoteDataSource remote = SupportRemoteDataSource(
        _client(adapter),
      );
      await expectLater(
        remote.file('x'),
        throwsA(
          isA<AppException>()
              .having((AppException e) => e.code, 'code', 'file_not_found')
              .having((AppException e) => e.statusCode, 'status', 404),
        ),
      );
      await expectLater(
        remote.file('y'),
        throwsA(
          isA<AppException>().having(
            (AppException e) => e.code,
            'code',
            'http_500',
          ),
        ),
      );
    });

    test('a 401 refreshes the token and the upload is retried', () async {
      final InMemoryTokenStorage tokens = InMemoryTokenStorage();
      await tokens.save(
        const StoredTokens(accessToken: 'old', refreshToken: 'r'),
      );
      final _ScriptedAdapter adapter = _ScriptedAdapter(
        <ResponseBody Function(RequestOptions)>[
          (_) => _json(<String, dynamic>{}, 401),
          (_) => _json(<String, dynamic>{'fileId': 'f2'}, 201),
        ],
      );
      final Dio dio = Dio(BaseOptions(baseUrl: 'http://localhost/api/v1'))
        ..httpClientAdapter = adapter;
      dio.interceptors.add(
        AuthInterceptor(
          dio: dio,
          tokens: tokens,
          refresh: (_) async =>
              const StoredTokens(accessToken: 'new', refreshToken: 'r2'),
          onSessionExpired: () {},
        ),
      );
      final Response<dynamic> response = await dio.post<dynamic>(
        '/support/attachments',
        data: FormData.fromMap(<String, dynamic>{
          'file': MultipartFile.fromBytes(<int>[1, 2], filename: 'a.png'),
        }),
      );
      expect(response.statusCode, 201);
      expect(adapter.requests, hasLength(2));
      expect(adapter.requests.last.headers['Authorization'], 'Bearer new');
    });

    test('creates a ticket with the dispute and parses the detail', () async {
      final _ScriptedAdapter adapter = _ScriptedAdapter(
        <ResponseBody Function(RequestOptions)>[
          (_) => _json(<String, dynamic>{
            'id': 't1',
            'ticketNumber': 'ST-1',
            'type': 'payment_issue',
            'subject': 's',
            'status': 'open',
            'messages': <Object>[],
            'dispute': <String, dynamic>{
              'reason': 'overcharged',
              'status': 'open',
              'chargedAmount': 40,
            },
          }, 201),
        ],
      );
      final TicketDetail detail =
          await SupportRemoteDataSource(_client(adapter)).create(
            const NewTicketRequest(
              type: TicketType.paymentIssue,
              subject: 's',
              message: 'm',
              tripId: 'trip1',
              dispute: DisputeDraft(reason: DisputeReason.overcharged),
            ),
          );
      expect(detail.dispute?.status, DisputeStatus.open);
      expect(adapter.requests.single.path, '/support/tickets');
      expect(adapter.requests.single.data, <String, dynamic>{
        'type': 'payment_issue',
        'tripId': 'trip1',
        'subject': 's',
        'message': 'm',
        'dispute': <String, dynamic>{'reason': 'overcharged'},
      });
    });

    test('lists tickets from an array or from the page envelope', () async {
      final Map<String, dynamic> row = <String, dynamic>{
        'id': 't1',
        'ticketNumber': 'ST-1',
        'subject': 's',
        'unread': 1,
      };
      final _ScriptedAdapter adapter = _ScriptedAdapter(
        <ResponseBody Function(RequestOptions)>[
          (_) => _json(<Object>[row], 200),
          (_) => _json(<String, dynamic>{
            'items': <Object>[row, row],
            'page': 2,
            'pageSize': 2,
            'total': 5,
          }, 200),
        ],
      );
      final SupportRemoteDataSource remote = SupportRemoteDataSource(
        _client(adapter),
      );
      final PageResult<TicketSummary> bare = await remote.tickets(
        const TicketsQuery(),
      );
      expect(bare.items, hasLength(1));
      expect(bare.hasMore, isFalse);
      final PageResult<TicketSummary> paged = await remote.tickets(
        const TicketsQuery(filter: TicketFilter.closed, page: 2),
      );
      expect(paged.items, hasLength(2));
      expect(paged.hasMore, isTrue);
      expect(adapter.requests.first.queryParameters, <String, dynamic>{
        'status': 'open',
        'page': 1,
      });
      expect(adapter.requests.last.queryParameters, <String, dynamic>{
        'status': 'closed',
        'page': 2,
      });
    });

    test('searches the help articles for an audience and a text', () async {
      final _ScriptedAdapter adapter = _ScriptedAdapter(
        <ResponseBody Function(RequestOptions)>[
          (_) => _json(<String, dynamic>{
            'items': <Object>[
              <String, dynamic>{'id': 'a1', 'slug': 's', 'title': 'T'},
            ],
          }, 200),
        ],
      );
      final PageResult<HelpArticleSummary> page =
          await SupportRemoteDataSource(_client(adapter)).articles(
            const HelpQuery(
              audience: 'driver',
              categoryId: 'c1',
              text: '  رسوم ',
            ),
          );
      expect(page.items.single.slug, 's');
      expect(adapter.requests.single.path, '/help/articles');
      expect(adapter.requests.single.queryParameters, <String, dynamic>{
        'audience': 'driver',
        'categoryId': 'c1',
        'q': 'رسوم',
        'page': 1,
      });
    });

    test('feedback, reply and rating post their bodies', () async {
      final _ScriptedAdapter adapter = _ScriptedAdapter(
        <ResponseBody Function(RequestOptions)>[
          (_) => ResponseBody.fromString('', 204),
          (_) => _json(<String, dynamic>{'id': 'm1', 'body': 'hi'}, 201),
          (_) => ResponseBody.fromString('', 204),
        ],
      );
      final SupportRemoteDataSource remote = SupportRemoteDataSource(
        _client(adapter),
      );
      await remote.feedback(
        const ArticleFeedback(articleId: 'a1', helpful: true),
      );
      final TicketMessage message = await remote.reply(
        const TicketReplyRequest(
          ticketId: 't1',
          body: 'hi',
          fileIds: <String>['f1'],
        ),
      );
      await remote.rate(
        const TicketRatingRequest(ticketId: 't1', score: 5, comment: 'ممتاز'),
      );
      expect(message.id, 'm1');
      expect(adapter.requests[0].path, '/help/articles/a1/feedback');
      expect(adapter.requests[0].data, <String, dynamic>{'helpful': true});
      expect(adapter.requests[1].path, '/support/tickets/t1/messages');
      expect(adapter.requests[1].data, <String, dynamic>{
        'body': 'hi',
        'fileIds': <String>['f1'],
      });
      expect(adapter.requests[2].path, '/support/tickets/t1/csat');
      expect(adapter.requests[2].data, <String, dynamic>{
        'score': 5,
        'comment': 'ممتاز',
      });
    });
  });

  test('the production wiring resolves every F18 use case', () async {
    await getIt.reset();
    registerCore(
      prefs: _prefs,
      tokens: InMemoryTokenStorage(),
      baseUrl: 'http://localhost:5000/api/v1',
      push: const NoopPushService(),
    );
    registerData(hubUrl: 'http://localhost:5000/hubs/trips');
    registerUseCases();

    expect(getIt<SupportRepository>(), isNotNull);
    expect(getIt<AttachmentPicker>(), isNotNull);
    expect(getIt<GetHelpCategories>(), isNotNull);
    expect(getIt<SearchHelpArticles>(), isNotNull);
    expect(getIt<GetHelpArticle>(), isNotNull);
    expect(getIt<SendArticleFeedback>(), isNotNull);
    expect(getIt<CreateTicket>(), isNotNull);
    expect(getIt<GetTickets>(), isNotNull);
    expect(getIt<GetTicket>(), isNotNull);
    expect(getIt<ReplyToTicket>(), isNotNull);
    expect(getIt<RateTicket>(), isNotNull);
    expect(getIt<WatchTicket>(), isNotNull);
    expect(getIt<GetSupportTrips>(), isNotNull);
    expect(getIt<PickSupportAttachments>(), isNotNull);
    expect(getIt<UploadSupportAttachment>(), isNotNull);
    expect(getIt<DownloadSupportFile>(), isNotNull);
  });
}
