import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:flutter/material.dart';

/// Numeric keypad: 3 columns, `cloud` 52px keys, `brand-soft` when pressed,
/// an empty cell, `0`, and a text "delete" key. Always laid out LTR.
class Keypad extends StatelessWidget {
  const Keypad({
    super.key,
    required this.onDigit,
    required this.onDelete,
    required this.deleteLabel,
    this.enabled = true,
  });

  final ValueChanged<String> onDigit;
  final VoidCallback onDelete;
  final String deleteLabel;
  final bool enabled;

  static const List<String> _digits = <String>[
    '1',
    '2',
    '3',
    '4',
    '5',
    '6',
    '7',
    '8',
    '9',
  ];
  static const int _columns = 3;
  static const double _maxWidth = 320;

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.ltr,
      child: Center(
        child: ConstrainedBox(
          constraints: const BoxConstraints(maxWidth: _maxWidth),
          child: GridView.count(
            crossAxisCount: _columns,
            shrinkWrap: true,
            physics: const NeverScrollableScrollPhysics(),
            mainAxisSpacing: AtaSpacing.sm,
            crossAxisSpacing: AtaSpacing.sm,
            childAspectRatio: (_maxWidth / _columns) / AtaSizes.keypadKey,
            children: <Widget>[
              for (final String digit in _digits)
                _Key(
                  label: digit,
                  onTap: enabled ? () => onDigit(digit) : null,
                ),
              const SizedBox.shrink(),
              _Key(label: '0', onTap: enabled ? () => onDigit('0') : null),
              _Key(
                label: deleteLabel,
                onTap: enabled ? onDelete : null,
                filled: false,
                semanticsLabel: 'keypad_delete',
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _Key extends StatelessWidget {
  const _Key({
    required this.label,
    required this.onTap,
    this.filled = true,
    this.semanticsLabel,
  });

  final String label;
  final VoidCallback? onTap;
  final bool filled;
  final String? semanticsLabel;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: filled ? AtaColors.cloud : Colors.transparent,
      borderRadius: AtaRadii.itemRadius,
      child: InkWell(
        onTap: onTap,
        borderRadius: AtaRadii.itemRadius,
        splashColor: AtaColors.brandSoft,
        highlightColor: filled ? AtaColors.brandSoft : AtaColors.cloud,
        child: Semantics(
          label: semanticsLabel,
          child: Center(
            child: Text(
              label,
              style: filled ? AtaText.keypad : AtaText.labelMuted,
            ),
          ),
        ),
      ),
    );
  }
}
