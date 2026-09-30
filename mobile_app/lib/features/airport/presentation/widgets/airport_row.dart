import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/features/airport/domain/entities/airport_selection.dart';
import 'package:ata_app/features/airport/presentation/cubit/airport_pickup_cubit.dart';
import 'package:ata_app/features/airport/presentation/cubit/airport_pickup_state.dart';
import 'package:ata_app/features/airport/presentation/widgets/airport_text.dart';
import 'package:ata_app/features/airport/presentation/widgets/airport_zone_sheet.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// "رحلة من أو إلى مطار؟" row of the request sheet: opens the airport sheet
/// and summarises the choice (airport, direction, pickup zone / terminal,
/// flight number) or what is still missing.
class AirportRow extends StatelessWidget {
  const AirportRow({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocBuilder<AirportPickupCubit, AirportPickupState>(
      builder: (BuildContext context, AirportPickupState state) {
        final AirportSelection? selection = state.selection;
        final bool missing = state.needsZone;
        return Material(
          type: MaterialType.transparency,
          child: InkWell(
            key: const ValueKey<String>('airport-row'),
            borderRadius: AtaRadii.smallRadius,
            onTap: () => AirportZoneSheet.show(context),
            child: Container(
              padding: const EdgeInsets.symmetric(
                horizontal: AtaSpacing.md,
                vertical: AtaSpacing.sm,
              ),
              decoration: BoxDecoration(
                color: selection != null && !missing
                    ? AtaColors.brandSoft
                    : null,
                borderRadius: AtaRadii.smallRadius,
                border: Border.all(
                  color: missing
                      ? AtaColors.danger
                      : selection != null
                      ? AtaColors.brand
                      : AtaColors.line,
                ),
              ),
              child: Row(
                children: <Widget>[
                  const AtaIcon(AtaIcons.location, color: AtaColors.brand),
                  const SizedBox(width: AtaSpacing.sm),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: <Widget>[
                        Text(
                          selection == null
                              ? l10n.airportRowTitle
                              : AirportText.title(l10n, selection),
                          style: AtaText.label,
                        ),
                        Text(
                          selection == null
                              ? l10n.airportRowCopy
                              : missing
                              ? l10n.airportPickupZoneRequiredError
                              : AirportText.detail(l10n, selection),
                          style: AtaText.caption.copyWith(
                            color: missing ? AtaColors.danger : null,
                          ),
                        ),
                      ],
                    ),
                  ),
                  if (selection != null)
                    TextButton(
                      key: const ValueKey<String>('airport-remove'),
                      onPressed: context.read<AirportPickupCubit>().clear,
                      child: Text(
                        l10n.airportRemove,
                        style: AtaText.label.copyWith(color: AtaColors.danger),
                      ),
                    )
                  else
                    const AtaIcon(
                      AtaIcons.chevron,
                      size: AtaSizes.iconSmall,
                      color: AtaColors.muted,
                    ),
                ],
              ),
            ),
          ),
        );
      },
    );
  }
}
