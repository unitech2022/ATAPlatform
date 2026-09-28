import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/selectable_tile.dart';
import 'package:flutter/material.dart';

/// Single-choice chips (report / lost item categories).
class ChoiceWrap<T> extends StatelessWidget {
  const ChoiceWrap({
    super.key,
    required this.values,
    required this.selected,
    required this.label,
    required this.onSelected,
    this.error,
  });

  final List<T> values;
  final T? selected;
  final String Function(T value) label;
  final ValueChanged<T>? onSelected;
  final String? error;

  static const EdgeInsets _padding = EdgeInsets.symmetric(
    horizontal: AtaSpacing.md,
    vertical: AtaSpacing.xs,
  );

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: <Widget>[
        Wrap(
          spacing: AtaSpacing.xs,
          runSpacing: AtaSpacing.xs,
          children: <Widget>[
            for (final T value in values)
              SelectableTile(
                selected: value == selected,
                padding: _padding,
                onTap: onSelected == null ? null : () => onSelected!(value),
                child: Text(label(value), style: AtaText.label),
              ),
          ],
        ),
        if (error != null)
          Padding(
            padding: const EdgeInsets.only(top: AtaSpacing.xxs),
            child: Text(
              error!,
              style: AtaText.caption.copyWith(color: AtaColors.danger),
            ),
          ),
      ],
    );
  }
}
