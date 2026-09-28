import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/features/passenger_home/domain/entities/ride_time.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/payment_row.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stage.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';

/// Localized copy for trip stages, steps, reasons and units.
abstract final class TripText {
  static const int _metersPerKm = 1000;
  static const int _secondsPerMinute = 60;

  /// Passenger-facing headline for a stage.
  static String passengerTitle(AppLocalizations l10n, TripStage stage) =>
      switch (stage) {
        TripStage.requested || TripStage.searching => l10n.searchingTitle,
        TripStage.driverAssigned => l10n.driverAssignedTitle,
        TripStage.driverEnRoute => l10n.driverEnRouteTitle,
        TripStage.driverArrived || TripStage.waiting => l10n.driverArrivedTitle,
        TripStage.pinVerified => l10n.readyToStartTitle,
        TripStage.inTrip => l10n.inTripTitle,
        TripStage.completed => l10n.receiptTitle,
        TripStage.cancelled => l10n.cancelledTitle,
        TripStage.noDrivers => l10n.noDriversTitle,
        TripStage.unknown => l10n.tripEyebrow,
      };

  /// Driver-facing headline for a stage.
  static String driverTitle(AppLocalizations l10n, TripStage stage) =>
      switch (stage) {
        TripStage.driverAssigned => l10n.stageDriverAssigned,
        TripStage.driverEnRoute => l10n.stageEnRoute,
        TripStage.driverArrived => l10n.stageArrived,
        TripStage.waiting => l10n.stageWaiting,
        TripStage.pinVerified => l10n.stagePinVerified,
        TripStage.inTrip => l10n.stageInTrip,
        TripStage.completed => l10n.stageCompleted,
        TripStage.cancelled || TripStage.noDrivers => l10n.cancelledTitle,
        _ => l10n.driverTripEyebrow,
      };

  static String stepLabel(AppLocalizations l10n, TripStep step) =>
      switch (step) {
        TripStep.enRoute => l10n.actionEnRoute,
        TripStep.arrived => l10n.actionArrived,
        TripStep.start => l10n.actionStart,
        TripStep.complete => l10n.actionComplete,
      };

  static String payment(AppLocalizations l10n, String apiValue) {
    for (final PaymentOption option in PaymentOption.values) {
      if (option.apiValue == apiValue) return paymentLabel(l10n, option);
    }
    return apiValue;
  }

  /// `1.2 كم` or `350 م`.
  static String distance(AppLocalizations l10n, int meters) =>
      meters >= _metersPerKm
      ? l10n.kmValue(Money.compact(meters / _metersPerKm))
      : l10n.metersValue(Money.integer(meters));

  /// `5 دقائق`.
  static String duration(AppLocalizations l10n, int seconds) =>
      l10n.minutesLabel((seconds / _secondsPerMinute).ceil());

  /// `mm:ss`, always Latin digits.
  static String clock(int seconds) {
    final int minutes = seconds ~/ _secondsPerMinute;
    final int rest = seconds % _secondsPerMinute;
    return '${minutes.toString().padLeft(2, '0')}:'
        '${rest.toString().padLeft(2, '0')}';
  }

  static String price(AppLocalizations l10n, num amount) =>
      l10n.priceWithCurrency(Money.compact(amount));
}
