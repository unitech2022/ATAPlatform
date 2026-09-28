import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/trip/domain/entities/cancel_preview.dart';
import 'package:ata_app/features/trip/domain/entities/cancellation_reason.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:ata_app/features/trip/domain/entities/reliability_summary.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:fpdart/fpdart.dart';

/// F14 cancellation engine and reliability endpoints.
abstract interface class CancellationRepository {
  /// `GET /catalog/cancellation-reasons?actor=&stage=`.
  Future<Either<Failure, List<CancellationReason>>> getReasons({
    required TripActor actor,
    String? stage,
  });

  /// `POST /passenger|driver/trips/{id}/cancel/preview`.
  Future<Either<Failure, CancelPreview>> preview({
    required String tripId,
    required TripActor actor,
    String? reasonCode,
  });

  /// `POST /driver/trips/{id}/no-show`.
  Future<Either<Failure, Trip>> markNoShow({
    required String tripId,
    GeoPoint? at,
  });

  /// `GET /passenger/reliability` or `GET /driver/reliability`.
  Future<Either<Failure, ReliabilitySummary>> getReliability(TripActor role);
}
