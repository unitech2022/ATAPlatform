import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/core/widgets/failure_view.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/design/widgets/selectable_tile.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/marketplace_trip.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/marketplace_cubit.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/marketplace_state.dart';
import 'package:ata_app/features/scheduled_rides/presentation/widgets/marketplace_tile.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// The "السوق" tab: a day filter and the upcoming scheduled requests.
class MarketplaceView extends StatelessWidget {
  const MarketplaceView({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<MarketplaceCubit, MarketplaceState>(
      builder: (BuildContext context, MarketplaceState state) {
        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: <Widget>[
            _DayFilter(state: state),
            const SizedBox(height: AtaSpacing.md),
            if (state.actionFailure != null) ...<Widget>[
              InlineError(
                message: failureText(state.actionFailure!, context.l10n),
              ),
              const SizedBox(height: AtaSpacing.md),
            ],
            ..._list(context, state),
          ],
        );
      },
    );
  }

  List<Widget> _list(BuildContext context, MarketplaceState state) {
    final AppLocalizations l10n = context.l10n;
    final MarketplaceCubit cubit = context.read<MarketplaceCubit>();
    if (state.loading && !state.loaded) {
      return const <Widget>[CenteredLoader()];
    }
    if (state.failure != null && state.trips.isEmpty) {
      return <Widget>[
        FailureView(failure: state.failure!, onRetry: cubit.load),
      ];
    }
    if (state.isEmpty) {
      return <Widget>[
        Text(
          l10n.marketEmptyTitle,
          style: AtaText.bodyStrong,
          textAlign: TextAlign.center,
        ),
        Text(
          l10n.marketEmptyCopy,
          style: AtaText.small,
          textAlign: TextAlign.center,
        ),
      ];
    }
    return <Widget>[
      for (final MarketplaceTrip trip in state.trips) ...<Widget>[
        MarketplaceTile(
          trip: trip,
          reserving: state.reservingId == trip.tripId,
          onReserve: state.isReserving
              ? null
              : () => cubit.reserve(trip.tripId),
        ),
        const SizedBox(height: AtaSpacing.sm),
      ],
      if (state.hasMore)
        AtaButton(
          key: const ValueKey<String>('market-more'),
          label: l10n.marketLoadMore,
          variant: AtaButtonVariant.outline,
          height: AtaSizes.buttonCompact,
          loading: state.loadingMore,
          onPressed: cubit.loadMore,
        ),
    ];
  }
}

class _DayFilter extends StatelessWidget {
  const _DayFilter({required this.state});

  final MarketplaceState state;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final MarketplaceCubit cubit = context.read<MarketplaceCubit>();
    final String locale = context.localeCode;
    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: Row(
        children: <Widget>[
          _Chip(
            key: const ValueKey<String>('market-day-all'),
            label: l10n.marketDayAll,
            selected: state.day == null,
            onTap: () => cubit.selectDay(null),
          ),
          for (final DateTime day in state.days) ...<Widget>[
            const SizedBox(width: AtaSpacing.xs),
            _Chip(
              key: ValueKey<String>('market-day-${day.month}-${day.day}'),
              label:
                  '${DateText.weekday(day, locale)} '
                  '${DateText.dayMonth(day, locale)}',
              selected: state.day == day,
              onTap: () => cubit.selectDay(day),
            ),
          ],
        ],
      ),
    );
  }
}

class _Chip extends StatelessWidget {
  const _Chip({
    super.key,
    required this.label,
    required this.selected,
    required this.onTap,
  });

  final String label;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return SelectableTile(
      selected: selected,
      onTap: onTap,
      padding: const EdgeInsets.symmetric(
        horizontal: AtaSpacing.md,
        vertical: AtaSpacing.xs,
      ),
      child: Text(
        label,
        style: AtaText.label.copyWith(
          color: selected ? AtaColors.brand : AtaColors.ink,
        ),
      ),
    );
  }
}
