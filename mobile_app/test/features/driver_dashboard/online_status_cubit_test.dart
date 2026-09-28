import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/driver_dashboard/domain/entities/driver_status.dart';
import 'package:ata_app/features/driver_dashboard/domain/usecases/get_driver_status.dart';
import 'package:ata_app/features/driver_dashboard/domain/usecases/set_driver_online.dart';
import 'package:ata_app/features/driver_dashboard/presentation/cubit/online_status_cubit.dart';
import 'package:ata_app/features/driver_dashboard/presentation/cubit/online_status_state.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:mocktail/mocktail.dart';

class _MockGetStatus extends Mock implements GetDriverStatus {}

class _MockSetOnline extends Mock implements SetDriverOnline {}

void main() {
  late _MockGetStatus getStatus;
  late _MockSetOnline setOnline;

  setUpAll(() => registerFallbackValue(const NoParams()));

  setUp(() {
    getStatus = _MockGetStatus();
    setOnline = _MockSetOnline();
  });

  blocTest<OnlineStatusCubit, OnlineStatusState>(
    'load applies the API status',
    build: () => OnlineStatusCubit(getStatus: getStatus, setOnline: setOnline),
    setUp: () => when(() => getStatus(any())).thenAnswer(
      (_) async => const Right<Failure, DriverStatus>(
        DriverStatus(isOnline: true, canGoOnline: true),
      ),
    ),
    act: (OnlineStatusCubit cubit) => cubit.load(),
    expect: () => <OnlineStatusState>[const OnlineStatusState(isOnline: true)],
  );

  blocTest<OnlineStatusCubit, OnlineStatusState>(
    'toggle is optimistic and confirmed by the API',
    build: () => OnlineStatusCubit(getStatus: getStatus, setOnline: setOnline),
    setUp: () => when(() => setOnline(true)).thenAnswer(
      (_) async => const Right<Failure, DriverStatus>(
        DriverStatus(isOnline: true, canGoOnline: true),
      ),
    ),
    act: (OnlineStatusCubit cubit) => cubit.toggle(),
    expect: () => <OnlineStatusState>[
      const OnlineStatusState(isOnline: true, updating: true),
      const OnlineStatusState(isOnline: true),
    ],
  );

  blocTest<OnlineStatusCubit, OnlineStatusState>(
    'toggle rolls back when the driver is not approved',
    build: () => OnlineStatusCubit(getStatus: getStatus, setOnline: setOnline),
    setUp: () => when(() => setOnline(true)).thenAnswer(
      (_) async => const Left<Failure, DriverStatus>(
        ServerFailure(
          code: 'driver_not_approved',
          message: '',
          statusCode: 403,
        ),
      ),
    ),
    act: (OnlineStatusCubit cubit) => cubit.toggle(),
    verify: (OnlineStatusCubit cubit) {
      expect(cubit.state.isOnline, isFalse);
      expect(cubit.state.updating, isFalse);
      expect(cubit.state.failure?.code, 'driver_not_approved');
    },
  );
}
