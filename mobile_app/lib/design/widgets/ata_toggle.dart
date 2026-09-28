import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_shadows.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:flutter/material.dart';

/// 48x28 toggle: `brand` when on, `line` when off, white 20px knob.
///
/// On dark surfaces use [AtaToggle.onDark] (translucent white track).
class AtaToggle extends StatelessWidget {
  const AtaToggle({
    super.key,
    required this.value,
    this.onChanged,
    this.onDark = false,
  });

  final bool value;
  final ValueChanged<bool>? onChanged;
  final bool onDark;

  static const double _padding = 4;
  static const Duration _duration = Duration(milliseconds: 180);

  @override
  Widget build(BuildContext context) {
    final Color track = value
        ? (onDark ? AtaColors.white25 : AtaColors.brand)
        : AtaColors.line;
    final Color knob = value || !onDark ? AtaColors.white : AtaColors.muted;
    return Semantics(
      toggled: value,
      child: GestureDetector(
        behavior: HitTestBehavior.opaque,
        onTap: onChanged == null ? null : () => onChanged!(!value),
        child: AnimatedContainer(
          duration: _duration,
          width: AtaSizes.toggleWidth,
          height: AtaSizes.toggleHeight,
          padding: const EdgeInsets.all(_padding),
          decoration: BoxDecoration(
            color: track,
            borderRadius: AtaRadii.pillRadius,
          ),
          child: AnimatedAlign(
            duration: _duration,
            alignment: value
                ? AlignmentDirectional.centerStart
                : AlignmentDirectional.centerEnd,
            child: Container(
              width: AtaSizes.toggleKnob,
              height: AtaSizes.toggleKnob,
              decoration: BoxDecoration(
                color: knob,
                shape: BoxShape.circle,
                boxShadow: AtaShadows.soft,
              ),
            ),
          ),
        ),
      ),
    );
  }
}
