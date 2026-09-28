import 'package:equatable/equatable.dart';

/// Estimated fare shown before requesting a ride.
class FareEstimate extends Equatable {
  const FareEstimate({required this.price, required this.etaMinutes});

  final double price;
  final int etaMinutes;

  @override
  List<Object?> get props => <Object?>[price, etaMinutes];
}
