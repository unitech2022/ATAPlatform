import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/marketplace_trip.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/reservation.dart';
import 'package:equatable/equatable.dart';

/// State of `MarketplaceCubit` (driver marketplace).
class MarketplaceState extends Equatable {
  const MarketplaceState({
    required this.now,
    this.trips = const <MarketplaceTrip>[],
    this.day,
    this.loading = false,
    this.loadingMore = false,
    this.loaded = false,
    this.hasMore = false,
    this.page = 1,
    this.failure,
    this.reservingId,
    this.reserved,
    this.actionFailure,
  });

  /// Days offered by the day filter (today and the next six).
  static const int filterDays = 7;

  final DateTime now;
  final List<MarketplaceTrip> trips;

  /// Selected day (local midnight); `null` = every upcoming request.
  final DateTime? day;
  final bool loading;
  final bool loadingMore;
  final bool loaded;
  final bool hasMore;
  final int page;
  final Failure? failure;

  /// Trip being reserved right now.
  final String? reservingId;

  /// The reservation just made (the page shows it and moves to "my
  /// reservations").
  final Reservation? reserved;

  /// `reservation_taken`, `reservation_conflict`, `reservation_limit_reached`…
  final Failure? actionFailure;

  bool get isEmpty => loaded && failure == null && trips.isEmpty;
  bool get isReserving => reservingId != null;

  /// Calendar days of the day filter.
  List<DateTime> get days => <DateTime>[
    for (int i = 0; i < filterDays; i++)
      DateTime(now.year, now.month, now.day + i),
  ];

  MarketplaceState copyWith({
    DateTime? now,
    List<MarketplaceTrip>? trips,
    DateTime? day,
    bool? loading,
    bool? loadingMore,
    bool? loaded,
    bool? hasMore,
    int? page,
    Failure? failure,
    String? reservingId,
    Reservation? reserved,
    Failure? actionFailure,
    bool clearDay = false,
    bool clearFailure = false,
    bool clearReserving = false,
    bool clearReserved = false,
    bool clearActionFailure = false,
  }) => MarketplaceState(
    now: now ?? this.now,
    trips: trips ?? this.trips,
    day: clearDay ? null : day ?? this.day,
    loading: loading ?? this.loading,
    loadingMore: loadingMore ?? this.loadingMore,
    loaded: loaded ?? this.loaded,
    hasMore: hasMore ?? this.hasMore,
    page: page ?? this.page,
    failure: clearFailure ? null : failure ?? this.failure,
    reservingId: clearReserving ? null : reservingId ?? this.reservingId,
    reserved: clearReserved ? null : reserved ?? this.reserved,
    actionFailure: clearActionFailure
        ? null
        : actionFailure ?? this.actionFailure,
  );

  @override
  List<Object?> get props => <Object?>[
    now,
    trips,
    day,
    loading,
    loadingMore,
    loaded,
    hasMore,
    page,
    failure,
    reservingId,
    reserved,
    actionFailure,
  ];
}
