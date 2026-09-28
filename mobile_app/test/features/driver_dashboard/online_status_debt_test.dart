import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/driver_dashboard/domain/entities/driver_status.dart';
import 'package:ata_app/features/driver_dashboard/domain/usecases/get_driver_status.dart';
import 'package:ata_app/features/driver_dashboard/domain/usecases/set_driver_online.dart';
import 'package:ata_app/features/driver_dashboard/presentation/cubit/online_status_cubit.dart';
import 'package:ata_app/features/driver_dashboard/presentation/cubit/online_status_state.dart';
import 'package:ata_app/l10n/generated/app_localizations_ar.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:mocktail/mocktail.dart';

class _MockGetStatus extends Mock implements GetDriverStatus {}

class _MockSetOnline extends Mock implements SetDriverOnline {}

void main() {
  late _MockGetStatus getStatus;
  late _MockSetOnline setOnline;

  const ServerFailure debtFailure = ServerFailure(
    code: 'cash_debt_limit_exceeded',
    message: '',
    statusCode: 403,
    details: <String, dynamic>{'cashDebt': 620.5, 'limit': 500},
  );

  setUpAll(() => registerFallbackValue(const NoParams()));

  setUp(() {
    getStatus = _MockGetStatus();
    setOnline = _MockSetOnline();
  });

  blocTest<OnlineStatusCubit, OnlineStatusState>(
    'going online above the cash-debt limit shows the block with amounts',
    build: () => OnlineStatusCubit(getStatus: getStatus, setOnline: setOnline),
    setUp: () => when(
      () => setOnline(true),
    ).thenAnswer((_) async => const Left<Failure, DriverStatus>(debtFailure)),
    act: (OnlineStatusCubit cubit) => cubit.toggle(),
    verify: (OnlineStatusCubit cubit) {
      expect(cubit.state.isOnline, isFalse);
      expect(
        cubit.state.debtBlock,
        const CashDebtBlock(cashDebt: 620.5, limit: 500),
      );
    },
  );

  blocTest<OnlineStatusCubit, OnlineStatusState>(
    'a status refused for debt keeps the block; a later OK clears it',
    build: () => OnlineStatusCubit(getStatus: getStatus, setOnline: setOnline),
    setUp: () {
      when(() => getStatus(any())).thenAnswer(
        (_) async => const Right<Failure, DriverStatus>(
          DriverStatus(
            isOnline: false,
            canGoOnline: false,
            reason: 'cash_debt_limit_exceeded',
          ),
        ),
      );
      when(() => setOnline(true)).thenAnswer(
        (_) async => const Right<Failure, DriverStatus>(
          DriverStatus(isOnline: true, canGoOnline: true),
        ),
      );
    },
    act: (OnlineStatusCubit cubit) async {
      await cubit.load();
      expect(cubit.state.debtBlock, const CashDebtBlock());
      await cubit.toggle();
    },
    verify: (OnlineStatusCubit cubit) {
      expect(cubit.state.isOnline, isTrue);
      expect(cubit.state.debtBlock, isNull);
    },
  );

  test('payment and payout errors have Arabic texts', () {
    final AppLocalizationsAr ar = AppLocalizationsAr();
    expect(failureText(debtFailure, ar), ar.cashDebtLimitError);
    expect(
      failureText(
        const ServerFailure(
          code: 'outstanding_balance',
          message: '',
          details: <String, dynamic>{'amount': 12.5},
        ),
        ar,
      ),
      ar.outstandingBalanceError('12.50'),
    );
    expect(
      failureText(
        const ServerFailure(code: 'payment_method_in_use', message: ''),
        ar,
      ),
      'البطاقة مرتبطة برحلة جارية',
    );
  });
}
