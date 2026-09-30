import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/core/widgets/failure_view.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/features/driver_wallet/presentation/widgets/driver_subpage.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/reservation.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/reservations_cubit.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/reservations_state.dart';
import 'package:ata_app/features/scheduled_rides/presentation/widgets/reservation_details.dart';
import 'package:ata_app/features/scheduled_rides/presentation/widgets/reservation_notices.dart';
import 'package:ata_app/features/scheduled_rides/presentation/widgets/scheduled_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// `/driver/scheduled/:tripId` (`ata://driver/scheduled/{tripId}`): one
/// reservation with its confirmation prompt (prominent "تأكيد" button and
/// the time left), the trip details and the release action.
class ReservationPage extends StatelessWidget {
  const ReservationPage({super.key, required this.tripId});

  final String tripId;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocProvider<ReservationsCubit>(
      create: (_) => ReservationsCubit(
        getReservations: getIt(),
        confirm: getIt(),
        release: getIt(),
        watchIncoming: getIt(),
      )..openTrip(tripId),
      child: BlocListener<ReservationsCubit, ReservationsState>(
        listenWhen: (ReservationsState p, ReservationsState c) =>
            c.event != null && p.event != c.event,
        listener: (BuildContext context, ReservationsState state) {
          final bool released = state.event == ReservationEvent.released;
          announceReservationEvent(context, state);
          if (released) {
            context.canPop()
                ? context.pop()
                : context.go(AppRoutes.driverScheduled);
          }
        },
        child: DriverSubpage.content(
          eyebrow: l10n.reservationDetailEyebrow,
          title: l10n.driverScheduledTitle,
          children: <Widget>[
            BlocBuilder<ReservationsCubit, ReservationsState>(
              builder: (BuildContext context, ReservationsState state) {
                final Reservation? r = state.find(tripId);
                if (r == null) {
                  if (state.failure != null) {
                    return FailureView(
                      failure: state.failure!,
                      onRetry: () =>
                          context.read<ReservationsCubit>().openTrip(tripId),
                    );
                  }
                  return state.loaded && !state.loading
                      ? _NotReserved(message: l10n.reservationNotFound)
                      : const CenteredLoader();
                }
                return Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: <Widget>[
                    Text(
                      '${ScheduledText.reservation(l10n, r.status)} · '
                      '${DateText.fullDayAndTime(r.scheduledAt, context.localeCode)}',
                      key: const ValueKey<String>('reservation-headline'),
                      style: AtaText.section,
                    ),
                    const SizedBox(height: AtaSpacing.md),
                    ReservationDetails(reservation: r, state: state),
                    if (state.actionFailure != null) ...<Widget>[
                      const SizedBox(height: AtaSpacing.md),
                      InlineError(
                        message: failureText(state.actionFailure!, l10n),
                      ),
                    ],
                  ],
                );
              },
            ),
          ],
        ),
      ),
    );
  }
}

/// A trip the driver has not reserved (for example the target of
/// `scheduled.favorite_request`, which is still in the marketplace).
class _NotReserved extends StatelessWidget {
  const _NotReserved({required this.message});

  final String message;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        InlineError(message: message),
        const SizedBox(height: AtaSpacing.md),
        AtaButton(
          key: const ValueKey<String>('open-marketplace'),
          label: context.l10n.reservationOpenMarket,
          height: AtaSizes.buttonCompact,
          onPressed: () => context.go(AppRoutes.driverScheduled),
        ),
      ],
    );
  }
}
