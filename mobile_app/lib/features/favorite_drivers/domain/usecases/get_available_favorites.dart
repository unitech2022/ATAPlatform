import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/available_favorite.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/favorite_params.dart';
import 'package:ata_app/features/favorite_drivers/domain/repositories/favorite_drivers_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Favourites that can take a trip from the pickup now.
class GetAvailableFavorites
    implements UseCase<List<AvailableFavorite>, AvailableFavoritesQuery> {
  const GetAvailableFavorites(this._repository);

  final FavoriteDriversRepository _repository;

  @override
  Future<Either<Failure, List<AvailableFavorite>>> call(
    AvailableFavoritesQuery params,
  ) => _repository.getAvailable(params);
}
