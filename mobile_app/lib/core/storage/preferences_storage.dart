import 'package:shared_preferences/shared_preferences.dart';
import 'package:uuid/uuid.dart';

/// Non-sensitive key/value preferences (locale, active role, device id,
/// cached profile).
class PreferencesStorage {
  PreferencesStorage(this._prefs);

  static const String _localeKey = 'ata.locale';
  static const String _roleKey = 'ata.activeRole';
  static const String _deviceIdKey = 'ata.deviceId';
  static const String _cachedUserKey = 'ata.cachedUser';
  static const String defaultLocale = 'ar';

  final SharedPreferences _prefs;

  String get localeCode => _prefs.getString(_localeKey) ?? defaultLocale;
  bool get hasLocale => _prefs.containsKey(_localeKey);
  Future<void> saveLocale(String code) => _prefs.setString(_localeKey, code);

  String? get activeRole => _prefs.getString(_roleKey);
  Future<void> saveActiveRole(String role) => _prefs.setString(_roleKey, role);

  String? get cachedUserJson => _prefs.getString(_cachedUserKey);
  Future<void> saveCachedUser(String json) =>
      _prefs.setString(_cachedUserKey, json);

  /// Stable per-install identifier sent as `X-Device-Id`.
  String get deviceId {
    final String? existing = _prefs.getString(_deviceIdKey);
    if (existing != null) return existing;
    final String created = const Uuid().v4();
    _prefs.setString(_deviceIdKey, created);
    return created;
  }

  Future<void> clearSession() async {
    await _prefs.remove(_roleKey);
    await _prefs.remove(_cachedUserKey);
  }
}
