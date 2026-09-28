import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';

/// F12 (safety, chat, lost items) and F14 (cancellation, reliability)
/// error texts; `null` for other codes.
String? safetyFailureText(ServerFailure failure, AppLocalizations l10n) {
  switch (failure.code) {
    case ErrorCodes.shareNotFound:
      return l10n.shareNotFoundError;
    case ErrorCodes.shareExpired:
      return l10n.shareExpiredError;
    case ErrorCodes.trustedContactsLimit:
      return l10n.trustedContactsLimitError;
    case ErrorCodes.trustedContactExists:
      return l10n.trustedContactExistsError;
    case ErrorCodes.chatClosed:
      return l10n.chatClosedError;
    case ErrorCodes.lostItemWindowClosed:
      return l10n.lostItemWindowClosedError;
    case ErrorCodes.cancellationReasonInvalid:
      return l10n.cancellationReasonInvalidError;
    case ErrorCodes.cancellationFeeChanged:
      final double? fee = failure.numDetail(ErrorCodes.fee);
      return fee == null
          ? l10n.cancellationFeeChangedError
          : '${l10n.cancellationFeeChangedError} '
                '(${l10n.priceWithCurrency(Money.fixed(fee))})';
    case ErrorCodes.noShowTooEarly:
      final int seconds = failure.intDetail(ErrorCodes.secondsRemaining) ?? 0;
      return l10n.noShowTooEarlyError(
        (seconds / Duration.secondsPerMinute).ceil(),
      );
    case ErrorCodes.accountRestricted:
      return accountRestrictedText(failure, l10n);
  }
  return null;
}

/// `403 account_restricted` with `details { level, restrictedUntil }`.
String accountRestrictedText(Failure failure, AppLocalizations l10n) {
  final DateTime? until = DateTime.tryParse(
    failure.details?[ErrorCodes.restrictedUntil]?.toString() ?? '',
  );
  if (until == null) return l10n.accountRestrictedError;
  return l10n.accountRestrictedUntil(
    DateText.dayAndTime(until, l10n.localeName),
  );
}
