import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/design/widgets/setting_row.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/reservation.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/reservations_cubit.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/reservations_state.dart';
import 'package:ata_app/features/scheduled_rides/presentation/widgets/confirm_prompt_card.dart';
import 'package:ata_app/features/scheduled_rides/presentation/widgets/reservation_notices.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// Driver overview: the T-60 / T-15 confirmation prompts of the driver's
/// reservations (with a confirm button) and the link to the marketplace.
class ScheduledOverviewCard extends StatelessWidget {
  const ScheduledOverviewCard({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocConsumer<ReservationsCubit, ReservationsState>(
      listenWhen: (ReservationsState p, ReservationsState c) =>
          c.event != null && p.event != c.event,
      listener: announceReservationEvent,
      builder: (BuildContext context, ReservationsState state) {
        final ReservationsCubit cubit = context.read<ReservationsCubit>();
        final Reservation? next = state.active.firstOrNull;
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
              const SizedBox(height: AtaSpacing.sm),
            ],
            SettingRow(
              leading: const IconBox.cloud(icon: AtaIcons.clock),
              title: l10n.driverScheduledTitle,
              subtitle: next == null
                  ? l10n.driverScheduledLinkCopy
                  : l10n.driverScheduledNext(
                      DateText.fullDayAndTime(
                        next.scheduledAt,
                        context.localeCode,
                      ),
                    ),
              last: true,
              onTap: () => context.push(AppRoutes.driverScheduled),
            ),
          ],
        );
      },
    );
  }
}
