import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/features/safety/domain/entities/lost_item.dart';
import 'package:ata_app/features/safety/domain/entities/safety_case.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/painting.dart';

/// Localized labels of the F12 enums and statuses.
abstract final class SafetyText {
  static String caseType(AppLocalizations l10n, String type) => switch (type) {
    'sos' => l10n.caseTypeSos,
    'unexpected_stop' => l10n.alertUnexpectedStop,
    'route_deviation' => l10n.alertRouteDeviation,
    'trip_overrun' => l10n.alertTripOverrun,
    _ => l10n.caseTypeReport,
  };

  static String caseStatus(AppLocalizations l10n, String status) =>
      switch (status) {
        'open' => l10n.caseStatusOpen,
        'in_progress' => l10n.caseStatusInProgress,
        'escalated' => l10n.caseStatusEscalated,
        'resolved' => l10n.caseStatusResolved,
        _ => status,
      };

  static (Color, Color) statusColors(String status) => switch (status) {
    'resolved' ||
    'returned' ||
    'found' ||
    'closed' => (AtaColors.brandSoft, AtaColors.brand),
    'escalated' || 'not_found' => (AtaColors.dangerSoft, AtaColors.danger),
    _ => (AtaColors.warningSoft, AtaColors.warning),
  };

  /// "Are you OK?" copy per alert type.
  static String alertCopy(AppLocalizations l10n, String type) => switch (type) {
    'unexpected_stop' => l10n.alertUnexpectedStopCopy,
    'route_deviation' => l10n.alertRouteDeviationCopy,
    'trip_overrun' => l10n.alertTripOverrunCopy,
    _ => l10n.safetyCheckCopy,
  };

  static String reportCategory(AppLocalizations l10n, SafetyReportCategory c) =>
      switch (c) {
        SafetyReportCategory.unsafeDriving => l10n.reportUnsafeDriving,
        SafetyReportCategory.harassment => l10n.reportHarassment,
        SafetyReportCategory.vehicleMismatch => l10n.reportVehicleMismatch,
        SafetyReportCategory.driverMismatch => l10n.reportDriverMismatch,
        SafetyReportCategory.passengerMisconduct =>
          l10n.reportPassengerMisconduct,
        SafetyReportCategory.other => l10n.reportOther,
      };

  static String lostCategory(AppLocalizations l10n, LostItemCategory c) =>
      switch (c) {
        LostItemCategory.phone => l10n.lostPhone,
        LostItemCategory.wallet => l10n.lostWallet,
        LostItemCategory.bag => l10n.lostBag,
        LostItemCategory.keys => l10n.lostKeys,
        LostItemCategory.documents => l10n.lostDocuments,
        LostItemCategory.other => l10n.lostOther,
      };

  static String lostStatus(AppLocalizations l10n, String status) =>
      switch (status) {
        'open' => l10n.lostStatusOpen,
        'driver_contacted' => l10n.lostStatusDriverContacted,
        'found' => l10n.lostStatusFound,
        'returned' => l10n.lostStatusReturned,
        'not_found' => l10n.lostStatusNotFound,
        'closed' => l10n.lostStatusClosed,
        _ => status,
      };
}
