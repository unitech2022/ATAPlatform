import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/presentation/widgets/trip_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// "Finding your captain": pulse, request summary and cancel (prototype
/// `requested` state of the home sheet).
class TripSearchingView extends StatelessWidget {
  const TripSearchingView({
    super.key,
    required this.trip,
    required this.onCancel,
    this.cancelling = false,
  });

  final Trip trip;
  final VoidCallback? onCancel;
  final bool cancelling;

  static const double _pulseSize = 112;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        const SizedBox(height: AtaSpacing.lg),
        Center(
          child: Container(
            width: _pulseSize,
            height: _pulseSize,
            alignment: Alignment.center,
            decoration: const BoxDecoration(
              color: AtaColors.brandSoft,
              shape: BoxShape.circle,
            ),
            child: const AtaIcon(
              AtaIcons.car,
              size: AtaSizes.iconHero + AtaSizes.iconSmall,
              color: AtaColors.brand,
            ),
          ),
        ),
        const SizedBox(height: AtaSpacing.xl),
        Text(
          l10n.searchingTitle,
          style: AtaText.headline,
          textAlign: TextAlign.center,
        ),
        const SizedBox(height: AtaSpacing.xs),
        Text(
          l10n.searchingCopy(l10n.minutesLabel(trip.etaMinutes)),
          style: AtaText.bodyMuted,
          textAlign: TextAlign.center,
        ),
        const SizedBox(height: AtaSpacing.xl),
        Container(
          padding: const EdgeInsets.all(AtaSpacing.md),
          decoration: const BoxDecoration(
            color: AtaColors.cloud,
            borderRadius: AtaRadii.itemRadius,
          ),
          child: Row(
            children: <Widget>[
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: <Widget>[
                    Text(
                      trip.category?.name ??
                          l10n.tripNumberLabel(trip.tripNumber),
                      style: AtaText.bodyStrong,
                    ),
                    Text(
                      TripText.payment(l10n, trip.paymentMethod),
                      style: AtaText.small,
                    ),
                  ],
                ),
              ),
              Column(
                crossAxisAlignment: CrossAxisAlignment.end,
                children: <Widget>[
                  Text(TripText.price(l10n, trip.fare), style: AtaText.section),
                  if (trip.stops.isNotEmpty)
                    Text(
                      l10n.extraStopsCount(trip.stops.length),
                      style: AtaText.caption,
                    ),
                ],
              ),
            ],
          ),
        ),
        if (trip.preferFemaleDriver) ...<Widget>[
          const SizedBox(height: AtaSpacing.md),
          Container(
            padding: const EdgeInsets.all(AtaSpacing.md),
            decoration: const BoxDecoration(
              color: AtaColors.brandSoft,
              borderRadius: AtaRadii.itemRadius,
            ),
            child: Row(
              children: <Widget>[
                const IconBox.brand(
                  icon: AtaIcons.user,
                  size: AtaSizes.iconBoxSmall,
                  round: true,
                ),
                const SizedBox(width: AtaSpacing.sm),
                Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: <Widget>[
                    Text(
                      l10n.femaleRequestedTitle,
                      style: AtaText.bodyStrong.copyWith(
                        color: AtaColors.brand,
                      ),
                    ),
                    Text(
                      l10n.femaleRequestedCopy,
                      style: AtaText.caption.copyWith(color: AtaColors.brand),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ],
        const SizedBox(height: AtaSpacing.xl),
        AtaButton(
          label: l10n.cancelRequest,
          variant: AtaButtonVariant.outline,
          loading: cancelling,
          onPressed: onCancel,
        ),
      ],
    );
  }
}
