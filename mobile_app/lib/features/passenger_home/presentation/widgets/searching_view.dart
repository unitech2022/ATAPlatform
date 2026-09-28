import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/features/passenger_home/domain/entities/ride_time.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_cubit.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_state.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// "Finding your captain" state with the request summary and cancel.
class SearchingView extends StatelessWidget {
  const SearchingView({super.key});

  static const double _pulseSize = 112;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocBuilder<HomeCubit, HomeState>(
      builder: (BuildContext context, HomeState state) {
        final String eta = l10n.minutesLabel(state.estimate.etaMinutes);
        final String price = l10n.priceWithCurrency(
          Money.compact(state.estimate.price),
        );
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
              l10n.searchingCopy(eta),
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
                          state.selectedCategory?.name ?? '',
                          style: AtaText.bodyStrong,
                        ),
                        Text(
                          _paymentCopy(l10n, state.payment),
                          style: AtaText.small,
                        ),
                      ],
                    ),
                  ),
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: <Widget>[
                      Text(price, style: AtaText.section),
                      if (state.stops.isNotEmpty)
                        Text(
                          l10n.extraStopsCount(state.stops.length),
                          style: AtaText.caption,
                        ),
                    ],
                  ),
                ],
              ),
            ),
            if (state.preferFemaleDriver) ...<Widget>[
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
                          style: AtaText.caption.copyWith(
                            color: AtaColors.brand,
                          ),
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
              onPressed: context.read<HomeCubit>().cancelRequest,
            ),
          ],
        );
      },
    );
  }

  String _paymentCopy(AppLocalizations l10n, PaymentOption option) =>
      switch (option) {
        PaymentOption.cash => l10n.cashOnArrival,
        PaymentOption.wallet => l10n.payWithWallet,
        PaymentOption.card => l10n.payWithCard,
      };
}
