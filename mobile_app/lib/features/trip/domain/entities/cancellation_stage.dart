import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stage.dart';

/// Cancellation stage of `docs/09` §F14.1, used to filter the reasons.
enum CancellationStage {
  beforeAccept('before_accept'),
  afterAccept('after_accept'),
  enRoute('en_route'),
  arrived('arrived'),
  waiting('waiting'),
  noShow('no_show'),
  scheduled('scheduled');

  const CancellationStage(this.apiValue);

  final String apiValue;

  /// Default free waiting (`Trips:FreeWaitingMinutes` = 3); the API has the
  /// authoritative value and decides the real stage and fee.
  static const Duration defaultFreeWaiting = Duration(minutes: 3);

  static CancellationStage? parse(String? value) {
    for (final CancellationStage stage in values) {
      if (stage.apiValue == value) return stage;
    }
    return null;
  }

  /// Best-effort stage of [trip] at [now] (the API recomputes it).
  static CancellationStage of(
    Trip trip, {
    required DateTime now,
    Duration freeWaiting = defaultFreeWaiting,
  }) {
    final TripStage status = trip.status;
    // A booking waiting for its time follows the scheduled-ride rules; once
    // the search starts the normal stages apply (`docs/11` §F17.3).
    if (status == TripStage.scheduled) return scheduled;
    if (status.isSearching) return beforeAccept;
    if (status == TripStage.driverAssigned) return afterAccept;
    if (status == TripStage.driverEnRoute) return enRoute;
    final DateTime? arrivedAt = trip.timeline.arrivedAt;
    if (arrivedAt != null && now.difference(arrivedAt) > freeWaiting) {
      return waiting;
    }
    return arrived;
  }
}
