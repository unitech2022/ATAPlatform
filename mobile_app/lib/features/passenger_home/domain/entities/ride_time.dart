/// When the ride should start.
enum RideTime { now, scheduled }

/// How the rider pays.
enum PaymentOption {
  cash('cash'),
  wallet('wallet'),
  card('card'),

  /// The company account (F19): offered to active members only.
  corporate('corporate');

  const PaymentOption(this.apiValue);

  final String apiValue;
}
