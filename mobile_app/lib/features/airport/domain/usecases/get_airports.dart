import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/airport/domain/entities/airport.dart';
import 'package:ata_app/features/airport/domain/repositories/airport_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Active airports with their terminals and pickup zones.
class GetAirports implements UseCase<List<Airport>, NoParams> {
  const GetAirports(this._repository);

  final AirportRepository _repository;

  @override
  Future<Either<Failure, List<Airport>>> call(NoParams params) =>
      _repository.getAirports();
}
