import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/features/driver_dashboard/presentation/cubit/online_status_state.dart';
import 'package:ata_app/features/driver_dashboard/presentation/widgets/debt_block_card.dart';
import 'package:ata_app/features/driver_dashboard/presentation/widgets/driver_wallet_links.dart';
import 'package:ata_app/features/driver_wallet/domain/repositories/driver_wallet_repository.dart';
import 'package:ata_app/features/driver_wallet/presentation/cubit/payout_summary_cubit.dart';
import 'package:ata_app/features/wallet/domain/entities/wallet_summary.dart';
import 'package:ata_app/features/wallet/domain/repositories/wallet_repository.dart';
import 'package:ata_app/features/wallet/presentation/pages/wallet_page.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/driver_wallet_fakes.dart';
import '../../helpers/fakes.dart';
import '../../helpers/test_app.dart';

void main() {
  setUp(() async {
    await registerTestDependencies();
    getIt.registerSingleton<DriverWalletRepository>(
      FakeDriverWalletRepository(),
    );
  });

  tearDown(getIt.reset);

  Future<void> pump(WidgetTester tester, Widget child) async {
    await tester.binding.setSurfaceSize(const Size(430, 1400));
    await tester.pumpWidget(wrapForTest(SingleChildScrollView(child: child)));
    await tester.pumpAndSettle();
  }

  testWidgets('overview wallet links show the cash debt and links', (
    WidgetTester tester,
  ) async {
    await pump(
      tester,
      BlocProvider<PayoutSummaryCubit>(
        create: (_) => PayoutSummaryCubit(getSummary: getIt())..load(),
        child: const DriverWalletLinks(),
      ),
    );
    expect(find.text('مستحقات النقد'), findsOneWidget);
    expect(find.text('الحد المسموح: 500 ر.س'), findsOneWidget);
    expect(find.text('كشف الأرباح'), findsOneWidget);
    expect(find.text('المتاح للتحويل: 640.00 ر.س'), findsOneWidget);
  });

  testWidgets('the go-online block explains the debt limit', (tester) async {
    await pump(
      tester,
      const DebtBlockCard(block: CashDebtBlock(cashDebt: 620, limit: 500)),
    );
    expect(find.text('لا يمكنك الاتصال الآن'), findsOneWidget);
    expect(find.text('620.00 ر.س'), findsOneWidget);
    expect(find.text('سداد'), findsOneWidget);
  });

  testWidgets('a negative wallet shows the top-up banner and saved cards', (
    tester,
  ) async {
    final FakeWalletRepository wallet =
        getIt<WalletRepository>() as FakeWalletRepository;
    wallet
      ..balance = -23.5
      ..extraMethods = const <PaymentMethod>[
        PaymentMethod(
          type: 'card',
          label: 'مدى •••• 4201',
          id: 'pm1',
          brand: 'mada',
          last4: '4201',
          isDefault: true,
        ),
      ];
    await tester.binding.setSurfaceSize(const Size(430, 1400));
    await tester.pumpWidget(wrapForTest(const WalletPage()));
    await tester.pumpAndSettle();
    expect(find.text('يوجد مبلغ مستحق على حسابك'), findsOneWidget);
    expect(find.text('23.50 ر.س'), findsOneWidget);
    expect(find.text('مدى •••• 4201'), findsOneWidget);
    expect(find.text('إدارة البطاقات'), findsOneWidget);
  });
}
