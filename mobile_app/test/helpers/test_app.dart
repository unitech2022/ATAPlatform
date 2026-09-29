import 'package:ata_app/app/app.dart';
import 'package:ata_app/app/bootstrap.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/di/use_case_module.dart';
import 'package:ata_app/core/storage/preferences_storage.dart';
import 'package:ata_app/design/theme/ata_theme.dart';
import 'package:ata_app/features/account/domain/repositories/account_repository.dart';
import 'package:ata_app/features/account/presentation/cubit/locale_cubit.dart';
import 'package:ata_app/features/auth/domain/repositories/auth_repository.dart';
import 'package:ata_app/features/catalog/domain/repositories/catalog_repository.dart';
import 'package:ata_app/features/driver_rewards/domain/repositories/driver_rewards_repository.dart';
import 'package:ata_app/features/favorite_drivers/domain/repositories/favorite_drivers_repository.dart';
import 'package:ata_app/features/notifications/domain/repositories/notifications_repository.dart';
import 'package:ata_app/features/passenger_home/domain/repositories/passenger_repository.dart';
import 'package:ata_app/features/pricing/domain/repositories/pricing_repository.dart';
import 'package:ata_app/features/promotions/domain/repositories/promotions_repository.dart';
import 'package:ata_app/features/rating/domain/repositories/rating_repository.dart';
import 'package:ata_app/features/rides/domain/repositories/rides_repository.dart';
import 'package:ata_app/features/safety/domain/repositories/safety_repository.dart';
import 'package:ata_app/features/trip/domain/repositories/cancellation_repository.dart';
import 'package:ata_app/features/trip/domain/repositories/location_repository.dart';
import 'package:ata_app/features/trip/domain/repositories/trip_repository.dart';
import 'package:ata_app/features/trip_chat/domain/repositories/trip_chat_repository.dart';
import 'package:ata_app/features/wallet/domain/repositories/wallet_repository.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'fakes.dart';
import 'favorites_fakes.dart';
import 'pricing_fakes.dart';
import 'rewards_fakes.dart';
import 'safety_fakes.dart';
import 'trip_fakes.dart';

/// Registers in-memory storage and fake repositories, then the real use cases.
Future<void> registerTestDependencies({FakeAuthRepository? auth}) async {
  await getIt.reset();
  SharedPreferences.setMockInitialValues(<String, Object>{});
  final SharedPreferences prefs = await SharedPreferences.getInstance();
  registerCore(
    prefs: PreferencesStorage(prefs),
    tokens: InMemoryTokenStorage(),
  );
  getIt
    ..registerSingleton<AuthRepository>(auth ?? FakeAuthRepository())
    ..registerSingleton<AccountRepository>(FakeAccountRepository())
    ..registerSingleton<CatalogRepository>(FakeCatalogRepository())
    ..registerSingleton<NotificationsRepository>(FakeNotificationsRepository())
    ..registerSingleton<PassengerRepository>(FakePassengerRepository())
    ..registerSingleton<PricingRepository>(FakePricingRepository())
    ..registerSingleton<RidesRepository>(FakeRidesRepository())
    ..registerSingleton<WalletRepository>(FakeWalletRepository())
    ..registerSingleton<TripRepository>(FakeTripRepository())
    ..registerSingleton<LocationRepository>(FakeLocationRepository())
    ..registerSingleton<SafetyRepository>(FakeSafetyRepository())
    ..registerSingleton<TripChatRepository>(FakeTripChatRepository())
    ..registerSingleton<CancellationRepository>(FakeCancellationRepository())
    ..registerSingleton<RatingRepository>(FakeRatingRepository())
    ..registerSingleton<PromotionsRepository>(FakePromotionsRepository())
    ..registerSingleton<FavoriteDriversRepository>(
      FakeFavoriteDriversRepository(),
    )
    ..registerSingleton<DriverRewardsRepository>(FakeDriverRewardsRepository());
  registerUseCases();
}

/// Builds the full app against the registered test dependencies (same
/// wiring as production: session, trips, deep links, push binding).
AtaApp buildTestApp() => bootstrapApp();

/// Wraps a widget with the theme, localizations and RTL direction.
Widget wrapForTest(Widget child, {Locale locale = const Locale('ar')}) {
  return MaterialApp(
    theme: AtaTheme.light(),
    locale: locale,
    supportedLocales: LocaleCubit.supported,
    localizationsDelegates: AppLocalizations.localizationsDelegates,
    home: Scaffold(body: child),
  );
}
