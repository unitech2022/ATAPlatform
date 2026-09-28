/// Build-time configuration, overridable with `--dart-define`.
abstract final class Env {
  /// Base URL of the ATA API (`--dart-define=API_BASE_URL=...`).
  static const String apiBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://10.0.2.2:5000/api/v1',
  );

  /// Public website URL of the driver document-upload portal.
  static const String driverPortalUrl = String.fromEnvironment(
    'DRIVER_PORTAL_URL',
    defaultValue: 'http://10.0.2.2:5173/driver',
  );

  /// Shows the `devCode` returned by the API on the OTP screen.
  static const bool showDevOtp = bool.fromEnvironment(
    'SHOW_DEV_OTP',
    defaultValue: true,
  );

  /// Explicit SignalR hub URL (`--dart-define=HUB_URL=...`). When empty the
  /// hub URL is derived from [apiBaseUrl].
  static const String hubUrlOverride = String.fromEnvironment('HUB_URL');

  /// Emits a fake Riyadh position instead of using the GPS
  /// (`--dart-define=SIMULATE_LOCATION=true`, handy on emulators).
  static const bool simulateLocation = bool.fromEnvironment(
    'SIMULATE_LOCATION',
  );

  /// OneSignal app id (`--dart-define=ONESIGNAL_APP_ID=...`). When empty,
  /// push is disabled and a no-op service is used.
  static const String oneSignalAppId = String.fromEnvironment(
    'ONESIGNAL_APP_ID',
  );

  /// Reported to the API in the `device.appVersion` field.
  static const String appVersion = '1.0.0';

  static const String _apiSuffix = '/api/v1';
  static const String _hubPath = '/hubs/trips';

  /// `<API origin>/hubs/trips` unless [hubUrlOverride] is set.
  static String get hubUrl =>
      hubUrlOverride.isNotEmpty ? hubUrlOverride : hubUrlFor(apiBaseUrl);

  /// Strips the `/api/v1` suffix from [baseUrl] and appends the hub path.
  static String hubUrlFor(String baseUrl) {
    String origin = baseUrl.trim();
    while (origin.endsWith('/')) {
      origin = origin.substring(0, origin.length - 1);
    }
    if (origin.endsWith(_apiSuffix)) {
      origin = origin.substring(0, origin.length - _apiSuffix.length);
    }
    return '$origin$_hubPath';
  }
}
