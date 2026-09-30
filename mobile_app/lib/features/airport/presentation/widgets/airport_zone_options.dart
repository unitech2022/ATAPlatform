import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/selectable_tile.dart';
import 'package:ata_app/features/airport/domain/entities/airport.dart';
import 'package:ata_app/features/airport/domain/entities/airport_selection.dart';
import 'package:ata_app/features/airport/presentation/cubit/airport_pickup_cubit.dart';
import 'package:ata_app/features/airport/presentation/cubit/airport_pickup_state.dart';
import 'package:ata_app/features/airport/presentation/widgets/airport_text.dart';
import 'package:ata_app/features/trip/domain/entities/trip_airport.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Body of the airport sheet once an airport is chosen: direction switch,
/// pickup zones grouped by terminal (or the dropoff terminal), the zone
/// instructions, the waiting policy and the flight number field.
class AirportZoneOptions extends StatelessWidget {
  const AirportZoneOptions({
    super.key,
    required this.selection,
    required this.state,
  });

  final AirportSelection selection;
  final AirportPickupState state;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final AirportPickupCubit cubit = context.read<AirportPickupCubit>();
    final Airport airport = selection.airport;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        Text('${airport.name} (${airport.code})', style: AtaText.bodyStrong),
        const SizedBox(height: AtaSpacing.xs),
        Row(
          children: <Widget>[
            for (final AirportDirection d
                in AirportDirection.values) ...<Widget>[
              Expanded(
                child: SelectableTile(
                  key: ValueKey<String>('direction-${d.apiValue}'),
                  selected: selection.direction == d,
                  onTap: () => cubit.setDirection(d),
                  padding: const EdgeInsets.symmetric(vertical: AtaSpacing.xs),
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
        const SizedBox(height: AtaSpacing.md),
        if (selection.isPickup)
          _PickupZones(selection: selection, cubit: cubit)
        else
          _Terminals(selection: selection, cubit: cubit),
        const SizedBox(height: AtaSpacing.md),
        _WaitingPolicy(selection: selection),
        const SizedBox(height: AtaSpacing.md),
        Text(l10n.airportFlightLabel, style: AtaText.label),
        const SizedBox(height: AtaSpacing.xs),
        TextFormField(
          key: const ValueKey<String>('airport-flight'),
          initialValue: state.flightInput,
          onChanged: cubit.setFlightNumber,
          textCapitalization: TextCapitalization.characters,
          textDirection: TextDirection.ltr,
          maxLength: _flightMaxLength,
          decoration: InputDecoration(
            hintText: l10n.airportFlightHint,
            counterText: '',
            errorText: state.flightInvalid ? l10n.airportFlightInvalid : null,
          ),
        ),
      ],
    );
  }

  static const int _flightMaxLength = 12;
}

class _PickupZones extends StatelessWidget {
  const _PickupZones({required this.selection, required this.cubit});

  final AirportSelection selection;
  final AirportPickupCubit cubit;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final List<AirportZone> zones = selection.airport.pickupZones;
    final List<String> terminals = <String>{
      for (final AirportZone z in zones) z.terminalCode ?? '',
    }.toList();
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        Text(l10n.airportChooseZone, style: AtaText.label),
        const SizedBox(height: AtaSpacing.xs),
        for (final String terminal in terminals) ...<Widget>[
          if (terminal.isNotEmpty)
            Padding(
              padding: const EdgeInsets.only(bottom: AtaSpacing.xxs),
              child: Text(
                l10n.airportTerminalLabel(terminal),
                style: AtaText.captionStrong.copyWith(color: AtaColors.muted),
              ),
            ),
          for (final AirportZone zone in zones.where(
            (AirportZone z) => (z.terminalCode ?? '') == terminal,
          )) ...<Widget>[
            SelectableTile(
              key: ValueKey<String>('zone-${zone.id}'),
              selected: selection.zone?.id == zone.id,
              onTap: () => cubit.selectZone(zone),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: <Widget>[
                  Text(zone.name, style: AtaText.bodyStrong),
                  if (zone.instructions != null &&
                      selection.zone?.id == zone.id)
                    Text(zone.instructions!, style: AtaText.small),
                ],
              ),
            ),
            const SizedBox(height: AtaSpacing.xs),
          ],
        ],
      ],
    );
  }
}

class _Terminals extends StatelessWidget {
  const _Terminals({required this.selection, required this.cubit});

  final AirportSelection selection;
  final AirportPickupCubit cubit;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        Text(l10n.airportChooseTerminal, style: AtaText.label),
        const SizedBox(height: AtaSpacing.xs),
        Wrap(
          spacing: AtaSpacing.xs,
          runSpacing: AtaSpacing.xs,
          children: <Widget>[
            for (final AirportZone terminal in selection.airport.terminals)
              SelectableTile(
                key: ValueKey<String>('terminal-${terminal.terminalCode}'),
                selected: selection.terminalCode == terminal.terminalCode,
                onTap: () => cubit.selectTerminal(
                  selection.terminalCode == terminal.terminalCode
                      ? null
                      : terminal.terminalCode,
                ),
                padding: const EdgeInsets.symmetric(
                  horizontal: AtaSpacing.md,
                  vertical: AtaSpacing.xs,
                ),
                child: Text(
                  terminal.name.isEmpty
                      ? l10n.airportTerminalLabel(terminal.terminalCode ?? '')
                      : terminal.name,
                  style: AtaText.label,
                ),
              ),
          ],
        ),
      ],
    );
  }
}

class _WaitingPolicy extends StatelessWidget {
  const _WaitingPolicy({required this.selection});

  final AirportSelection selection;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final int minutes =
        selection.freeWaitingMinutes ?? AirportZone.defaultFreeWaitingMinutes;
    return Container(
      key: const ValueKey<String>('airport-waiting-policy'),
      padding: const EdgeInsets.all(AtaSpacing.md),
      decoration: const BoxDecoration(
        color: AtaColors.cloud,
        borderRadius: AtaRadii.itemRadius,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: <Widget>[
          Text(l10n.airportWaitingPolicyTitle, style: AtaText.label),
          const SizedBox(height: AtaSpacing.xxs),
          Text(l10n.airportWaitingPolicyCopy(minutes), style: AtaText.small),
        ],
      ),
    );
  }
}
