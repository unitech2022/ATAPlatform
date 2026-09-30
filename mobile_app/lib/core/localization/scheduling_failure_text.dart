import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';

/// F17 error texts (scheduled rides, reservations, airports); `null` for
/// other codes.
String? schedulingFailureText(ServerFailure failure, AppLocalizations l10n) {
  switch (failure.code) {
    case ErrorCodes.scheduleWindowExceeded:
      final DateTime? max = _date(failure, ErrorCodes.maxScheduledAt);
      return max == null
          ? l10n.scheduleWindowExceededError
          : l10n.scheduleWindowExceededUntilError(_when(max, l10n));
    case ErrorCodes.scheduleLeadTooShort:
      final DateTime? min = _date(failure, ErrorCodes.minScheduledAt);
      return min == null
          ? l10n.scheduleLeadTooShortError
          : l10n.scheduleLeadTooShortUntilError(_when(min, l10n));
    case ErrorCodes.scheduledLimitReached:
      final int? max = failure.intDetail(ErrorCodes.max);
      return max == null
          ? l10n.scheduledLimitReachedError
          : l10n.scheduledLimitReachedMaxError(max);
    case ErrorCodes.reservationTaken:
      return l10n.reservationTakenError;
    case ErrorCodes.reservationConflict:
      return l10n.reservationConflictError;
    case ErrorCodes.reservationLimitReached:
      return l10n.reservationLimitReachedError;
    case ErrorCodes.reservationNotConfirmable:
      return switch (failure.details?[ErrorCodes.reason]) {
        'offline' => l10n.reservationNotConfirmableOfflineError,
        'on_trip' => l10n.reservationNotConfirmableOnTripError,
        'not_due' => l10n.reservationNotConfirmableNotDueError,
        'expired' => l10n.reservationNotConfirmableExpiredError,
        _ => l10n.reservationNotConfirmableError,
      };
    case ErrorCodes.airportPickupZoneRequired:
      return l10n.airportPickupZoneRequiredError;
    case ErrorCodes.airportCategoryNotApplicable:
      return l10n.airportCategoryNotApplicableError;
    case ErrorCodes.notInAirportWaitingArea:
      return l10n.notInAirportWaitingAreaError;
  }
  return null;
}

DateTime? _date(ServerFailure failure, String key) =>
    DateTime.tryParse(failure.details?[key]?.toString() ?? '');

String _when(DateTime date, AppLocalizations l10n) =>
    DateText.dayAndTime(date, l10n.localeName);
