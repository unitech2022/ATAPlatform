import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/favorite_driver.dart';
import 'package:ata_app/features/favorite_drivers/domain/repositories/favorite_drivers_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `GET /passenger/favorite-drivers`.
class GetFavoriteDrivers implements UseCase<List<FavoriteDriver>, NoParams> {
  const GetFavoriteDrivers(this._repository);

  final FavoriteDriversRepository _repository;

  @override
  Future<Either<Failure, List<FavoriteDriver>>> call(NoParams params) =>
      _repository.getFavorites();
}
