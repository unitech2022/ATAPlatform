import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/features/trip/domain/entities/restriction_level.dart';
import 'package:ata_app/features/trip/presentation/widgets/cancellation_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

/// "You can't go online": the reliability restriction (F14) with its end
/// date and a link to the reliability details.
class RestrictionBlockCard extends StatelessWidget {
  const RestrictionBlockCard({super.key, required this.restriction});

  final AccountRestriction restriction;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final DateTime? until = restriction.restrictedUntil;
    return Container(
      padding: const EdgeInsets.all(AtaSpacing.md),
      decoration: const BoxDecoration(
        color: AtaColors.dangerSoft,
        borderRadius: AtaRadii.itemRadius,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Text(
            l10n.cantGoOnlineTitle,
            style: AtaText.bodyStrong.copyWith(color: AtaColors.danger),
          ),
          const SizedBox(height: AtaSpacing.xxs),
          Text(
            until == null
                ? l10n.accountRestrictedError
                : l10n.accountRestrictedUntil(
                    DateText.dayAndTime(until, context.localeCode),
                  ),
            style: AtaText.small.copyWith(color: AtaColors.danger),
          ),
          const SizedBox(height: AtaSpacing.xxs),
          Text(
            CancellationText.levelExplanation(
              l10n,
              restriction.level,
              driver: true,
            ),
            style: AtaText.caption,
          ),
          const SizedBox(height: AtaSpacing.sm),
          AtaButton(
            label: l10n.reliabilityDetails,
            variant: AtaButtonVariant.dangerOutline,
            height: AtaSizes.buttonCompact,
            onPressed: () => context.push(AppRoutes.driverReliability),
          ),
        ],
      ),
    );
  }
}
