/// Lifecycle state of a trip (`status` field of the `Trip` object).
enum TripStage {
  requested('requested'),
  searching('searching'),
  driverAssigned('driver_assigned'),
  driverEnRoute('driver_en_route'),
  driverArrived('driver_arrived'),
  waiting('waiting'),
  pinVerified('pin_verified'),
  inTrip('in_trip'),
  completed('completed'),
  cancelled('cancelled'),
  noDrivers('no_drivers'),
  unknown('unknown');

  const TripStage(this.apiValue);

  final String apiValue;

  static TripStage parse(String? value) {
    for (final TripStage stage in values) {
      if (stage.apiValue == value) return stage;
    }
    return unknown;
  }

  /// The trip is over (successfully or not).
  bool get isTerminal =>
      this == completed || this == cancelled || this == noDrivers;

  /// Still looking for a driver.
  bool get isSearching => this == requested || this == searching;

  /// A driver is assigned and the trip has not ended.
  bool get hasDriver => !isTerminal && !isSearching && this != unknown;

  /// The driver is at the pickup point.
  bool get isWaiting => this == driverArrived || this == waiting;

  /// The passenger is on board (or about to be).
  bool get isRiding => this == pinVerified || this == inTrip;

  /// Cancellation is allowed until the trip starts.
  bool get canCancel => !isTerminal && this != inTrip && this != unknown;
}
