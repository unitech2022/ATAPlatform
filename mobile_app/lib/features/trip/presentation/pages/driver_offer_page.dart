import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/decorative_background.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/features/trip/domain/entities/offer.dart';
import 'package:ata_app/features/trip/presentation/cubit/driver_offer_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/driver_offer_state.dart';
import 'package:ata_app/features/trip/presentation/cubit/driver_trip_cubit.dart';
import 'package:ata_app/features/trip/presentation/widgets/offer_stats.dart';
import 'package:ata_app/features/trip/presentation/widgets/route_summary.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Driver `/driver/offer`: full-screen dispatch offer with a 20 s countdown.
class DriverOfferPage extends StatelessWidget {
  const DriverOfferPage({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocListener<DriverOfferCubit, DriverOfferState>(
      listenWhen: (DriverOfferState p, DriverOfferState c) =>
          p.status != c.status && c.status == DriverOfferStatus.accepted,
      listener: (BuildContext context, DriverOfferState state) {
        final DriverOfferCubit cubit = context.read<DriverOfferCubit>();
        if (state.trip != null) {
          context.read<DriverTripCubit>().adopt(state.trip!);
        }
        cubit.acknowledge();
      },
      child: Scaffold(
        body: PageBackground(
          child: SafeArea(
            child: BlocBuilder<DriverOfferCubit, DriverOfferState>(
              builder: (BuildContext context, DriverOfferState state) {
                final Offer? offer = state.offer;
                if (offer == null) return const _NoOffer();
                return _OfferBody(offer: offer, state: state);
              },
            ),
          ),
        ),
      ),
    );
  }
}

class _OfferBody extends StatelessWidget {
  const _OfferBody({required this.offer, required this.state});

  final Offer offer;
  final DriverOfferState state;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final DriverOfferCubit cubit = context.read<DriverOfferCubit>();
    final bool pending = state.status == DriverOfferStatus.pending;
    return SingleChildScrollView(
      padding: const EdgeInsets.fromLTRB(
        AtaSpacing.gutter,
        AtaSpacing.xl,
        AtaSpacing.gutter,
        AtaSpacing.xxl,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: <Widget>[
              Text(l10n.offerEyebrow, style: AtaText.eyebrow),
              AtaBadge(
                label: l10n.offerSecondsLeft(state.secondsLeft),
                background: AtaColors.ink,
                foreground: AtaColors.white,
              ),
            ],
          ),
          const SizedBox(height: AtaSpacing.xs),
          Text(l10n.offerTitle, style: AtaText.title),
          const SizedBox(height: AtaSpacing.lg),
          AtaCard(
            padding: const EdgeInsets.all(AtaSpacing.md),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: <Widget>[
                RouteSummary(
                  pickup: offer.pickup,
                  dropoff: offer.dropoff,
                  stops: offer.stops,
                  background: AtaColors.white,
                ),
                const SizedBox(height: AtaSpacing.sm),
                OfferStats(offer: offer),
              ],
            ),
          ),
          const SizedBox(height: AtaSpacing.md),
          OfferEarnings(offer: offer),
          const SizedBox(height: AtaSpacing.md),
          Row(
            children: <Widget>[
              const IconBox.cloud(icon: AtaIcons.user, round: true),
              const SizedBox(width: AtaSpacing.sm),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: <Widget>[
                    Text(l10n.offerPassenger, style: AtaText.caption),
                    Text(offer.passengerFirstName, style: AtaText.bodyStrong),
                  ],
                ),
              ),
              if (offer.passengerRating != null)
                AtaBadge(
                  label: l10n.ratingValue(
                    Money.compact(offer.passengerRating!),
                  ),
                ),
            ],
          ),
          if (state.failure != null) ...<Widget>[
            const SizedBox(height: AtaSpacing.md),
            InlineError(message: failureText(state.failure!, l10n)),
          ],
          const SizedBox(height: AtaSpacing.xl),
          AtaButton(
            label: l10n.acceptOffer,
            variant: AtaButtonVariant.brand,
            icon: AtaIcons.check,
            loading: state.status == DriverOfferStatus.accepting,
            onPressed: pending ? cubit.accept : null,
          ),
          const SizedBox(height: AtaSpacing.sm),
          AtaButton(
            label: l10n.rejectOffer,
            variant: AtaButtonVariant.outline,
            loading: state.status == DriverOfferStatus.rejecting,
            onPressed: pending ? cubit.reject : null,
          ),
        ],
      ),
    );
  }
}

class _NoOffer extends StatelessWidget {
  const _NoOffer();

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(AtaSpacing.gutter),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: <Widget>[
            Text(l10n.offerExpiredTitle, style: AtaText.headline),
            const SizedBox(height: AtaSpacing.xs),
            Text(
              l10n.offerExpiredCopy,
              style: AtaText.bodyMuted,
              textAlign: TextAlign.center,
            ),
          ],
        ),
      ),
    );
  }
}
