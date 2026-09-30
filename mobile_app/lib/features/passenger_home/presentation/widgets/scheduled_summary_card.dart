import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_cubit.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_state.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/schedule_time_cubit.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/schedule_time_state.dart';
import 'package:ata_app/features/scheduled_rides/presentation/widgets/schedule_picker_sheet.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// The confirmed booking time of a scheduled request, shown clearly before
/// booking: date and time, the free-cancel deadline, the fixed (no surge)
/// price label and the favourite-driver priority note.
class ScheduledSummaryCard extends StatelessWidget {
  const ScheduledSummaryCard({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final String locale = context.localeCode;
    return BlocBuilder<HomeCubit, HomeState>(
      buildWhen: (HomeState p, HomeState c) =>
          p.scheduledAt != c.scheduledAt ||
          p.rideTime != c.rideTime ||
          p.favorite != c.favorite,
      builder: (BuildContext context, HomeState home) {
        final DateTime? at = home.scheduledAt;
        if (!home.isScheduled || at == null) return const SizedBox.shrink();
        final ScheduleTimeState schedule = context
            .watch<ScheduleTimeCubit>()
            .state;
        final bool expired = schedule.confirmedIssue != null;
        return Padding(
          padding: const EdgeInsets.only(top: AtaSpacing.md),
          child: Container(
            key: const ValueKey<String>('scheduled-summary'),
            padding: const EdgeInsets.all(AtaSpacing.md),
            decoration: BoxDecoration(
              color: expired ? AtaColors.warningSoft : AtaColors.brandSoft,
              borderRadius: AtaRadii.itemRadius,
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: <Widget>[
                Row(
                  children: <Widget>[
                    const AtaIcon(
                      AtaIcons.clock,
                      size: AtaSizes.iconMedium,
                      color: AtaColors.brand,
                    ),
                    const SizedBox(width: AtaSpacing.sm),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: <Widget>[
                          Text(
                            l10n.scheduleConfirmedLabel,
                            style: AtaText.caption,
                          ),
                          Text(
                            DateText.fullDayAndTime(at, locale),
                            style: AtaText.bodyStrong,
                          ),
                        ],
                      ),
                    ),
                    PillButton(
                      label: l10n.scheduleEdit,
                      background: AtaColors.white,
                      foreground: AtaColors.brand,
                      elevated: false,
                      onTap: () => SchedulePickerSheet.show(
                        context,
                        rideCategoryId: home.selectedCategory?.id,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: AtaSpacing.xs),
                if (expired)
                  Text(
                    l10n.scheduleConfirmedExpired,
                    style: AtaText.small.copyWith(color: AtaColors.warning),
                  )
                else ...<Widget>[
                  Text(
                    l10n.cancelFreeUntil(
                      DateText.fullDayAndTime(
                        schedule.rules.freeCancelUntil(at),
                        locale,
                      ),
                    ),
                    style: AtaText.caption,
                  ),
                  const SizedBox(height: AtaSpacing.xs),
                  AtaBadge(label: l10n.scheduleFixedPrice),
                ],
                if (home.effectiveFavoriteDriverId != null) ...<Widget>[
                  const SizedBox(height: AtaSpacing.xs),
                  Text(
                    l10n.scheduleFavoritePriority(home.favorite?.name ?? ''),
                    style: AtaText.caption,
                  ),
                ],
              ],
            ),
          ),
        );
      },
    );
  }
}
