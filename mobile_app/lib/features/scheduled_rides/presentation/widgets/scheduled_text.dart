import 'package:ata_app/features/scheduled_rides/domain/entities/scheduled_trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_scheduling.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';

/// Localized copy for scheduled rides and reservations.
abstract final class ScheduledText {
  static const int _secondsPerMinute = 60;
  static const int _minutesPerHour = 60;
  static const int _hoursPerDay = 24;

  /// Rider-facing status of a booking.
  static String phase(AppLocalizations l10n, ScheduledPhase phase) =>
      switch (phase) {
        ScheduledPhase.waitingForDriver => l10n.scheduledPhaseWaiting,
        ScheduledPhase.driverReserved => l10n.scheduledPhaseReserved,
        ScheduledPhase.driverConfirmed => l10n.scheduledPhaseConfirmed,
        ScheduledPhase.searching => l10n.scheduledPhaseSearching,
        ScheduledPhase.inProgress => l10n.scheduledPhaseInProgress,
        ScheduledPhase.ended => l10n.scheduledPhaseEnded,
      };

  /// Driver-facing status of a reservation.
  static String reservation(AppLocalizations l10n, ReservationStatus status) =>
      switch (status) {
        ReservationStatus.reserved => l10n.reservationStatusReserved,
        ReservationStatus.confirmed => l10n.reservationStatusConfirmed,
        ReservationStatus.assigned => l10n.reservationStatusAssigned,
        ReservationStatus.released => l10n.reservationStatusReleased,
        ReservationStatus.noShow => l10n.reservationStatusNoShow,
        ReservationStatus.completed => l10n.reservationStatusCompleted,
        ReservationStatus.cancelled => l10n.reservationStatusCancelled,
        ReservationStatus.unknown => l10n.reservationStatusReserved,
      };

  /// `2 يوم و3 ساعة`, `3 ساعة و20 دقيقة` or `mm:ss`.
  static String countdown(AppLocalizations l10n, Duration left) {
    final int totalMinutes = left.inMinutes;
    final int hours = totalMinutes ~/ _minutesPerHour;
    if (hours >= _hoursPerDay) {
      return l10n.countdownDaysHours(
        hours ~/ _hoursPerDay,
        hours % _hoursPerDay,
      );
    }
    if (hours >= 1) {
      return l10n.countdownHoursMinutes(hours, totalMinutes % _minutesPerHour);
    }
    return clock(left);
  }

  /// `mm:ss` of a short duration (confirmation deadlines).
  static String clock(Duration left) {
    final int seconds = left.inSeconds < 0 ? 0 : left.inSeconds;
    final String m = (seconds ~/ _secondsPerMinute).toString().padLeft(2, '0');
    final String s = (seconds % _secondsPerMinute).toString().padLeft(2, '0');
    return '$m:$s';
  }
}
