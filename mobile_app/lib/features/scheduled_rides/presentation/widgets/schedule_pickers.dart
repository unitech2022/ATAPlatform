import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/selectable_tile.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/schedule_time_cubit.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/schedule_time_state.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';

/// Day, hour and minute choosers of the schedule picker. Options outside
/// the booking window are disabled.
class SchedulePickers extends StatelessWidget {
  const SchedulePickers({super.key, required this.state, required this.cubit});

  final ScheduleTimeState state;
  final ScheduleTimeCubit cubit;

  static const int _hours = 24;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final String locale = context.localeCode;
    final DateTime? day = state.day;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        Text(l10n.scheduleDayHeading, style: AtaText.label),
        const SizedBox(height: AtaSpacing.xs),
        SingleChildScrollView(
          scrollDirection: Axis.horizontal,
          child: Row(
            children: <Widget>[
              for (final DateTime d in state.days) ...<Widget>[
                _Chip(
                  key: ValueKey<String>('sched-day-${d.month}-${d.day}'),
                  selected: day == d,
                  onTap: () => cubit.selectDay(d),
                  child: Column(
                    children: <Widget>[
                      Text(DateText.weekday(d, locale), style: AtaText.caption),
                      Text(
                        DateText.dayMonth(d, locale),
                        style: AtaText.captionStrong,
                      ),
                    ],
                  ),
                ),
                const SizedBox(width: AtaSpacing.xs),
              ],
            ],
          ),
        ),
        const SizedBox(height: AtaSpacing.md),
        Text(l10n.scheduleHourHeading, style: AtaText.label),
        const SizedBox(height: AtaSpacing.xs),
        Wrap(
          spacing: AtaSpacing.xs,
          runSpacing: AtaSpacing.xs,
          children: <Widget>[
            for (int h = 0; h < _hours; h++)
              _Chip(
                key: ValueKey<String>('sched-hour-$h'),
                selected: state.hour == h,
                onTap: day != null && state.isHourAvailable(day, h)
                    ? () => cubit.selectHour(h)
                    : null,
                child: Text(
                  _hourLabel(h, locale),
                  style: AtaText.captionStrong,
                ),
              ),
          ],
        ),
        const SizedBox(height: AtaSpacing.md),
        Text(l10n.scheduleMinuteHeading, style: AtaText.label),
        const SizedBox(height: AtaSpacing.xs),
        Wrap(
          spacing: AtaSpacing.xs,
          runSpacing: AtaSpacing.xs,
          children: <Widget>[
            for (int m = 0; m < 60; m += ScheduleTimeState.minuteStep)
              _Chip(
                key: ValueKey<String>('sched-minute-$m'),
                selected: state.minute == m,
                onTap:
                    day != null &&
                        state.hour != null &&
                        state.isAvailable(day, hour: state.hour!, minute: m)
                    ? () => cubit.selectMinute(m)
                    : null,
                child: Text(
                  m.toString().padLeft(2, '0'),
                  style: AtaText.captionStrong,
                ),
              ),
          ],
        ),
      ],
    );
  }

  static String _hourLabel(int hour, String locale) =>
      DateText.time(DateTime(2000, 1, 1, hour), locale).replaceAll(':00', '');
}

class _Chip extends StatelessWidget {
  const _Chip({
    super.key,
    required this.selected,
    required this.onTap,
    required this.child,
  });

  final bool selected;
  final VoidCallback? onTap;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    final bool enabled = onTap != null;
    return Opacity(
      opacity: enabled ? 1 : 0.35,
      child: SelectableTile(
        selected: selected,
        onTap: onTap,
        radius: AtaRadii.smallRadius,
        padding: const EdgeInsets.symmetric(
          horizontal: AtaSpacing.sm,
          vertical: AtaSpacing.xs,
        ),
        unselectedBackground: enabled ? AtaColors.white : AtaColors.cloud,
        child: child,
      ),
    );
  }
}
