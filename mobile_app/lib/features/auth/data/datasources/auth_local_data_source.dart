import 'dart:convert';

import 'package:ata_app/core/storage/preferences_storage.dart';
import 'package:ata_app/core/storage/token_storage.dart';
import 'package:ata_app/features/auth/data/models/user_model.dart';
import 'package:ata_app/features/auth/domain/entities/user_role.dart';

/// Tokens in secure storage; role and a cached user in preferences.
class AuthLocalDataSource {
  const AuthLocalDataSource({required this._tokens, required this._prefs});

  final TokenStorage _tokens;
  final PreferencesStorage _prefs;

  Future<StoredTokens?> readTokens() => _tokens.read();

  UserRole? readActiveRole() => UserRole.tryParse(_prefs.activeRole);

  UserModel? readCachedUser() {
    final String? raw = _prefs.cachedUserJson;
    if (raw == null) return null;
    return UserModel.fromJson(jsonDecode(raw) as Map<String, dynamic>);
  }

  Future<void> saveSession({
    required StoredTokens tokens,
    required UserRole role,
    required UserModel user,
  }) async {
    await _tokens.save(tokens);
    await _prefs.saveActiveRole(role.apiValue);
    await saveUser(user);
  }

  Future<void> saveUser(UserModel user) =>
      _prefs.saveCachedUser(jsonEncode(user.toJson()));

  Future<void> clear() async {
    await _tokens.clear();
    await _prefs.clearSession();
  }
}
