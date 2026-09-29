import 'package:equatable/equatable.dart';

/// An unrated completed trip (`GET /passenger|driver/ratings/pending`).
class PendingRating extends Equatable {
  const PendingRating({
    required this.tripId,
    this.tripNumber = '',
    this.counterpartName = '',
    this.completedAt,
    this.rateUntil,
  });

  final String tripId;
  final String tripNumber;
  final String counterpartName;
  final DateTime? completedAt;

  /// End of the 72 h window (`Ratings:WindowHours`).
  final DateTime? rateUntil;

  /// Still inside the rating window at [now] (unknown window = open).
  bool isOpenAt(DateTime now) => rateUntil == null || now.isBefore(rateUntil!);

  @override
  List<Object?> get props => <Object?>[
    tripId,
    tripNumber,
    counterpartName,
    completedAt,
    rateUntil,
  ];
}
