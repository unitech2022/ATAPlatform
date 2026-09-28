import 'package:ata_app/app/router/app_redirect.dart';
import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/features/auth/domain/entities/driver_summary.dart';
import 'package:ata_app/features/auth/domain/entities/user_role.dart';
import 'package:ata_app/features/auth/presentation/cubit/session_state.dart';
import 'package:flutter_test/flutter_test.dart';

import '../helpers/fakes.dart';

void main() {
  test('unknown session always shows the splash', () {
    expect(
      resolveRedirect(const SessionState.unknown(), AppRoutes.home),
      AppRoutes.splash,
    );
    expect(
      resolveRedirect(const SessionState.unknown(), AppRoutes.splash),
      isNull,
    );
  });

  test('signed-out users stay inside /auth', () {
    const SessionState state = SessionState.unauthenticated();
    expect(resolveRedirect(state, AppRoutes.home), AppRoutes.language);
    expect(resolveRedirect(state, AppRoutes.phone), isNull);
  });

  test('new riders must complete the terms step', () {
    const SessionState state = SessionState.authenticated(testSession);
    expect(resolveRedirect(state, AppRoutes.home), AppRoutes.terms);
    expect(resolveRedirect(state, AppRoutes.terms), isNull);
  });

  test('onboarded riders go home and cannot open driver pages', () {
    final SessionState state = SessionState.authenticated(
      testSession.copyWith(
        user: testUser.copyWith(termsAcceptedAt: DateTime.utc(2026)),
      ),
    );
    expect(resolveRedirect(state, AppRoutes.language), AppRoutes.home);
    expect(resolveRedirect(state, AppRoutes.driver), AppRoutes.home);
    expect(resolveRedirect(state, AppRoutes.wallet), isNull);
  });

  test('drivers are gated by the application status', () {
    final SessionState pending = SessionState.authenticated(
      testSession.copyWith(
        activeRole: UserRole.driver,
        driver: const DriverSummary(
          applicationNumber: 'ATA-1',
          applicationStatus: DriverApplicationStatus.submitted,
        ),
      ),
    );
    expect(resolveRedirect(pending, AppRoutes.home), AppRoutes.driverPending);
    expect(resolveRedirect(pending, AppRoutes.driverPending), isNull);

    final SessionState approved = SessionState.authenticated(
      testSession.copyWith(
        activeRole: UserRole.driver,
        driver: const DriverSummary(
          applicationNumber: 'ATA-1',
          applicationStatus: DriverApplicationStatus.approved,
        ),
      ),
    );
    expect(
      resolveRedirect(approved, AppRoutes.driverPending),
      AppRoutes.driver,
    );
    expect(resolveRedirect(approved, AppRoutes.driver), isNull);
  });
}
