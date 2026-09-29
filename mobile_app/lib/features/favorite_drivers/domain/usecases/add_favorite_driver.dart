import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/favorite_driver.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/favorite_params.dart';
import 'package:ata_app/features/favorite_drivers/domain/repositories/favorite_drivers_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Adds a driver to my favourites by `driverId` or by a completed `tripId`
/// (exactly one of them). The API enforces the shared completed trip
/// (`422 favorite_not_eligible`), the limit (`422 favorites_limit`) and
/// duplicates (`409 favorite_exists`).
class AddFavoriteDriver implements UseCase<FavoriteDriver, AddFavoriteParams> {
  const AddFavoriteDriver(this._repository);

  final FavoriteDriversRepository _repository;

  static const String validationFailed = 'validation_failed';

  @override
  Future<Either<Failure, FavoriteDriver>> call(AddFavoriteParams params) {
    final bool hasDriver = (params.driverId ?? '').isNotEmpty;
    final bool hasTrip = (params.tripId ?? '').isNotEmpty;
    if (hasDriver == hasTrip) {
      return Future<Either<Failure, FavoriteDriver>>.value(
        const Left<Failure, FavoriteDriver>(
          ServerFailure(
            code: validationFailed,
            message: '',
            details: <String, dynamic>{'driverId': 'required'},
          ),
        ),
      );
    }
    return _repository.add(params);
  }
}
