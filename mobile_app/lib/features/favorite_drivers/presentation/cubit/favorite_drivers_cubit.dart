import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/favorite_driver.dart';
import 'package:ata_app/features/favorite_drivers/domain/usecases/get_favorite_drivers.dart';
import 'package:ata_app/features/favorite_drivers/domain/usecases/remove_favorite_driver.dart';
import 'package:ata_app/features/favorite_drivers/presentation/cubit/favorite_drivers_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// `/account/favorite-drivers`: my favourite drivers and their removal
/// (the page asks for confirmation before calling [remove]).
class FavoriteDriversCubit extends Cubit<FavoriteDriversState> {
  FavoriteDriversCubit({
    required this._getFavorites,
    required this._removeFavorite,
  }) : super(const FavoriteDriversState());

  final GetFavoriteDrivers _getFavorites;
  final RemoveFavoriteDriver _removeFavorite;

  Future<void> load() async {
    emit(state.copyWith(loading: true, clearFailure: true));
    final result = await _getFavorites(const NoParams());
    if (isClosed) return;
    emit(
      result.fold(
        (failure) => state.copyWith(loading: false, failure: failure),
        (List<FavoriteDriver> items) =>
            state.copyWith(loading: false, loaded: true, items: items),
      ),
    );
  }

  /// Removes [driver] from the favourites. A trip in progress is not
  /// affected (`docs/10` §F16.2).
  Future<void> remove(FavoriteDriver driver) async {
    if (state.removingId != null) return;
    emit(state.copyWith(removingId: driver.driverId, clearFailure: true));
    final result = await _removeFavorite(driver.driverId);
    if (isClosed) return;
    emit(
      result.fold(
        (failure) => state.copyWith(clearRemoving: true, failure: failure),
        (_) => state.copyWith(
          clearRemoving: true,
          lastRemoved: driver,
          items: state.items
              .where((FavoriteDriver d) => d.driverId != driver.driverId)
              .toList(growable: false),
        ),
      ),
    );
  }
}
