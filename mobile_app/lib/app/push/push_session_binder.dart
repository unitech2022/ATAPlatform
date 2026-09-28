import 'dart:async';

import 'package:ata_app/core/push/push_service.dart';
import 'package:ata_app/features/account/domain/usecases/register_push_device.dart';
import 'package:ata_app/features/account/presentation/cubit/locale_cubit.dart';
import 'package:ata_app/features/auth/domain/entities/auth_session.dart';
import 'package:ata_app/features/auth/domain/entities/user_role.dart';
import 'package:ata_app/features/auth/presentation/cubit/session_cubit.dart';
import 'package:ata_app/features/auth/presentation/cubit/session_state.dart';
import 'package:flutter/widgets.dart';

/// OneSignal tag keys (`docs/03` OneSignal section).
abstract final class PushTags {
  static const String role = 'role';
  static const String lang = 'lang';
  static const String city = 'city';

  /// The app only serves Riyadh until the profile exposes a city.
  static const String defaultCity = 'riyadh';
}

/// Keeps the push provider in step with the session (listener only, no UI):
///
/// * sign-in → `login(userId)`, tags `role` / `lang` / `city`, language,
///   the permission prompt (never before a sign-in) and `PUT /me/devices`;
/// * role change → `role` tag; locale change → `lang` tag + language;
/// * sign-out → `logout()`.
///
/// Calls are serialized so fast session changes never interleave.
class PushSessionBinder {
  PushSessionBinder({
    required this._push,
    required this._registerDevice,
    this.city = PushTags.defaultCity,
  });

  final PushService _push;
  final RegisterPushDevice _registerDevice;
  final String city;

  String? _userId;
  UserRole? _role;
  String _language = 'ar';
  Future<void> _queue = Future<void>.value();
  final List<StreamSubscription<Object?>> _subscriptions =
      <StreamSubscription<Object?>>[];

  /// Completes once every queued provider call has run.
  @visibleForTesting
  Future<void> get settled => _queue;

  /// Follows [session] and [language] (current value, then every change).
  void bind({
    required SessionState session,
    required Stream<SessionState> sessionChanges,
    required String language,
    required Stream<String> languageChanges,
  }) {
    _language = language;
    _enqueue(() => _onSession(session));
    _subscriptions
      ..add(
        sessionChanges.listen(
          (SessionState state) => _enqueue(() => _onSession(state)),
        ),
      )
      ..add(
        languageChanges.listen(
          (String code) => _enqueue(() => _onLanguage(code)),
        ),
      );
  }

  /// Convenience wiring for the app-wide cubits.
  void bindCubits(SessionCubit session, LocaleCubit locale) => bind(
    session: session.state,
    sessionChanges: session.stream,
    language: locale.state.languageCode,
    languageChanges: locale.stream.map((Locale l) => l.languageCode),
  );

  void _enqueue(Future<void> Function() action) {
    _queue = _queue.then((_) => action()).catchError((Object error) {
      debugPrint('push: $error');
    });
  }

  Future<void> _onSession(SessionState state) async {
    final AuthSession? session = state.session;
    if (state.isAuthenticated && session != null) {
      if (_userId != session.user.id) {
        await _signIn(session);
      } else if (_role != session.activeRole) {
        _role = session.activeRole;
        await _push.setTags(<String, String>{
          PushTags.role: session.activeRole.apiValue,
        });
      }
      return;
    }
    if (state.status == SessionStatus.unauthenticated && _userId != null) {
      _userId = null;
      _role = null;
      await _push.logout();
    }
  }

  Future<void> _signIn(AuthSession session) async {
    _userId = session.user.id;
    _role = session.activeRole;
    await _push.login(session.user.id);
    await _push.setTags(<String, String>{
      PushTags.role: session.activeRole.apiValue,
      PushTags.lang: _language,
      PushTags.city: city,
    });
    await _push.setLanguage(_language);
    await _push.requestPermission();
    await _registerDevice(_push.subscriptionId);
  }

  Future<void> _onLanguage(String code) async {
    if (code == _language) return;
    _language = code;
    if (_userId == null) return;
    await _push.setTags(<String, String>{PushTags.lang: code});
    await _push.setLanguage(code);
  }

  Future<void> close() async {
    for (final StreamSubscription<Object?> s in _subscriptions) {
      await s.cancel();
    }
    _subscriptions.clear();
  }
}
