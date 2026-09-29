import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/design/widgets/star_rating.dart';
import 'package:ata_app/features/rating/domain/entities/submitted_rating.dart';
import 'package:ata_app/features/rating/presentation/cubit/rating_cubit.dart';
import 'package:ata_app/features/rating/presentation/cubit/rating_state.dart';
import 'package:ata_app/features/rating/presentation/widgets/rating_tag_chips.dart';
import 'package:ata_app/features/rating/presentation/widgets/rating_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Stars, tags, comment and submit for the [RatingCubit] above; once sent
/// it shows the thank-you state with [onDone].
class RatingForm extends StatelessWidget {
  const RatingForm({super.key, required this.onDone});

  final VoidCallback onDone;

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<RatingCubit, RatingState>(
      builder: (BuildContext context, RatingState state) =>
          state.status == RatingStatus.done && state.result != null
          ? _Thanks(onDone: onDone)
          : _Editor(state: state, onDone: onDone),
    );
  }
}

class _Editor extends StatelessWidget {
  const _Editor({required this.state, required this.onDone});

  final RatingState state;
  final VoidCallback onDone;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final RatingCubit cubit = context.read<RatingCubit>();
    final bool editing = state.status == RatingStatus.editing;
    return Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        Text(
          RatingText.question(
            l10n,
            state.subject.rater,
            state.subject.counterpartName,
          ),
          style: AtaText.section,
          textAlign: TextAlign.center,
        ),
        const SizedBox(height: AtaSpacing.md),
        Center(
          child: StarRating(
            value: state.stars,
            size: AtaSizes.iconHero,
            onChanged: editing ? cubit.setStars : null,
            semanticLabel: (int n) => RatingText.stars(l10n, n),
          ),
        ),
        const SizedBox(height: AtaSpacing.xs),
        Text(
          RatingText.stars(l10n, state.stars),
          style: AtaText.captionStrong.copyWith(
            color: state.starsMissing ? AtaColors.danger : AtaColors.muted,
          ),
          textAlign: TextAlign.center,
        ),
        if (state.hasStars) ...<Widget>[
          const SizedBox(height: AtaSpacing.lg),
          Text(
            state.isNegative
                ? l10n.ratingTagsNegative
                : l10n.ratingTagsPositive,
            style: AtaText.label,
          ),
          const SizedBox(height: AtaSpacing.xs),
          if (state.loadingTags)
            const CenteredLoader()
          else
            RatingTagChips(
              tags: state.tags,
              selected: state.selectedTags,
              negative: state.isNegative,
              onToggle: editing ? cubit.toggleTag : null,
            ),
          const SizedBox(height: AtaSpacing.md),
          TextField(
            key: const ValueKey<String>('rating-comment'),
            enabled: editing,
            onChanged: cubit.commentChanged,
            maxLength: RatingDraft.maxCommentLength,
            minLines: 2,
            maxLines: 4,
            style: AtaText.body,
            decoration: InputDecoration(hintText: l10n.ratingCommentHint),
          ),
        ],
        if (state.failure != null) ...<Widget>[
          const SizedBox(height: AtaSpacing.sm),
          InlineError(message: failureText(state.failure!, l10n)),
        ],
        const SizedBox(height: AtaSpacing.md),
        if (state.isFinished)
          AtaButton(label: l10n.done, onPressed: onDone)
        else
          AtaButton(
            label: l10n.ratingSubmit,
            variant: AtaButtonVariant.brand,
            loading: state.isSubmitting,
            onPressed: state.isSubmitting || state.commentTooLong
                ? null
                : cubit.submit,
          ),
      ],
    );
  }
}

class _Thanks extends StatelessWidget {
  const _Thanks({required this.onDone});

  final VoidCallback onDone;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        const Center(
          child: IconBox.brand(
            icon: AtaIcons.check,
            size: AtaSizes.iconBoxHero,
            iconSize: AtaSizes.iconHero,
            round: true,
          ),
        ),
        const SizedBox(height: AtaSpacing.md),
        Text(
          l10n.ratingThanks,
          style: AtaText.headline,
          textAlign: TextAlign.center,
        ),
        const SizedBox(height: AtaSpacing.xxs),
        Text(
          l10n.ratingThanksCopy,
          style: AtaText.bodyMuted,
          textAlign: TextAlign.center,
        ),
        const SizedBox(height: AtaSpacing.lg),
        AtaButton(label: l10n.done, onPressed: onDone),
      ],
    );
  }
}
