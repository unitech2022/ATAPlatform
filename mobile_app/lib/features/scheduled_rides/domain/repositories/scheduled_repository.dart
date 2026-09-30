import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/marketplace_trip.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/reservation.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/scheduled_queries.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/scheduled_trip.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/scheduling_rules.dart';
import 'package:fpdart/fpdart.dart';

/// Scheduled rides for riders and drivers (F17).
abstract interface class ScheduledRepository {
  /// `GET /passenger/scheduling/rules`.
  Future<Either<Failure, SchedulingRules>> getRules({String? rideCategoryId});

  /// `GET /passenger/trips/scheduled`.
  Future<Either<Failure, List<ScheduledTrip>>> getScheduledTrips();

  /// `GET /driver/scheduled/marketplace`.
  Future<Either<Failure, PageResult<MarketplaceTrip>>> getMarketplace(
    MarketplaceQuery query,
  );

  /// `POST /driver/scheduled/{tripId}/reserve`.
  Future<Either<Failure, Reservation>> reserve(String tripId);

  /// `POST /driver/scheduled/{tripId}/confirm`.
  Future<Either<Failure, ConfirmResult>> confirm(String tripId);

  /// `POST /driver/scheduled/{tripId}/release`.
  Future<Either<Failure, Reservation>> release(ReleaseParams params);

  /// `GET /driver/scheduled`.
  Future<Either<Failure, PageResult<Reservation>>> getReservations(
    ReservationsQuery query,
  );
}
