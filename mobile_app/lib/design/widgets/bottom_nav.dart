import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_shadows.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:flutter/material.dart';

/// One destination of [BottomNav].
class BottomNavItem {
  const BottomNavItem({required this.icon, required this.label});

  final AtaIcons icon;
  final String label;
}

/// Floating dark navigation bar: `ink` background inside 16px margins,
/// 16px corners, active item on a `brand` tile.
class BottomNav extends StatelessWidget {
  const BottomNav({
    super.key,
    required this.items,
    required this.currentIndex,
    required this.onSelected,
  });

  final List<BottomNavItem> items;
  final int currentIndex;
  final ValueChanged<int> onSelected;

  static const double margin = AtaSpacing.md;
  static const double _padding = AtaSpacing.xs;
  static const double _minItemWidth = 64;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(margin),
      child: Container(
        padding: const EdgeInsets.all(_padding),
        decoration: BoxDecoration(
          color: AtaColors.ink,
          borderRadius: AtaRadii.itemRadius,
          boxShadow: AtaShadows.float,
        ),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceAround,
          children: <Widget>[
            for (int i = 0; i < items.length; i++)
              _NavTile(
                item: items[i],
                active: i == currentIndex,
                onTap: () => onSelected(i),
              ),
          ],
        ),
      ),
    );
  }
}

class _NavTile extends StatelessWidget {
  const _NavTile({
    required this.item,
    required this.active,
    required this.onTap,
  });

  final BottomNavItem item;
  final bool active;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final Color color = active ? AtaColors.white : AtaColors.white60;
    return Material(
      color: active ? AtaColors.brand : Colors.transparent,
      borderRadius: AtaRadii.smallRadius,
      child: InkWell(
        onTap: onTap,
        borderRadius: AtaRadii.smallRadius,
        child: Container(
          constraints: const BoxConstraints(minWidth: BottomNav._minItemWidth),
          padding: const EdgeInsets.symmetric(
            horizontal: AtaSpacing.sm,
            vertical: AtaSpacing.xs,
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: <Widget>[
              AtaIcon(item.icon, color: color),
              const SizedBox(height: AtaSpacing.xxs),
              Text(
                item.label,
                style: AtaText.captionStrong.copyWith(color: color),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
