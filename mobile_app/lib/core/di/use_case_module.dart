import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/features/account/domain/usecases/change_language.dart';
import 'package:ata_app/features/account/domain/usecases/delete_account.dart';
import 'package:ata_app/features/account/domain/usecases/get_notification_preferences.dart';
import 'package:ata_app/features/account/domain/usecases/get_profile.dart';
import 'package:ata_app/features/account/domain/usecases/get_saved_locale.dart';
import 'package:ata_app/features/account/domain/usecases/update_notification_preferences.dart';
import 'package:ata_app/features/auth/domain/usecases/complete_rider_profile.dart';
import 'package:ata_app/features/auth/domain/usecases/logout.dart';
import 'package:ata_app/features/auth/domain/usecases/request_otp.dart';
import 'package:ata_app/features/auth/domain/usecases/restore_session.dart';
import 'package:ata_app/features/auth/domain/usecases/verify_otp.dart';
import 'package:ata_app/features/catalog/domain/usecases/get_cities.dart';
import 'package:ata_app/features/catalog/domain/usecases/get_document_types.dart';
import 'package:ata_app/features/catalog/domain/usecases/get_ride_categories.dart';
import 'package:ata_app/features/driver_dashboard/domain/usecases/get_driver_status.dart';
import 'package:ata_app/features/driver_dashboard/domain/usecases/get_driver_trips.dart';
import 'package:ata_app/features/driver_dashboard/domain/usecases/get_earnings_summary.dart';
import 'package:ata_app/features/driver_dashboard/domain/usecases/set_driver_online.dart';
import 'package:ata_app/features/driver_onboarding/domain/usecases/get_driver_application.dart';
import 'package:ata_app/features/notifications/domain/usecases/get_notifications.dart';
import 'package:ata_app/features/notifications/domain/usecases/mark_notifications_read.dart';
import 'package:ata_app/features/passenger_home/domain/usecases/update_passenger_preferences.dart';
import 'package:ata_app/features/rides/domain/usecases/get_passenger_trips.dart';
import 'package:ata_app/features/wallet/domain/usecases/get_wallet.dart';
import 'package:ata_app/features/wallet/domain/usecases/get_wallet_transactions.dart';
import 'package:ata_app/features/wallet/domain/usecases/top_up_wallet.dart';

/// Registers every use case against the repository interfaces, so tests can
/// swap repositories for fakes before calling this.
void registerUseCases() {
  getIt
    // auth
    ..registerLazySingleton<RequestOtp>(() => RequestOtp(getIt()))
    ..registerLazySingleton<VerifyOtp>(() => VerifyOtp(getIt()))
    ..registerLazySingleton<RestoreSession>(() => RestoreSession(getIt()))
    ..registerLazySingleton<CompleteRiderProfile>(
      () => CompleteRiderProfile(getIt()),
    )
    ..registerLazySingleton<Logout>(() => Logout(getIt()))
    // account
    ..registerLazySingleton<GetProfile>(() => GetProfile(getIt()))
    ..registerLazySingleton<GetNotificationPreferences>(
      () => GetNotificationPreferences(getIt()),
    )
    ..registerLazySingleton<UpdateNotificationPreferences>(
      () => UpdateNotificationPreferences(getIt()),
    )
    ..registerLazySingleton<DeleteAccount>(() => DeleteAccount(getIt()))
    ..registerLazySingleton<ChangeLanguage>(() => ChangeLanguage(getIt()))
    ..registerLazySingleton<GetSavedLocale>(() => GetSavedLocale(getIt()))
    // catalog
    ..registerLazySingleton<GetRideCategories>(() => GetRideCategories(getIt()))
    ..registerLazySingleton<GetDocumentTypes>(() => GetDocumentTypes(getIt()))
    ..registerLazySingleton<GetCities>(() => GetCities(getIt()))
    // passenger
    ..registerLazySingleton<UpdatePassengerPreferences>(
      () => UpdatePassengerPreferences(getIt()),
    )
    ..registerLazySingleton<GetPassengerTrips>(() => GetPassengerTrips(getIt()))
    // wallet
    ..registerLazySingleton<GetWallet>(() => GetWallet(getIt()))
    ..registerLazySingleton<GetWalletTransactions>(
      () => GetWalletTransactions(getIt()),
    )
    ..registerLazySingleton<TopUpWallet>(() => TopUpWallet(getIt()))
    // notifications
    ..registerLazySingleton<GetNotifications>(() => GetNotifications(getIt()))
    ..registerLazySingleton<MarkNotificationsRead>(
      () => MarkNotificationsRead(getIt()),
    )
    // driver
    ..registerLazySingleton<GetDriverApplication>(
      () => GetDriverApplication(getIt()),
    )
    ..registerLazySingleton<GetDriverStatus>(() => GetDriverStatus(getIt()))
    ..registerLazySingleton<SetDriverOnline>(() => SetDriverOnline(getIt()))
    ..registerLazySingleton<GetEarningsSummary>(
      () => GetEarningsSummary(getIt()),
    )
    ..registerLazySingleton<GetDriverTrips>(() => GetDriverTrips(getIt()));
}
