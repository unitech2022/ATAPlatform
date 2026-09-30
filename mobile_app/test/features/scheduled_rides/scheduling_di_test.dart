import 'package:ata_app/core/di/data_module.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/di/use_case_module.dart';
import 'package:ata_app/core/push/noop_push_service.dart';
import 'package:ata_app/core/storage/preferences_storage.dart';
import 'package:ata_app/features/airport/domain/usecases/get_airport_queue.dart';
import 'package:ata_app/features/airport/domain/usecases/get_airports.dart';
import 'package:ata_app/features/airport/domain/usecases/join_airport_queue.dart';
import 'package:ata_app/features/airport/domain/usecases/leave_airport_queue.dart';
import 'package:ata_app/features/airport/domain/usecases/resolve_airport.dart';
import 'package:ata_app/features/airport/domain/usecases/watch_airport_queue.dart';
import 'package:ata_app/features/scheduled_rides/domain/usecases/confirm_reservation.dart';
import 'package:ata_app/features/scheduled_rides/domain/usecases/get_marketplace_trips.dart';
import 'package:ata_app/features/scheduled_rides/domain/usecases/get_my_reservations.dart';
import 'package:ata_app/features/scheduled_rides/domain/usecases/get_scheduled_trips.dart';
import 'package:ata_app/features/scheduled_rides/domain/usecases/get_scheduling_rules.dart';
import 'package:ata_app/features/scheduled_rides/domain/usecases/release_reservation.dart';
import 'package:ata_app/features/scheduled_rides/domain/usecases/reserve_scheduled_trip.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import '../../helpers/fakes.dart';

void main() {
  tearDown(getIt.reset);

  test('the production wiring resolves every F17 use case', () async {
    await getIt.reset();
    SharedPreferences.setMockInitialValues(<String, Object>{});
    registerCore(
      prefs: PreferencesStorage(await SharedPreferences.getInstance()),
      tokens: InMemoryTokenStorage(),
      baseUrl: 'http://localhost:5000/api/v1',
      push: const NoopPushService(),
    );
    registerData(hubUrl: 'http://localhost:5000/hubs/trips');
    registerUseCases();

    expect(getIt<GetSchedulingRules>(), isNotNull);
    expect(getIt<GetScheduledTrips>(), isNotNull);
    expect(getIt<GetMarketplaceTrips>(), isNotNull);
    expect(getIt<ReserveScheduledTrip>(), isNotNull);
    expect(getIt<ConfirmReservation>(), isNotNull);
    expect(getIt<ReleaseReservation>(), isNotNull);
    expect(getIt<GetMyReservations>(), isNotNull);
    expect(getIt<GetAirports>(), isNotNull);
    expect(getIt<ResolveAirport>(), isNotNull);
    expect(getIt<GetAirportQueue>(), isNotNull);
    expect(getIt<JoinAirportQueue>(), isNotNull);
    expect(getIt<LeaveAirportQueue>(), isNotNull);
    expect(getIt<WatchAirportQueue>(), isNotNull);
  });
}
