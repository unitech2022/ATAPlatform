import 'package:ata_app/features/rating/domain/entities/rating_tag.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';

/// Localized labels of the rating UI.
abstract final class RatingText {
  /// API name, or the seeded label for [RatingTag.code].
  static String tag(AppLocalizations l10n, RatingTag tag) =>
      tag.name.isNotEmpty ? tag.name : tagCode(l10n, tag.code);

  static String tagCode(AppLocalizations l10n, String code) => switch (code) {
    'driving' => l10n.ratingTagDriving,
    'cleanliness' => l10n.ratingTagCleanliness,
    'behaviour' => l10n.ratingTagBehaviour,
    'navigation' => l10n.ratingTagNavigation,
    'vehicle_condition' => l10n.ratingTagVehicleCondition,
    'punctuality' => l10n.ratingTagPunctuality,
    _ => code,
  };

  /// "How was your trip with …?" for riders, "How was …?" for drivers.
  static String question(AppLocalizations l10n, TripActor rater, String name) {
    if (rater == TripActor.driver) {
      return name.isEmpty
          ? l10n.ratePassengerQuestion
          : l10n.ratePassengerQuestionNamed(name);
    }
    return name.isEmpty
        ? l10n.rateDriverQuestion
        : l10n.rateDriverQuestionNamed(name);
  }

  static String stars(AppLocalizations l10n, int stars) => switch (stars) {
    1 => l10n.ratingStars1,
    2 => l10n.ratingStars2,
    3 => l10n.ratingStars3,
    4 => l10n.ratingStars4,
    5 => l10n.ratingStars5,
    _ => l10n.ratingStarsNone,
  };
}
