import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/features/support/domain/entities/help.dart';
import 'package:ata_app/features/support/domain/entities/ticket.dart';
import 'package:ata_app/features/support/domain/entities/ticket_enums.dart';
import 'package:ata_app/features/support/domain/repositories/support_repository.dart';
import 'package:ata_app/features/support/presentation/pages/help_article_page.dart';
import 'package:ata_app/features/support/presentation/pages/new_ticket_page.dart';
import 'package:ata_app/features/support/presentation/pages/support_page.dart';
import 'package:ata_app/features/support/presentation/pages/ticket_thread_page.dart';
import 'package:ata_app/features/support/presentation/pages/tickets_page.dart';
import 'package:ata_app/features/support/presentation/widgets/markdown_view.dart';
import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:intl/date_symbol_data_local.dart';

import '../../helpers/support_fakes.dart';
import '../../helpers/support_routed.dart';
import '../../helpers/test_app.dart';

void main() {
  late FakeSupportRepository repo;

  setUpAll(() => initializeDateFormatting('ar'));

  setUp(() async {
    await registerTestDependencies();
    repo = getIt<SupportRepository>() as FakeSupportRepository;
  });
  tearDown(getIt.reset);

  Future<void> open(WidgetTester tester, String location) async {
    await tester.binding.setSurfaceSize(const Size(430, 1800));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    await tester.pumpWidget(wrapSupport(location));
    await tester.pumpAndSettle();
  }

  Future<void> leave(WidgetTester tester) async {
    await tester.pumpWidget(const SizedBox.shrink());
    await tester.pump(const Duration(minutes: 1));
  }

  group('مركز المساعدة', () {
    testWidgets('shows the topics, "تذاكري" with its unread badge and '
        '"تواصل معنا"', (WidgetTester tester) async {
      repo.tickets = <TicketSummary>[
        fakeTicketSummary('1', unread: 2),
        fakeTicketSummary('2', unread: 1),
      ];
      await open(tester, '/support');

      expect(repo.categoryAudiences, <String>['passenger']);
      expect(find.byType(SupportPage), findsOneWidget);
      expect(find.text('المساعدة والدعم'), findsOneWidget);
      expect(find.text('الرحلات'), findsOneWidget);
      expect(find.text('الدفع والمحفظة'), findsOneWidget);
      expect(find.text('3 مقال'), findsOneWidget);
      expect(find.text('تذاكري'), findsOneWidget);
      expect(find.text('3 جديد'), findsOneWidget);
      expect(find.text('تواصل معنا'), findsOneWidget);
      await leave(tester);
    });

    testWidgets('the driver copy asks for the driver audience', (
      WidgetTester tester,
    ) async {
      await open(tester, '/driver/support');
      expect(find.byType(SupportPage), findsOneWidget);
      expect(repo.categoryAudiences, <String>['driver']);
      expect(find.text('الرحلات'), findsOneWidget);
      await leave(tester);
    });

    testWidgets('a category lists its articles and "كل المواضيع" goes back', (
      WidgetTester tester,
    ) async {
      await open(tester, '/support');
      await tester.tap(find.text('الرحلات'));
      await tester.pumpAndSettle();
      expect(repo.queries.last.categoryId, 'c1');
      expect(repo.queries.last.audience, 'passenger');
      expect(find.text('كيف أجدول رحلة؟'), findsOneWidget);
      expect(find.text('رسوم الإلغاء'), findsNothing);

      await tester.tap(find.text('كل المواضيع'));
      await tester.pumpAndSettle();
      expect(find.text('الدفع والمحفظة'), findsOneWidget);
      await leave(tester);
    });

    testWidgets('search waits for the pause in typing, then lists matches', (
      WidgetTester tester,
    ) async {
      await open(tester, '/support');
      await tester.enterText(find.byType(TextField), 'رسوم');
      await tester.pump(const Duration(milliseconds: 100));
      expect(repo.queries, isEmpty);

      await tester.pump(const Duration(milliseconds: 400));
      await tester.pumpAndSettle();
      expect(repo.queries.single.text, 'رسوم');
      expect(find.text('رسوم الإلغاء'), findsOneWidget);
      expect(find.text('كيف أجدول رحلة؟'), findsNothing);
      await leave(tester);
    });

    testWidgets('no match offers to contact support', (
      WidgetTester tester,
    ) async {
      await open(tester, '/support');
      await tester.enterText(find.byType(TextField), 'لا شيء');
      await tester.pump(const Duration(milliseconds: 500));
      await tester.pumpAndSettle();
      expect(find.text('لا توجد مقالات مطابقة'), findsOneWidget);

      await tester.tap(find.text('تواصل معنا').last);
      await tester.pumpAndSettle();
      expect(find.byType(NewTicketPage), findsOneWidget);
      await leave(tester);
    });

    testWidgets('an article opens from the results', (
      WidgetTester tester,
    ) async {
      await open(tester, '/support');
      await tester.tap(find.text('الرحلات'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('كيف أجدول رحلة؟'));
      await tester.pumpAndSettle();
      expect(find.byType(HelpArticlePage), findsOneWidget);
      await leave(tester);
    });
  });

  group('المقال', () {
    testWidgets('renders the Markdown, related articles and the feedback', (
      WidgetTester tester,
    ) async {
      await open(tester, '/support/articles/schedule-a-ride');
      expect(find.byType(MarkdownView), findsOneWidget);
      expect(find.text('الخطوات'), findsOneWidget);
      expect(find.text('اختر الموعد'), findsOneWidget);
      expect(find.text('أكّد الحجز'), findsOneWidget);
      expect(find.text('مقالات ذات صلة'), findsOneWidget);
      expect(find.text('رسوم الإلغاء'), findsOneWidget);
      expect(find.text('هل كان المقال مفيداً؟'), findsOneWidget);

      await tester.ensureVisible(
        find.byKey(const ValueKey<String>('feedback-yes')),
      );
      await tester.tap(find.byKey(const ValueKey<String>('feedback-yes')));
      await tester.pumpAndSettle();
      expect(repo.feedbacks.single.helpful, isTrue);
      expect(find.text('شكراً على رأيك'), findsOneWidget);
      await leave(tester);
    });

    testWidgets('a "no" offers a ticket', (WidgetTester tester) async {
      await open(tester, '/support/articles/schedule-a-ride');
      await tester.ensureVisible(
        find.byKey(const ValueKey<String>('feedback-no')),
      );
      await tester.tap(find.byKey(const ValueKey<String>('feedback-no')));
      await tester.pumpAndSettle();
      expect(repo.feedbacks.single.helpful, isFalse);
      expect(find.textContaining('آسفون لذلك'), findsOneWidget);
      await leave(tester);
    });

    testWidgets('HTML and scripts in the article never reach the screen', (
      WidgetTester tester,
    ) async {
      repo.article = const HelpArticle(
        id: 'a9',
        slug: 'evil',
        title: 'مقال',
        body:
            'مرحباً <script>alert(1)</script> [اضغط](javascript:alert(1)) '
            '<img src=x onerror=alert(2)> **آمن**',
      );
      await open(tester, '/support/articles/evil');
      final String shown = tester
          .widgetList<Text>(find.byType(Text))
          .map((Text t) => t.textSpan?.toPlainText() ?? t.data ?? '')
          .join(' ');
      expect(shown, contains('مرحباً'));
      expect(shown, contains('آمن'));
      expect(shown, isNot(contains('alert')));
      expect(shown, isNot(contains('<')));
      await leave(tester);
    });

    testWidgets('a missing article shows the error with a retry', (
      WidgetTester tester,
    ) async {
      repo.articleFailure = apiFailure('http_404');
      await open(tester, '/support/articles/none');
      expect(find.text('إعادة المحاولة'), findsOneWidget);
      await leave(tester);
    });
  });

  group('تذاكري', () {
    testWidgets('lists the tickets with status chips and unread badges', (
      WidgetTester tester,
    ) async {
      repo.tickets = <TicketSummary>[
        fakeTicketSummary('1', unread: 2, tripNumber: 'T-9'),
        fakeTicketSummary('2', status: TicketStatus.pendingUser),
        fakeTicketSummary('3', status: TicketStatus.resolved),
        fakeTicketSummary('4', status: TicketStatus.closed),
      ];
      await open(tester, '/support/tickets');

      expect(find.byType(TicketsPage), findsOneWidget);
      expect(find.text('تذكرة 1'), findsOneWidget);
      expect(find.text('2 جديد'), findsOneWidget);
      expect(find.text('بانتظار ردك'), findsOneWidget);
      expect(find.text('تم الحل'), findsOneWidget);
      expect(find.text('تذكرة 4'), findsNothing);
      expect(find.textContaining('T-9'), findsOneWidget);

      await tester.tap(find.text('المغلقة'));
      await tester.pumpAndSettle();
      expect(find.text('تذكرة 4'), findsOneWidget);
      expect(find.text('تذكرة 1'), findsNothing);
      await leave(tester);
    });

    testWidgets('an empty list says so', (WidgetTester tester) async {
      await open(tester, '/support/tickets');
      expect(find.text('لا توجد تذاكر هنا'), findsOneWidget);
      await leave(tester);
    });

    testWidgets('opening a ticket clears its badge and shows the thread', (
      WidgetTester tester,
    ) async {
      repo.tickets = <TicketSummary>[fakeTicketSummary('1', unread: 2)];
      await open(tester, '/support/tickets');
      await tester.tap(find.text('تذكرة 1'));
      await tester.pumpAndSettle();
      expect(find.byType(TicketThreadPage), findsOneWidget);
      expect(repo.opened, contains('1'));
      await leave(tester);
    });

    testWidgets('"تذكرة جديدة" opens the form', (WidgetTester tester) async {
      await open(tester, '/support/tickets');
      await tester.tap(find.text('تذكرة جديدة'));
      await tester.pumpAndSettle();
      expect(find.byType(NewTicketPage), findsOneWidget);
      await leave(tester);
    });

    testWidgets('a failure offers a retry', (WidgetTester tester) async {
      repo.ticketsFailure = apiFailure('boom');
      await open(tester, '/support/tickets');
      expect(find.text('إعادة المحاولة'), findsOneWidget);
      repo.ticketsFailure = null;
      repo.tickets = <TicketSummary>[fakeTicketSummary('1')];
      await tester.tap(find.text('إعادة المحاولة'));
      await tester.pumpAndSettle();
      expect(find.text('تذكرة 1'), findsOneWidget);
      await leave(tester);
    });
  });
}
