import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/favorite_drivers/data/datasources/favorite_drivers_remote_data_source.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/available_favorite.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/favorite_driver.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/favorite_params.dart';
import 'package:ata_app/features/favorite_drivers/domain/repositories/favorite_drivers_repository.dart';
import 'package:fpdart/fpdart.dart';

/// [FavoriteDriversRepository] backed by the API.
class FavoriteDriversRepositoryImpl implements FavoriteDriversRepository {
  const FavoriteDriversRepositoryImpl(this._remote);

  final FavoriteDriversRemoteDataSource _remote;

  @override
  Future<Either<Failure, List<FavoriteDriver>>> getFavorites() =>
      guard(_remote.favorites);

  @override
  Future<Either<Failure, FavoriteDriver>> add(AddFavoriteParams params) =>
      guard(() => _remote.add(params));

  @override
  Future<Either<Failure, Unit>> remove(String driverId) => guard(() async {
    await _remote.remove(driverId);
    return unit;
  });

  @override
  Future<Either<Failure, List<AvailableFavorite>>> getAvailable(
    AvailableFavoritesQuery query,
  ) => guard(() => _remote.available(query));
}
