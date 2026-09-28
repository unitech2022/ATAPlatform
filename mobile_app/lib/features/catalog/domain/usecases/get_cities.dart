import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/catalog/domain/entities/city.dart';
import 'package:ata_app/features/catalog/domain/repositories/catalog_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Loads the served cities.
class GetCities implements UseCase<List<City>, NoParams> {
  const GetCities(this._repository);

  final CatalogRepository _repository;

  @override
  Future<Either<Failure, List<City>>> call(NoParams params) =>
      _repository.getCities();
}
