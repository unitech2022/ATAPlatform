import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/catalog/domain/entities/ride_category.dart';
import 'package:ata_app/features/catalog/domain/repositories/catalog_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Loads the ride categories sorted by `sortOrder`.
class GetRideCategories implements UseCase<List<RideCategory>, NoParams> {
  const GetRideCategories(this._repository);

  final CatalogRepository _repository;

  @override
  Future<Either<Failure, List<RideCategory>>> call(NoParams params) async {
    final result = await _repository.getRideCategories();
    return result.map(
      (List<RideCategory> categories) => List<RideCategory>.of(categories)
        ..sort(
          (RideCategory a, RideCategory b) =>
              a.sortOrder.compareTo(b.sortOrder),
        ),
    );
  }
}
