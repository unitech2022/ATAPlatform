import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:flutter/material.dart';

/// Large amount followed by a smaller currency suffix (`125.00 ر.س`).
/// Digits are always Latin; the row is rendered LTR-safe.
class MoneyText extends StatelessWidget {
  const MoneyText({
    super.key,
    required this.amount,
    required this.currency,
    required this.style,
    this.currencyStyle,
    this.alignment = MainAxisAlignment.start,
  });

  final String amount;
  final String currency;
  final TextStyle style;
  final TextStyle? currencyStyle;
  final MainAxisAlignment alignment;

  static const double _currencyScale = 0.5;

  @override
  Widget build(BuildContext context) {
    final TextStyle suffix =
        currencyStyle ??
        style.copyWith(fontSize: (style.fontSize ?? 16) * _currencyScale);
    return Row(
      mainAxisSize: MainAxisSize.min,
      mainAxisAlignment: alignment,
      crossAxisAlignment: CrossAxisAlignment.baseline,
      textBaseline: TextBaseline.alphabetic,
      children: <Widget>[
        Text(amount, style: style, textDirection: TextDirection.ltr),
        const SizedBox(width: AtaSpacing.xs),
        Text(currency, style: suffix),
      ],
    );
  }
}
