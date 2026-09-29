import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/favorite_driver.dart';
import 'package:equatable/equatable.dart';

/// State of [FavoriteDriversCubit].
class FavoriteDriversState extends Equatable {
  const FavoriteDriversState({
    this.items = const <FavoriteDriver>[],
    this.loading = false,
    this.loaded = false,
    this.removingId,
    this.lastRemoved,
    this.failure,
  });

  final List<FavoriteDriver> items;
  final bool loading;

  /// The list was fetched at least once (drives the empty state).
  final bool loaded;

  /// Driver whose removal is in flight.
  final String? removingId;

  /// The driver removed last (the page shows a confirmation snackbar).
  final FavoriteDriver? lastRemoved;
  final Failure? failure;

  bool get isEmpty => loaded && !loading && items.isEmpty && failure == null;

  FavoriteDriversState copyWith({
    List<FavoriteDriver>? items,
    bool? loading,
    bool? loaded,
    String? removingId,
    FavoriteDriver? lastRemoved,
    Failure? failure,
    bool clearRemoving = false,
    bool clearFailure = false,
  }) => FavoriteDriversState(
    items: items ?? this.items,
    loading: loading ?? this.loading,
    loaded: loaded ?? this.loaded,
    removingId: clearRemoving ? null : removingId ?? this.removingId,
    lastRemoved: lastRemoved ?? this.lastRemoved,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[
    items,
    loading,
    loaded,
    removingId,
    lastRemoved,
    failure,
  ];
}
