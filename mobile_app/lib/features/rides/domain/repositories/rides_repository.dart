import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/features/rides/domain/entities/trip_summary.dart';
import 'package:fpdart/fpdart.dart';

/// Passenger trip history (`/passenger/trips`).
abstract interface class RidesRepository {
  Future<Either<Failure, PageResult<TripSummary>>> getTrips({
    String status = 'all',
    int page = 1,
  });
}
