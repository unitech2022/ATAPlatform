import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/widgets/failure_view.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/design/widgets/selectable_tile.dart';
import 'package:ata_app/design/widgets/sheet_handle.dart';
import 'package:ata_app/features/airport/domain/entities/airport.dart';
import 'package:ata_app/features/airport/domain/entities/airport_selection.dart';
import 'package:ata_app/features/airport/presentation/cubit/airport_pickup_cubit.dart';
import 'package:ata_app/features/airport/presentation/cubit/airport_pickup_state.dart';
import 'package:ata_app/features/airport/presentation/widgets/airport_text.dart';
import 'package:ata_app/features/airport/presentation/widgets/airport_zone_options.dart';
import 'package:ata_app/features/trip/domain/entities/trip_airport.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Airport picker of the request sheet: airport and direction, pickup zone
/// (with its instructions) or optional dropoff terminal, the waiting policy
/// and the optional flight number.
class AirportZoneSheet extends StatelessWidget {
  const AirportZoneSheet({super.key});

  static Future<void> show(BuildContext context) {
    final AirportPickupCubit cubit = context.read<AirportPickupCubit>();
    if (cubit.state.status == AirportCatalogStatus.idle) cubit.loadAirports();
    return showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      builder: (_) => BlocProvider<AirportPickupCubit>.value(
        value: cubit,
        child: const AirportZoneSheet(),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocBuilder<AirportPickupCubit, AirportPickupState>(
      builder: (BuildContext context, AirportPickupState state) {
        final AirportPickupCubit cubit = context.read<AirportPickupCubit>();
        final AirportSelection? selection = state.selection;
        return SingleChildScrollView(
          padding: EdgeInsets.fromLTRB(
            AtaSpacing.lg,
            AtaSpacing.lg,
            AtaSpacing.lg,
            AtaSpacing.lg +
                MediaQuery.viewInsetsOf(context).bottom +
                MediaQuery.paddingOf(context).bottom,
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: <Widget>[
              const SheetHandle(),
              Text(l10n.airportSheetTitle, style: AtaText.section),
              const SizedBox(height: AtaSpacing.md),
              if (state.status == AirportCatalogStatus.loading)
                const CenteredLoader()
              else if (state.status == AirportCatalogStatus.failure &&
                  selection == null)
                FailureView(
                  failure: state.failure!,
                  onRetry: cubit.loadAirports,
                )
              else if (selection == null)
                _AirportList(state: state, cubit: cubit)
              else
                AirportZoneOptions(selection: selection, state: state),
              const SizedBox(height: AtaSpacing.md),
              AtaButton(
                key: const ValueKey<String>('airport-done'),
                label: l10n.airportDone,
                height: AtaSizes.buttonCompact,
                onPressed: () => Navigator.of(context).pop(),
              ),
            ],
          ),
        );
      },
    );
  }
}

class _AirportList extends StatelessWidget {
  const _AirportList({required this.state, required this.cubit});

  final AirportPickupState state;
  final AirportPickupCubit cubit;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    if (state.airports.isEmpty) {
      return InlineError(message: l10n.airportNoAirports);
    }
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        Text(l10n.airportChoose, style: AtaText.small),
        const SizedBox(height: AtaSpacing.sm),
        for (final Airport airport in state.airports) ...<Widget>[
          Container(
            padding: const EdgeInsets.all(AtaSpacing.md),
            decoration: BoxDecoration(
              border: Border.all(color: AtaColors.line),
              borderRadius: AtaRadii.itemRadius,
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: <Widget>[
                Text(
                  '${airport.name} (${airport.code})',
                  style: AtaText.bodyStrong,
                ),
                const SizedBox(height: AtaSpacing.xs),
                Row(
                  children: <Widget>[
                    for (final AirportDirection d
                        in AirportDirection.values) ...<Widget>[
                      Expanded(
                        child: SelectableTile(
                          key: ValueKey<String>(
                            'airport-${airport.code}-${d.apiValue}',
                          ),
                          selected: false,
                          onTap: () => cubit.choose(airport, d),
                          padding: const EdgeInsets.symmetric(
                            vertical: AtaSpacing.xs,
                          ),
                          child: Text(
                            AirportText.direction(l10n, d),
                            textAlign: TextAlign.center,
                            style: AtaText.label,
                          ),
                        ),
                      ),
                      if (d != AirportDirection.values.last)
                        const SizedBox(width: AtaSpacing.xs),
                    ],
                  ],
                ),
              ],
            ),
          ),
          const SizedBox(height: AtaSpacing.sm),
        ],
      ],
    );
  }
}
