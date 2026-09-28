import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// The three onboarding steps; [activeIndex] is highlighted in brand.
class ApplicationStepsList extends StatelessWidget {
  const ApplicationStepsList({super.key, required this.activeIndex});

  final int activeIndex;

  static const double _numberSize = 32;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final List<(String, String)> steps = <(String, String)>[
      (l10n.step1Title, l10n.step1Copy),
      (l10n.step2Title, l10n.step2Copy),
      (l10n.step3Title, l10n.step3Copy),
    ];
    return Column(
      children: <Widget>[
        for (int i = 0; i < steps.length; i++) ...<Widget>[
          if (i > 0) const SizedBox(height: AtaSpacing.sm),
          _StepTile(
            number: '${i + 1}',
            title: steps[i].$1,
            copy: steps[i].$2,
            active: i == activeIndex,
          ),
        ],
      ],
    );
  }
}

class _StepTile extends StatelessWidget {
  const _StepTile({
    required this.number,
    required this.title,
    required this.copy,
    required this.active,
  });

  final String number;
  final String title;
  final String copy;
  final bool active;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(AtaSpacing.md),
      decoration: BoxDecoration(
        color: active ? AtaColors.brandSoft : AtaColors.white,
        borderRadius: AtaRadii.itemRadius,
        border: Border.all(color: active ? AtaColors.brand : AtaColors.line),
      ),
      child: Row(
        children: <Widget>[
          Container(
            width: ApplicationStepsList._numberSize,
            height: ApplicationStepsList._numberSize,
            alignment: Alignment.center,
            decoration: BoxDecoration(
              color: active ? AtaColors.brand : AtaColors.cloud,
              shape: BoxShape.circle,
            ),
            child: Text(
              number,
              style: AtaText.label.copyWith(
                color: active ? AtaColors.white : AtaColors.muted,
              ),
            ),
          ),
          const SizedBox(width: AtaSpacing.md),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: <Widget>[
                Text(title, style: AtaText.bodyStrong),
                Text(copy, style: AtaText.caption),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
