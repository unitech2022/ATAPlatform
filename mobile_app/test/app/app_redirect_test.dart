import 'package:ata_app/app/router/app_redirect.dart';
import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/features/auth/domain/entities/driver_summary.dart';
import 'package:ata_app/features/auth/domain/entities/user_role.dart';
import 'package:ata_app/features/auth/presentation/cubit/session_state.dart';
import 'package:ata_app/features/trip/presentation/cubit/trip_presence.dart';
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

  test('an active trip pins the rider to /trip', () {
    final SessionState rider = SessionState.authenticated(
      testSession.copyWith(
        user: testUser.copyWith(termsAcceptedAt: DateTime.utc(2026)),
      ),
    );
    const TripPresence presence = TripPresence(hasPassengerTrip: true);
    expect(
      resolveRedirect(rider, AppRoutes.home, presence: presence),
      AppRoutes.trip,
    );
    expect(resolveRedirect(rider, AppRoutes.trip, presence: presence), isNull);
    expect(resolveRedirect(rider, AppRoutes.trip), AppRoutes.home);
  });

  test('drivers are sent to the trip, then the offer, then the dashboard', () {
    final SessionState driver = SessionState.authenticated(
      testSession.copyWith(
        activeRole: UserRole.driver,
        driver: const DriverSummary(
          applicationNumber: 'ATA-1',
          applicationStatus: DriverApplicationStatus.approved,
        ),
      ),
    );
    const TripPresence both = TripPresence(hasDriverTrip: true, hasOffer: true);
    expect(
      resolveRedirect(driver, AppRoutes.driver, presence: both),
      AppRoutes.driverTrip,
    );
    const TripPresence offer = TripPresence(hasOffer: true);
    expect(
      resolveRedirect(driver, AppRoutes.driver, presence: offer),
      AppRoutes.driverOffer,
    );
    expect(
      resolveRedirect(driver, AppRoutes.driverOffer, presence: offer),
      isNull,
    );
    expect(resolveRedirect(driver, AppRoutes.driverOffer), AppRoutes.driver);
    expect(resolveRedirect(driver, AppRoutes.driverTrip), AppRoutes.driver);
  });
}
