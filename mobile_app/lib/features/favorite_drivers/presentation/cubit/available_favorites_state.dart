import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/available_favorite.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/favorite_params.dart';
import 'package:equatable/equatable.dart';

/// Progress of the availability lookup.
enum AvailableFavoritesStatus { idle, loading, ready, failure }

/// State of [AvailableFavoritesCubit].
class AvailableFavoritesState extends Equatable {
  const AvailableFavoritesState({
    this.query,
    this.status = AvailableFavoritesStatus.idle,
    this.items = const <AvailableFavorite>[],
    this.failure,
  });

  final AvailableFavoritesQuery? query;
  final AvailableFavoritesStatus status;

  /// Favourites that can take a trip now; the last answer is kept while a
  /// new one loads.
  final List<AvailableFavorite> items;
  final Failure? failure;

  bool get isLoading => status == AvailableFavoritesStatus.loading;

  /// First lookup still running: nothing to show yet.
  bool get isInitialLoading => isLoading && items.isEmpty;

  AvailableFavorite? byId(String? driverId) {
    for (final AvailableFavorite item in items) {
      if (item.driverId == driverId) return item;
    }
    return null;
  }

  AvailableFavoritesState copyWith({
    AvailableFavoritesQuery? query,
    AvailableFavoritesStatus? status,
    List<AvailableFavorite>? items,
    Failure? failure,
    bool clearFailure = false,
  }) => AvailableFavoritesState(
    query: query ?? this.query,
    status: status ?? this.status,
    items: items ?? this.items,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[query, status, items, failure];
}
