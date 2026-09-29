import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/features/rating/domain/entities/rating_tag.dart';
import 'package:ata_app/features/rating/presentation/widgets/rating_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// Selectable tag chips; negative ratings use the danger palette.
class RatingTagChips extends StatelessWidget {
  const RatingTagChips({
    super.key,
    required this.tags,
    required this.selected,
    required this.negative,
    required this.onToggle,
  });

  final List<RatingTag> tags;
  final List<String> selected;
  final bool negative;
  final ValueChanged<String>? onToggle;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = AppLocalizations.of(context);
    final Color accent = negative ? AtaColors.danger : AtaColors.brand;
    final Color soft = negative ? AtaColors.dangerSoft : AtaColors.brandSoft;
    return Wrap(
      spacing: AtaSpacing.xs,
      runSpacing: AtaSpacing.xs,
      children: <Widget>[
        for (final RatingTag tag in tags)
          _Chip(
            key: ValueKey<String>('tag-${tag.code}'),
            label: RatingText.tag(l10n, tag),
            selected: selected.contains(tag.code),
            accent: accent,
            soft: soft,
            onTap: onToggle == null ? null : () => onToggle!(tag.code),
          ),
      ],
    );
  }
}

class _Chip extends StatelessWidget {
  const _Chip({
    super.key,
    required this.label,
    required this.selected,
    required this.accent,
    required this.soft,
    required this.onTap,
  });

  final String label;
  final bool selected;
  final Color accent;
  final Color soft;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    return Semantics(
      selected: selected,
      button: true,
      child: Material(
        color: selected ? soft : AtaColors.white,
        shape: RoundedRectangleBorder(
          borderRadius: AtaRadii.pillRadius,
          side: BorderSide(color: selected ? accent : AtaColors.line),
        ),
        child: InkWell(
          onTap: onTap,
          borderRadius: AtaRadii.pillRadius,
          child: Padding(
            padding: const EdgeInsets.symmetric(
              horizontal: AtaSpacing.sm,
              vertical: AtaSpacing.xs,
            ),
            child: Text(
              label,
              style: AtaText.label.copyWith(
                color: selected ? accent : AtaColors.ink,
              ),
            ),
          ),
        ),
      ),
    );
  }
}
