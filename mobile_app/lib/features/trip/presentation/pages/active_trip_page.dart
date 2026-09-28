import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/map_section.dart';
import 'package:ata_app/features/safety/presentation/widgets/trip_safety_overlay.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/presentation/cubit/active_trip_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/active_trip_state.dart';
import 'package:ata_app/features/trip/presentation/widgets/active_trip_sheet.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Passenger `/trip`: map with the live ETA, the per-status sheet and the
/// safety overlay (SOS once a driver is assigned, "are you OK?").
class ActiveTripPage extends StatelessWidget {
  const ActiveTripPage({super.key});

  @override
  Widget build(BuildContext context) {
    return LayoutBuilder(
      builder: (BuildContext context, BoxConstraints constraints) {
        return Stack(
          children: <Widget>[
            Positioned.fill(
              child: BlocSelector<ActiveTripCubit, ActiveTripState, int?>(
                selector: (ActiveTripState state) =>
                    state.etaMinutes ?? state.trip?.etaMinutes,
                builder: (BuildContext context, int? eta) => MapSection(
                  etaLabel: eta == null
                      ? context.l10n.etaUnknown
                      : context.l10n.minutesLabel(eta),
                ),
              ),
            ),
            Positioned(
              top: 0,
              left: 0,
              right: 0,
              child: BlocSelector<ActiveTripCubit, ActiveTripState, Trip?>(
                selector: (ActiveTripState state) => state.trip,
                builder: (BuildContext context, Trip? trip) =>
                    TripSafetyOverlay(
                      tripId: trip?.id,
                      role: 'passenger',
                      fallback: trip?.pickup.point,
                      showSos: trip?.status.hasDriver ?? false,
                    ),
              ),
            ),
            Align(
              alignment: Alignment.bottomCenter,
              child: ConstrainedBox(
                constraints: BoxConstraints(
                  maxHeight:
                      constraints.maxHeight * AtaSizes.sheetMaxHeightFactor,
                ),
                child: const ActiveTripSheet(),
              ),
            ),
          ],
        );
      },
    );
  }
}
