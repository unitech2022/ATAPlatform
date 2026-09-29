import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:equatable/equatable.dart';

/// A rating tag from `GET /catalog/rating-tags?target=`. With 1–3 stars it
/// reads as "what went wrong", with 4–5 as "what you liked".
class RatingTag extends Equatable {
  const RatingTag({required this.code, this.name = ''});

  /// Driver tags (the passenger rates the driver).
  static const List<String> driverCodes = <String>[
    'driving',
    'cleanliness',
    'behaviour',
    'navigation',
    'vehicle_condition',
  ];

  /// Passenger tags (the driver rates the passenger).
  static const List<String> passengerCodes = <String>[
    'punctuality',
    'behaviour',
    'cleanliness',
  ];

  /// Seeded tags used when the catalog cannot be loaded; the UI names them.
  static List<RatingTag> defaultsFor(RatingTargetRole target) => <RatingTag>[
    for (final String code
        in target == RatingTargetRole.driver ? driverCodes : passengerCodes)
      RatingTag(code: code),
  ];

  final String code;

  /// API-localized name; empty for the local defaults.
  final String name;

  @override
  List<Object?> get props => <Object?>[code, name];
}

/// Who is being rated (`target` of the tag catalog).
enum RatingTargetRole {
  driver('driver'),
  passenger('passenger');

  const RatingTargetRole(this.apiValue);

  final String apiValue;

  /// The rater's counterpart: riders rate drivers and vice versa.
  static RatingTargetRole ratedBy(TripActor rater) =>
      rater == TripActor.passenger ? driver : passenger;
}
