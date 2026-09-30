import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/design/widgets/selectable_tile.dart';
import 'package:ata_app/features/rides/domain/entities/trip_summary.dart';
import 'package:ata_app/features/support/presentation/cubit/new_ticket_cubit.dart';
import 'package:ata_app/features/support/presentation/cubit/new_ticket_state.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Picks the trip a ticket is about from the recent finished trips
/// (required for trip, payment and lost item tickets, optional otherwise).
class TripPicker extends StatelessWidget {
  const TripPicker({super.key, required this.state});

  final NewTicketState state;

  static const int _maxTrips = 10;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final NewTicketCubit cubit = context.read<NewTicketCubit>();
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        Text(
          state.needsTrip
              ? l10n.newTicketTripLabel
              : '${l10n.newTicketTripLabel} · ${l10n.newTicketTripOptional}',
          style: AtaText.label,
        ),
        if (state.needsTrip && !state.hasTrip)
          Text(
            l10n.newTicketTripRequired,
            style: AtaText.caption.copyWith(color: AtaColors.warning),
          ),
        const SizedBox(height: AtaSpacing.xs),
        if (state.tripsLoading)
          const CenteredLoader()
        else if (state.tripsFailure != null)
          Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: <Widget>[
              InlineError(message: failureText(state.tripsFailure!, l10n)),
              const SizedBox(height: AtaSpacing.xs),
              AtaButton(
                label: l10n.retry,
                variant: AtaButtonVariant.outline,
                height: AtaSizes.buttonCompact,
                onPressed: cubit.retryTrips,
              ),
            ],
          )
        else ...<Widget>[
          if (!state.needsTrip)
            _tile(
              selected: !state.hasTrip,
              onTap: () => cubit.selectTrip(null),
              child: Text(l10n.newTicketNoTrip, style: AtaText.label),
            ),
          if (state.trips.isEmpty && state.needsTrip)
            Text(l10n.newTicketNoTrips, style: AtaText.small),
          for (final TripSummary t in state.trips.take(_maxTrips))
            _tile(
              key: ValueKey<String>('trip-${t.id}'),
              selected: state.tripId == t.id,
              onTap: () => cubit.selectTrip(t.id),
              child: _TripRow(trip: t),
            ),
        ],
      ],
    );
  }

  Widget _tile({
    Key? key,
    required bool selected,
    required VoidCallback onTap,
    required Widget child,
  }) => Padding(
    key: key,
    padding: const EdgeInsets.only(bottom: AtaSpacing.xs),
    child: SelectableTile(selected: selected, onTap: onTap, child: child),
  );
}

class _TripRow extends StatelessWidget {
  const _TripRow({required this.trip});

  final TripSummary trip;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final DateTime? at = trip.displayDate;
    return Row(
      children: <Widget>[
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: <Widget>[
              Text(
                l10n.tripRoute(trip.pickupName, trip.destinationName),
                style: AtaText.label,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
              Text(
                <String>[
                  if (trip.tripNumber.isNotEmpty) trip.tripNumber,
                  if (at != null) DateText.dayAndTime(at, context.localeCode),
                ].join(' · '),
                style: AtaText.caption,
              ),
            ],
          ),
        ),
        Text(
          l10n.priceWithCurrency(Money.compact(trip.displayAmount)),
          style: AtaText.label,
        ),
      ],
    );
  }
}
