import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/widgets/failure_view.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/features/airport/domain/entities/airport_queue_status.dart';
import 'package:ata_app/features/airport/presentation/cubit/airport_queue_cubit.dart';
import 'package:ata_app/features/airport/presentation/cubit/airport_queue_state.dart';
import 'package:ata_app/features/driver_wallet/presentation/widgets/driver_subpage.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:ata_app/features/trip/presentation/cubit/location_stream_cubit.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// `/driver/airport-queue`: the driver's position in the airport queue with
/// join / leave (`docs/11` §F17.7).
class AirportQueuePage extends StatelessWidget {
  const AirportQueuePage({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocProvider<AirportQueueCubit>(
      create: (_) => AirportQueueCubit(
        getQueue: getIt(),
        join: getIt(),
        leave: getIt(),
        watch: getIt(),
      )..start(),
      child: DriverSubpage.content(
        eyebrow: l10n.airportQueueEyebrow,
        title: l10n.airportQueueTitle,
        copy: l10n.airportQueueCopy,
        children: const <Widget>[_QueueBody()],
      ),
    );
  }
}

class _QueueBody extends StatelessWidget {
  const _QueueBody();

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<AirportQueueCubit, AirportQueueState>(
      builder: (BuildContext context, AirportQueueState state) {
        final AppLocalizations l10n = context.l10n;
        final AirportQueueCubit cubit = context.read<AirportQueueCubit>();
        final AirportQueueStatus? queue = state.queue;
        if (queue == null) {
          return state.failure == null
              ? const CenteredLoader()
              : FailureView(failure: state.failure!, onRetry: cubit.refresh);
        }
        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: <Widget>[
            if (queue.inQueue)
              _PositionCard(queue: queue)
            else
              Text(
                queue.eligibleAirport == null
                    ? l10n.airportQueueNotNearby
                    : l10n.airportQueueEligible(queue.eligibleAirport!.name),
                key: const ValueKey<String>('airport-queue-not-in'),
                style: AtaText.bodyStrong,
              ),
            const SizedBox(height: AtaSpacing.md),
            if (state.actionFailure != null) ...<Widget>[
              InlineError(message: failureText(state.actionFailure!, l10n)),
              const SizedBox(height: AtaSpacing.md),
            ],
            if (queue.inQueue)
              AtaButton(
                key: const ValueKey<String>('airport-queue-leave'),
                label: l10n.airportQueueLeave,
                variant: AtaButtonVariant.dangerOutline,
                height: AtaSizes.buttonCompact,
                loading: state.busy,
                onPressed: cubit.leave,
              )
            else if (queue.eligibleAirport != null)
              AtaButton(
                key: const ValueKey<String>('airport-queue-join'),
                label: l10n.airportQueueJoin,
                height: AtaSizes.buttonCompact,
                loading: state.busy,
                onPressed: () => _join(context, cubit),
              ),
            const SizedBox(height: AtaSpacing.md),
            Text(l10n.airportQueueExitNote, style: AtaText.caption),
          ],
        );
      },
    );
  }

  void _join(BuildContext context, AirportQueueCubit cubit) {
    final GeoPoint? point = context
        .read<LocationStreamCubit>()
        .state
        .position
        ?.point;
    cubit.join(point ?? GeoPoint.riyadh);
  }
}

class _PositionCard extends StatelessWidget {
  const _PositionCard({required this.queue});

  final AirportQueueStatus queue;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final int? wait = queue.estimatedWaitMinutes;
    return Container(
      key: const ValueKey<String>('airport-queue-position'),
      padding: const EdgeInsets.all(AtaSpacing.xl),
      decoration: const BoxDecoration(
        color: AtaColors.brandSoft,
        borderRadius: AtaRadii.itemRadius,
      ),
      child: Column(
        children: <Widget>[
          Text(queue.airport?.name ?? '', style: AtaText.label),
          const SizedBox(height: AtaSpacing.xs),
          Text(
            l10n.airportQueuePosition(queue.position ?? 0, queue.total ?? 0),
            style: AtaText.headline.copyWith(color: AtaColors.brand),
          ),
          if (wait != null) ...<Widget>[
            const SizedBox(height: AtaSpacing.xxs),
            Text(l10n.airportQueueWait(wait), style: AtaText.small),
          ],
        ],
      ),
    );
  }
}
