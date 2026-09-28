import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/env/env.dart';
import 'package:ata_app/core/storage/preferences_storage.dart';
import 'package:ata_app/features/account/data/datasources/account_remote_data_source.dart';
import 'package:ata_app/features/account/data/repositories/account_repository_impl.dart';
import 'package:ata_app/features/account/domain/repositories/account_repository.dart';
import 'package:ata_app/features/auth/data/datasources/auth_local_data_source.dart';
import 'package:ata_app/features/auth/data/datasources/auth_remote_data_source.dart';
import 'package:ata_app/features/auth/data/repositories/auth_repository_impl.dart';
import 'package:ata_app/features/auth/domain/repositories/auth_repository.dart';
import 'package:ata_app/features/catalog/data/datasources/catalog_remote_data_source.dart';
import 'package:ata_app/features/catalog/data/repositories/catalog_repository_impl.dart';
import 'package:ata_app/features/catalog/domain/repositories/catalog_repository.dart';
import 'package:ata_app/features/driver_dashboard/data/datasources/driver_dashboard_remote_data_source.dart';
import 'package:ata_app/features/driver_dashboard/data/repositories/driver_dashboard_repository_impl.dart';
import 'package:ata_app/features/driver_dashboard/domain/repositories/driver_dashboard_repository.dart';
import 'package:ata_app/features/driver_onboarding/data/datasources/driver_onboarding_remote_data_source.dart';
import 'package:ata_app/features/driver_onboarding/data/repositories/driver_onboarding_repository_impl.dart';
import 'package:ata_app/features/driver_onboarding/domain/repositories/driver_onboarding_repository.dart';
import 'package:ata_app/features/notifications/data/datasources/notifications_remote_data_source.dart';
import 'package:ata_app/features/notifications/data/repositories/notifications_repository_impl.dart';
import 'package:ata_app/features/notifications/domain/repositories/notifications_repository.dart';
import 'package:ata_app/features/passenger_home/data/datasources/passenger_remote_data_source.dart';
import 'package:ata_app/features/passenger_home/data/repositories/passenger_repository_impl.dart';
import 'package:ata_app/features/passenger_home/domain/repositories/passenger_repository.dart';
import 'package:ata_app/features/rides/data/datasources/rides_remote_data_source.dart';
import 'package:ata_app/features/rides/data/repositories/rides_repository_impl.dart';
import 'package:ata_app/features/rides/domain/repositories/rides_repository.dart';
import 'package:ata_app/features/trip/data/datasources/signalr_trip_realtime_data_source.dart';
import 'package:ata_app/features/trip/data/datasources/trip_realtime_data_source.dart';
import 'package:ata_app/features/trip/data/datasources/trip_remote_data_source.dart';
import 'package:ata_app/features/trip/data/repositories/geolocator_location_repository.dart';
import 'package:ata_app/features/trip/data/repositories/simulated_location_repository.dart';
import 'package:ata_app/features/trip/data/repositories/trip_repository_impl.dart';
import 'package:ata_app/features/trip/domain/repositories/location_repository.dart';
import 'package:ata_app/features/trip/domain/repositories/trip_repository.dart';
import 'package:ata_app/features/wallet/data/datasources/wallet_remote_data_source.dart';
import 'package:ata_app/features/wallet/data/repositories/wallet_repository_impl.dart';
import 'package:ata_app/features/wallet/domain/repositories/wallet_repository.dart';

/// Registers data sources and repository implementations.
void registerData({
  String hubUrl = '',
  bool simulateLocation = Env.simulateLocation,
}) {
  getIt
    ..registerLazySingleton<AuthLocalDataSource>(
      () => AuthLocalDataSource(tokens: getIt(), prefs: getIt()),
    )
    ..registerLazySingleton<AuthRepository>(
      () => AuthRepositoryImpl(
        remote: AuthRemoteDataSource(
          getIt(),
          deviceId: getIt<PreferencesStorage>().deviceId,
        ),
        local: getIt(),
      ),
    )
    ..registerLazySingleton<AccountRepository>(
      () => AccountRepositoryImpl(
        remote: AccountRemoteDataSource(getIt()),
        authLocal: getIt(),
        prefs: getIt(),
      ),
    )
    ..registerLazySingleton<CatalogRepository>(
      () => CatalogRepositoryImpl(CatalogRemoteDataSource(getIt())),
    )
    ..registerLazySingleton<PassengerRepository>(
      () => PassengerRepositoryImpl(PassengerRemoteDataSource(getIt())),
    )
    ..registerLazySingleton<RidesRepository>(
      () => RidesRepositoryImpl(RidesRemoteDataSource(getIt())),
    )
    ..registerLazySingleton<WalletRepository>(
      () => WalletRepositoryImpl(WalletRemoteDataSource(getIt())),
    )
    ..registerLazySingleton<NotificationsRepository>(
      () => NotificationsRepositoryImpl(NotificationsRemoteDataSource(getIt())),
    )
    ..registerLazySingleton<DriverOnboardingRepository>(
      () => DriverOnboardingRepositoryImpl(
        DriverOnboardingRemoteDataSource(getIt()),
      ),
    )
    ..registerLazySingleton<DriverDashboardRepository>(
      () => DriverDashboardRepositoryImpl(
        DriverDashboardRemoteDataSource(getIt()),
      ),
    )
    ..registerLazySingleton<TripRealtimeDataSource>(
      () => SignalRTripRealtimeDataSource(
        hubUrl: hubUrl.isEmpty ? Env.hubUrl : hubUrl,
        tokens: getIt(),
      ),
    )
    ..registerLazySingleton<TripRepository>(
      () => TripRepositoryImpl(
        remote: TripRemoteDataSource(getIt()),
        realtime: getIt(),
      ),
    )
    ..registerLazySingleton<LocationRepository>(
      () => simulateLocation
          ? const SimulatedLocationRepository()
          : const GeolocatorLocationRepository(),
    );
}
