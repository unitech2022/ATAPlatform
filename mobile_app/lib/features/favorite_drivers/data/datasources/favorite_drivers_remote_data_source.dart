import 'package:ata_app/core/network/api_client.dart';
import 'package:ata_app/features/favorite_drivers/data/models/favorite_models.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/available_favorite.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/favorite_driver.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/favorite_params.dart';

/// `/passenger/favorite-drivers` (F16.3).
class FavoriteDriversRemoteDataSource {
  const FavoriteDriversRemoteDataSource(this._api);

  final ApiClient _api;

  static const String basePath = '/passenger/favorite-drivers';
  static const String availablePath = '$basePath/available';

  Future<List<FavoriteDriver>> favorites() async => _list(
    await _api.get(basePath),
  ).map(FavoriteModels.driver).toList(growable: false);

  Future<FavoriteDriver> add(AddFavoriteParams params) async =>
      FavoriteModels.driver(
        await _api.post(basePath, body: FavoriteModels.addBody(params))
            as Map<String, dynamic>,
      );

  Future<void> remove(String driverId) => _api.delete('$basePath/$driverId');

  Future<List<AvailableFavorite>> available(
    AvailableFavoritesQuery query,
  ) async => _list(
    await _api.get(availablePath, query: FavoriteModels.availableQuery(query)),
  ).map(FavoriteModels.available).toList(growable: false);

  /// Accepts a bare array or a `{ items: [...] }` page.
  static List<Map<String, dynamic>> _list(Object? body) {
    final Object? items = body is Map<String, dynamic> ? body['items'] : body;
    return items is List<dynamic>
        ? items.whereType<Map<String, dynamic>>().toList(growable: false)
        : const <Map<String, dynamic>>[];
  }
}
