import 'dart:async';

import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/trip/domain/entities/driver_location.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:ata_app/features/trip/domain/entities/offer.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_estimate.dart';
import 'package:ata_app/features/trip/domain/entities/trip_parties.dart';
import 'package:ata_app/features/trip/domain/entities/trip_request.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stage.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stop.dart';
import 'package:ata_app/features/trip/domain/entities/trip_timeline.dart';
import 'package:ata_app/features/trip/domain/repositories/location_repository.dart';
import 'package:ata_app/features/trip/domain/repositories/trip_repository.dart';
import 'package:fpdart/fpdart.dart';

const TripStop testPickup = TripStop(
  name: 'حي النرجس',
  address: 'الرياض',
  point: GeoPoint.riyadh,
);

const TripStop testDropoff = TripStop(
  name: 'واجهة الرياض',
  address: 'الرياض',
  point: GeoPoint(lat: 24.8433, lng: 46.7275),
);

const TripRequest testTripRequest = TripRequest(
  pickup: testPickup,
  dropoff: testDropoff,
  rideCategoryId: 'c1',
);

const TripEstimate testEstimate = TripEstimate(
  distanceMeters: 12000,
  durationSeconds: 1200,
  categories: <EstimateCategory>[
    EstimateCategory(
      rideCategoryId: 'c1',
      code: 'economy',
      name: 'اقتصادي',
      etaMinutes: 3,
      estimatedFare: 38,
      driverNetEarnings: 30.4,
    ),
  ],
);

const Trip testTrip = Trip(
  id: 't1',
  tripNumber: 'T-20260928-00001',
  status: TripStage.searching,
  pickup: testPickup,
  dropoff: testDropoff,
  category: TripCategory(id: 'c1', code: 'economy', name: 'اقتصادي'),
  estimatedFare: 38,
  estimatedDistanceMeters: 12000,
  estimatedDurationSeconds: 1200,
);

const TripDriver testDriver = TripDriver(
  id: 'd1',
  fullName: 'خالد أحمد',
  ratingAvg: 4.9,
  phoneMasked: '05XXXX1234',
);

const TripVehicle testVehicle = TripVehicle(
  make: 'تويوتا',
  model: 'كامري',
  color: 'أبيض',
  plateNumber: 'أ ب ج 2841',
);

Trip tripAt(TripStage stage, {DateTime? arrivedAt}) => Trip(
  id: testTrip.id,
  tripNumber: testTrip.tripNumber,
  status: stage,
  pickup: testPickup,
  dropoff: testDropoff,
  category: testTrip.category,
  estimatedFare: 38,
  finalFare: stage == TripStage.completed ? 41 : null,
  estimatedDistanceMeters: 12000,
  estimatedDurationSeconds: 1200,
  driver: stage.hasDriver || stage.isTerminal ? testDriver : null,
  vehicle: stage.hasDriver || stage.isTerminal ? testVehicle : null,
  passenger: const TripPassenger(firstName: 'عبدالله', phoneMasked: '05XX'),
  pin: stage.hasDriver ? '4821' : null,
  timeline: TripTimeline(arrivedAt: arrivedAt),
);

Offer testOffer({DateTime? expiresAt}) => Offer(
  id: 'o1',
  tripId: 't1',
  pickup: testPickup,
  dropoff: testDropoff,
  distanceToPickupMeters: 850,
  etaSeconds: 180,
  tripDistanceMeters: 12000,
  passengerPrice: 38,
  driverNetEarnings: 30.4,
  expiresAt: expiresAt ?? DateTime.now().add(const Duration(seconds: 20)),
  passengerFirstName: 'عبدالله',
  passengerRating: 4.8,
);

/// In-memory trip repository driven by controllers.
class FakeTripRepository implements TripRepository {
  final StreamController<Trip?> trips = StreamController<Trip?>.broadcast();
  final StreamController<Offer?> offers = StreamController<Offer?>.broadcast();
  final StreamController<DriverLocationUpdate> locations =
      StreamController<DriverLocationUpdate>.broadcast();
  final List<DriverPosition> sentPositions = <DriverPosition>[];
  Trip? active;
  Offer? activeOffer;

  @override
  Future<Either<Failure, TripEstimate>> estimate(TripRequest request) async =>
      const Right<Failure, TripEstimate>(testEstimate);

  @override
  Future<Either<Failure, Trip>> requestTrip(TripRequest request) async =>
      const Right<Failure, Trip>(testTrip);

  @override
  Future<Either<Failure, Trip?>> getActiveTrip(TripActor actor) async =>
      Right<Failure, Trip?>(active);

  @override
  Future<Either<Failure, Trip>> getTrip(String tripId) async =>
      Right<Failure, Trip>(active ?? testTrip);

  @override
  Future<Either<Failure, Trip>> cancelTrip({
    required String tripId,
    required TripActor actor,
    required String reasonCode,
    String? note,
    double? expectedFee,
    int? expectedPenaltyPoints,
  }) async => Right<Failure, Trip>(tripAt(TripStage.cancelled));

  @override
  Stream<Trip?> watchActiveTrip(TripActor actor) => trips.stream;

  @override
  Stream<DriverLocationUpdate> watchDriverLocation() => locations.stream;

  @override
  Future<Either<Failure, Unit>> sendLocation(DriverPosition position) async {
    sentPositions.add(position);
    return const Right<Failure, Unit>(unit);
  }

  @override
  Future<Either<Failure, Offer?>> getActiveOffer() async =>
      Right<Failure, Offer?>(activeOffer);

  @override
  Stream<Offer?> watchOffers() => offers.stream;

  @override
  Future<Either<Failure, Trip>> acceptOffer(String offerId) async =>
      Right<Failure, Trip>(tripAt(TripStage.driverAssigned));

  @override
  Future<Either<Failure, Unit>> rejectOffer(
    String offerId, {
    String? reason,
  }) async => const Right<Failure, Unit>(unit);

  @override
  Future<Either<Failure, Trip>> advance({
    required String tripId,
    required TripStep step,
    GeoPoint? at,
  }) async => Right<Failure, Trip>(
    tripAt(switch (step) {
      TripStep.enRoute => TripStage.driverEnRoute,
      TripStep.arrived => TripStage.waiting,
      TripStep.start => TripStage.inTrip,
      TripStep.complete => TripStage.completed,
    }),
  );

  @override
  Future<Either<Failure, Trip>> verifyPin({
    required String tripId,
    required String pin,
  }) async => pin == '4821'
      ? Right<Failure, Trip>(tripAt(TripStage.pinVerified))
      : const Left<Failure, Trip>(
          ServerFailure(
            code: 'pin_invalid',
            message: '',
            details: <String, dynamic>{'attemptsLeft': 4},
            statusCode: 400,
          ),
        );
}

/// Location repository that never touches the device.
class FakeLocationRepository implements LocationRepository {
  FakeLocationRepository({this.access = LocationAccess.granted});

  LocationAccess access;
  final StreamController<DriverPosition> feed =
      StreamController<DriverPosition>.broadcast();

  @override
  Future<LocationAccess> requestAccess() async => access;

  @override
  Stream<DriverPosition> positions() => feed.stream;
}
