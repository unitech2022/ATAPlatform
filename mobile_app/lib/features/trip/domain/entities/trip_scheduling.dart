import 'package:ata_app/features/trip/domain/entities/trip_parties.dart';
import 'package:equatable/equatable.dart';

/// Status of a driver's reservation of a scheduled trip (`docs/11` §F17.2).
enum ReservationStatus {
  reserved('reserved'),
  confirmed('confirmed'),
  assigned('assigned'),
  released('released'),
  noShow('no_show'),
  completed('completed'),
  cancelled('cancelled'),
  unknown('');

  const ReservationStatus(this.apiValue);

  final String apiValue;

  static ReservationStatus parse(String? value) => values.firstWhere(
    (ReservationStatus s) => s.apiValue == value && s != unknown,
    orElse: () => unknown,
  );

  /// The reservation still holds the trip for the driver.
  bool get isActive =>
      this == reserved || this == confirmed || this == assigned;
}

/// `Trip.scheduling.reservation`: the reserved driver, seen by the rider.
class TripReservationInfo extends Equatable {
  const TripReservationInfo({
    required this.status,
    this.driverFirstName = '',
    this.driverPhotoUrl,
    this.ratingAvg,
    this.vehicle,
    this.reservedAt,
  });

  final ReservationStatus status;
  final String driverFirstName;
  final String? driverPhotoUrl;
  final double? ratingAvg;
  final TripVehicle? vehicle;
  final DateTime? reservedAt;

  /// The driver confirmed the first (T-60) prompt or is assigned.
  bool get isConfirmed =>
      status == ReservationStatus.confirmed ||
      status == ReservationStatus.assigned;

  @override
  List<Object?> get props => <Object?>[
    status,
    driverFirstName,
    driverPhotoUrl,
    ratingAvg,
    vehicle,
    reservedAt,
  ];
}

/// `Trip.scheduling` of a scheduled booking (`docs/11` §F17.4).
class TripScheduling extends Equatable {
  const TripScheduling({
    this.freeCancelUntil,
    this.searchStartsAt,
    this.reservation,
  });

  /// End of the free cancellation window.
  final DateTime? freeCancelUntil;

  /// When the normal driver search starts if nobody confirmed.
  final DateTime? searchStartsAt;
  final TripReservationInfo? reservation;

  @override
  List<Object?> get props => <Object?>[
    freeCancelUntil,
    searchStartsAt,
    reservation,
  ];
}
