import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/localization/safety_failure_text.dart';
import 'package:ata_app/core/utils/money.dart';
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
  static const String offerOutOfRange = 'offer_out_of_range';
  static const String quoteExpired = 'quote_expired';
  static const String paymentFailed = 'payment_failed';
  static const String paymentMethodExpired = 'payment_method_expired';
  static const String paymentMethodInUse = 'payment_method_in_use';
  static const String paymentProviderUnavailable =
      'payment_provider_unavailable';
  static const String outstandingBalance = 'outstanding_balance';
  static const String cashDebtLimitExceeded = 'cash_debt_limit_exceeded';
  static const String payoutBelowMinimum = 'payout_below_minimum';
  static const String payoutPendingExists = 'payout_pending_exists';
  static const String ibanMissing = 'iban_missing';
  static const String insufficientBalance = 'insufficient_balance';
  static const String shareNotFound = 'share_not_found';
  static const String shareExpired = 'share_expired';
  static const String trustedContactsLimit = 'trusted_contacts_limit';
  static const String trustedContactExists = 'trusted_contact_exists';
  static const String chatClosed = 'chat_closed';
  static const String lostItemWindowClosed = 'lost_item_window_closed';
  static const String cancellationReasonInvalid = 'cancellation_reason_invalid';
  static const String cancellationFeeChanged = 'cancellation_fee_changed';
  static const String noShowTooEarly = 'no_show_too_early';
  static const String accountRestricted = 'account_restricted';
  static const String validationFailed = 'validation_failed';
  static const String conflict = 'conflict';
  static const String secondsRemaining = 'secondsRemaining';
  static const String restrictedUntil = 'restrictedUntil';
  static const String level = 'level';
  static const String fee = 'fee';
  static const String amount = 'amount';
  static const String minAmount = 'minAmount';
  static const String offerMin = 'offerMin';
  static const String offerMax = 'offerMax';
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
    case ErrorCodes.offerOutOfRange:
      return l10n.offerOutOfRange(
        Money.compact(failure.numDetail(ErrorCodes.offerMin) ?? 0),
        Money.compact(failure.numDetail(ErrorCodes.offerMax) ?? 0),
      );
    case ErrorCodes.quoteExpired:
      return l10n.quoteExpiredError;
  }
  return _paymentText(failure, l10n) ??
      safetyFailureText(failure, l10n) ??
      (failure.message.isEmpty ? l10n.errorUnexpected : failure.message);
}

/// F11 payment, wallet and payout errors.
String? _paymentText(ServerFailure failure, AppLocalizations l10n) {
  switch (failure.code) {
    case ErrorCodes.paymentFailed:
      return l10n.paymentFailedError;
    case ErrorCodes.paymentMethodExpired:
      return l10n.paymentMethodExpiredError;
    case ErrorCodes.paymentMethodInUse:
      return l10n.paymentMethodInUseError;
    case ErrorCodes.paymentProviderUnavailable:
      return l10n.paymentProviderUnavailableError;
    case ErrorCodes.outstandingBalance:
      return l10n.outstandingBalanceError(
        Money.fixed(failure.numDetail(ErrorCodes.amount)?.abs() ?? 0),
      );
    case ErrorCodes.cashDebtLimitExceeded:
      return l10n.cashDebtLimitError;
    case ErrorCodes.payoutBelowMinimum:
      return l10n.payoutBelowMinimumError(
        Money.compact(failure.numDetail(ErrorCodes.minAmount) ?? 0),
      );
    case ErrorCodes.payoutPendingExists:
      return l10n.payoutPendingExistsError;
    case ErrorCodes.ibanMissing:
      return l10n.ibanMissingError;
    case ErrorCodes.insufficientBalance:
      return l10n.insufficientBalanceError;
  }
  return null;
}
