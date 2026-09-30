import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/airport/domain/entities/airport.dart';
import 'package:ata_app/features/airport/domain/repositories/airport_repository.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:fpdart/fpdart.dart';

/// The airport around a point, or null.
class ResolveAirport implements UseCase<AirportResolution?, GeoPoint> {
  const ResolveAirport(this._repository);

  final AirportRepository _repository;

  @override
  Future<Either<Failure, AirportResolution?>> call(GeoPoint params) =>
      _repository.resolve(params);
}
