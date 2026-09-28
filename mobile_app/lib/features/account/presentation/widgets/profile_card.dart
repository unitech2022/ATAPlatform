import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/features/account/domain/entities/profile.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// Avatar, name, member-since and the rating box.
class ProfileCard extends StatelessWidget {
  const ProfileCard({super.key, required this.name, this.passenger});

  final String name;
  final PassengerProfile? passenger;

  static const double _avatarIcon = 44;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final String year = passenger?.memberSince ?? '';
    return AtaCard(
      child: Column(
        children: <Widget>[
          const IconBox(
            icon: AtaIcons.user,
            size: AtaSizes.avatar,
            iconSize: _avatarIcon,
            round: true,
          ),
          const SizedBox(height: AtaSpacing.md),
          Text(name, style: AtaText.headline),
          if (year.isNotEmpty) ...<Widget>[
            const SizedBox(height: AtaSpacing.xxs),
            Text(l10n.memberSince(year), style: AtaText.small),
          ],
          const SizedBox(height: AtaSpacing.lg),
          Container(
            width: double.infinity,
            padding: const EdgeInsets.all(AtaSpacing.md),
            decoration: const BoxDecoration(
              color: AtaColors.cloud,
              borderRadius: AtaRadii.itemRadius,
            ),
            child: Column(
              children: <Widget>[
                Text(
                  Money.compact(passenger?.ratingAvg ?? 5),
                  style: AtaText.headline.copyWith(color: AtaColors.brand),
                ),
                Text(l10n.passengerRating, style: AtaText.caption),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
