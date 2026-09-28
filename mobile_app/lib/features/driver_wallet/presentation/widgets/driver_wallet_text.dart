import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/payout.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/statement_period.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// Localized driver-wallet labels.
abstract final class DriverWalletText {
  static String period(AppLocalizations l10n, StatementPeriod period) =>
      switch (period) {
        StatementPeriod.today => l10n.periodToday,
        StatementPeriod.week => l10n.periodWeek,
        StatementPeriod.month => l10n.periodMonth,
      };

  /// Label, foreground and background of a payout status badge.
  static (String, Color, Color) status(
    AppLocalizations l10n,
    PayoutStatus status,
  ) => switch (status) {
    PayoutStatus.requested => (
      l10n.payoutRequested,
      AtaColors.warning,
      AtaColors.warningSoft,
    ),
    PayoutStatus.approved => (
      l10n.payoutApproved,
      AtaColors.ink,
      AtaColors.cloud,
    ),
    PayoutStatus.paid => (
      l10n.payoutPaid,
      AtaColors.brand,
      AtaColors.brandSoft,
    ),
    PayoutStatus.rejected => (
      l10n.payoutRejected,
      AtaColors.danger,
      AtaColors.dangerSoft,
    ),
    PayoutStatus.cancelled => (
      l10n.payoutCancelled,
      AtaColors.muted,
      AtaColors.cloud,
    ),
  };

  /// Reason why a payout cannot be requested right now.
  static String? reason(AppLocalizations l10n, String? reason) =>
      switch (reason) {
        'iban_missing' => l10n.ibanMissingError,
        'payout_pending_exists' => l10n.payoutPendingExistsError,
        'cash_debt_outstanding' => l10n.payoutCashDebtReason,
        'below_minimum' => l10n.payoutBelowMinimumReason,
        _ => null,
      };
}
