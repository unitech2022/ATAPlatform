import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/features/rides/domain/entities/trip_summary.dart';
import 'package:ata_app/features/rides/domain/repositories/rides_repository.dart';
import 'package:ata_app/features/support/domain/entities/attachments.dart';
import 'package:ata_app/features/support/domain/entities/fare_dispute.dart';
import 'package:ata_app/features/support/domain/entities/ticket.dart';
import 'package:ata_app/features/support/domain/entities/ticket_enums.dart';
import 'package:ata_app/features/support/domain/repositories/support_repository.dart';
import 'package:ata_app/features/support/presentation/pages/new_ticket_page.dart';
import 'package:ata_app/features/support/presentation/pages/ticket_thread_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:intl/date_symbol_data_local.dart';

import '../../helpers/fakes.dart';
import '../../helpers/support_fakes.dart';
import '../../helpers/support_routed.dart';
import '../../helpers/test_app.dart';

void main() {
  late FakeSupportRepository repo;
  late FakeAttachmentPicker picker;
  late FakeRidesRepository rides;

  setUpAll(() => initializeDateFormatting('ar'));

  setUp(() async {
    await registerTestDependencies();
    repo = getIt<SupportRepository>() as FakeSupportRepository;
    picker = getIt<AttachmentPicker>() as FakeAttachmentPicker;
    rides = getIt<RidesRepository>() as FakeRidesRepository;
    rides.trips = <TripSummary>[
      fakeSupportTrip('a'),
      fakeSupportTrip('b', status: TripStatus.cancelled, fare: 10),
    ];
  });
  tearDown(getIt.reset);

  Future<void> open(WidgetTester tester, String location) async {
    await tester.binding.setSurfaceSize(const Size(430, 2200));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    await tester.pumpWidget(wrapSupport(location));
    await tester.pumpAndSettle();
  }

  Future<void> leave(WidgetTester tester) async {
    await tester.pumpWidget(const SizedBox.shrink());
    await tester.pump(const Duration(minutes: 1));
  }

  Finder key(String value) => find.byKey(ValueKey<String>(value));

  bool submitEnabled(WidgetTester tester) =>
      tester.widget<AtaButton>(key('ticket-submit')).onPressed != null;

  Future<void> tapVisible(WidgetTester tester, Finder finder) async {
    await tester.ensureVisible(finder);
    await tester.tap(finder);
    await tester.pumpAndSettle();
  }

  group('إنشاء تذكرة', () {
    testWidgets('the whole flow: type, trip, subject, details, attachment, '
        'send, then the thread opens', (WidgetTester tester) async {
      await open(tester, '/support/tickets/new');
      expect(find.byType(NewTicketPage), findsOneWidget);
      expect(submitEnabled(tester), isFalse);

      await tapVisible(tester, find.text('مشكلة في رحلة'));
      // A trip is required for this type.
      expect(find.text('اختر الرحلة التي يخصها طلبك'), findsOneWidget);
      expect(find.text('بدون رحلة'), findsNothing);
      await tester.enterText(key('ticket-subject'), 'السائق أخذ مساراً أطول');
      await tester.enterText(key('ticket-message'), 'تفاصيل المشكلة');
      await tester.pump();
      expect(submitEnabled(tester), isFalse);

      await tapVisible(tester, key('trip-a'));
      expect(find.text('اختر الرحلة التي يخصها طلبك'), findsNothing);
      expect(submitEnabled(tester), isTrue);

      picker.next = <PickedAttachment>[pickedImage('proof.png')];
      await tapVisible(tester, key('add-attachment'));
      expect(find.text('proof.png'), findsOneWidget);

      await tapVisible(tester, key('ticket-submit'));
      final request = repo.created.single;
      expect(request.type, TicketType.tripIssue);
      expect(request.tripId, 'a');
      expect(request.subject, 'السائق أخذ مساراً أطول');
      expect(request.message, 'تفاصيل المشكلة');
      expect(request.fileIds, <String>['f1']);
      expect(request.dispute, isNull);

      // The new thread is open.
      expect(find.byType(TicketThreadPage), findsOneWidget);
      expect(find.text('تذكرة new0'), findsOneWidget);
      await leave(tester);
    });

    testWidgets('a type that needs no trip offers "بدون رحلة" and sends none', (
      WidgetTester tester,
    ) async {
      await open(tester, '/support/tickets/new?type=account');
      expect(find.text('بدون رحلة'), findsOneWidget);
      await tester.enterText(key('ticket-subject'), 'تغيير الرقم');
      await tester.enterText(key('ticket-message'), 'أريد تغيير رقم جوالي');
      await tester.pump();
      expect(submitEnabled(tester), isTrue);
      await tapVisible(tester, key('ticket-submit'));
      expect(repo.created.single.tripId, isNull);
      expect(repo.created.single.type, TicketType.account);
      await leave(tester);
    });

    testWidgets('the link parameters preselect the type and the trip', (
      WidgetTester tester,
    ) async {
      await open(tester, '/support/tickets/new?type=trip_issue&tripId=b');
      await tester.enterText(key('ticket-subject'), 'موضوع');
      await tester.enterText(key('ticket-message'), 'نص');
      await tester.pump();
      expect(submitEnabled(tester), isTrue);
      await tapVisible(tester, key('ticket-submit'));
      expect(repo.created.single.tripId, 'b');
      expect(repo.created.single.type, TicketType.tripIssue);
      await leave(tester);
    });

    testWidgets('at most five attachments; the sixth shows the API limit', (
      WidgetTester tester,
    ) async {
      await open(tester, '/support/tickets/new?type=other');
      picker.next = <PickedAttachment>[
        for (int i = 0; i < 5; i++) pickedImage('p$i.jpg'),
      ];
      await tapVisible(tester, key('add-attachment'));
      for (int i = 0; i < 5; i++) {
        expect(find.text('p$i.jpg'), findsOneWidget);
      }
      expect(tester.widget<AtaButton>(key('add-attachment')).onPressed, isNull);

      // A pick that brings too many files keeps what fits.
      await tester.tap(find.byTooltip('إزالة المرفق').first);
      await tester.tap(find.byTooltip('إزالة المرفق').first);
      await tester.pump();
      picker.next = <PickedAttachment>[
        for (int i = 0; i < 4; i++) pickedImage('q$i.jpg'),
      ];
      await tapVisible(tester, key('add-attachment'));
      expect(find.text('الحد الأقصى 5 مرفقات'), findsOneWidget);
      await leave(tester);
    });

    testWidgets('an unsupported file is refused with a message', (
      WidgetTester tester,
    ) async {
      await open(tester, '/support/tickets/new?type=other');
      picker.next = <PickedAttachment>[
        const PickedAttachment(name: 'run.exe', sizeBytes: 10, path: '/x'),
      ];
      await tapVisible(tester, key('add-attachment'));
      expect(find.textContaining('نوع الملف غير مدعوم'), findsOneWidget);
      expect(find.text('run.exe'), findsNothing);
      await leave(tester);
    });

    testWidgets(
      'sending waits for uploads and a failed upload can be retried',
      (WidgetTester tester) async {
        repo.uploadFailure = apiFailure('unsupported_file_type');
        await open(tester, '/support/tickets/new?type=other');
        await tester.enterText(key('ticket-subject'), 'موضوع');
        await tester.enterText(key('ticket-message'), 'نص');
        picker.next = <PickedAttachment>[pickedImage('a.jpg')];
        await tapVisible(tester, key('add-attachment'));
        expect(find.text('فشل الرفع · اضغط للإعادة'), findsOneWidget);

        repo.uploadFailure = null;
        await tester.tap(find.text('فشل الرفع · اضغط للإعادة'));
        await tester.pumpAndSettle();
        expect(find.text('فشل الرفع · اضغط للإعادة'), findsNothing);
        await tapVisible(tester, key('ticket-submit'));
        expect(repo.created.single.fileIds, <String>['f2']);
        await leave(tester);
      },
    );
  });

  group('اعتراض على الأجرة', () {
    Future<void> fillAndSend(WidgetTester tester) async {
      await tester.enterText(key('ticket-message'), 'المسار كان أطول');
      await tester.pump();
      await tapVisible(tester, key('ticket-submit'));
    }

    testWidgets('from the receipt: payment issue with the dispute on, the '
        'reason and the requested refund travel with the ticket', (
      WidgetTester tester,
    ) async {
      await open(
        tester,
        '/support/tickets/new?type=payment_issue&tripId=a&dispute=1',
      );
      expect(find.text('اعتراض على الأجرة'), findsOneWidget);
      expect(find.text('سبب الاعتراض'), findsOneWidget);
      // The subject is prefilled.
      expect(find.text('اعتراض على أجرة الرحلة'), findsOneWidget);

      await tapVisible(tester, find.text('المسار كان أطول من اللازم'));
      await tester.enterText(key('dispute-refund'), '12.5');
      await fillAndSend(tester);

      final request = repo.created.single;
      expect(request.type, TicketType.paymentIssue);
      expect(request.tripId, 'a');
      expect(request.dispute?.reason, DisputeReason.routeLonger);
      expect(request.dispute?.requestedRefundAmount, 12.5);
      await leave(tester);
    });

    testWidgets('the refund is optional and the dispute can be switched off', (
      WidgetTester tester,
    ) async {
      await open(
        tester,
        '/support/tickets/new?type=payment_issue&tripId=a&dispute=1',
      );
      await fillAndSend(tester);
      expect(repo.created.last.dispute?.requestedRefundAmount, isNull);
      expect(repo.created.last.dispute?.reason, DisputeReason.overcharged);
      await leave(tester);

      await open(
        tester,
        '/support/tickets/new?type=payment_issue&tripId=a&dispute=1',
      );
      await tapVisible(tester, key('dispute-toggle'));
      expect(find.text('سبب الاعتراض'), findsNothing);
      await fillAndSend(tester);
      expect(repo.created.last.dispute, isNull);
      await leave(tester);
    });

    testWidgets('a refund that is not a number blocks sending', (
      WidgetTester tester,
    ) async {
      await open(
        tester,
        '/support/tickets/new?type=payment_issue&tripId=a&dispute=1',
      );
      await tester.enterText(key('ticket-message'), 'نص');
      await tester.enterText(key('dispute-refund'), 'abc');
      await tester.pump();
      expect(find.text('أدخل مبلغاً صحيحاً'), findsOneWidget);
      expect(submitEnabled(tester), isFalse);
      await leave(tester);
    });

    testWidgets('dispute_window_closed is explained and the form stays', (
      WidgetTester tester,
    ) async {
      repo.createFailure = apiFailure('dispute_window_closed');
      await open(
        tester,
        '/support/tickets/new?type=payment_issue&tripId=a&dispute=1',
      );
      await fillAndSend(tester);
      expect(find.text('انتهت مدة الاعتراض على الأجرة'), findsOneWidget);
      expect(find.byType(NewTicketPage), findsOneWidget);
      await leave(tester);
    });

    testWidgets('dispute_exists is explained', (WidgetTester tester) async {
      repo.createFailure = apiFailure('dispute_exists');
      await open(
        tester,
        '/support/tickets/new?type=payment_issue&tripId=a&dispute=1',
      );
      await fillAndSend(tester);
      expect(find.text('يوجد اعتراض سابق على هذه الرحلة'), findsOneWidget);
      await leave(tester);
    });

    testWidgets('the dispute section needs a payment issue with a trip', (
      WidgetTester tester,
    ) async {
      await open(tester, '/support/tickets/new?type=trip_issue&tripId=a');
      expect(find.text('اعتراض على الأجرة'), findsNothing);
      await tapVisible(tester, find.text('الدفع والأجرة'));
      expect(find.text('اعتراض على الأجرة'), findsOneWidget);
      await leave(tester);
    });
  });

  group('محادثة التذكرة', () {
    testWidgets('shows the messages, the status banner and the reply box', (
      WidgetTester tester,
    ) async {
      await open(tester, '/support/tickets/t1');
      expect(find.text('تذكرة t1'), findsOneWidget);
      expect(find.text('مرحباً، لدي مشكلة في الرحلة'), findsOneWidget);
      expect(find.text('أهلاً بك، نراجع طلبك الآن'), findsOneWidget);
      expect(find.text('فريق دعم ATA'), findsOneWidget);
      expect(find.text('أنت'), findsOneWidget);
      expect(find.text('استلمنا تذكرتك وسيراجعها الفريق'), findsOneWidget);
      expect(find.text('مفتوحة'), findsOneWidget);
      expect(find.text('اكتب ردك…'), findsOneWidget);
      expect(repo.opened, <String>['t1']);
      await leave(tester);
    });

    testWidgets('the status text follows the ticket', (
      WidgetTester tester,
    ) async {
      repo.details['t1'] = fakeTicketDetail(
        't1',
        status: TicketStatus.pendingUser,
      );
      await open(tester, '/support/tickets/t1');
      expect(find.text('بانتظار ردك'), findsOneWidget);
      expect(find.text('الفريق ينتظر ردك لمتابعة الطلب'), findsOneWidget);
      await leave(tester);
    });

    testWidgets('a reply is sent and appears in the thread', (
      WidgetTester tester,
    ) async {
      await open(tester, '/support/tickets/t1');
      await tester.enterText(find.byType(TextField).first, 'شكراً لكم');
      await tester.pump();
      await tester.tap(key('reply-send'));
      await tester.pumpAndSettle();
      expect(repo.replies.single.body, 'شكراً لكم');
      expect(repo.replies.single.ticketId, 't1');
      expect(find.text('شكراً لكم'), findsOneWidget);
      await leave(tester);
    });

    testWidgets('a reply can carry attachments', (WidgetTester tester) async {
      await open(tester, '/support/tickets/t1');
      picker.next = <PickedAttachment>[pickedImage('screen.png')];
      await tester.tap(key('add-attachment'));
      await tester.pumpAndSettle();
      await tester.enterText(find.byType(TextField).first, 'مرفق');
      await tester.pump();
      await tester.tap(key('reply-send'));
      await tester.pumpAndSettle();
      expect(repo.replies.single.fileIds, <String>['f1']);
      // The attachment bar is emptied after sending.
      expect(find.text('screen.png'), findsNothing);
      await leave(tester);
    });

    testWidgets('the send button is off without text', (
      WidgetTester tester,
    ) async {
      await open(tester, '/support/tickets/t1');
      final IconButton send = tester.widget<IconButton>(key('reply-send'));
      expect(send.onPressed, isNull);
      await leave(tester);
    });

    testWidgets(
      'a closed ticket has no reply box, only "create a new ticket"',
      (WidgetTester tester) async {
        repo.details['t1'] = fakeTicketDetail(
          't1',
          status: TicketStatus.closed,
          canReply: false,
        );
        await open(tester, '/support/tickets/t1');
        expect(find.text('هذه التذكرة مغلقة'), findsOneWidget);
        expect(find.text('أُغلقت هذه التذكرة'), findsOneWidget);
        expect(find.text('اكتب ردك…'), findsNothing);
        expect(key('reply-send'), findsNothing);

        await tester.tap(key('create-new-ticket'));
        await tester.pumpAndSettle();
        expect(find.byType(NewTicketPage), findsOneWidget);
        await leave(tester);
      },
    );

    testWidgets('409 ticket_closed on send switches to the closed state', (
      WidgetTester tester,
    ) async {
      repo.replyFailure = apiFailure('ticket_closed');
      await open(tester, '/support/tickets/t1');
      await tester.enterText(find.byType(TextField).first, 'متأخر');
      await tester.pump();
      await tester.tap(key('reply-send'));
      await tester.pumpAndSettle();
      expect(find.text('هذه التذكرة مغلقة'), findsOneWidget);
      expect(key('create-new-ticket'), findsOneWidget);
      expect(key('reply-send'), findsNothing);
      await leave(tester);
    });

    testWidgets('the fare dispute card shows the decision and the refund', (
      WidgetTester tester,
    ) async {
      repo.details['t1'] = const TicketDetail(
        id: 't1',
        ticketNumber: 'ST-1',
        subject: 'أجرة',
        type: TicketType.paymentIssue,
        status: TicketStatus.resolved,
        messages: <TicketMessage>[
          TicketMessage(id: 'm1', body: 'اعتراض'),
          TicketMessage(
            id: 'm2',
            body: 'تمت الموافقة جزئياً على الاعتراض',
            authorRole: 'system',
          ),
        ],
        dispute: FareDispute(
          reason: DisputeReason.routeLonger,
          status: DisputeStatus.partiallyApproved,
          chargedAmount: 38.5,
          requestedRefundAmount: 12,
          resolution: 'refund_partial',
          approvedRefundAmount: 8,
        ),
      );
      await open(tester, '/support/tickets/t1');
      expect(find.text('الاعتراض على الأجرة'), findsOneWidget);
      expect(find.text('موافقة جزئية'), findsOneWidget);
      expect(find.text('المبلغ المحتسب: 38.50 ر.س'), findsOneWidget);
      expect(find.text('المبلغ المطلوب: 12.00 ر.س'), findsOneWidget);
      expect(find.text('المبلغ المسترد: 8.00 ر.س'), findsOneWidget);
      // System notes are centered captions, not bubbles.
      expect(find.text('تمت الموافقة جزئياً على الاعتراض'), findsOneWidget);
      await leave(tester);
    });

    testWidgets('an open dispute has no refund line yet', (
      WidgetTester tester,
    ) async {
      repo.details['t1'] = const TicketDetail(
        id: 't1',
        ticketNumber: 'ST-1',
        subject: 'أجرة',
        type: TicketType.paymentIssue,
        dispute: FareDispute(
          reason: DisputeReason.overcharged,
          status: DisputeStatus.underReview,
          chargedAmount: 40,
        ),
      );
      await open(tester, '/support/tickets/t1');
      expect(find.text('قيد المراجعة'), findsWidgets);
      expect(find.textContaining('المبلغ المسترد'), findsNothing);
      expect(find.text('لا يوجد استرداد'), findsNothing);
      await leave(tester);
    });

    testWidgets('a rejected dispute says there is no refund', (
      WidgetTester tester,
    ) async {
      repo.details['t1'] = const TicketDetail(
        id: 't1',
        ticketNumber: 'ST-1',
        subject: 'أجرة',
        dispute: FareDispute(
          reason: DisputeReason.other,
          status: DisputeStatus.rejected,
          chargedAmount: 40,
        ),
      );
      await open(tester, '/support/tickets/t1');
      expect(find.text('مرفوض'), findsOneWidget);
      expect(find.text('لا يوجد استرداد'), findsOneWidget);
      await leave(tester);
    });

    testWidgets('image attachments are fetched with the session and shown', (
      WidgetTester tester,
    ) async {
      repo.details['t1'] = fakeTicketDetail(
        't1',
        messages: <TicketMessage>[
          const TicketMessage(
            id: 'm1',
            body: 'انظر المرفق',
            authorRole: 'agent',
            attachments: <TicketAttachment>[
              TicketAttachment(
                fileId: 'img1',
                fileName: 'a.png',
                contentType: 'image/png',
              ),
              TicketAttachment(
                fileId: 'doc1',
                fileName: 'invoice.pdf',
                contentType: 'application/pdf',
              ),
            ],
          ),
        ],
      );
      repo.files['img1'] = tinyPng;
      await open(tester, '/support/tickets/t1');
      expect(find.text('invoice.pdf'), findsOneWidget);
      // The image is downloaded on open (with the session token); the pdf
      // only when tapped.
      expect(repo.downloads, <String>['img1']);
      expect(find.byType(Image), findsOneWidget);
      await leave(tester);
    });
  });

  testWidgets('a pdf attachment is downloaded only when tapped', (
    WidgetTester tester,
  ) async {
    repo.details['t1'] = fakeTicketDetail(
      't1',
      messages: <TicketMessage>[
        const TicketMessage(
          id: 'm1',
          body: 'الفاتورة',
          authorRole: 'agent',
          attachments: <TicketAttachment>[
            TicketAttachment(
              fileId: 'doc1',
              fileName: 'invoice.pdf',
              contentType: 'application/pdf',
            ),
          ],
        ),
      ],
    );
    await open(tester, '/support/tickets/t1');
    expect(repo.downloads, isEmpty);
    await tester.tap(find.text('invoice.pdf'));
    await tester.pumpAndSettle();
    expect(repo.downloads, <String>['doc1']);
    await leave(tester);
  });

  group('تقييم الدعم (CSAT)', () {
    testWidgets('after resolved: stars and a comment, once', (
      WidgetTester tester,
    ) async {
      repo.details['t1'] = fakeTicketDetail(
        't1',
        status: TicketStatus.resolved,
        canRate: true,
      );
      await open(tester, '/support/tickets/t1');
      expect(find.text('كيف كانت تجربتك مع الدعم؟'), findsOneWidget);
      expect(tester.widget<AtaButton>(key('csat-submit')).onPressed, isNull);

      await tester.tap(key('star-4'));
      await tester.pump();
      await tester.enterText(key('csat-comment'), 'ردّوا بسرعة');
      await tester.pump();
      await tester.tap(key('csat-submit'));
      await tester.pumpAndSettle();

      expect(repo.ratings.single.score, 4);
      expect(repo.ratings.single.comment, 'ردّوا بسرعة');
      expect(find.text('شكراً لتقييمك'), findsOneWidget);
      expect(key('csat-submit'), findsNothing);
      await leave(tester);
    });

    testWidgets('a closed ticket can be rated too, and the reply box is gone', (
      WidgetTester tester,
    ) async {
      repo.details['t1'] = fakeTicketDetail(
        't1',
        status: TicketStatus.closed,
        canReply: false,
        canRate: true,
      );
      await open(tester, '/support/tickets/t1');
      expect(find.text('كيف كانت تجربتك مع الدعم؟'), findsOneWidget);
      expect(key('create-new-ticket'), findsOneWidget);
      await leave(tester);
    });

    testWidgets('no prompt while the ticket is open or when not allowed', (
      WidgetTester tester,
    ) async {
      repo.details['t1'] = fakeTicketDetail('t1', canRate: true);
      await open(tester, '/support/tickets/t1');
      expect(find.text('كيف كانت تجربتك مع الدعم؟'), findsNothing);
      await leave(tester);

      repo.details['t2'] = fakeTicketDetail(
        't2',
        status: TicketStatus.resolved,
      );
      await open(tester, '/support/tickets/t2');
      expect(find.text('كيف كانت تجربتك مع الدعم؟'), findsNothing);
      await leave(tester);
    });

    testWidgets('an already rated ticket only thanks', (
      WidgetTester tester,
    ) async {
      repo.details['t1'] = fakeTicketDetail(
        't1',
        status: TicketStatus.resolved,
        canRate: true,
        csatScore: 5,
      );
      await open(tester, '/support/tickets/t1');
      expect(find.text('شكراً لتقييمك'), findsOneWidget);
      expect(key('csat-submit'), findsNothing);
      await leave(tester);
    });
  });
}
