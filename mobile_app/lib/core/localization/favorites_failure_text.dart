import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';

/// F16 error texts (favourite drivers); `null` for other codes.
String? favoritesFailureText(ServerFailure failure, AppLocalizations l10n) {
  switch (failure.code) {
    case ErrorCodes.favoriteNotEligible:
      return l10n.favoriteNotEligibleError;
    case ErrorCodes.favoriteExists:
      return l10n.favoriteExistsError;
    case ErrorCodes.favoritesLimit:
      return l10n.favoritesLimitError;
    case ErrorCodes.validationFailed:
      if (isNotFavorite(failure)) return l10n.favoriteNotFavoriteError;
  }
  return null;
}

/// `422 validation_failed { favoriteDriverId: "not_favorite" }`.
bool isNotFavorite(Failure failure) =>
    failure.code == ErrorCodes.validationFailed &&
    failure.details?[ErrorCodes.favoriteDriverId] == ErrorCodes.notFavorite;
