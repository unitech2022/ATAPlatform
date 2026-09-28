import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:flutter/material.dart';

/// A tappable tile with a 2px border that becomes `brand` + `brand-soft`
/// when [selected]. Used for ride categories, payment methods, amounts and
/// language options.
class SelectableTile extends StatelessWidget {
  const SelectableTile({
    super.key,
    required this.selected,
    required this.onTap,
    required this.child,
    this.padding = const EdgeInsets.all(AtaSpacing.md),
    this.unselectedBackground = AtaColors.white,
    this.unselectedBorder = AtaColors.line,
    this.radius = AtaRadii.itemRadius,
    this.shadow,
  });

  final bool selected;
  final VoidCallback? onTap;
  final Widget child;
  final EdgeInsetsGeometry padding;
  final Color unselectedBackground;
  final Color unselectedBorder;
  final BorderRadius radius;
  final List<BoxShadow>? shadow;

  @override
  Widget build(BuildContext context) {
    return Semantics(
      selected: selected,
      button: true,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 150),
        decoration: BoxDecoration(
          color: selected ? AtaColors.brandSoft : unselectedBackground,
          borderRadius: radius,
          border: Border.all(
            color: selected ? AtaColors.brand : unselectedBorder,
            width: AtaSizes.borderThick,
          ),
          boxShadow: shadow,
        ),
        child: Material(
          type: MaterialType.transparency,
          child: InkWell(
            onTap: onTap,
            borderRadius: radius,
            child: Padding(padding: padding, child: child),
          ),
        ),
      ),
    );
  }
}

/// Radio indicator used at the end of payment-method tiles.
class RadioDot extends StatelessWidget {
  const RadioDot({super.key, required this.selected});

  final bool selected;

  static const double _size = 16;
  static const double _selectedBorder = 4;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: _size,
      height: _size,
      decoration: BoxDecoration(
        color: AtaColors.white,
        shape: BoxShape.circle,
        border: Border.all(
          color: selected ? AtaColors.brand : AtaColors.line,
          width: selected ? _selectedBorder : AtaSizes.borderThick,
        ),
      ),
    );
  }
}
