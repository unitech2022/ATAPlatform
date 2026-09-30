import 'dart:async';

import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/core/utils/countdown.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/marketplace_trip.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/reservation.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/scheduled_queries.dart';
import 'package:ata_app/features/scheduled_rides/domain/usecases/get_marketplace_trips.dart';
import 'package:ata_app/features/scheduled_rides/domain/usecases/reserve_scheduled_trip.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/marketplace_state.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// The driver's marketplace of upcoming scheduled requests in the zone
/// (`docs/11` §F17.4): a day filter, refreshed every 60 s, and the
/// reservation of a trip. A taken trip leaves the list.
class MarketplaceCubit extends Cubit<MarketplaceState> {
  MarketplaceCubit({
    required this._getMarketplace,
    required this._reserve,
    this._ticker = periodicTicker,
    DateTime Function()? now,
  }) : _clock = now ?? DateTime.now,
       super(MarketplaceState(now: (now ?? DateTime.now)()));

  static const Duration refreshInterval = Duration(seconds: 60);

  final GetMarketplaceTrips _getMarketplace;
  final ReserveScheduledTrip _reserve;
  final Ticker _ticker;
  final DateTime Function() _clock;

  GeoPoint _position = GeoPoint.riyadh;
  StreamSubscription<void>? _refresh;

  /// Loads the first page around [position] and starts the 60 s refresh.
  Future<void> load({GeoPoint? position}) async {
    if (position != null) _position = position;
    _refresh ??= _ticker(refreshInterval).listen((_) => refresh());
    emit(
      state.copyWith(
        now: _clock(),
        loading: true,
        clearFailure: true,
        clearActionFailure: true,
      ),
    );
    await _fetch(page: 1, replace: true);
  }

  /// Reloads the first page without the loading state.
  Future<void> refresh() async {
    if (state.loading || state.isReserving) return;
    emit(state.copyWith(now: _clock()));
    await _fetch(page: 1, replace: true, silent: true);
  }

  /// Filters by [day] (`null` = all).
  Future<void> selectDay(DateTime? day) async {
    if (day == state.day) return;
    emit(state.copyWith(day: day, clearDay: day == null));
    await load();
  }

  Future<void> loadMore() async {
    if (!state.hasMore || state.loadingMore || state.loading) return;
    emit(state.copyWith(loadingMore: true));
    await _fetch(page: state.page + 1, replace: false);
  }

  Future<void> _fetch({
    required int page,
    required bool replace,
    bool silent = false,
  }) async {
    final DateTime? day = state.day;
    final result = await _getMarketplace(
      MarketplaceQuery(
        position: _position,
        from: day,
        to: day == null ? null : DateTime(day.year, day.month, day.day + 1),
        page: page,
      ),
    );
    if (isClosed) return;
    result.fold(
      (Failure failure) => emit(
        silent
            ? state
            : state.copyWith(
                loading: false,
                loadingMore: false,
                loaded: true,
                failure: failure,
              ),
      ),
      (PageResult<MarketplaceTrip> data) => emit(
        state.copyWith(
          trips: replace
              ? data.items
              : <MarketplaceTrip>[...state.trips, ...data.items],
          loading: false,
          loadingMore: false,
          loaded: true,
          hasMore: data.hasMore,
          page: data.page,
          clearFailure: true,
        ),
      ),
    );
  }

  /// Reserves [tripId]. Success removes the trip and exposes
  /// [MarketplaceState.reserved]; `reservation_taken` also drops it.
  Future<void> reserve(String tripId) async {
    if (state.isReserving) return;
    emit(
      state.copyWith(
        reservingId: tripId,
        clearActionFailure: true,
        clearReserved: true,
      ),
    );
    final result = await _reserve(tripId);
    if (isClosed) return;
    result.fold(
      (Failure failure) => emit(
        state.copyWith(
          clearReserving: true,
          actionFailure: failure,
          trips: failure.code == ErrorCodes.reservationTaken
              ? _without(tripId)
              : null,
        ),
      ),
      (Reservation reservation) => emit(
        state.copyWith(
          clearReserving: true,
          reserved: reservation,
          trips: _without(tripId),
        ),
      ),
    );
  }

  /// The reservation notice was shown.
  void clearNotice() =>
      emit(state.copyWith(clearReserved: true, clearActionFailure: true));

  List<MarketplaceTrip> _without(String tripId) => state.trips
      .where((MarketplaceTrip t) => t.tripId != tripId)
      .toList(growable: false);

  @override
  Future<void> close() async {
    await _refresh?.cancel();
    return super.close();
  }
}
