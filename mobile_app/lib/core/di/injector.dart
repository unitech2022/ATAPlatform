import 'package:ata_app/core/di/data_module.dart';
import 'package:ata_app/core/di/use_case_module.dart';
import 'package:ata_app/core/env/env.dart';
import 'package:ata_app/core/network/api_client.dart';
import 'package:ata_app/core/push/noop_push_service.dart';
import 'package:ata_app/core/push/onesignal_push_service.dart';
import 'package:ata_app/core/push/push_service.dart';
import 'package:ata_app/core/session/session_events.dart';
import 'package:ata_app/core/storage/preferences_storage.dart';
import 'package:ata_app/core/storage/token_storage.dart';
import 'package:get_it/get_it.dart';
import 'package:shared_preferences/shared_preferences.dart';

/// Service locator. Registration is manual (no code generation).
final GetIt getIt = GetIt.instance;

/// Production wiring: storage, network, data sources, repositories and
/// use cases.
Future<void> configureDependencies() async {
  final SharedPreferences prefs = await SharedPreferences.getInstance();
  registerCore(prefs: PreferencesStorage(prefs), tokens: SecureTokenStorage());
  registerData();
  registerUseCases();
  await getIt<PushService>().initialize();
}

/// OneSignal when `ONESIGNAL_APP_ID` is set, a no-op service otherwise.
PushService createPushService({String appId = Env.oneSignalAppId}) =>
    appId.isEmpty
    ? const NoopPushService()
    : OneSignalPushService(appId: appId);

/// Registers the infrastructure shared by every feature.
void registerCore({
  required PreferencesStorage prefs,
  required TokenStorage tokens,
  String baseUrl = Env.apiBaseUrl,
  PushService? push,
}) {
  getIt
    ..registerSingleton<PushService>(push ?? createPushService())
    ..registerSingleton<PreferencesStorage>(prefs)
    ..registerSingleton<TokenStorage>(tokens)
    ..registerSingleton<SessionEvents>(SessionEvents())
    ..registerLazySingleton<ApiClient>(
      () => ApiClient(
        baseUrl: baseUrl,
        tokens: tokens,
        prefs: prefs,
        onSessionExpired: getIt<SessionEvents>().notifyExpired,
      ),
    );
}
