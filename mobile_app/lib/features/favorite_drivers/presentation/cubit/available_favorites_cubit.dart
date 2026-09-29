import 'dart:async';

import 'package:ata_app/core/utils/countdown.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/available_favorite.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/favorite_params.dart';
import 'package:ata_app/features/favorite_drivers/domain/usecases/get_available_favorites.dart';
import 'package:ata_app/features/favorite_drivers/presentation/cubit/available_favorites_state.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Favourites available now around the pickup (`GET
/// /passenger/favorite-drivers/available`): looked up [debounce] after the
/// pickup or category changes and refreshed every [interval] while the home
/// sheet is open.
class AvailableFavoritesCubit extends Cubit<AvailableFavoritesState> {
  AvailableFavoritesCubit({
    required this._getAvailable,
    this.debounce = defaultDebounce,
    this.interval = defaultInterval,
    this._ticker = periodicTicker,
  }) : super(const AvailableFavoritesState());

  static const Duration defaultDebounce = Duration(milliseconds: 600);
  static const Duration defaultInterval = Duration(seconds: 30);

  final GetAvailableFavorites _getAvailable;
  final Duration debounce;
  final Duration interval;
  final Ticker _ticker;

  Timer? _debounceTimer;
  StreamSubscription<void>? _tick;
  int _generation = 0;

  /// Looks up the favourites for [pickup] and [rideCategoryId]; identical
  /// input is ignored.
  void watch(GeoPoint pickup, {String? rideCategoryId}) {
    final AvailableFavoritesQuery query = AvailableFavoritesQuery(
      pickup: pickup,
      rideCategoryId: rideCategoryId,
    );
    _tick ??= _ticker(interval).listen((_) => refresh());
    if (query == state.query &&
        state.status != AvailableFavoritesStatus.failure) {
      return;
    }
    emit(
      state.copyWith(query: query, status: AvailableFavoritesStatus.loading),
    );
    _debounceTimer?.cancel();
    _debounceTimer = Timer(debounce, _load);
  }

  /// Re-runs the last lookup at once.
  Future<void> refresh() {
    if (state.query == null) return Future<void>.value();
    _debounceTimer?.cancel();
    return _load();
  }

  Future<void> _load() async {
    final AvailableFavoritesQuery? query = state.query;
    if (query == null) return;
    final int generation = ++_generation;
    final result = await _getAvailable(query);
    if (isClosed || generation != _generation) return;
    emit(
      result.fold(
        (failure) => state.copyWith(
          status: AvailableFavoritesStatus.failure,
          items: const <AvailableFavorite>[],
          failure: failure,
        ),
        (List<AvailableFavorite> items) => state.copyWith(
          status: AvailableFavoritesStatus.ready,
          items: items,
          clearFailure: true,
        ),
      ),
    );
  }

  @override
  Future<void> close() async {
    _debounceTimer?.cancel();
    await _tick?.cancel();
    return super.close();
  }
}
