import 'package:ata_app/features/rating/domain/entities/rating_tag.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:equatable/equatable.dart';

/// The trip being rated, who rates it and the counterpart's display name.
class RatingSubject extends Equatable {
  const RatingSubject({
    required this.tripId,
    required this.rater,
    this.counterpartName = '',
  });

  final String tripId;
  final TripActor rater;
  final String counterpartName;

  RatingTargetRole get target => RatingTargetRole.ratedBy(rater);

  @override
  List<Object?> get props => <Object?>[tripId, rater, counterpartName];
}
