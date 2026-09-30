import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/design/widgets/setting_row.dart';
import 'package:ata_app/features/airport/domain/entities/airport_queue_status.dart';
import 'package:ata_app/features/airport/presentation/cubit/airport_queue_cubit.dart';
import 'package:ata_app/features/airport/presentation/cubit/airport_queue_state.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// "طابور المطار" card of the driver overview: shown only when the driver
/// is queued or stands at an airport; opens the queue page.
class AirportQueueCard extends StatelessWidget {
  const AirportQueueCard({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocBuilder<AirportQueueCubit, AirportQueueState>(
      builder: (BuildContext context, AirportQueueState state) {
        final AirportQueueStatus? queue = state.queue;
        if (queue == null || !queue.isRelevant) return const SizedBox.shrink();
        final String airport = queue.relevantAirport?.name ?? '';
        return Padding(
          padding: const EdgeInsets.only(bottom: AtaSpacing.xl),
          child: AtaCard(
            key: const ValueKey<String>('airport-queue-card'),
            padding: const EdgeInsets.all(AtaSpacing.lg),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: <Widget>[
                Text(l10n.airportQueueTitle, style: AtaText.section),
                SettingRow(
                  leading: const IconBox.cloud(icon: AtaIcons.location),
                  title: queue.inQueue
                      ? l10n.airportQueuePosition(
                          queue.position ?? 0,
                          queue.total ?? 0,
                        )
                      : l10n.airportQueueEligible(airport),
                  subtitle: queue.inQueue ? airport : l10n.airportQueueJoinHint,
                  last: true,
                  titleStyle: queue.inQueue
                      ? AtaText.bodyStrong.copyWith(color: AtaColors.brand)
                      : null,
                  onTap: () => context.push(AppRoutes.driverAirportQueue),
                ),
              ],
            ),
          ),
        );
      },
    );
  }
}
