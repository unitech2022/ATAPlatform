import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/available_favorite.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/favorite_driver.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/favorite_params.dart';
import 'package:fpdart/fpdart.dart';

/// My favourite drivers (F16).
abstract interface class FavoriteDriversRepository {
  Future<Either<Failure, List<FavoriteDriver>>> getFavorites();

  Future<Either<Failure, FavoriteDriver>> add(AddFavoriteParams params);

  Future<Either<Failure, Unit>> remove(String driverId);

  Future<Either<Failure, List<AvailableFavorite>>> getAvailable(
    AvailableFavoritesQuery query,
  );
}
