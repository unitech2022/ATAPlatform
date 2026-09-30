import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/reservations_cubit.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/reservations_state.dart';
import 'package:ata_app/features/trip/presentation/cubit/driver_trip_cubit.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Listener body of [ReservationsCubit] events: announces the result in a
/// snackbar and, after the final confirmation, hands the now-assigned trip
/// to the driver's trip feed (the router then opens `/driver/trip`).
void announceReservationEvent(BuildContext context, ReservationsState state) {
  final AppLocalizations l10n = context.l10n;
  final String? message = switch (state.event) {
    ReservationEvent.confirmed => l10n.reservationConfirmed,
    ReservationEvent.finalConfirmed => l10n.reservationFinalConfirmed,
    ReservationEvent.released =>
      state.releasedPenaltyPoints > 0
          ? l10n.reservationReleasedPoints(state.releasedPenaltyPoints)
          : l10n.reservationReleased,
    null => null,
  };
  if (message != null) {
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(message)));
  }
  if (state.event == ReservationEvent.finalConfirmed &&
      state.assignedTrip != null) {
    context.read<DriverTripCubit>().adopt(state.assignedTrip!);
  }
  context.read<ReservationsCubit>().clearNotice();
}
