import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/core/widgets/failure_view.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/features/trip/domain/entities/reliability_summary.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/presentation/cubit/reliability_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/reliability_state.dart';
import 'package:ata_app/features/trip/presentation/widgets/cancellation_text.dart';
import 'package:ata_app/features/trip/presentation/widgets/reliability_card.dart';
import 'package:ata_app/features/trip/presentation/widgets/trip_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Body of `/account/reliability` and `/driver/reliability`.
class ReliabilityDetails extends StatelessWidget {
  const ReliabilityDetails({super.key, required this.role});

  final TripActor role;

  @override
  Widget build(BuildContext context) {
    return BlocProvider<ReliabilityCubit>(
      create: (_) =>
          ReliabilityCubit(getReliability: getIt(), role: role)..load(),
      child: BlocBuilder<ReliabilityCubit, ReliabilityState>(
        builder: (BuildContext context, ReliabilityState state) {
          final ReliabilitySummary? summary = state.summary;
          if (summary == null) {
            return state.failure == null
                ? const CenteredLoader()
                : FailureView(
                    failure: state.failure!,
                    onRetry: context.read<ReliabilityCubit>().load,
                  );
          }
          return _Details(summary: summary);
        },
      ),
    );
  }
}

class _Details extends StatelessWidget {
  const _Details({required this.summary});

  final ReliabilitySummary summary;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final ReliabilityNextLevel? next = summary.nextLevel;
    final List<(String, String)> stats = <(String, String)>[
      (l10n.tripsAcceptedLabel, '${summary.tripsAccepted}'),
      (l10n.tripsCompletedLabel, '${summary.tripsCompleted}'),
      (l10n.cancellationsAtFaultLabel, '${summary.cancellationsAtFault}'),
      (
        l10n.reliabilityRateLabel,
        CancellationText.percent(summary.reliabilityRate),
      ),
      (l10n.noShowCountLabel, '${summary.noShowCount}'),
      if (summary.matchingFactor != null)
        (
          l10n.matchingFactorLabel,
          '×${Money.compact(summary.matchingFactor!)}',
        ),
      if (summary.incentiveMultiplier != null)
        (
          l10n.incentiveMultiplierLabel,
          '×${Money.compact(summary.incentiveMultiplier!)}',
        ),
    ];
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        const ReliabilityCard(),
        const SizedBox(height: AtaSpacing.md),
        AtaCard(
          padding: const EdgeInsets.all(AtaSpacing.lg),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: <Widget>[
              Text(
                l10n.reliabilityWindow(summary.windowDays),
                style: AtaText.caption,
              ),
              for (final (String label, String value) in stats)
                _Line(label: label, value: value),
              if (next != null) ...<Widget>[
                const SizedBox(height: AtaSpacing.sm),
                Text(
                  l10n.nextLevelLine(
                    CancellationText.levelLabel(l10n, next.level),
                    '${next.minPenaltyPoints ?? '-'}',
                    next.minCancellationRate == null
                        ? '-'
                        : CancellationText.percent(next.minCancellationRate!),
                  ),
                  style: AtaText.small,
                ),
              ],
            ],
          ),
        ),
        const SizedBox(height: AtaSpacing.md),
        Text(l10n.recentCancellations, style: AtaText.section),
        const SizedBox(height: AtaSpacing.xs),
        if (summary.recentEvents.isEmpty)
          Text(l10n.noRecentCancellations, style: AtaText.bodyMuted)
        else
          for (final ReliabilityEvent event in summary.recentEvents)
            _EventTile(event: event),
      ],
    );
  }
}

class _Line extends StatelessWidget {
  const _Line({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: AtaSpacing.xxs),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: <Widget>[
          Text(label, style: AtaText.small),
          Text(value, style: AtaText.label, textDirection: TextDirection.ltr),
        ],
      ),
    );
  }
}

class _EventTile extends StatelessWidget {
  const _EventTile({required this.event});

  final ReliabilityEvent event;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final DateTime? date = event.createdAt;
    final String excuse = CancellationText.excuseStatus(
      l10n,
      event.excuseStatus,
    );
    final List<String> meta = <String>[
      if (date != null) DateText.dayAndTime(date, context.localeCode),
      if (event.feeCharged > 0) TripText.price(l10n, event.feeCharged),
      if (event.penaltyPoints > 0) l10n.penaltyPointsValue(event.penaltyPoints),
      if (excuse.isNotEmpty) excuse,
    ];
    return Padding(
      padding: const EdgeInsets.only(bottom: AtaSpacing.xs),
      child: AtaCard(
        padding: const EdgeInsets.all(AtaSpacing.md),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: <Widget>[
            Text(
              event.reasonName.isEmpty ? event.tripNumber : event.reasonName,
              style: AtaText.bodyStrong,
            ),
            Text(
              '${event.tripNumber} · ${meta.join(' · ')}',
              style: AtaText.caption,
            ),
          ],
        ),
      ),
    );
  }
}
