import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/features/driver_onboarding/domain/entities/driver_application.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// Make / model / color / year / plate.
class VehicleCard extends StatelessWidget {
  const VehicleCard({super.key, required this.vehicle});

  final Vehicle? vehicle;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final Vehicle? v = vehicle;
    return AtaCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: <Widget>[
          const IconBox(
            icon: AtaIcons.car,
            size: AtaSizes.iconBoxLarge,
            iconSize: AtaSizes.iconLarge,
            radius: AtaSpacing.md,
          ),
          const SizedBox(height: AtaSpacing.xl),
          if (v == null)
            Text(l10n.noVehicle, style: AtaText.bodyMuted)
          else ...<Widget>[
            Text('${v.make} ${v.model}', style: AtaText.headline),
            const SizedBox(height: AtaSpacing.xxs),
            Text(l10n.vehicleMeta(v.color, '${v.year}'), style: AtaText.small),
            const SizedBox(height: AtaSpacing.xl),
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(AtaSpacing.md),
              decoration: const BoxDecoration(
                color: AtaColors.cloud,
                borderRadius: AtaRadii.itemRadius,
              ),
              child: Column(
                children: <Widget>[
                  Text(l10n.plateNumber, style: AtaText.caption),
                  const SizedBox(height: AtaSpacing.xs),
                  Text(
                    v.plateNumber,
                    style: AtaText.headline.copyWith(letterSpacing: 4),
                  ),
                ],
              ),
            ),
          ],
          const SizedBox(height: AtaSpacing.xl),
          AtaButton(
            label: l10n.updateVehicle,
            variant: AtaButtonVariant.outline,
            height: AtaSizes.buttonCompact,
            onPressed: null,
          ),
        ],
      ),
    );
  }
}
