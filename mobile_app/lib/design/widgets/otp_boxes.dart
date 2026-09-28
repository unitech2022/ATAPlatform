import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:flutter/material.dart';

/// Four 56x56 OTP boxes with a 2px border; filled boxes use `brand` border
/// and `brand-soft` background. Always laid out LTR.
class OtpBoxes extends StatelessWidget {
  const OtpBoxes({super.key, required this.code, this.length = defaultLength});

  final String code;
  final int length;

  static const int defaultLength = 4;

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.ltr,
      child: Row(
        mainAxisAlignment: MainAxisAlignment.center,
        children: <Widget>[
          for (int i = 0; i < length; i++) ...<Widget>[
            if (i > 0) const SizedBox(width: AtaSpacing.sm),
            _Box(digit: i < code.length ? code[i] : null),
          ],
        ],
      ),
    );
  }
}

class _Box extends StatelessWidget {
  const _Box({required this.digit});

  final String? digit;

  @override
  Widget build(BuildContext context) {
    final bool filled = digit != null;
    return AnimatedContainer(
      duration: const Duration(milliseconds: 120),
      width: AtaSizes.otpBox,
      height: AtaSizes.otpBox,
      alignment: Alignment.center,
      decoration: BoxDecoration(
        color: filled ? AtaColors.brandSoft : AtaColors.white,
        borderRadius: AtaRadii.itemRadius,
        border: Border.all(
          color: filled ? AtaColors.brand : AtaColors.line,
          width: AtaSizes.borderThick,
        ),
      ),
      child: Text(digit ?? '', style: AtaText.keypad),
    );
  }
}
