import 'dart:async';

import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/features/airport/domain/entities/airport.dart';
import 'package:ata_app/features/airport/domain/entities/airport_queue_status.dart';
import 'package:ata_app/features/airport/domain/repositories/airport_repository.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/marketplace_trip.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/reservation.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/scheduled_queries.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/scheduled_trip.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/scheduling_rules.dart';
import 'package:ata_app/features/scheduled_rides/domain/repositories/scheduled_repository.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_airport.dart';
import 'package:ata_app/features/trip/domain/entities/trip_scheduling.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stage.dart';
import 'package:fpdart/fpdart.dart';

import 'trip_fakes.dart';

/// A fixed "now" for scheduling tests: Sunday 2026-09-27 10:00 (local).
final DateTime schedulingNow = DateTime(2026, 9, 27, 10);

/// A scheduled booking 3 days ahead, optionally with a reserved driver.
Trip scheduledTripAt(
  DateTime at, {
  TripStage status = TripStage.scheduled,
  ReservationStatus? reservation,
  String id = 't1',
  TripAirport? airport,
}) => Trip(
  id: id,
  tripNumber: 'T-20260927-00007',
  status: status,
  bookingType: 'scheduled',
  scheduledAt: at,
  pickup: testPickup,
  dropoff: testDropoff,
  category: testTrip.category,
  estimatedFare: 38,
  estimatedDistanceMeters: 12000,
  estimatedDurationSeconds: 1200,
  airport: airport,
  scheduling: TripScheduling(
    freeCancelUntil: at.subtract(const Duration(minutes: 60)),
    searchStartsAt: at.subtract(const Duration(minutes: 10)),
    reservation: reservation == null
        ? null
        : TripReservationInfo(
            status: reservation,
            driverFirstName: 'محمد',
            ratingAvg: 4.9,
            vehicle: testVehicle,
          ),
  ),
);

MarketplaceTrip marketTrip(String id, {DateTime? at, bool airport = false}) =>
    MarketplaceTrip(
      tripId: id,
      scheduledAt: at ?? schedulingNow.add(const Duration(days: 1)),
      categoryCode: 'economy',
      categoryName: 'اقتصادي',
      pickupArea: 'حي الملقا',
      dropoffArea: 'مطار الملك خالد',
      distanceToPickupKm: 8.4,
      tripDistanceMeters: 32000,
      estimatedFare: 95,
      driverNetEarnings: 76,
      isAirport: airport,
    );

Reservation reservationOf(
  String tripId, {
  ReservationStatus status = ReservationStatus.reserved,
  DateTime? at,
  DateTime? confirmDeadline,
  DateTime? finalConfirmDeadline,
  DateTime? freeReleaseUntil,
}) => Reservation(
  id: 'r-$tripId',
  tripId: tripId,
  status: status,
  scheduledAt: at ?? schedulingNow.add(const Duration(days: 1)),
  pickup: testPickup,
  dropoff: testDropoff,
  passengerFirstName: 'سارة',
  estimatedFare: 95,
  driverNetEarnings: 76,
  confirmDeadline: confirmDeadline,
  finalConfirmDeadline: finalConfirmDeadline,
  freeReleaseUntil: freeReleaseUntil,
);

PageResult<T> pageOf<T>(List<T> items, {int page = 1, int total = -1}) =>
    PageResult<T>(
      items: items,
      page: page,
      pageSize: items.length,
      total: total < 0 ? items.length : total,
    );

/// In-memory F17 scheduled-rides repository.
class FakeScheduledRepository implements ScheduledRepository {
  SchedulingRules rules = SchedulingRules.fallback;
  Failure? rulesFailure;
  List<ScheduledTrip> scheduled = <ScheduledTrip>[];
  Failure? scheduledFailure;
  PageResult<MarketplaceTrip> market = pageOf<MarketplaceTrip>(
    <MarketplaceTrip>[],
  );
  PageResult<Reservation> active = pageOf<Reservation>(<Reservation>[]);
  PageResult<Reservation> history = pageOf<Reservation>(<Reservation>[]);
  Failure? reserveFailure;
  Failure? confirmFailure;
  Failure? releaseFailure;
  ConfirmResult? confirmResult;
  int releasePenalty = 0;

  final List<String?> rulesCategories = <String?>[];
  final List<MarketplaceQuery> marketQueries = <MarketplaceQuery>[];
  final List<String> reserved = <String>[];
  final List<String> confirmed = <String>[];
  final List<ReleaseParams> released = <ReleaseParams>[];
  final List<ReservationsQuery> reservationQueries = <ReservationsQuery>[];

  @override
  Future<Either<Failure, SchedulingRules>> getRules({
    String? rideCategoryId,
  }) async {
    rulesCategories.add(rideCategoryId);
    final Failure? failure = rulesFailure;
    return failure == null
        ? Right<Failure, SchedulingRules>(rules)
        : Left<Failure, SchedulingRules>(failure);
  }

  @override
  Future<Either<Failure, List<ScheduledTrip>>> getScheduledTrips() async {
    final Failure? failure = scheduledFailure;
    return failure == null
        ? Right<Failure, List<ScheduledTrip>>(scheduled)
        : Left<Failure, List<ScheduledTrip>>(failure);
  }

  @override
  Future<Either<Failure, PageResult<MarketplaceTrip>>> getMarketplace(
    MarketplaceQuery query,
  ) async {
    marketQueries.add(query);
    return Right<Failure, PageResult<MarketplaceTrip>>(market);
  }

  @override
  Future<Either<Failure, Reservation>> reserve(String tripId) async {
    reserved.add(tripId);
    final Failure? failure = reserveFailure;
    return failure == null
        ? Right<Failure, Reservation>(reservationOf(tripId))
        : Left<Failure, Reservation>(failure);
  }

  @override
  Future<Either<Failure, ConfirmResult>> confirm(String tripId) async {
    confirmed.add(tripId);
    final Failure? failure = confirmFailure;
    if (failure != null) return Left<Failure, ConfirmResult>(failure);
    return Right<Failure, ConfirmResult>(
      confirmResult ??
          ConfirmResult(
            reservation: reservationOf(
              tripId,
              status: ReservationStatus.confirmed,
            ),
          ),
    );
  }

  @override
  Future<Either<Failure, Reservation>> release(ReleaseParams params) async {
    released.add(params);
    final Failure? failure = releaseFailure;
    if (failure != null) return Left<Failure, Reservation>(failure);
    return Right<Failure, Reservation>(
      Reservation(
        id: 'r-${params.tripId}',
        tripId: params.tripId,
        status: ReservationStatus.released,
        scheduledAt: schedulingNow,
        penaltyPoints: releasePenalty,
      ),
    );
  }

  @override
  Future<Either<Failure, PageResult<Reservation>>> getReservations(
    ReservationsQuery query,
  ) async {
    reservationQueries.add(query);
    return Right<Failure, PageResult<Reservation>>(
      query.list == ReservationList.active ? active : history,
    );
  }
}

/// The seeded King Khalid airport with one terminal and two pickup zones.
const Airport testAirport = Airport(
  id: 'a1',
  code: 'RUH',
  name: 'مطار الملك خالد الدولي',
  point: GeoPoint(lat: 24.9576, lng: 46.6988),
  terminals: <AirportZone>[
    AirportZone(id: 'te1', code: 'T1', terminalCode: 'T1', name: 'صالة 1'),
  ],
  pickupZones: <AirportZone>[
    AirportZone(
      id: 'z1',
      code: 'T1-P1',
      terminalCode: 'T1',
      name: 'منطقة الالتقاط 1 - صالة 1',
      point: GeoPoint(lat: 24.9601, lng: 46.7002),
      instructions: 'عند البوابة 3',
      freeWaitingMinutes: 20,
    ),
    AirportZone(
      id: 'z2',
      code: 'T1-P2',
      terminalCode: 'T1',
      name: 'منطقة الالتقاط 2 - صالة 1',
      point: GeoPoint(lat: 24.9610, lng: 46.7010),
    ),
  ],
);

/// In-memory F17 airport repository.
class FakeAirportRepository implements AirportRepository {
  List<Airport> airports = const <Airport>[testAirport];
  Failure? airportsFailure;
  AirportResolution? resolution;
  AirportQueueStatus queue = const AirportQueueStatus();
  Failure? queueFailure;
  Failure? joinFailure;
  final List<GeoPoint> resolved = <GeoPoint>[];
  final List<GeoPoint> joined = <GeoPoint>[];
  int leaves = 0;
  final StreamController<AirportQueuePosition> updates =
      StreamController<AirportQueuePosition>.broadcast();

  @override
  Future<Either<Failure, List<Airport>>> getAirports() async {
    final Failure? failure = airportsFailure;
    return failure == null
        ? Right<Failure, List<Airport>>(airports)
        : Left<Failure, List<Airport>>(failure);
  }

  @override
  Future<Either<Failure, AirportResolution?>> resolve(GeoPoint point) async {
    resolved.add(point);
    return Right<Failure, AirportResolution?>(resolution);
  }

  @override
  Future<Either<Failure, AirportQueueStatus>> getQueue() async {
    final Failure? failure = queueFailure;
    return failure == null
        ? Right<Failure, AirportQueueStatus>(queue)
        : Left<Failure, AirportQueueStatus>(failure);
  }

  @override
  Future<Either<Failure, AirportQueueStatus>> joinQueue(GeoPoint point) async {
    joined.add(point);
    final Failure? failure = joinFailure;
    if (failure != null) return Left<Failure, AirportQueueStatus>(failure);
    queue = const AirportQueueStatus(
      inQueue: true,
      airport: AirportRef(id: 'a1', code: 'RUH', name: 'مطار الملك خالد'),
      position: 7,
      total: 23,
      estimatedWaitMinutes: 25,
    );
    return Right<Failure, AirportQueueStatus>(queue);
  }

  @override
  Future<Either<Failure, Unit>> leaveQueue() async {
    leaves++;
    return const Right<Failure, Unit>(unit);
  }

  @override
  Stream<AirportQueuePosition> watchQueue() => updates.stream;
}
