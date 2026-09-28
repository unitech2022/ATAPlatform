import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/ata_logo.dart';
import 'package:ata_app/design/widgets/dark_card.dart';
import 'package:ata_app/design/widgets/money_text.dart';
import 'package:flutter/material.dart';

/// Dark balance card with the logo, brand circle and masked number.
class BalanceCard extends StatelessWidget {
  const BalanceCard({super.key, required this.balance});

  final double balance;

  static const String _masked = '••••  2841';
  static const double _logoHeight = 36;
  static const double _logoWidth = 80;

  @override
  Widget build(BuildContext context) {
    return DarkCard(
      decorated: true,
      padding: const EdgeInsets.all(AtaSpacing.xl + AtaSpacing.xxs),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: <Widget>[
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: <Widget>[
              Container(
                padding: const EdgeInsets.symmetric(horizontal: AtaSpacing.xs),
                decoration: const BoxDecoration(
                  color: AtaColors.white,
                  borderRadius: AtaRadii.tinyRadius,
                ),
                child: const AtaLogo(height: _logoHeight, width: _logoWidth),
              ),
              const AtaIcon(
                AtaIcons.wallet,
                size: AtaSizes.iconMedium,
                color: AtaColors.brand,
              ),
            ],
          ),
          const SizedBox(height: AtaSpacing.xxxl),
          Text(
            context.l10n.currentBalance,
            style: AtaText.small.copyWith(color: AtaColors.white60),
          ),
          const SizedBox(height: AtaSpacing.xs),
          MoneyText(
            amount: Money.fixed(balance),
            currency: context.l10n.currency,
            style: AtaText.display.copyWith(color: AtaColors.white),
          ),
          const SizedBox(height: AtaSpacing.xxxl),
          Text(
            _masked,
            style: AtaText.small.copyWith(
              color: AtaColors.white70,
              letterSpacing: 2,
            ),
            textDirection: TextDirection.ltr,
          ),
        ],
      ),
    );
  }
}
