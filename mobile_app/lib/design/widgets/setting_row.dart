import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:flutter/material.dart';

/// Settings list row: 44px leading box, title, muted subtitle, trailing
/// widget (chevron by default) and a `line` divider below unless [last].
class SettingRow extends StatelessWidget {
  const SettingRow({
    super.key,
    required this.leading,
    required this.title,
    this.subtitle,
    this.onTap,
    this.trailing,
    this.showChevron = true,
    this.last = false,
    this.verticalPadding = AtaSpacing.md,
    this.titleStyle,
  });

  final Widget leading;
  final String title;
  final String? subtitle;
  final VoidCallback? onTap;
  final Widget? trailing;
  final bool showChevron;
  final bool last;
  final double verticalPadding;
  final TextStyle? titleStyle;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      child: Container(
        padding: EdgeInsets.symmetric(vertical: verticalPadding),
        decoration: BoxDecoration(
          border: last
              ? null
              : const Border(
                  bottom: BorderSide(
                    color: AtaColors.line,
                    width: AtaSizes.borderThin,
                  ),
                ),
        ),
        child: Row(
          children: <Widget>[
            leading,
            const SizedBox(width: AtaSpacing.md),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: <Widget>[
                  Text(title, style: titleStyle ?? AtaText.bodyStrong),
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
            if (showChevron) ...<Widget>[
              const SizedBox(width: AtaSpacing.sm),
              const AtaIcon(
                AtaIcons.chevron,
                size: AtaSizes.iconSmall,
                color: AtaColors.muted,
              ),
            ],
          ],
        ),
      ),
    );
  }
}
