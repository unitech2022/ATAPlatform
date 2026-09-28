import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_shadows.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:flutter/material.dart';

/// 80px white header with a `line` bottom border and the soft shadow.
class AtaHeader extends StatelessWidget {
  const AtaHeader({super.key, required this.leading, required this.trailing});

  final List<Widget> leading;
  final List<Widget> trailing;

  @override
  Widget build(BuildContext context) {
    return Container(
      height: AtaSizes.header,
      padding: const EdgeInsets.symmetric(horizontal: AtaSpacing.gutter),
      decoration: BoxDecoration(
        color: AtaColors.white,
        border: const Border(bottom: BorderSide(color: AtaColors.line)),
        boxShadow: AtaShadows.soft,
      ),
      child: Row(
        children: <Widget>[
          ..._spaced(leading),
          const Spacer(),
          ..._spaced(trailing),
        ],
      ),
    );
  }

  List<Widget> _spaced(List<Widget> widgets) => <Widget>[
    for (int i = 0; i < widgets.length; i++) ...<Widget>[
      if (i > 0) const SizedBox(width: AtaSpacing.sm),
      widgets[i],
    ],
  ];
}

/// Round 44px header action (bell, menu, clock) with an optional badge.
class HeaderIconButton extends StatelessWidget {
  const HeaderIconButton({
    super.key,
    required this.child,
    required this.onTap,
    this.background = AtaColors.cloud,
    this.badge,
  });

  final Widget child;
  final VoidCallback onTap;
  final Color background;
  final Widget? badge;

  static const double _badgeOffset = 4;

  @override
  Widget build(BuildContext context) {
    return Stack(
      clipBehavior: Clip.none,
      children: <Widget>[
        Material(
          color: background,
          shape: const CircleBorder(),
          child: InkWell(
            onTap: onTap,
            customBorder: const CircleBorder(),
            child: SizedBox(
              width: AtaSizes.iconBox,
              height: AtaSizes.iconBox,
              child: Center(child: child),
            ),
          ),
        ),
        if (badge != null)
          PositionedDirectional(
            end: _badgeOffset,
            top: _badgeOffset,
            child: badge!,
          ),
      ],
    );
  }
}
