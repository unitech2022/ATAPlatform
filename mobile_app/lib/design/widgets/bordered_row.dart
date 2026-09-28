import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:flutter/material.dart';

/// Rounded row with a `line` border: leading box, title/subtitle, trailing.
/// Used for trips, notifications, payment rows and document lists.
class BorderedRow extends StatelessWidget {
  const BorderedRow({
    super.key,
    required this.leading,
    required this.title,
    this.subtitle,
    this.trailing,
    this.titleTrailing,
    this.onTap,
    this.background = AtaColors.white,
    this.borderColor = AtaColors.line,
    this.padding = const EdgeInsets.all(AtaSpacing.md),
  });

  final Widget leading;
  final String title;
  final String? subtitle;
  final Widget? trailing;
  final Widget? titleTrailing;
  final VoidCallback? onTap;
  final Color background;
  final Color borderColor;
  final EdgeInsetsGeometry padding;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: background,
      borderRadius: AtaRadii.itemRadius,
      child: InkWell(
        onTap: onTap,
        borderRadius: AtaRadii.itemRadius,
        child: Container(
          padding: padding,
          decoration: BoxDecoration(
            borderRadius: AtaRadii.itemRadius,
            border: Border.all(color: borderColor),
          ),
          child: Row(
            children: <Widget>[
              leading,
              const SizedBox(width: AtaSpacing.md),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: <Widget>[
                    Row(
                      children: <Widget>[
                        Flexible(
                          child: Text(
                            title,
                            style: AtaText.bodyStrong,
                            overflow: TextOverflow.ellipsis,
                          ),
                        ),
                        if (titleTrailing != null) ...<Widget>[
                          const SizedBox(width: AtaSpacing.xs),
                          titleTrailing!,
                        ],
                      ],
                    ),
                    if (subtitle != null) ...<Widget>[
                      const SizedBox(height: AtaSpacing.xxs),
                      Text(subtitle!, style: AtaText.caption),
                    ],
                  ],
                ),
              ),
              if (trailing != null) ...<Widget>[
                const SizedBox(width: AtaSpacing.sm),
                trailing!,
              ],
            ],
          ),
        ),
      ),
    );
  }
}
