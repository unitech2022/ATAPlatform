import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/favorite_drivers/domain/repositories/favorite_drivers_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `DELETE /passenger/favorite-drivers/{driverId}`.
class RemoveFavoriteDriver implements UseCase<Unit, String> {
  const RemoveFavoriteDriver(this._repository);

  final FavoriteDriversRepository _repository;

  @override
  Future<Either<Failure, Unit>> call(String driverId) =>
      _repository.remove(driverId);
}
