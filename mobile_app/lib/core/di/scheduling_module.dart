import 'package:ata_app/core/di/injector.dart';
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

/// F17 use cases: scheduled rides (rider and driver) and airports.
void registerSchedulingUseCases() {
  getIt
    // scheduled rides
    ..registerLazySingleton<GetSchedulingRules>(
      () => GetSchedulingRules(getIt()),
    )
    ..registerLazySingleton<GetScheduledTrips>(() => GetScheduledTrips(getIt()))
    ..registerLazySingleton<GetMarketplaceTrips>(
      () => GetMarketplaceTrips(getIt()),
    )
    ..registerLazySingleton<ReserveScheduledTrip>(
      () => ReserveScheduledTrip(getIt()),
    )
    ..registerLazySingleton<ConfirmReservation>(
      () => ConfirmReservation(getIt()),
    )
    ..registerLazySingleton<ReleaseReservation>(
      () => ReleaseReservation(getIt()),
    )
    ..registerLazySingleton<GetMyReservations>(() => GetMyReservations(getIt()))
    // airport
    ..registerLazySingleton<GetAirports>(() => GetAirports(getIt()))
    ..registerLazySingleton<ResolveAirport>(() => ResolveAirport(getIt()))
    ..registerLazySingleton<GetAirportQueue>(() => GetAirportQueue(getIt()))
    ..registerLazySingleton<JoinAirportQueue>(() => JoinAirportQueue(getIt()))
    ..registerLazySingleton<LeaveAirportQueue>(() => LeaveAirportQueue(getIt()))
    ..registerLazySingleton<WatchAirportQueue>(
      () => WatchAirportQueue(getIt()),
    );
}
