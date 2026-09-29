import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/available_favorite.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/favorite_driver.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/favorite_params.dart';
import 'package:ata_app/features/favorite_drivers/domain/repositories/favorite_drivers_repository.dart';
import 'package:fpdart/fpdart.dart';

const FavoriteDriver testFavoriteDriver = FavoriteDriver(
  driverId: 'd1',
  firstName: 'محمد',
  ratingAvg: 4.93,
  vehicle: FavoriteVehicle(make: 'Toyota', model: 'Camry', color: 'أبيض'),
  rideCategoryCode: 'economy',
  tripsTogether: 6,
);

const AvailableFavorite testAvailableFavorite = AvailableFavorite(
  driverId: 'd1',
  firstName: 'محمد',
  ratingAvg: 4.93,
  etaMinutes: 4,
  discount: FavoriteDiscount(percent: 10, maxAmount: 15),
);

/// In-memory F16 repository.
class FakeFavoriteDriversRepository implements FavoriteDriversRepository {
  List<FavoriteDriver> favorites = <FavoriteDriver>[];
  List<AvailableFavorite> available = <AvailableFavorite>[];
  Failure? addFailure;
  Failure? removeFailure;
  Failure? availableFailure;
  final List<AddFavoriteParams> added = <AddFavoriteParams>[];
  final List<String> removed = <String>[];
  final List<AvailableFavoritesQuery> queries = <AvailableFavoritesQuery>[];

  @override
  Future<Either<Failure, List<FavoriteDriver>>> getFavorites() async =>
      Right<Failure, List<FavoriteDriver>>(List<FavoriteDriver>.of(favorites));

  @override
  Future<Either<Failure, FavoriteDriver>> add(AddFavoriteParams params) async {
    added.add(params);
    final Failure? failure = addFailure;
    return failure == null
        ? const Right<Failure, FavoriteDriver>(testFavoriteDriver)
        : Left<Failure, FavoriteDriver>(failure);
  }

  @override
  Future<Either<Failure, Unit>> remove(String driverId) async {
    removed.add(driverId);
    final Failure? failure = removeFailure;
    return failure == null
        ? const Right<Failure, Unit>(unit)
        : Left<Failure, Unit>(failure);
  }

  @override
  Future<Either<Failure, List<AvailableFavorite>>> getAvailable(
    AvailableFavoritesQuery query,
  ) async {
    queries.add(query);
    final Failure? failure = availableFailure;
    return failure == null
        ? Right<Failure, List<AvailableFavorite>>(available)
        : Left<Failure, List<AvailableFavorite>>(failure);
  }
}
