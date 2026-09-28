import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/design/widgets/selectable_tile.dart';
import 'package:flutter/material.dart';

/// A payment method row with a radio indicator.
class PaymentMethodTile extends StatelessWidget {
  const PaymentMethodTile({
    super.key,
    required this.title,
    required this.subtitle,
    required this.selected,
    this.onTap,
  });

  final String title;
  final String subtitle;
  final bool selected;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    return SelectableTile(
      selected: selected,
      onTap: onTap,
      child: Row(
        children: <Widget>[
          selected
              ? const IconBox(
                  icon: AtaIcons.wallet,
                  background: AtaColors.white,
                  foreground: AtaColors.brand,
                )
              : const IconBox.cloud(icon: AtaIcons.wallet),
          const SizedBox(width: AtaSpacing.md),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: <Widget>[
                Text(title, style: AtaText.bodyStrong),
                Text(subtitle, style: AtaText.caption),
              ],
            ),
          ),
          RadioDot(selected: selected),
        ],
      ),
    );
  }
}
