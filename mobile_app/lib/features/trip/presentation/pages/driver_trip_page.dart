import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/widgets/map_canvas.dart';
import 'package:ata_app/features/trip/presentation/cubit/driver_trip_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/driver_trip_state.dart';
import 'package:ata_app/features/trip/presentation/widgets/driver_trip_sheet.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Driver `/driver/trip`: map canvas plus the per-status action sheet.
class DriverTripPage extends StatelessWidget {
  const DriverTripPage({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        bottom: false,
        child: LayoutBuilder(
          builder: (BuildContext context, BoxConstraints constraints) {
            return Stack(
              children: <Widget>[
                Positioned.fill(
                  child: BlocSelector<DriverTripCubit, DriverTripState, int>(
                    selector: (DriverTripState state) =>
                        state.trip?.etaMinutes ?? 0,
                    builder: (BuildContext context, int eta) =>
                        MapCanvas(etaLabel: context.l10n.minutesLabel(eta)),
                  ),
                ),
                Align(
                  alignment: Alignment.bottomCenter,
                  child: ConstrainedBox(
                    constraints: BoxConstraints(
                      maxHeight:
                          constraints.maxHeight * AtaSizes.sheetMaxHeightFactor,
                    ),
                    child: const DriverTripSheet(),
                  ),
                ),
              ],
            );
          },
        ),
      ),
    );
  }
}
