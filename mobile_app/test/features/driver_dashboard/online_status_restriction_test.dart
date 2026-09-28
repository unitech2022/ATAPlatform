import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/driver_dashboard/domain/entities/driver_status.dart';
import 'package:ata_app/features/driver_dashboard/domain/usecases/get_driver_status.dart';
import 'package:ata_app/features/driver_dashboard/domain/usecases/set_driver_online.dart';
import 'package:ata_app/features/driver_dashboard/presentation/cubit/online_status_cubit.dart';
import 'package:ata_app/features/driver_dashboard/presentation/cubit/online_status_state.dart';
import 'package:ata_app/features/trip/domain/entities/restriction_level.dart';
import 'package:ata_app/l10n/generated/app_localizations_ar.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:intl/date_symbol_data_local.dart';
import 'package:mocktail/mocktail.dart';

class _MockGetStatus extends Mock implements GetDriverStatus {}

class _MockSetOnline extends Mock implements SetDriverOnline {}

void main() {
  late _MockGetStatus getStatus;
  late _MockSetOnline setOnline;

  const ServerFailure restricted = ServerFailure(
    code: 'account_restricted',
    message: '',
    statusCode: 403,
    details: <String, dynamic>{
      'level': 'temporarily_restricted',
      'restrictedUntil': '2026-09-29T12:00:00Z',
    },
  );

  setUpAll(() async {
    registerFallbackValue(const NoParams());
    await initializeDateFormatting('ar');
  });

  setUp(() {
    getStatus = _MockGetStatus();
    setOnline = _MockSetOnline();
  });

  blocTest<OnlineStatusCubit, OnlineStatusState>(
    '403 account_restricted blocks going online with the end date',
    build: () => OnlineStatusCubit(getStatus: getStatus, setOnline: setOnline),
    setUp: () => when(
      () => setOnline(true),
    ).thenAnswer((_) async => const Left<Failure, DriverStatus>(restricted)),
    act: (OnlineStatusCubit cubit) => cubit.toggle(),
    verify: (OnlineStatusCubit cubit) {
      expect(cubit.state.isOnline, isFalse);
      expect(cubit.state.debtBlock, isNull);
      expect(
        cubit.state.restriction,
        AccountRestriction(
          level: RestrictionLevel.temporarilyRestricted,
          restrictedUntil: DateTime.utc(2026, 9, 29, 12),
        ),
      );
    },
  );

  blocTest<OnlineStatusCubit, OnlineStatusState>(
    'a status that can go online again clears the restriction',
    build: () => OnlineStatusCubit(getStatus: getStatus, setOnline: setOnline),
    seed: () => const OnlineStatusState(
      restriction: AccountRestriction(level: RestrictionLevel.suspended),
    ),
    setUp: () => when(() => getStatus(any())).thenAnswer(
      (_) async => const Right<Failure, DriverStatus>(
        DriverStatus(isOnline: false, canGoOnline: true),
      ),
    ),
    act: (OnlineStatusCubit cubit) => cubit.load(),
    verify: (OnlineStatusCubit cubit) =>
        expect(cubit.state.restriction, isNull),
  );

  test('F12/F14 error codes have Arabic texts', () {
    final AppLocalizationsAr l10n = AppLocalizationsAr();
    expect(failureText(restricted, l10n), contains('مقيّد'));
    for (final String code in <String>[
      'trusted_contacts_limit',
      'trusted_contact_exists',
      'chat_closed',
      'lost_item_window_closed',
      'cancellation_reason_invalid',
      'cancellation_fee_changed',
      'share_expired',
    ]) {
      expect(
        failureText(ServerFailure(code: code, message: ''), l10n),
        isNot(l10n.errorUnexpected),
      );
    }
    expect(
      failureText(
        const ServerFailure(
          code: 'no_show_too_early',
          message: '',
          details: <String, dynamic>{'secondsRemaining': 90},
        ),
        l10n,
      ),
      contains('2'),
    );
  });
}
