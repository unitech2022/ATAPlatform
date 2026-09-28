import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';

/// Known API error codes that have dedicated translations.
abstract final class ErrorCodes {
  static const String otpInvalid = 'otp_invalid';
  static const String otpExpired = 'otp_expired';
  static const String otpLocked = 'otp_locked';
  static const String rateLimited = 'rate_limited';
  static const String phoneInvalid = 'phone_invalid';
  static const String driverNotApproved = 'driver_not_approved';
  static const String tripActiveExists = 'trip_active_exists';
  static const String offerExpired = 'offer_expired';
  static const String pinInvalid = 'pin_invalid';
  static const String pinLocked = 'pin_locked';
  static const String attemptsLeft = 'attemptsLeft';
  static const String retryAfterSeconds = 'retryAfterSeconds';
}

/// Resolves a user-facing message for a [Failure].
String failureText(Failure failure, AppLocalizations l10n) {
  switch (failure) {
    case NetworkFailure():
      return l10n.errorNetwork;
    case UnauthorizedFailure():
      return l10n.errorUnauthorized;
    case UnexpectedFailure():
      return l10n.errorUnexpected;
    case ServerFailure():
      return _serverText(failure, l10n);
  }
}

String _serverText(ServerFailure failure, AppLocalizations l10n) {
  switch (failure.code) {
    case ErrorCodes.otpInvalid:
      return l10n.otpInvalid(failure.intDetail(ErrorCodes.attemptsLeft) ?? 0);
    case ErrorCodes.otpExpired:
      return l10n.otpExpired;
    case ErrorCodes.otpLocked:
      return l10n.otpLocked;
    case ErrorCodes.rateLimited:
      return l10n.rateLimited(
        failure.intDetail(ErrorCodes.retryAfterSeconds) ?? 0,
      );
    case ErrorCodes.phoneInvalid:
      return l10n.phoneInvalid;
    case ErrorCodes.driverNotApproved:
      return l10n.driverNotApproved;
    case ErrorCodes.tripActiveExists:
      return l10n.tripActiveExists;
    case ErrorCodes.offerExpired:
      return l10n.offerExpiredError;
    case ErrorCodes.pinInvalid:
      return l10n.pinInvalid(failure.intDetail(ErrorCodes.attemptsLeft) ?? 0);
    case ErrorCodes.pinLocked:
      return l10n.pinLocked;
  }
  return failure.message.isEmpty ? l10n.errorUnexpected : failure.message;
}
