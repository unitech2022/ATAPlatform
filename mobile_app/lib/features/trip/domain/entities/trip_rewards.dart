import 'package:equatable/equatable.dart';

/// `Trip.myRating` / `canRate` / `rateUntil` (F15).
class TripRatingInfo extends Equatable {
  const TripRatingInfo({
    this.myStars,
    this.myTags = const <String>[],
    this.canRate,
    this.rateUntil,
  });

  /// Rating window when the API omits `rateUntil` (`Ratings:WindowHours`).
  static const Duration window = Duration(hours: 72);

  /// Stars I gave; `null` while unrated.
  final int? myStars;
  final List<String> myTags;

  /// `null` when the API does not send it.
  final bool? canRate;
  final DateTime? rateUntil;

  bool get isRated => myStars != null;

  /// Rating is possible at [now]: the API flag when present, otherwise
  /// unrated and inside 72 h of [completedAt].
  bool canRateAt(DateTime now, {DateTime? completedAt}) {
    if (isRated) return false;
    final DateTime? until = rateUntil ?? completedAt?.add(window);
    if (until != null && !now.isBefore(until)) return false;
    return canRate ?? until != null;
  }

  @override
  List<Object?> get props => <Object?>[myStars, myTags, canRate, rateUntil];
}

/// `Trip.promotion`: the promo code reserved / applied on the trip.
class TripPromotion extends Equatable {
  const TripPromotion({
    required this.code,
    this.status = 'reserved',
    this.discountAmount,
  });

  final String code;

  /// `reserved`, `applied` or `released`.
  final String status;

  /// Known once applied at completion.
  final double? discountAmount;

  bool get isReleased => status == 'released';

  @override
  List<Object?> get props => <Object?>[code, status, discountAmount];
}
