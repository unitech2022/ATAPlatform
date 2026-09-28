import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/map_section.dart';
import 'package:ata_app/features/trip/presentation/cubit/active_trip_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/active_trip_state.dart';
import 'package:ata_app/features/trip/presentation/widgets/active_trip_sheet.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Passenger `/trip`: map with the live ETA and the per-status sheet.
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
