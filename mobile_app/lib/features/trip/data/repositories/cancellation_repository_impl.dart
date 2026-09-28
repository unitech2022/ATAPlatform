import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/trip/data/datasources/cancellation_remote_data_source.dart';
import 'package:ata_app/features/trip/domain/entities/cancel_preview.dart';
import 'package:ata_app/features/trip/domain/entities/cancellation_reason.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:ata_app/features/trip/domain/entities/reliability_summary.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/domain/repositories/cancellation_repository.dart';
import 'package:fpdart/fpdart.dart';

/// [CancellationRepository] over the REST API.
class CancellationRepositoryImpl implements CancellationRepository {
  const CancellationRepositoryImpl(this._remote);

  final CancellationRemoteDataSource _remote;

  static String _actor(TripActor actor) =>
      actor == TripActor.passenger ? 'passenger' : 'driver';

  static String _prefix(TripActor actor) => actor == TripActor.passenger
      ? CancellationRemoteDataSource.passengerPrefix
      : CancellationRemoteDataSource.driverPrefix;

  @override
  Future<Either<Failure, List<CancellationReason>>> getReasons({
    required TripActor actor,
    String? stage,
  }) => guard(() => _remote.reasons(actor: _actor(actor), stage: stage));

  @override
  Future<Either<Failure, CancelPreview>> preview({
    required String tripId,
    required TripActor actor,
    String? reasonCode,
  }) => guard(
    () => _remote.preview(_prefix(actor), tripId, reasonCode: reasonCode),
  );

  @override
  Future<Either<Failure, Trip>> markNoShow({
    required String tripId,
    GeoPoint? at,
  }) => guard(
    () => _remote.noShow(tripId, <String, dynamic>{
      'lat': ?at?.lat,
      'lng': ?at?.lng,
    }),
  );

  @override
  Future<Either<Failure, ReliabilitySummary>> getReliability(TripActor role) =>
      guard(() => _remote.reliability(_prefix(role)));
}
