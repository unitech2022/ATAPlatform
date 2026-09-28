/// Reason codes accepted by the cancel endpoints.
enum CancelReason {
  changedMind('changed_mind'),
  driverLate('driver_late'),
  wrongPickup('wrong_pickup'),
  other('other');

  const CancelReason(this.apiValue);

  final String apiValue;
}
