import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/features/scheduled_rides/data/datasources/scheduled_remote_data_source.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/marketplace_trip.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/reservation.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/scheduled_queries.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/scheduled_trip.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/scheduling_rules.dart';
import 'package:ata_app/features/scheduled_rides/domain/repositories/scheduled_repository.dart';
import 'package:fpdart/fpdart.dart';

/// [ScheduledRepository] backed by the API.
class ScheduledRepositoryImpl implements ScheduledRepository {
  const ScheduledRepositoryImpl(this._remote);

  final ScheduledRemoteDataSource _remote;

  @override
  Future<Either<Failure, SchedulingRules>> getRules({String? rideCategoryId}) =>
      guard(() => _remote.rules(rideCategoryId: rideCategoryId));

  @override
  Future<Either<Failure, List<ScheduledTrip>>> getScheduledTrips() =>
      guard(() async {
        final trips = await _remote.scheduledTrips();
        return trips.map(ScheduledTrip.new).toList(growable: false);
      });

  @override
  Future<Either<Failure, PageResult<MarketplaceTrip>>> getMarketplace(
    MarketplaceQuery query,
  ) => guard(() => _remote.marketplace(query));

  @override
  Future<Either<Failure, Reservation>> reserve(String tripId) =>
      guard(() => _remote.reserve(tripId));

  @override
  Future<Either<Failure, ConfirmResult>> confirm(String tripId) =>
      guard(() => _remote.confirm(tripId));

  @override
  Future<Either<Failure, Reservation>> release(ReleaseParams params) =>
      guard(() => _remote.release(params.tripId, reason: params.reason));

  @override
  Future<Either<Failure, PageResult<Reservation>>> getReservations(
    ReservationsQuery query,
  ) => guard(() => _remote.reservations(query));
}
