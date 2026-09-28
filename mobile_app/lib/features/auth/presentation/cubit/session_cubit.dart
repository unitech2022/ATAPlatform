import 'dart:async';

import 'package:ata_app/core/session/session_events.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/auth/domain/entities/auth_session.dart';
import 'package:ata_app/features/auth/domain/entities/driver_summary.dart';
import 'package:ata_app/features/auth/domain/entities/user.dart';
import 'package:ata_app/features/auth/domain/usecases/logout.dart';
import 'package:ata_app/features/auth/domain/usecases/restore_session.dart';
import 'package:ata_app/features/auth/presentation/cubit/session_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Owns the signed-in session for the whole app.
class SessionCubit extends Cubit<SessionState> {
  SessionCubit({
    required this._restoreSession,
    required this._logout,
    required SessionEvents events,
  }) : super(const SessionState.unknown()) {
    _expiredSubscription = events.expired.listen((_) => expire());
  }

  final RestoreSession _restoreSession;
  final Logout _logout;
  late final StreamSubscription<void> _expiredSubscription;

  Future<void> restore() async {
    final result = await _restoreSession(const NoParams());
    emit(
      result.fold(
        (_) => const SessionState.unauthenticated(),
        (AuthSession? session) => session == null
            ? const SessionState.unauthenticated()
            : SessionState.authenticated(session),
      ),
    );
  }

  void signedIn(AuthSession session) =>
      emit(SessionState.authenticated(session));

  void updateUser(User user) {
    final AuthSession? current = state.session;
    if (current != null) {
      emit(SessionState.authenticated(current.copyWith(user: user)));
    }
  }

  void updateDriverStatus(DriverApplicationStatus status) {
    final AuthSession? current = state.session;
    final DriverSummary? driver = current?.driver;
    if (current == null || driver == null) return;
    emit(
      SessionState.authenticated(
        current.copyWith(driver: driver.copyWith(applicationStatus: status)),
      ),
    );
  }

  Future<void> signOut() async {
    await _logout(const NoParams());
    emit(const SessionState.unauthenticated());
  }

  /// Called when a token refresh fails; local data was already cleared.
  void expire() => emit(const SessionState.unauthenticated());

  @override
  Future<void> close() async {
    await _expiredSubscription.cancel();
    return super.close();
  }
}
