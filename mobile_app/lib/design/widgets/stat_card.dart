import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:flutter/material.dart';

/// Statistic card: icon in a `brand-soft` box, meta text, muted title and a
/// 30px bold value.
class StatCard extends StatelessWidget {
  const StatCard({
    super.key,
    required this.icon,
    required this.title,
    required this.value,
    this.meta,
  });

  final AtaIcons icon;
  final String title;
  final String value;
  final String? meta;

  @override
  Widget build(BuildContext context) {
    return AtaCard(
      padding: const EdgeInsets.all(AtaSpacing.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: <Widget>[
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: <Widget>[
              IconBox(icon: icon),
              if (meta != null)
                Flexible(
                  child: Text(
                    meta!,
                    style: AtaText.caption,
                    overflow: TextOverflow.ellipsis,
                  ),
                ),
            ],
          ),
          const SizedBox(height: AtaSpacing.lg),
          Text(title, style: AtaText.labelMuted),
          const SizedBox(height: AtaSpacing.xs),
          Text(value, style: AtaText.stat),
        ],
      ),
    );
  }
}
