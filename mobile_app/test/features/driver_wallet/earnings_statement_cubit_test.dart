import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/driver_wallet/data/datasources/driver_wallet_remote_data_source.dart';
import 'package:ata_app/features/driver_wallet/data/models/driver_wallet_models.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/earnings_statement.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/payout.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/payout_summary.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/statement_period.dart';
import 'package:ata_app/features/driver_wallet/domain/usecases/get_earnings_statement.dart';
import 'package:ata_app/features/driver_wallet/presentation/cubit/earnings_statement_cubit.dart';
import 'package:ata_app/features/driver_wallet/presentation/cubit/earnings_statement_state.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:mocktail/mocktail.dart';

class _MockGetStatement extends Mock implements GetEarningsStatement {}

void main() {
  final DateTime now = DateTime(2026, 9, 28, 15, 30);
  late _MockGetStatement getStatement;

  final EarningsStatement statement = EarningsStatement(
    from: DateTime(2026, 9, 22),
    to: DateTime(2026, 9, 28),
    totals: const StatementTotals(trips: 12, earnings: 480, net: 300),
  );

  setUpAll(
    () => registerFallbackValue(DateRange(from: DateTime(0), to: DateTime(0))),
  );

  setUp(() {
    getStatement = _MockGetStatement();
    when(
      () => getStatement(any()),
    ).thenAnswer((_) async => Right<Failure, EarningsStatement>(statement));
  });

  test('periods cover today, the last 7 days and the month so far', () {
    expect(
      StatementPeriod.today.rangeAt(now),
      DateRange(from: DateTime(2026, 9, 28), to: DateTime(2026, 9, 28)),
    );
    expect(
      StatementPeriod.week.rangeAt(now),
      DateRange(from: DateTime(2026, 9, 22), to: DateTime(2026, 9, 28)),
    );
    expect(
      StatementPeriod.month.rangeAt(now),
      DateRange(from: DateTime(2026, 9), to: DateTime(2026, 9, 28)),
    );
    expect(
      DriverWalletRemoteDataSource.dateParam(DateTime(2026, 9, 1)),
      '2026-09-01',
    );
  });

  blocTest<EarningsStatementCubit, EarningsStatementState>(
    'selecting a period loads its range',
    build: () =>
        EarningsStatementCubit(getStatement: getStatement, now: () => now),
    act: (EarningsStatementCubit cubit) => cubit.select(StatementPeriod.month),
    expect: () => <EarningsStatementState>[
      const EarningsStatementState(
        period: StatementPeriod.month,
        loading: true,
      ),
      EarningsStatementState(
        period: StatementPeriod.month,
        statement: statement,
      ),
    ],
    verify: (_) => verify(
      () => getStatement(
        DateRange(from: DateTime(2026, 9), to: DateTime(2026, 9, 28)),
      ),
    ).called(1),
  );

  test('driver wallet models parse statement, summary and payouts', () {
    final EarningsStatement parsed = DriverWalletModels.statement(
      <String, dynamic>{
        'from': '2026-09-01',
        'to': '2026-09-28',
        'totals': <String, dynamic>{
          'trips': 84,
          'grossFares': 3120.0,
          'commission': 624.0,
          'earnings': 2496.0,
          'cashCollected': 1400.0,
          'net': 1258.0,
        },
        'days': <Map<String, dynamic>>[
          <String, dynamic>{'date': '2026-09-28', 'trips': 6, 'earnings': 180},
        ],
      },
    );
    expect(parsed.totals.trips, 84);
    expect(parsed.totals.commission, 624);
    expect(parsed.days.single.trips, 6);

    final PayoutSummary summary = DriverWalletModels.summary(<String, dynamic>{
      'balance': 640.0,
      'cashDebt': 320.0,
      'availableForPayout': 640.0,
      'minPayoutAmount': 100,
      'ibanMasked': 'SA03 **** **** 1234',
      'canRequest': true,
      'pendingPayout': <String, dynamic>{
        'id': 'po1',
        'payoutNumber': 'PO-1',
        'amount': 500,
        'status': 'approved',
      },
    });
    expect(summary.cashDebt, 320);
    expect(summary.cashDebtLimit, PayoutSummary.defaultCashDebtLimit);
    expect(summary.debtRatio, closeTo(0.64, 0.001));
    expect(summary.pendingPayout?.status, PayoutStatus.approved);
    expect(PayoutStatus.parse('paid'), PayoutStatus.paid);
  });
}
