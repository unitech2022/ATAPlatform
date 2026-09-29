import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:flutter/material.dart';

/// Shown when the reliability multiplier (`docs/09`) reduces rewards.
class ReducedRewardNotice extends StatelessWidget {
  const ReducedRewardNotice({super.key, required this.multiplier});

  final double multiplier;

  @override
  Widget build(BuildContext context) {
    return Container(
      key: const ValueKey<String>('reduced-reward-notice'),
      padding: const EdgeInsets.all(AtaSpacing.md),
      decoration: const BoxDecoration(
        color: AtaColors.warningSoft,
        borderRadius: AtaRadii.itemRadius,
      ),
      child: Text(
        context.l10n.incentiveReducedNotice(Money.compact(multiplier)),
        style: AtaText.label.copyWith(color: AtaColors.warning),
      ),
    );
  }
}
