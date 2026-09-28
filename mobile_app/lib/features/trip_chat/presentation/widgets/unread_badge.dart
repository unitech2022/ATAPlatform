import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:flutter/material.dart';

/// Small red counter for unread chat messages.
class UnreadBadge extends StatelessWidget {
  const UnreadBadge({super.key, required this.count});

  final int count;

  static const int _max = 9;
  static const double _minSize = 18;

  @override
  Widget build(BuildContext context) {
    return Container(
      constraints: const BoxConstraints(
        minWidth: _minSize,
        minHeight: _minSize,
      ),
      padding: const EdgeInsets.symmetric(horizontal: AtaSpacing.xxs),
      alignment: Alignment.center,
      decoration: const BoxDecoration(
        color: AtaColors.danger,
        borderRadius: BorderRadius.all(Radius.circular(_minSize)),
      ),
      child: Text(
        count > _max ? '$_max+' : '$count',
        style: AtaText.caption.copyWith(color: AtaColors.white, height: 1.2),
      ),
    );
  }
}
