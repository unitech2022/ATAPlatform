import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/features/trip/domain/entities/reliability_summary.dart';
import 'package:ata_app/features/trip/presentation/cubit/reliability_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/reliability_state.dart';
import 'package:ata_app/features/trip/presentation/widgets/cancellation_text.dart';
import 'package:ata_app/features/trip/presentation/widgets/reliability_level_badge.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Compact reliability summary (account page / driver dashboard): level,
/// cancellation rate, penalty points, restriction end and what it means.
/// Needs a [ReliabilityCubit] above; hidden until a summary is loaded.
class ReliabilityCard extends StatelessWidget {
  const ReliabilityCard({super.key, this.onDetails});

  final VoidCallback? onDetails;

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<ReliabilityCubit, ReliabilityState>(
      builder: (BuildContext context, ReliabilityState state) {
        final ReliabilitySummary? summary = state.summary;
        if (summary == null) return const SizedBox.shrink();
        return _Card(summary: summary, onDetails: onDetails);
      },
    );
  }
}

class _Card extends StatelessWidget {
  const _Card({required this.summary, this.onDetails});

  final ReliabilitySummary summary;
  final VoidCallback? onDetails;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final DateTime? until = summary.restrictedUntil;
    return AtaCard(
      onTap: onDetails,
      padding: const EdgeInsets.all(AtaSpacing.lg),
      borderColor: summary.level.isRestricted ? AtaColors.danger : null,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Row(
            children: <Widget>[
              Expanded(
                child: Text(l10n.reliabilityTitle, style: AtaText.section),
              ),
              ReliabilityLevelBadge(level: summary.level),
            ],
          ),
          const SizedBox(height: AtaSpacing.sm),
          Row(
            children: <Widget>[
              Expanded(
                child: ReliabilityMetric(
                  label: l10n.cancellationRateLabel,
                  value: CancellationText.percent(summary.cancellationRate),
                ),
              ),
              Expanded(
                child: ReliabilityMetric(
                  label: l10n.penaltyPointsLabel,
                  value: '${summary.penaltyPoints}',
                ),
              ),
              if (summary.acceptanceRate != null)
                Expanded(
                  child: ReliabilityMetric(
                    label: l10n.acceptanceRateLabel,
                    value: CancellationText.percent(summary.acceptanceRate!),
                  ),
                ),
            ],
          ),
          if (until != null) ...<Widget>[
            const SizedBox(height: AtaSpacing.sm),
            Text(
              l10n.restrictedUntilLine(
                DateText.dayAndTime(until, context.localeCode),
              ),
              style: AtaText.label.copyWith(color: AtaColors.danger),
            ),
          ],
          const SizedBox(height: AtaSpacing.xs),
          Text(
            CancellationText.levelExplanation(
              l10n,
              summary.level,
              driver: summary.isDriver,
            ),
            style: AtaText.small,
          ),
          if (onDetails != null) ...<Widget>[
            const SizedBox(height: AtaSpacing.sm),
            Row(
              children: <Widget>[
                Text(
                  l10n.reliabilityDetails,
                  style: AtaText.label.copyWith(color: AtaColors.brand),
                ),
                const SizedBox(width: AtaSpacing.xs),
                const AtaIcon(
                  AtaIcons.arrow,
                  size: AtaSizes.iconSmall,
                  color: AtaColors.brand,
                ),
              ],
            ),
          ],
        ],
      ),
    );
  }
}

/// Label + big value.
class ReliabilityMetric extends StatelessWidget {
  const ReliabilityMetric({
    super.key,
    required this.label,
    required this.value,
  });

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: <Widget>[
        Text(label, style: AtaText.caption),
        Text(value, style: AtaText.section, textDirection: TextDirection.ltr),
      ],
    );
  }
}
