import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/di/payments_module.dart';
import 'package:ata_app/core/di/rewards_module.dart';
import 'package:ata_app/core/di/safety_module.dart';
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
import 'package:ata_app/features/pricing/domain/usecases/get_demand_at_location.dart';
import 'package:ata_app/features/pricing/domain/usecases/get_fare_quote.dart';
import 'package:ata_app/features/rides/domain/usecases/get_passenger_trips.dart';
import 'package:ata_app/features/trip/domain/usecases/accept_offer.dart';
import 'package:ata_app/features/trip/domain/usecases/advance_trip.dart';
import 'package:ata_app/features/trip/domain/usecases/cancel_trip.dart';
import 'package:ata_app/features/trip/domain/usecases/estimate_trip.dart';
import 'package:ata_app/features/trip/domain/usecases/get_active_offer.dart';
import 'package:ata_app/features/trip/domain/usecases/get_active_trip.dart';
import 'package:ata_app/features/trip/domain/usecases/get_trip.dart';
import 'package:ata_app/features/trip/domain/usecases/reject_offer.dart';
import 'package:ata_app/features/trip/domain/usecases/request_location_access.dart';
import 'package:ata_app/features/trip/domain/usecases/request_trip.dart';
import 'package:ata_app/features/trip/domain/usecases/send_driver_location.dart';
import 'package:ata_app/features/trip/domain/usecases/verify_pin.dart';
import 'package:ata_app/features/trip/domain/usecases/watch_active_trip.dart';
import 'package:ata_app/features/trip/domain/usecases/watch_device_position.dart';
import 'package:ata_app/features/trip/domain/usecases/watch_driver_location.dart';
import 'package:ata_app/features/trip/domain/usecases/watch_offers.dart';
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
    // pricing
    ..registerLazySingleton<GetFareQuote>(() => GetFareQuote(getIt()))
    ..registerLazySingleton<GetDemandAtLocation>(
      () => GetDemandAtLocation(getIt()),
    )
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
    ..registerLazySingleton<GetDriverTrips>(() => GetDriverTrips(getIt()))
    // trip
    ..registerLazySingleton<EstimateTrip>(() => EstimateTrip(getIt()))
    ..registerLazySingleton<RequestTrip>(() => RequestTrip(getIt()))
    ..registerLazySingleton<CancelTrip>(() => CancelTrip(getIt()))
    ..registerLazySingleton<GetActiveTrip>(() => GetActiveTrip(getIt()))
    ..registerLazySingleton<GetTrip>(() => GetTrip(getIt()))
    ..registerLazySingleton<WatchActiveTrip>(() => WatchActiveTrip(getIt()))
    ..registerLazySingleton<WatchDriverLocation>(
      () => WatchDriverLocation(getIt()),
    )
    ..registerLazySingleton<GetActiveOffer>(() => GetActiveOffer(getIt()))
    ..registerLazySingleton<WatchOffers>(() => WatchOffers(getIt()))
    ..registerLazySingleton<AcceptOffer>(() => AcceptOffer(getIt()))
    ..registerLazySingleton<RejectOffer>(() => RejectOffer(getIt()))
    ..registerLazySingleton<AdvanceTrip>(() => AdvanceTrip(getIt()))
    ..registerLazySingleton<VerifyPin>(() => VerifyPin(getIt()))
    ..registerLazySingleton<SendDriverLocation>(
      () => SendDriverLocation(getIt()),
    )
    ..registerLazySingleton<RequestLocationAccess>(
      () => RequestLocationAccess(getIt()),
    )
    ..registerLazySingleton<WatchDevicePosition>(
      () => WatchDevicePosition(getIt()),
    );
  registerPaymentAndPushUseCases();
  registerSafetyAndCancellationUseCases();
  registerRewardsUseCases();
}
