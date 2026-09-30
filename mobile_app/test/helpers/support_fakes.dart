import 'dart:async';
import 'dart:convert';
import 'dart:typed_data';

import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/features/driver_dashboard/domain/entities/driver_status.dart';
import 'package:ata_app/features/driver_dashboard/domain/entities/earnings_summary.dart';
import 'package:ata_app/features/driver_dashboard/domain/repositories/driver_dashboard_repository.dart';
import 'package:ata_app/features/rides/domain/entities/trip_summary.dart';
import 'package:ata_app/features/support/domain/entities/attachments.dart';
import 'package:ata_app/features/support/domain/entities/help.dart';
import 'package:ata_app/features/support/domain/entities/ticket.dart';
import 'package:ata_app/features/support/domain/entities/ticket_enums.dart';
import 'package:ata_app/features/support/domain/entities/ticket_requests.dart';
import 'package:ata_app/features/support/domain/repositories/support_repository.dart';
import 'package:fpdart/fpdart.dart';

/// A valid 1x1 PNG (for attachments the UI decodes).
final Uint8List tinyPng = base64Decode(
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==',
);

Right<Failure, T> _ok<T>(T value) => Right<Failure, T>(value);
Left<Failure, T> _err<T>(Failure f) => Left<Failure, T>(f);

/// A `ServerFailure` with the API error code.
ServerFailure apiFailure(String code, {Map<String, dynamic>? details}) =>
    ServerFailure(code: code, message: '', details: details);

const List<HelpCategory> fakeCategories = <HelpCategory>[
  HelpCategory(
    id: 'c1',
    code: 'trips',
    name: 'الرحلات',
    icon: 'car',
    articlesCount: 3,
  ),
  HelpCategory(
    id: 'c2',
    code: 'payments',
    name: 'الدفع والمحفظة',
    articlesCount: 2,
  ),
];

const List<HelpArticleSummary> fakeArticles = <HelpArticleSummary>[
  HelpArticleSummary(
    id: 'a1',
    slug: 'schedule-a-ride',
    title: 'كيف أجدول رحلة؟',
    excerpt: 'يمكنك جدولة رحلة حتى 7 أيام',
    categoryId: 'c1',
  ),
  HelpArticleSummary(
    id: 'a2',
    slug: 'cancellation-fees',
    title: 'رسوم الإلغاء',
    excerpt: 'متى تُحتسب رسوم الإلغاء',
    categoryId: 'c2',
  ),
];

/// Builds a finished trip for the trip picker.
TripSummary fakeSupportTrip(
  String id, {
  TripStatus status = TripStatus.completed,
  double fare = 38,
}) => TripSummary(
  id: id,
  tripNumber: 'T-$id',
  destinationName: 'المطار',
  pickupName: 'الملقا',
  status: status,
  fare: fare,
  categoryName: 'اقتصادي',
  completedAt: DateTime.utc(2026, 9, 28, 10),
);

TicketSummary fakeTicketSummary(
  String id, {
  TicketStatus status = TicketStatus.open,
  int unread = 0,
  String? tripNumber,
}) => TicketSummary(
  id: id,
  ticketNumber: 'ST-20260930-$id',
  subject: 'تذكرة $id',
  type: TicketType.tripIssue,
  status: status,
  tripNumber: tripNumber,
  unread: unread,
  lastMessageAt: DateTime.utc(2026, 9, 30, 9),
);

TicketDetail fakeTicketDetail(
  String id, {
  TicketStatus status = TicketStatus.open,
  List<TicketMessage>? messages,
  bool canReply = true,
  bool canRate = false,
  int? csatScore,
}) => TicketDetail(
  id: id,
  ticketNumber: 'ST-20260930-$id',
  subject: 'تذكرة $id',
  type: TicketType.tripIssue,
  status: status,
  messages:
      messages ??
      <TicketMessage>[
        TicketMessage(
          id: 'm1',
          body: 'مرحباً، لدي مشكلة في الرحلة',
          createdAt: DateTime.utc(2026, 9, 30, 8),
        ),
        TicketMessage(
          id: 'm2',
          body: 'أهلاً بك، نراجع طلبك الآن',
          authorRole: 'agent',
          authorName: 'فريق دعم ATA',
          createdAt: DateTime.utc(2026, 9, 30, 9),
        ),
      ],
  canReply: canReply,
  canRate: canRate,
  csatScore: csatScore,
  createdAt: DateTime.utc(2026, 9, 30, 8),
);

/// In-memory F18 repository (no network).
class FakeSupportRepository implements SupportRepository {
  FakeSupportRepository() {
    articles = List<HelpArticleSummary>.of(fakeArticles);
  }

  // Help center.
  List<HelpCategory> categories = List<HelpCategory>.of(fakeCategories);
  late List<HelpArticleSummary> articles;
  Failure? categoriesFailure;
  final List<String> categoryAudiences = <String>[];
  Failure? searchFailure;
  int pageSize = 20;
  final List<HelpQuery> queries = <HelpQuery>[];
  HelpArticle article = const HelpArticle(
    id: 'a1',
    slug: 'schedule-a-ride',
    title: 'كيف أجدول رحلة؟',
    body: '## الخطوات\n\n- اختر الموعد\n- أكّد الحجز\n\n**مهم:** قبل 30 دقيقة.',
    categoryName: 'الرحلات',
    related: <RelatedArticle>[
      RelatedArticle(slug: 'cancellation-fees', title: 'رسوم الإلغاء'),
    ],
  );
  Failure? articleFailure;
  Failure? feedbackFailure;
  final List<ArticleFeedback> feedbacks = <ArticleFeedback>[];

  // Attachments.
  Failure? uploadFailure;
  final List<PickedAttachment> uploads = <PickedAttachment>[];
  Map<String, Uint8List> files = <String, Uint8List>{};
  Failure? fileFailure;
  final List<String> downloads = <String>[];

  // Tickets.
  List<TicketSummary> tickets = <TicketSummary>[];
  final Map<String, TicketDetail> details = <String, TicketDetail>{};
  Failure? ticketsFailure;
  Failure? ticketFailure;
  Failure? createFailure;
  Failure? replyFailure;
  Failure? rateFailure;
  final List<TicketsQuery> ticketQueries = <TicketsQuery>[];
  final List<NewTicketRequest> created = <NewTicketRequest>[];
  final List<TicketReplyRequest> replies = <TicketReplyRequest>[];
  final List<TicketRatingRequest> ratings = <TicketRatingRequest>[];
  final List<String> opened = <String>[];
  final StreamController<String> updates = StreamController<String>.broadcast();
  int _seq = 0;

  @override
  Future<Either<Failure, List<HelpCategory>>> getHelpCategories(
    String audience,
  ) async {
    categoryAudiences.add(audience);
    return categoriesFailure != null
        ? _err(categoriesFailure!)
        : _ok(List<HelpCategory>.of(categories));
  }

  @override
  Future<Either<Failure, PageResult<HelpArticleSummary>>> searchHelpArticles(
    HelpQuery query,
  ) async {
    queries.add(query);
    if (searchFailure != null) return _err(searchFailure!);
    final String q = query.text.trim();
    final List<HelpArticleSummary> matches = articles
        .where(
          (HelpArticleSummary a) =>
              (query.categoryId == null || a.categoryId == query.categoryId) &&
              (q.isEmpty || a.title.contains(q) || a.excerpt.contains(q)),
        )
        .toList();
    final int from = (query.page - 1) * pageSize;
    final List<HelpArticleSummary> slice = matches
        .skip(from)
        .take(pageSize)
        .toList();
    return _ok(
      PageResult<HelpArticleSummary>(
        items: slice,
        page: query.page,
        pageSize: pageSize,
        total: matches.length,
      ),
    );
  }

  @override
  Future<Either<Failure, HelpArticle>> getHelpArticle(String slug) async =>
      articleFailure != null ? _err(articleFailure!) : _ok(article);

  @override
  Future<Either<Failure, Unit>> sendArticleFeedback(
    ArticleFeedback feedback,
  ) async {
    feedbacks.add(feedback);
    return feedbackFailure != null ? _err(feedbackFailure!) : _ok(unit);
  }

  @override
  Future<Either<Failure, UploadedAttachment>> uploadAttachment(
    PickedAttachment file,
  ) async {
    uploads.add(file);
    if (uploadFailure != null) return _err(uploadFailure!);
    return _ok(
      UploadedAttachment(
        fileId: 'f${uploads.length}',
        fileName: file.name,
        contentType: file.contentType ?? '',
        sizeBytes: file.sizeBytes,
      ),
    );
  }

  @override
  Future<Either<Failure, TicketDetail>> createTicket(
    NewTicketRequest request,
  ) async {
    created.add(request);
    if (createFailure != null) return _err(createFailure!);
    final String id = 'new${_seq++}';
    final TicketDetail detail = fakeTicketDetail(
      id,
      messages: <TicketMessage>[
        TicketMessage(id: 'nm$id', body: request.message),
      ],
    );
    details[id] = detail;
    return _ok(detail);
  }

  @override
  Future<Either<Failure, PageResult<TicketSummary>>> getTickets(
    TicketsQuery query,
  ) async {
    ticketQueries.add(query);
    if (ticketsFailure != null) return _err(ticketsFailure!);
    final List<TicketSummary> matches = tickets
        .where(
          (TicketSummary t) =>
              (query.filter == TicketFilter.closed) == t.status.isClosed,
        )
        .toList();
    final int from = (query.page - 1) * pageSize;
    return _ok(
      PageResult<TicketSummary>(
        items: matches.skip(from).take(pageSize).toList(),
        page: query.page,
        pageSize: pageSize,
        total: matches.length,
      ),
    );
  }

  @override
  Future<Either<Failure, TicketDetail>> getTicket(String id) async {
    opened.add(id);
    if (ticketFailure != null) return _err(ticketFailure!);
    return _ok(details[id] ?? fakeTicketDetail(id));
  }

  @override
  Future<Either<Failure, TicketMessage>> replyToTicket(
    TicketReplyRequest request,
  ) async {
    replies.add(request);
    if (replyFailure != null) return _err(replyFailure!);
    final TicketMessage message = TicketMessage(
      id: 'r${replies.length}',
      body: request.body,
      createdAt: DateTime.utc(2026, 9, 30, 10),
    );
    final TicketDetail current =
        details[request.ticketId] ?? fakeTicketDetail(request.ticketId);
    details[request.ticketId] = current.copyWith(
      messages: <TicketMessage>[...current.messages, message],
    );
    return _ok(message);
  }

  @override
  Future<Either<Failure, Unit>> rateTicket(TicketRatingRequest request) async {
    ratings.add(request);
    return rateFailure != null ? _err(rateFailure!) : _ok(unit);
  }

  @override
  Future<Either<Failure, Uint8List>> downloadFile(String fileId) async {
    downloads.add(fileId);
    if (fileFailure != null) return _err(fileFailure!);
    return _ok(files[fileId] ?? Uint8List.fromList(<int>[1, 2, 3]));
  }

  @override
  Stream<String> watchTicketUpdates() => updates.stream;
}

/// Hands out preset files instead of opening the native chooser.
class FakeAttachmentPicker implements AttachmentPicker {
  /// What the next `pick` returns (cleared after use).
  List<PickedAttachment> next = <PickedAttachment>[];
  final List<int> requestedCounts = <int>[];

  @override
  Future<List<PickedAttachment>> pick({required int maxCount}) async {
    requestedCounts.add(maxCount);
    final List<PickedAttachment> result = next;
    next = <PickedAttachment>[];
    return result;
  }
}

/// A valid small image pick.
PickedAttachment pickedImage(String name, {int size = 1024}) =>
    PickedAttachment(name: name, sizeBytes: size, path: '/tmp/$name');

/// The driver's trips for the support trip picker (the dashboard calls are
/// not needed by these tests).
class FakeDriverTripsRepository implements DriverDashboardRepository {
  List<TripSummary> trips = <TripSummary>[];
  Failure? failure;

  @override
  Future<Either<Failure, PageResult<TripSummary>>> getTrips({
    int page = 1,
  }) async {
    final Failure? error = failure;
    if (error != null) return _err(error);
    return _ok(
      PageResult<TripSummary>(
        items: trips,
        page: 1,
        pageSize: trips.length,
        total: trips.length,
      ),
    );
  }

  @override
  Future<Either<Failure, DriverStatus>> getStatus() =>
      throw UnimplementedError();

  @override
  Future<Either<Failure, DriverStatus>> setOnline({required bool isOnline}) =>
      throw UnimplementedError();

  @override
  Future<Either<Failure, EarningsSummary>> getEarningsSummary() =>
      throw UnimplementedError();
}
