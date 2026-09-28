import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/presentation/widgets/trip_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// Completed-trip summary: fare, distance, duration, payment, rating CTA.
class TripReceiptView extends StatelessWidget {
  const TripReceiptView({super.key, required this.trip, required this.onDone});

  final Trip trip;
  final VoidCallback onDone;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        const Center(
          child: IconBox.brand(
            icon: AtaIcons.check,
            size: AtaSizes.iconBoxHero,
            iconSize: AtaSizes.iconHero,
            round: true,
          ),
        ),
        const SizedBox(height: AtaSpacing.md),
        Text(
          l10n.receiptTitle,
          style: AtaText.headline,
          textAlign: TextAlign.center,
        ),
        const SizedBox(height: AtaSpacing.xxs),
        Text(
          l10n.receiptCopy,
          style: AtaText.bodyMuted,
          textAlign: TextAlign.center,
        ),
        const SizedBox(height: AtaSpacing.lg),
        Container(
          padding: const EdgeInsets.all(AtaSpacing.md),
          decoration: const BoxDecoration(
            color: AtaColors.cloud,
            borderRadius: AtaRadii.itemRadius,
          ),
          child: Column(
            children: <Widget>[
              _ReceiptRow(
                label: l10n.receiptFare,
                value: TripText.price(l10n, trip.fare),
                strong: true,
              ),
              _ReceiptRow(
                label: l10n.receiptDistance,
                value: TripText.distance(l10n, trip.distanceMeters),
              ),
              _ReceiptRow(
                label: l10n.receiptDuration,
                value: TripText.duration(l10n, trip.durationSeconds),
              ),
              _ReceiptRow(
                label: l10n.receiptPayment,
                value: TripText.payment(l10n, trip.paymentMethod),
              ),
              _ReceiptRow(
                label: l10n.tripNumberLabel(''),
                value: trip.tripNumber,
              ),
            ],
          ),
        ),
        const SizedBox(height: AtaSpacing.lg),
        AtaButton(
          label: '${l10n.rateTrip} · ${l10n.comingSoon}',
          variant: AtaButtonVariant.brand,
          onPressed: null,
        ),
        const SizedBox(height: AtaSpacing.sm),
        AtaButton(label: l10n.done, onPressed: onDone),
      ],
    );
  }
}

class _ReceiptRow extends StatelessWidget {
  const _ReceiptRow({
    required this.label,
    required this.value,
    this.strong = false,
  });

  final String label;
  final String value;
  final bool strong;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: AtaSpacing.xxs),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: <Widget>[
          Text(label, style: AtaText.small),
          Text(
            value,
            textDirection: TextDirection.ltr,
            style: strong ? AtaText.section : AtaText.bodyStrong,
          ),
        ],
      ),
    );
  }
}
