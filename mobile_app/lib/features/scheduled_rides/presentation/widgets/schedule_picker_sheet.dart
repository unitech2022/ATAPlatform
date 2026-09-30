import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/sheet_handle.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/schedule_time_cubit.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/schedule_time_state.dart';
import 'package:ata_app/features/scheduled_rides/presentation/widgets/schedule_pickers.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Bottom sheet that picks the booking day and time, limited to the window
/// of the scheduling rules, and shows the time that will be booked before
/// the rider confirms it.
class SchedulePickerSheet extends StatelessWidget {
  const SchedulePickerSheet({super.key});

  /// Opens the picker on the [ScheduleTimeCubit] of [context]; the rules
  /// are read for [rideCategoryId].
  static Future<void> show(BuildContext context, {String? rideCategoryId}) {
    final ScheduleTimeCubit cubit = context.read<ScheduleTimeCubit>()
      ..open(rideCategoryId: rideCategoryId);
    return showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      builder: (_) => BlocProvider<ScheduleTimeCubit>.value(
        value: cubit,
        child: const SchedulePickerSheet(),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocBuilder<ScheduleTimeCubit, ScheduleTimeState>(
      builder: (BuildContext context, ScheduleTimeState state) {
        final ScheduleTimeCubit cubit = context.read<ScheduleTimeCubit>();
        return SingleChildScrollView(
          padding: EdgeInsets.fromLTRB(
            AtaSpacing.lg,
            AtaSpacing.lg,
            AtaSpacing.lg,
            AtaSpacing.lg + MediaQuery.paddingOf(context).bottom,
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: <Widget>[
              const SheetHandle(),
              Text(l10n.schedulePickerTitle, style: AtaText.section),
              const SizedBox(height: AtaSpacing.xxs),
              Text(
                l10n.schedulePickerCopy(
                  l10n.minutesLabel(state.rules.minLeadMinutes),
                  l10n.scheduleWindowDays(state.rules.maxDaysAhead),
                ),
                style: AtaText.small,
              ),
              if (state.rulesFailure != null) ...<Widget>[
                const SizedBox(height: AtaSpacing.xs),
                Text(
                  '${l10n.scheduleRulesFallback} '
                  '${failureText(state.rulesFailure!, l10n)}',
                  style: AtaText.caption.copyWith(color: AtaColors.warning),
                ),
              ],
              const SizedBox(height: AtaSpacing.md),
              SchedulePickers(state: state, cubit: cubit),
              const SizedBox(height: AtaSpacing.md),
              _Summary(state: state),
              const SizedBox(height: AtaSpacing.md),
              AtaButton(
                key: const ValueKey<String>('schedule-confirm'),
                label: l10n.scheduleConfirm,
                height: AtaSizes.buttonCompact,
                onPressed: state.canConfirm
                    ? () {
                        if (cubit.confirm()) Navigator.of(context).pop();
                      }
                    : null,
              ),
              const SizedBox(height: AtaSpacing.xs),
              AtaButton(
                label: l10n.cancel,
                variant: AtaButtonVariant.soft,
                height: AtaSizes.buttonCompact,
                onPressed: () => Navigator.of(context).pop(),
              ),
            ],
          ),
        );
      },
    );
  }
}

/// The time that will be booked (or why the picked time cannot be).
class _Summary extends StatelessWidget {
  const _Summary({required this.state});

  final ScheduleTimeState state;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final String locale = context.localeCode;
    final DateTime? draft = state.draft;
    final ScheduleIssue? issue = state.draftIssue;
    final String? problem = switch (issue) {
      ScheduleIssue.tooSoon => l10n.scheduleTooSoon(
        DateText.fullDayAndTime(state.minAt, locale),
      ),
      ScheduleIssue.tooFar => l10n.scheduleTooFar(
        DateText.fullDayAndTime(state.maxAt, locale),
      ),
      null => null,
    };
    return Container(
      key: const ValueKey<String>('schedule-summary'),
      padding: const EdgeInsets.all(AtaSpacing.md),
      decoration: BoxDecoration(
        color: problem == null ? AtaColors.brandSoft : AtaColors.dangerSoft,
        borderRadius: AtaRadii.itemRadius,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: <Widget>[
          Text(l10n.scheduleSelectedLabel, style: AtaText.caption),
          const SizedBox(height: AtaSpacing.xxs),
          Text(
            draft == null
                ? l10n.scheduleNothingPicked
                : DateText.fullDayAndTime(draft, locale),
            style: AtaText.section,
          ),
          if (problem != null) ...<Widget>[
            const SizedBox(height: AtaSpacing.xxs),
            Text(
              problem,
              style: AtaText.small.copyWith(color: AtaColors.danger),
            ),
          ] else if (draft != null) ...<Widget>[
            const SizedBox(height: AtaSpacing.xxs),
            Text(
              l10n.cancelFreeUntil(
                DateText.fullDayAndTime(
                  state.rules.freeCancelUntil(draft),
                  locale,
                ),
              ),
              style: AtaText.caption,
            ),
            Text(l10n.scheduleFixedPrice, style: AtaText.caption),
          ],
        ],
      ),
    );
  }
}
