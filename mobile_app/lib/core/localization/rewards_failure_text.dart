import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';

/// F15 error texts (ratings, promo codes, incentives); `null` for other
/// codes.
String? rewardsFailureText(ServerFailure failure, AppLocalizations l10n) {
  switch (failure.code) {
    case ErrorCodes.ratingWindowClosed:
      return l10n.ratingWindowClosedError;
    case ErrorCodes.ratingExists:
      return l10n.ratingExistsError;
    case ErrorCodes.promoNotFound:
      return l10n.promoNotFoundError;
    case ErrorCodes.promoExpired:
      return l10n.promoExpiredError;
    case ErrorCodes.promoUsageLimitReached:
      return failure.details?[ErrorCodes.scope] == 'user'
          ? l10n.promoUserLimitError
          : l10n.promoUsageLimitError;
    case ErrorCodes.promoNotEligible:
      return promoReasonText(
        failure.details?[ErrorCodes.reason]?.toString(),
        l10n,
      );
    case ErrorCodes.incentiveOptInClosed:
      return l10n.incentiveOptInClosedError;
  }
  return null;
}

/// `promo_not_eligible` with its `details.reason` (`docs/10` §F15.5); also
/// used for the `promotion.reason` of a quote.
String promoReasonText(String? reason, AppLocalizations l10n) =>
    switch (reason) {
      'first_trip_only' => l10n.promoReasonFirstTrip,
      'new_users_only' => l10n.promoReasonNewUsers,
      'city' => l10n.promoReasonCity,
      'category' => l10n.promoReasonCategory,
      'zone' => l10n.promoReasonZone,
      'payment_method' => l10n.promoReasonPaymentMethod,
      'booking_type' => l10n.promoReasonBookingType,
      'min_fare' => l10n.promoReasonMinFare,
      'pricing_mode' => l10n.promoReasonPricingMode,
      _ => l10n.promoNotEligibleError,
    };
