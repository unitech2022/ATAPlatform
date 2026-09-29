import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:flutter/material.dart';

/// Five stars; filled up to [value]. Tappable when [onChanged] is set
/// (each star has the key `star-<n>`).
class StarRating extends StatelessWidget {
  const StarRating({
    super.key,
    required this.value,
    this.onChanged,
    this.size = AtaSizes.iconLarge,
    this.color = AtaColors.warning,
    this.semanticLabel,
  });

  static const int max = 5;

  final int value;
  final ValueChanged<int>? onChanged;
  final double size;
  final Color color;

  /// Accessibility label of the n-th star.
  final String Function(int stars)? semanticLabel;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: <Widget>[for (int n = 1; n <= max; n++) _star(n)],
    );
  }

  Widget _star(int n) {
    final Widget icon = AtaIcon(
      AtaIcons.star,
      size: size,
      color: n <= value ? color : AtaColors.line,
      filled: n <= value,
    );
    final ValueChanged<int>? onChanged = this.onChanged;
    if (onChanged == null) return icon;
    return Semantics(
      button: true,
      selected: n <= value,
      label: semanticLabel?.call(n),
      child: InkResponse(
        key: ValueKey<String>('star-$n'),
        onTap: () => onChanged(n),
        radius: size,
        child: Padding(
          padding: const EdgeInsets.all(AtaSpacing.xxs),
          child: icon,
        ),
      ),
    );
  }
}
