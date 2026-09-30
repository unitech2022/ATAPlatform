import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';

/// F18 (help center, tickets, fare disputes, attachments) error texts;
/// `null` for other codes.
String? supportFailureText(ServerFailure failure, AppLocalizations l10n) {
  switch (failure.code) {
    case ErrorCodes.ticketClosed:
      return l10n.ticketClosedError;
    case ErrorCodes.disputeExists:
      return l10n.disputeExistsError;
    case ErrorCodes.disputeWindowClosed:
      return l10n.disputeWindowClosedError;
    case ErrorCodes.attachmentLimit:
      return l10n.attachmentLimitError;
    case ErrorCodes.unsupportedFileType:
      return l10n.unsupportedFileTypeError;
    case ErrorCodes.fileTooLarge:
      return l10n.fileTooLargeError;
  }
  return null;
}
