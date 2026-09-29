import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/ata_toggle.dart';
import 'package:ata_app/features/rating/presentation/cubit/rating_cubit.dart';
import 'package:ata_app/features/rating/presentation/cubit/rating_state.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// "أضف إلى المفضلة" option of the rider's rating form (F16): the driver is
/// added once the rating is sent.
class RatingFavoriteOption extends StatelessWidget {
  const RatingFavoriteOption({super.key, required this.state});

  final RatingState state;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final RatingCubit cubit = context.read<RatingCubit>();
    final String name = state.subject.counterpartName;
    final bool editing = state.status == RatingStatus.editing;
    return Container(
      key: const ValueKey<String>('rating-favorite-option'),
      padding: const EdgeInsets.all(AtaSpacing.sm),
      decoration: BoxDecoration(
        color: state.addToFavorites ? AtaColors.brandSoft : AtaColors.cloud,
        borderRadius: AtaRadii.itemRadius,
      ),
      child: Row(
        children: <Widget>[
          const AtaIcon(AtaIcons.heart, color: AtaColors.danger, filled: true),
          const SizedBox(width: AtaSpacing.sm),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: <Widget>[
                Text(l10n.favoriteAdd, style: AtaText.label),
                Text(
                  name.isEmpty
                      ? l10n.favoriteAddOptionCopyAnon
                      : l10n.favoriteAddOptionCopy(name),
                  style: AtaText.caption,
                ),
              ],
            ),
          ),
          AtaToggle(
            value: state.addToFavorites,
            onChanged: editing ? (_) => cubit.toggleAddToFavorites() : null,
          ),
        ],
      ),
    );
  }
}

/// Thank-you notice of the favourites option: added, or why it failed.
class RatingFavoriteNotice extends StatelessWidget {
  const RatingFavoriteNotice({super.key, required this.state});

  final RatingState state;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    switch (state.favoriteOutcome) {
      case RatingFavoriteOutcome.none:
        return const SizedBox.shrink();
      case RatingFavoriteOutcome.added:
        return Text(
          l10n.favoriteRatingAdded,
          key: const ValueKey<String>('rating-favorite-added'),
          style: AtaText.label.copyWith(color: AtaColors.brand),
          textAlign: TextAlign.center,
        );
      case RatingFavoriteOutcome.failed:
        return Text(
          l10n.favoriteRatingFailed(failureText(state.favoriteFailure!, l10n)),
          key: const ValueKey<String>('rating-favorite-failed'),
          style: AtaText.label.copyWith(color: AtaColors.warning),
          textAlign: TextAlign.center,
        );
    }
  }
}
