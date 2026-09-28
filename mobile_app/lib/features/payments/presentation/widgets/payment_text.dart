import 'package:ata_app/features/payments/domain/entities/saved_card.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';

/// Localized payment labels shared by the payments and wallet screens.
abstract final class PaymentText {
  /// `مدى`, `Visa`, `Mastercard`.
  static String brand(AppLocalizations l10n, String? brand) => switch (brand) {
    'mada' => l10n.cardBrandMada,
    'visa' => l10n.cardBrandVisa,
    'mastercard' => l10n.cardBrandMastercard,
    _ => l10n.paymentCard,
  };

  /// `مدى •••• 4201`.
  static String card(AppLocalizations l10n, SavedCard card) =>
      l10n.cardMasked(brand(l10n, card.brand), card.last4);

  /// `cash` / `wallet` / `card` of a receipt or trip.
  static String method(AppLocalizations l10n, String method) =>
      switch (method) {
        'wallet' => l10n.paymentWallet,
        'card' => l10n.paymentCard,
        _ => l10n.paymentCash,
      };
}
