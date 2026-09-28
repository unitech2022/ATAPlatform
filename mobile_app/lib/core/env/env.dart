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

  /// Reported to the API in the `device.appVersion` field.
  static const String appVersion = '1.0.0';
}
