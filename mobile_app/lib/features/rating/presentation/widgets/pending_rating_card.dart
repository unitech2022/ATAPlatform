import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/features/rating/domain/entities/pending_rating.dart';
import 'package:ata_app/features/rating/domain/entities/rating_subject.dart';
import 'package:ata_app/features/rating/presentation/cubit/pending_rating_cubit.dart';
import 'package:ata_app/features/rating/presentation/cubit/pending_rating_state.dart';
import 'package:ata_app/features/rating/presentation/widgets/rating_sheet.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Dismissible "rate your last trip" prompt driven by the app-wide
/// [PendingRatingCubit] (rider home sheet, driver overview).
class PendingRatingCard extends StatelessWidget {
  const PendingRatingCard({super.key, this.bottomSpacing = AtaSpacing.md});

  final double bottomSpacing;

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<PendingRatingCubit, PendingRatingState>(
      builder: (BuildContext context, PendingRatingState state) {
        final PendingRating? prompt = state.prompt;
        final TripActor? role = state.role;
        if (prompt == null || role == null) return const SizedBox.shrink();
        return Padding(
          padding: EdgeInsets.only(bottom: bottomSpacing),
          child: _Card(
            prompt: prompt,
            subject: RatingSubject(
              tripId: prompt.tripId,
              rater: role,
              counterpartName: prompt.counterpartName,
            ),
          ),
        );
      },
    );
  }
}

class _Card extends StatelessWidget {
  const _Card({required this.prompt, required this.subject});

  final PendingRating prompt;
  final RatingSubject subject;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final PendingRatingCubit cubit = context.read<PendingRatingCubit>();
    final String name = prompt.counterpartName;
    return Container(
      key: const ValueKey<String>('pending-rating-card'),
      padding: const EdgeInsets.all(AtaSpacing.sm),
      decoration: BoxDecoration(
        color: AtaColors.warningSoft,
        borderRadius: AtaRadii.itemRadius,
        border: Border.all(color: AtaColors.line),
      ),
      child: Row(
        children: <Widget>[
          const IconBox(
            icon: AtaIcons.star,
            background: AtaColors.white,
            foreground: AtaColors.warning,
          ),
          const SizedBox(width: AtaSpacing.sm),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: <Widget>[
                Text(l10n.pendingRatingTitle, style: AtaText.label),
                Text(
                  name.isEmpty
                      ? l10n.pendingRatingCopy
                      : l10n.pendingRatingCopyNamed(name),
                  style: AtaText.caption,
                ),
              ],
            ),
          ),
          PillButton(
            label: l10n.rateNow,
            background: AtaColors.warning,
            foreground: AtaColors.white,
            elevated: false,
            onTap: () => RatingSheet.show(context, subject: subject),
          ),
          IconButton(
            tooltip: l10n.later,
            onPressed: () => cubit.dismiss(prompt.tripId),
            icon: const Icon(Icons.close, color: AtaColors.muted),
          ),
        ],
      ),
    );
  }
}
