import 'package:ata_app/features/trip/domain/entities/restriction_level.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_cancellation.dart';
import 'package:ata_app/features/trip/presentation/widgets/trip_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';

/// Localized copy for F14 cancellations and reliability levels.
abstract final class CancellationText {
  /// Passenger end-of-trip line about the cancellation fee, or `null`.
  static String? passengerCopy(AppLocalizations l10n, Trip trip) {
    final TripCancellation? c = trip.cancellation;
    if (c == null) return null;
    if (c.isPendingReview) return l10n.cancelFeePendingReview;
    if (c.wasCharged) {
      return l10n.cancelFeeCharged(TripText.price(l10n, c.feeCharged!));
    }
    return null;
  }

  /// Driver end-of-trip copy (no-show result, compensation), or `null`.
  static String? driverCopy(AppLocalizations l10n, Trip trip) {
    final TripCancellation? c = trip.cancellation;
    if (c == null) return null;
    final double compensation = c.compensation ?? 0;
    final String base = c.isNoShow
        ? l10n.noShowRecorded
        : l10n.driverTripCancelledCopy;
    return compensation > 0
        ? '$base ${l10n.compensationLine(TripText.price(l10n, compensation))}'
        : base;
  }

  static String levelLabel(AppLocalizations l10n, RestrictionLevel level) =>
      switch (level) {
        RestrictionLevel.none => l10n.levelNone,
        RestrictionLevel.warning => l10n.levelWarning,
        RestrictionLevel.matchingDeprioritized => l10n.levelDeprioritized,
        RestrictionLevel.incentivesReduced => l10n.levelIncentivesReduced,
        RestrictionLevel.temporarilyRestricted => l10n.levelRestricted,
        RestrictionLevel.suspended => l10n.levelSuspended,
      };

  /// What the level means for the user.
  static String levelExplanation(
    AppLocalizations l10n,
    RestrictionLevel level, {
    required bool driver,
  }) => switch (level) {
    RestrictionLevel.none => l10n.levelNoneCopy,
    RestrictionLevel.warning => l10n.levelWarningCopy,
    RestrictionLevel.matchingDeprioritized =>
      driver ? l10n.levelDeprioritizedCopy : l10n.levelWarningCopy,
    RestrictionLevel.incentivesReduced =>
      driver ? l10n.levelIncentivesReducedCopy : l10n.levelWarningCopy,
    RestrictionLevel.temporarilyRestricted =>
      driver ? l10n.levelRestrictedDriverCopy : l10n.levelRestrictedRiderCopy,
    RestrictionLevel.suspended => l10n.levelSuspendedCopy,
  };

  static String excuseStatus(AppLocalizations l10n, String status) =>
      switch (status) {
        'pending' => l10n.excusePending,
        'approved' => l10n.excuseApproved,
        'rejected' => l10n.excuseRejected,
        _ => '',
      };

  /// `18%` (rates come as 0..1).
  static String percent(double rate) => '${(rate * 100).round()}%';
}
