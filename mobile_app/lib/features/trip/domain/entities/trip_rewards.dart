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

/// `Trip.favorite.status` (`docs/10` §F16.1).
enum FavoriteStatus {
  /// The exclusive offer is pending with the favourite driver.
  requested('requested'),
  accepted('accepted'),

  /// Not eligible when the trip was requested: normal matching at once.
  unavailable('unavailable'),
  rejected('rejected'),
  expired('expired'),
  unknown('');

  const FavoriteStatus(this.apiValue);

  final String apiValue;

  static FavoriteStatus parse(String? value) => values.firstWhere(
    (FavoriteStatus s) => s.apiValue == value && s != unknown,
    orElse: () => unknown,
  );
}

/// `Trip.favorite`: the favourite driver the rider asked for (F16).
class TripFavorite extends Equatable {
  const TripFavorite({
    required this.driverId,
    this.driverName = '',
    this.status = FavoriteStatus.requested,
    this.discountApplied = false,
  });

  final String driverId;
  final String driverName;
  final FavoriteStatus status;
  final bool discountApplied;

  /// The exclusive offer is still with the favourite driver.
  bool get isPending => status == FavoriteStatus.requested;
  bool get isAccepted => status == FavoriteStatus.accepted;

  /// The search moved on to normal matching without the favourite.
  bool get fellBack =>
      status == FavoriteStatus.unavailable ||
      status == FavoriteStatus.rejected ||
      status == FavoriteStatus.expired;

  @override
  List<Object?> get props => <Object?>[
    driverId,
    driverName,
    status,
    discountApplied,
  ];
}
