/// When the ride should start.
enum RideTime { now, scheduled }

/// How the rider pays.
enum PaymentOption {
  cash('cash'),
  wallet('wallet'),
  card('card');

  const PaymentOption(this.apiValue);

  final String apiValue;
}
