import 'package:ata_app/core/utils/phone_number.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_shadows.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:flutter/material.dart';

/// Read-only phone display: `+966` prefix box and the digits typed on the
/// keypad, always LTR, with the focused (brand) border.
class PhoneField extends StatelessWidget {
  const PhoneField({
    super.key,
    required this.digits,
    required this.countryCode,
    required this.placeholder,
  });

  final String digits;
  final String countryCode;
  final String placeholder;

  static const double _letterSpacing = 1.5;

  @override
  Widget build(BuildContext context) {
    return Directionality(
      textDirection: TextDirection.ltr,
      child: Container(
        height: AtaSizes.phoneFieldHeight,
        clipBehavior: Clip.antiAlias,
        decoration: BoxDecoration(
          color: AtaColors.white,
          borderRadius: AtaRadii.itemRadius,
          border: Border.all(
            color: AtaColors.brand,
            width: AtaSizes.borderThick,
          ),
          boxShadow: AtaShadows.brand,
        ),
        child: Row(
          children: <Widget>[
            Container(
              height: double.infinity,
              padding: const EdgeInsets.symmetric(horizontal: AtaSpacing.md),
              alignment: Alignment.center,
              decoration: const BoxDecoration(
                color: AtaColors.cloud,
                border: Border(right: BorderSide(color: AtaColors.line)),
              ),
              child: Text(countryCode, style: AtaText.bodyStrong),
            ),
            Expanded(
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: AtaSpacing.md),
                child: Text(
                  digits.isEmpty ? placeholder : PhoneNumber.grouped(digits),
                  style: AtaText.section.copyWith(
                    letterSpacing: _letterSpacing,
                    color: digits.isEmpty ? AtaColors.line : AtaColors.ink,
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
