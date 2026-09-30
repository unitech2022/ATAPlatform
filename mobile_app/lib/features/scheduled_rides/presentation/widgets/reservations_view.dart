import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/widgets/failure_view.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/design/widgets/selectable_tile.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/reservation.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/scheduled_queries.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/reservations_cubit.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/reservations_state.dart';
import 'package:ata_app/features/scheduled_rides/presentation/widgets/confirm_prompt_card.dart';
import 'package:ata_app/features/scheduled_rides/presentation/widgets/reservation_tile.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// The "حجوزاتي" tab: due confirmation prompts, then upcoming / past
/// reservations.
class ReservationsView extends StatelessWidget {
  const ReservationsView({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocBuilder<ReservationsCubit, ReservationsState>(
      builder: (BuildContext context, ReservationsState state) {
        final ReservationsCubit cubit = context.read<ReservationsCubit>();
        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: <Widget>[
            for (final Reservation r in state.pending) ...<Widget>[
              ConfirmPromptCard(
                reservation: r,
                now: state.now,
                busy: state.busyTripId == r.tripId,
                onConfirm: () => cubit.confirm(r.tripId),
                onOpen: () =>
                    context.push(AppRoutes.driverScheduledTrip(r.tripId)),
              ),
              const SizedBox(height: AtaSpacing.md),
            ],
            Row(
              children: <Widget>[
                for (final ReservationList list in ReservationList.values)
                  Expanded(
                    child: Padding(
                      padding: const EdgeInsetsDirectional.only(
                        end: AtaSpacing.xs,
                      ),
                      child: SelectableTile(
                        key: ValueKey<String>('reservations-${list.apiValue}'),
                        selected: state.list == list,
                        onTap: () => cubit.selectList(list),
                        padding: const EdgeInsets.symmetric(
                          vertical: AtaSpacing.xs,
                        ),
                        child: Text(
                          list == ReservationList.active
                              ? l10n.reservationsActive
                              : l10n.reservationsHistory,
                          textAlign: TextAlign.center,
                          style: AtaText.label.copyWith(
                            color: state.list == list
                                ? AtaColors.brand
                                : AtaColors.ink,
                          ),
                        ),
                      ),
                    ),
                  ),
              ],
            ),
            const SizedBox(height: AtaSpacing.md),
            if (state.actionFailure != null) ...<Widget>[
              InlineError(message: failureText(state.actionFailure!, l10n)),
              const SizedBox(height: AtaSpacing.md),
            ],
            ..._list(context, state),
          ],
        );
      },
    );
  }

  List<Widget> _list(BuildContext context, ReservationsState state) {
    final AppLocalizations l10n = context.l10n;
    final ReservationsCubit cubit = context.read<ReservationsCubit>();
    if (state.loading && state.current.isEmpty) {
      return const <Widget>[Center(child: CircularProgressIndicator())];
    }
    if (state.failure != null && state.current.isEmpty) {
      return <Widget>[
        FailureView(failure: state.failure!, onRetry: cubit.load),
      ];
    }
    if (state.isEmpty) {
      return <Widget>[
        Text(
          l10n.reservationsEmpty,
          style: AtaText.small,
          textAlign: TextAlign.center,
        ),
      ];
    }
    return <Widget>[
      for (final Reservation r in state.current) ...<Widget>[
        ReservationTile(
          reservation: r,
          onTap: () => context.push(AppRoutes.driverScheduledTrip(r.tripId)),
        ),
        const SizedBox(height: AtaSpacing.sm),
      ],
    ];
  }
}
