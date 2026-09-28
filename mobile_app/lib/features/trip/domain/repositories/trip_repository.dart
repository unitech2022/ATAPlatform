import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/trip/domain/entities/cancel_reason.dart';
import 'package:ata_app/features/trip/domain/entities/driver_location.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:ata_app/features/trip/domain/entities/offer.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_estimate.dart';
import 'package:ata_app/features/trip/domain/entities/trip_request.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:fpdart/fpdart.dart';

/// `/passenger/trips`, `/driver/*` and the `/hubs/trips` realtime feed.
abstract interface class TripRepository {
  Future<Either<Failure, TripEstimate>> estimate(TripRequest request);
  Future<Either<Failure, Trip>> requestTrip(TripRequest request);
  Future<Either<Failure, Trip?>> getActiveTrip(TripActor actor);
  Future<Either<Failure, Trip>> getTrip(String tripId);
  Future<Either<Failure, Trip>> cancelTrip({
    required String tripId,
    required TripActor actor,
    required CancelReason reason,
    String? note,
  });

  /// Merged hub + polling feed; `null` means no active trip.
  Stream<Trip?> watchActiveTrip(TripActor actor);

  /// Live driver position for the passenger's active trip.
  Stream<DriverLocationUpdate> watchDriverLocation();

  Future<Either<Failure, Unit>> sendLocation(DriverPosition position);
  Future<Either<Failure, Offer?>> getActiveOffer();

  /// Merged hub + polling feed; `null` means no pending offer.
  Stream<Offer?> watchOffers();

  Future<Either<Failure, Trip>> acceptOffer(String offerId);
  Future<Either<Failure, Unit>> rejectOffer(String offerId, {String? reason});
  Future<Either<Failure, Trip>> advance({
    required String tripId,
    required TripStep step,
    GeoPoint? at,
  });
  Future<Either<Failure, Trip>> verifyPin({
    required String tripId,
    required String pin,
  });
}
