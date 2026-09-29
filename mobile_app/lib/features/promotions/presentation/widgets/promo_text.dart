import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/features/promotions/domain/entities/promotion.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';

/// Localized labels of promotions.
abstract final class PromoText {
  /// "خصم 20% حتى 15 ر.س", "خصم 10 ر.س", "رسوم حجز مجانية".
  static String value(AppLocalizations l10n, Promotion p) {
    switch (p.type) {
      case PromotionType.percent:
        final String percent = l10n.promoPercentOff(Money.compact(p.value));
        final double? cap = p.maxDiscount;
        return cap == null
            ? percent
            : l10n.promoUpTo(
                percent,
                l10n.priceWithCurrency(Money.compact(cap)),
              );
      case PromotionType.fixed:
        return l10n.promoAmountOff(
          l10n.priceWithCurrency(Money.compact(p.value)),
        );
      case PromotionType.freeBookingFee:
        return l10n.promoFreeBookingFee;
      case PromotionType.unknown:
        return p.name;
    }
  }

  /// Minimum fare, first trip only and expiry.
  static List<String> conditions(
    AppLocalizations l10n,
    Promotion p,
    String localeCode,
  ) => <String>[
    if (p.minFare != null)
      l10n.promoMinFare(l10n.priceWithCurrency(Money.compact(p.minFare!))),
    if (p.firstTripOnly) l10n.promoFirstTripOnly,
    if (p.validTo != null)
      l10n.promoValidTo(DateText.longDate(p.validTo!, localeCode)),
  ];

  static String tab(AppLocalizations l10n, PromotionStatus status) =>
      switch (status) {
        PromotionStatus.available => l10n.promoTabAvailable,
        PromotionStatus.used => l10n.promoTabUsed,
        PromotionStatus.expired => l10n.promoTabExpired,
      };
}
