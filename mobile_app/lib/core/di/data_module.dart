import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/env/env.dart';
import 'package:ata_app/core/storage/preferences_storage.dart';
import 'package:ata_app/features/account/data/datasources/account_remote_data_source.dart';
import 'package:ata_app/features/account/data/repositories/account_repository_impl.dart';
import 'package:ata_app/features/account/domain/repositories/account_repository.dart';
import 'package:ata_app/features/airport/data/datasources/airport_remote_data_source.dart';
import 'package:ata_app/features/airport/data/repositories/airport_repository_impl.dart';
import 'package:ata_app/features/airport/domain/repositories/airport_repository.dart';
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
import 'package:ata_app/features/driver_rewards/data/datasources/driver_rewards_remote_data_source.dart';
import 'package:ata_app/features/driver_rewards/data/repositories/driver_rewards_repository_impl.dart';
import 'package:ata_app/features/driver_rewards/domain/repositories/driver_rewards_repository.dart';
import 'package:ata_app/features/driver_wallet/data/datasources/driver_wallet_remote_data_source.dart';
import 'package:ata_app/features/driver_wallet/data/repositories/driver_wallet_repository_impl.dart';
import 'package:ata_app/features/driver_wallet/domain/repositories/driver_wallet_repository.dart';
import 'package:ata_app/features/favorite_drivers/data/datasources/favorite_drivers_remote_data_source.dart';
import 'package:ata_app/features/favorite_drivers/data/repositories/favorite_drivers_repository_impl.dart';
import 'package:ata_app/features/favorite_drivers/domain/repositories/favorite_drivers_repository.dart';
import 'package:ata_app/features/notifications/data/datasources/notifications_remote_data_source.dart';
import 'package:ata_app/features/notifications/data/repositories/notifications_repository_impl.dart';
import 'package:ata_app/features/notifications/domain/repositories/notifications_repository.dart';
import 'package:ata_app/features/passenger_home/data/datasources/passenger_remote_data_source.dart';
import 'package:ata_app/features/passenger_home/data/repositories/passenger_repository_impl.dart';
import 'package:ata_app/features/passenger_home/domain/repositories/passenger_repository.dart';
import 'package:ata_app/features/payments/data/datasources/payments_remote_data_source.dart';
import 'package:ata_app/features/payments/data/repositories/payments_repository_impl.dart';
import 'package:ata_app/features/payments/data/tokenizers/sandbox_card_tokenizer.dart';
import 'package:ata_app/features/payments/domain/repositories/card_tokenizer.dart';
import 'package:ata_app/features/payments/domain/repositories/payments_repository.dart';
import 'package:ata_app/features/pricing/data/datasources/pricing_remote_data_source.dart';
import 'package:ata_app/features/pricing/data/repositories/pricing_repository_impl.dart';
import 'package:ata_app/features/pricing/domain/repositories/pricing_repository.dart';
import 'package:ata_app/features/promotions/data/datasources/promotions_remote_data_source.dart';
import 'package:ata_app/features/promotions/data/repositories/promotions_repository_impl.dart';
import 'package:ata_app/features/promotions/domain/repositories/promotions_repository.dart';
import 'package:ata_app/features/rating/data/datasources/rating_remote_data_source.dart';
import 'package:ata_app/features/rating/data/repositories/rating_repository_impl.dart';
import 'package:ata_app/features/rating/domain/repositories/rating_repository.dart';
import 'package:ata_app/features/rides/data/datasources/rides_remote_data_source.dart';
import 'package:ata_app/features/rides/data/repositories/rides_repository_impl.dart';
import 'package:ata_app/features/rides/domain/repositories/rides_repository.dart';
import 'package:ata_app/features/safety/data/datasources/safety_remote_data_source.dart';
import 'package:ata_app/features/safety/data/repositories/safety_repository_impl.dart';
import 'package:ata_app/features/safety/domain/repositories/safety_repository.dart';
import 'package:ata_app/features/scheduled_rides/data/datasources/scheduled_remote_data_source.dart';
import 'package:ata_app/features/scheduled_rides/data/repositories/scheduled_repository_impl.dart';
import 'package:ata_app/features/scheduled_rides/domain/repositories/scheduled_repository.dart';
import 'package:ata_app/features/support/data/datasources/support_remote_data_source.dart';
import 'package:ata_app/features/support/data/pickers/file_picker_attachment_picker.dart';
import 'package:ata_app/features/support/data/repositories/support_repository_impl.dart';
import 'package:ata_app/features/support/domain/repositories/support_repository.dart';
import 'package:ata_app/features/trip/data/datasources/cancellation_remote_data_source.dart';
import 'package:ata_app/features/trip/data/datasources/signalr_trip_realtime_data_source.dart';
import 'package:ata_app/features/trip/data/datasources/trip_realtime_data_source.dart';
import 'package:ata_app/features/trip/data/datasources/trip_remote_data_source.dart';
import 'package:ata_app/features/trip/data/repositories/cancellation_repository_impl.dart';
import 'package:ata_app/features/trip/data/repositories/geolocator_location_repository.dart';
import 'package:ata_app/features/trip/data/repositories/simulated_location_repository.dart';
import 'package:ata_app/features/trip/data/repositories/trip_repository_impl.dart';
import 'package:ata_app/features/trip/domain/repositories/cancellation_repository.dart';
import 'package:ata_app/features/trip/domain/repositories/location_repository.dart';
import 'package:ata_app/features/trip/domain/repositories/trip_repository.dart';
import 'package:ata_app/features/trip_chat/data/datasources/trip_chat_remote_data_source.dart';
import 'package:ata_app/features/trip_chat/data/repositories/trip_chat_repository_impl.dart';
import 'package:ata_app/features/trip_chat/domain/repositories/trip_chat_repository.dart';
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
    ..registerLazySingleton<PricingRepository>(
      () => PricingRepositoryImpl(PricingRemoteDataSource(getIt())),
    )
    ..registerLazySingleton<RidesRepository>(
      () => RidesRepositoryImpl(RidesRemoteDataSource(getIt())),
    )
    ..registerLazySingleton<WalletRepository>(
      () => WalletRepositoryImpl(WalletRemoteDataSource(getIt())),
    )
    ..registerLazySingleton<NotificationsRepository>(
      () => NotificationsRepositoryImpl(
        NotificationsRemoteDataSource(getIt()),
        getIt(),
      ),
    )
    ..registerLazySingleton<PaymentsRepository>(
      () => PaymentsRepositoryImpl(PaymentsRemoteDataSource(getIt())),
    )
    // Swap for the provider SDK tokenizer (MoyasarCardTokenizer) in F11 prod.
    ..registerLazySingleton<CardTokenizer>(() => const SandboxCardTokenizer())
    ..registerLazySingleton<DriverWalletRepository>(
      () => DriverWalletRepositoryImpl(DriverWalletRemoteDataSource(getIt())),
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
    ..registerLazySingleton<CancellationRepository>(
      () => CancellationRepositoryImpl(CancellationRemoteDataSource(getIt())),
    )
    ..registerLazySingleton<SafetyRepository>(
      () => SafetyRepositoryImpl(
        remote: SafetyRemoteDataSource(getIt()),
        realtime: getIt(),
        push: getIt(),
      ),
    )
    ..registerLazySingleton<TripChatRepository>(
      () => TripChatRepositoryImpl(
        remote: TripChatRemoteDataSource(getIt()),
        realtime: getIt(),
      ),
    )
    ..registerLazySingleton<RatingRepository>(
      () => RatingRepositoryImpl(RatingRemoteDataSource(getIt())),
    )
    ..registerLazySingleton<PromotionsRepository>(
      () => PromotionsRepositoryImpl(PromotionsRemoteDataSource(getIt())),
    )
    ..registerLazySingleton<FavoriteDriversRepository>(
      () => FavoriteDriversRepositoryImpl(
        FavoriteDriversRemoteDataSource(getIt()),
      ),
    )
    ..registerLazySingleton<DriverRewardsRepository>(
      () => DriverRewardsRepositoryImpl(DriverRewardsRemoteDataSource(getIt())),
    )
    ..registerLazySingleton<ScheduledRepository>(
      () => ScheduledRepositoryImpl(ScheduledRemoteDataSource(getIt())),
    )
    ..registerLazySingleton<AirportRepository>(
      () => AirportRepositoryImpl(
        remote: AirportRemoteDataSource(getIt()),
        realtime: getIt(),
      ),
    )
    ..registerLazySingleton<SupportRepository>(
      () => SupportRepositoryImpl(
        remote: SupportRemoteDataSource(getIt()),
        realtime: getIt(),
      ),
    )
    ..registerLazySingleton<AttachmentPicker>(
      () => const FilePickerAttachmentPicker(),
    )
    ..registerLazySingleton<LocationRepository>(
      () => simulateLocation
          ? const SimulatedLocationRepository()
          : const GeolocatorLocationRepository(),
    );
}
