import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_shadows.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:flutter/material.dart';

/// Large tappable card of the language and role screens. [dark] renders the
/// `ink` variant with white text.
class ChoiceCard extends StatelessWidget {
  const ChoiceCard({
    super.key,
    required this.leading,
    required this.title,
    required this.copy,
    required this.cta,
    required this.onTap,
    this.tag,
    this.dark = false,
    this.mirrorArrow = false,
  });

  final Widget leading;
  final String title;
  final String copy;
  final String cta;
  final VoidCallback onTap;
  final String? tag;
  final bool dark;
  final bool mirrorArrow;

  @override
  Widget build(BuildContext context) {
    final Color copyColor = dark ? AtaColors.white60 : AtaColors.muted;
    return Material(
      color: dark ? AtaColors.ink : AtaColors.white,
      borderRadius: AtaRadii.cardRadius,
      child: InkWell(
        onTap: onTap,
        borderRadius: AtaRadii.cardRadius,
        child: Container(
          padding: const EdgeInsets.all(AtaSpacing.xl + AtaSpacing.xxs),
          decoration: BoxDecoration(
            borderRadius: AtaRadii.cardRadius,
            boxShadow: dark ? AtaShadows.button : AtaShadows.soft,
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: <Widget>[
              Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: <Widget>[
                  leading,
                  if (tag != null)
                    AtaBadge(
                      label: tag!,
                      background: dark
                          ? AtaColors.white10
                          : AtaColors.brandSoft,
                    ),
                ],
              ),
              const SizedBox(height: AtaSpacing.xxxl),
              Text(
                title,
                style: AtaText.headline.copyWith(
                  color: dark ? AtaColors.white : AtaColors.ink,
                ),
              ),
              const SizedBox(height: AtaSpacing.sm),
              Text(copy, style: AtaText.body.copyWith(color: copyColor)),
              const SizedBox(height: AtaSpacing.xxl),
              Row(
                children: <Widget>[
                  Flexible(
                    child: Text(
                      cta,
                      style: AtaText.bodyStrong.copyWith(
                        color: AtaColors.brand,
                      ),
                    ),
                  ),
                  const SizedBox(width: AtaSpacing.xs),
                  AtaIcon(
                    AtaIcons.arrow,
                    color: AtaColors.brand,
                    mirrored: mirrorArrow,
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}
