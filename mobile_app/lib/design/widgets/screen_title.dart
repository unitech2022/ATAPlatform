import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:flutter/material.dart';

/// Eyebrow (brand) + large title + muted copy, used at the top of pages.
class ScreenTitle extends StatelessWidget {
  const ScreenTitle({
    super.key,
    required this.eyebrow,
    required this.title,
    this.copy,
    this.centered = false,
    this.titleStyle = AtaText.title,
  });

  final String eyebrow;
  final String title;
  final String? copy;
  final bool centered;
  final TextStyle titleStyle;

  @override
  Widget build(BuildContext context) {
    final TextAlign align = centered ? TextAlign.center : TextAlign.start;
    return Column(
      crossAxisAlignment: centered
          ? CrossAxisAlignment.center
          : CrossAxisAlignment.start,
      children: <Widget>[
        Text(eyebrow, style: AtaText.eyebrow, textAlign: align),
        const SizedBox(height: AtaSpacing.xs),
        Text(title, style: titleStyle, textAlign: align),
        if (copy != null) ...<Widget>[
          const SizedBox(height: AtaSpacing.sm),
          Text(copy!, style: AtaText.bodyMuted, textAlign: align),
        ],
      ],
    );
  }
}
