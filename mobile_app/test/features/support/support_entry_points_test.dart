import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/design/theme/ata_theme.dart';
import 'package:ata_app/design/widgets/ata_header.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/features/account/presentation/cubit/locale_cubit.dart';
import 'package:ata_app/features/auth/presentation/cubit/session_cubit.dart';
import 'package:ata_app/features/driver_dashboard/presentation/widgets/settings_tab.dart';
import 'package:ata_app/features/notifications/presentation/cubit/deep_link_cubit.dart';
import 'package:ata_app/features/passenger_home/presentation/pages/home_page.dart';
import 'package:ata_app/features/safety/domain/entities/lost_item.dart';
import 'package:ata_app/features/safety/domain/entities/safety_case.dart';
import 'package:ata_app/features/safety/domain/repositories/safety_repository.dart';
import 'package:ata_app/features/safety/presentation/pages/lost_items_page.dart';
import 'package:ata_app/features/safety/presentation/pages/safety_cases_page.dart';
import 'package:ata_app/features/safety/presentation/widgets/trip_help_actions.dart';
import 'package:ata_app/features/support/presentation/pages/support_page.dart';
import 'package:ata_app/features/support/presentation/pages/ticket_thread_page.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/date_symbol_data_local.dart';

import '../../helpers/fakes.dart';
import '../../helpers/safety_fakes.dart';
import '../../helpers/test_app.dart';

final _rider = testSession.copyWith(
  user: testUser.copyWith(termsAcceptedAt: DateTime.utc(2026)),
);

/// Hosts [child] at `/` and prints where a tap navigated to.
Widget _routed(Widget child, {List<String> paths = const <String>[]}) {
  final GoRouter router = GoRouter(
    routes: <RouteBase>[
      GoRoute(
        path: '/',
        builder: (_, _) => Scaffold(body: SingleChildScrollView(child: child)),
      ),
      for (final String path in <String>[
        '/support/tickets/new',
        '/support/tickets/:id',
        '/driver/support',
        ...paths,
      ])
        GoRoute(
          path: path,
          builder: (_, GoRouterState state) =>
              Scaffold(body: Center(child: Text('at:${state.uri}'))),
        ),
    ],
  );
  return MaterialApp.router(
    theme: AtaTheme.light(),
    locale: const Locale('ar'),
    supportedLocales: LocaleCubit.supported,
    localizationsDelegates: AppLocalizations.localizationsDelegates,
    routerConfig: router,
  );
}

void main() {
  setUpAll(() => initializeDateFormatting('ar'));
  setUp(registerTestDependencies);
  tearDown(getIt.reset);

  group('routes', () {
    test('the link builders and the thread check', () {
      expect(AppRoutes.supportRoot(), '/support');
      expect(AppRoutes.supportRoot(driver: true), '/driver/support');
      expect(AppRoutes.supportTicket('t1'), '/support/tickets/t1');
      expect(
        AppRoutes.supportTicket('t1', driver: true),
        '/driver/support/tickets/t1',
      );
      expect(AppRoutes.supportArticle('a b'), '/support/articles/a%20b');
      expect(
        AppRoutes.tripIssueTicket('trip1'),
        '/support/tickets/new?type=trip_issue&tripId=trip1',
      );
      expect(
        AppRoutes.fareDisputeTicket('trip1'),
        '/support/tickets/new?type=payment_issue&tripId=trip1&dispute=1',
      );
      expect(AppRoutes.newSupportTicket(), '/support/tickets/new');
      expect(
        AppRoutes.newSupportTicket(driver: true),
        '/driver/support/tickets/new',
      );
    });

    test('support belongs to the account tab', () {
      expect(AppRoutes.tabIndexFor('/support'), 3);
      expect(AppRoutes.tabIndexFor('/support/tickets/t1'), 3);
      expect(AppRoutes.tabIndexFor('/rides'), 1);
    });

    test('only a ticket thread hides the bottom navigation', () {
      expect(AppRoutes.hidesBottomNav('/support/tickets/t1'), isTrue);
      expect(AppRoutes.hidesBottomNav('/support/tickets/new'), isFalse);
      expect(AppRoutes.hidesBottomNav('/support/tickets'), isFalse);
      expect(AppRoutes.hidesBottomNav('/support'), isFalse);
      expect(AppRoutes.hidesBottomNav('/support/articles/x'), isFalse);
    });
  });

  group('من الإيصال', () {
    testWidgets('"مشكلة في الرحلة؟" opens a trip issue about the trip', (
      WidgetTester tester,
    ) async {
      await tester.binding.setSurfaceSize(const Size(430, 1200));
      addTearDown(() => tester.binding.setSurfaceSize(null));
      await tester.pumpWidget(_routed(const TripHelpActions(tripId: 'T1')));
      await tester.pumpAndSettle();
      await tester.tap(find.text('مشكلة في الرحلة؟'));
      await tester.pumpAndSettle();
      expect(
        find.text('at:/support/tickets/new?type=trip_issue&tripId=T1'),
        findsOneWidget,
      );
    });

    testWidgets('"مشكلة في الأجرة" opens the fare dispute of the trip', (
      WidgetTester tester,
    ) async {
      await tester.binding.setSurfaceSize(const Size(430, 1200));
      addTearDown(() => tester.binding.setSurfaceSize(null));
      await tester.pumpWidget(_routed(const TripHelpActions(tripId: 'T1')));
      await tester.pumpAndSettle();
      await tester.tap(find.text('مشكلة في الأجرة'));
      await tester.pumpAndSettle();
      expect(
        find.text(
          'at:/support/tickets/new?type=payment_issue&tripId=T1&dispute=1',
        ),
        findsOneWidget,
      );
    });

    testWidgets('lost item and safety report rows are still there', (
      WidgetTester tester,
    ) async {
      await tester.pumpWidget(_routed(const TripHelpActions(tripId: 'T1')));
      await tester.pumpAndSettle();
      expect(find.text('الإبلاغ عن غرض مفقود'), findsOneWidget);
    });
  });

  group('روابط من السلامة والمفقودات', () {
    testWidgets('a lost item report with a ticket links to its thread', (
      WidgetTester tester,
    ) async {
      final FakeSafetyRepository safety =
          getIt<SafetyRepository>() as FakeSafetyRepository;
      safety.lostItems = <LostItemReport>[
        const LostItemReport(
          id: 'l1',
          reportNumber: 'LI-1',
          supportTicketId: 'st1',
        ),
        const LostItemReport(id: 'l2', reportNumber: 'LI-2'),
      ];
      await tester.binding.setSurfaceSize(const Size(430, 1600));
      addTearDown(() => tester.binding.setSurfaceSize(null));
      await tester.pumpWidget(_routed(const LostItemsPage()));
      await tester.pumpAndSettle();

      // Only the report that has a ticket shows the link.
      expect(find.text('متابعة مع الدعم'), findsOneWidget);
      await tester.tap(find.text('متابعة مع الدعم'));
      await tester.pumpAndSettle();
      expect(find.text('at:/support/tickets/st1'), findsOneWidget);
    });

    testWidgets('a safety case with a ticket links to its thread', (
      WidgetTester tester,
    ) async {
      final FakeSafetyRepository safety =
          getIt<SafetyRepository>() as FakeSafetyRepository;
      safety.cases = <SafetyCaseSummary>[
        const SafetyCaseSummary(
          id: 'c1',
          caseNumber: 'SC-1',
          supportTicketId: 'st9',
        ),
      ];
      await tester.binding.setSurfaceSize(const Size(430, 1600));
      addTearDown(() => tester.binding.setSurfaceSize(null));
      await tester.pumpWidget(_routed(const SafetyCasesPage(caseId: 'c1')));
      await tester.pumpAndSettle();
      expect(find.text('متابعة مع الدعم'), findsOneWidget);
      await tester.tap(find.text('متابعة مع الدعم'));
      await tester.pumpAndSettle();
      expect(find.text('at:/support/tickets/st9'), findsOneWidget);
    });
  });

  group('إعدادات السائق', () {
    testWidgets('"المساعدة والدعم" opens the driver help center', (
      WidgetTester tester,
    ) async {
      await tester.binding.setSurfaceSize(const Size(430, 2200));
      addTearDown(() => tester.binding.setSurfaceSize(null));
      final LocaleCubit locale = LocaleCubit(
        getSavedLocale: getIt(),
        changeLanguage: getIt(),
      );
      final SessionCubit session = SessionCubit(
        restoreSession: getIt(),
        logout: getIt(),
        events: getIt(),
      );
      addTearDown(() async {
        await locale.close();
        await session.close();
      });
      await tester.pumpWidget(
        MultiBlocProvider(
          providers: <BlocProvider<dynamic>>[
            BlocProvider<LocaleCubit>.value(value: locale),
            BlocProvider<SessionCubit>.value(value: session),
          ],
          child: _routed(const SettingsTab()),
        ),
      );
      await tester.pumpAndSettle();
      expect(find.byType(IconBox), findsWidgets);
      await tester.ensureVisible(find.text('المساعدة والدعم'));
      await tester.tap(find.text('المساعدة والدعم'));
      await tester.pumpAndSettle();
      expect(find.text('at:/driver/support'), findsOneWidget);
    });
  });

  group('التطبيق كاملاً (راكب)', () {
    Future<void> openApp(WidgetTester tester) async {
      await registerTestDependencies(
        auth: FakeAuthRepository(restored: _rider),
      );
      // The request sheet of the home page needs the wide test surface.
      await tester.binding.setSurfaceSize(const Size(900, 3200));
      addTearDown(() => tester.binding.setSurfaceSize(null));
      await tester.pumpWidget(buildTestApp());
      await tester.pump();
      await tester.pump(const Duration(seconds: 1));
      await tester.pumpAndSettle();
      expect(find.byType(HomePage), findsOneWidget);
    }

    Future<void> leave(WidgetTester tester) async {
      await tester.pumpWidget(const SizedBox.shrink());
      await tester.pump(const Duration(minutes: 2));
    }

    testWidgets('the header menu has "المساعدة والدعم"', (
      WidgetTester tester,
    ) async {
      await openApp(tester);
      // The menu button is the last header button.
      await tester.tap(find.byType(HeaderIconButton).last);
      await tester.pumpAndSettle();
      await tester.tap(find.text('المساعدة والدعم'));
      await tester.pumpAndSettle();
      expect(find.byType(SupportPage), findsOneWidget);
      await leave(tester);
    });

    testWidgets('a support.reply push opens the ticket thread', (
      WidgetTester tester,
    ) async {
      await openApp(tester);
      final DeepLinkCubit links = tester
          .element(find.byType(HomePage))
          .read<DeepLinkCubit>();
      links.open('ata://support/tickets/t7');
      await tester.pumpAndSettle();
      expect(find.byType(TicketThreadPage), findsOneWidget);
      expect(find.text('تذكرة t7'), findsOneWidget);
      await leave(tester);
    });

    testWidgets('ata://support opens the help center', (
      WidgetTester tester,
    ) async {
      await openApp(tester);
      tester
          .element(find.byType(HomePage))
          .read<DeepLinkCubit>()
          .open('ata://support');
      await tester.pumpAndSettle();
      expect(find.byType(SupportPage), findsOneWidget);
      await leave(tester);
    });
  });
}
