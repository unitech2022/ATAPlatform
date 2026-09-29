import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/features/favorite_drivers/presentation/cubit/add_favorite_cubit.dart';
import 'package:ata_app/features/favorite_drivers/presentation/cubit/add_favorite_state.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// "أضف إلى المفضلة" for the driver of a completed trip: a full button
/// (receipt, end-of-trip view) or a heart icon ([compact], rides history).
class AddFavoriteButton extends StatelessWidget {
  const AddFavoriteButton({
    super.key,
    required this.tripId,
    this.compact = false,
  });

  final String tripId;
  final bool compact;

  @override
  Widget build(BuildContext context) {
    return BlocProvider<AddFavoriteCubit>(
      create: (_) => AddFavoriteCubit(tripId: tripId, addFavorite: getIt()),
      child: BlocConsumer<AddFavoriteCubit, AddFavoriteState>(
        listenWhen: (AddFavoriteState p, AddFavoriteState c) =>
            compact && c.status == AddFavoriteStatus.failed,
        listener: (BuildContext context, AddFavoriteState state) =>
            ScaffoldMessenger.maybeOf(context)?.showSnackBar(
              SnackBar(
                content: Text(failureText(state.failure!, context.l10n)),
              ),
            ),
        builder: (BuildContext context, AddFavoriteState state) =>
            compact ? _Heart(state: state) : _Full(state: state),
      ),
    );
  }
}

String _label(AppLocalizations l10n, AddFavoriteState state) =>
    switch (state.status) {
      AddFavoriteStatus.adding => l10n.favoriteAdding,
      AddFavoriteStatus.added => l10n.favoriteAdded,
      AddFavoriteStatus.alreadyFavorite => l10n.favoriteAlready,
      _ => l10n.favoriteAdd,
    };

class _Full extends StatelessWidget {
  const _Full({required this.state});

  final AddFavoriteState state;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        AtaButton(
          key: const ValueKey<String>('add-favorite-button'),
          label: _label(l10n, state),
          icon: state.isFavorite ? AtaIcons.check : AtaIcons.heart,
          variant: AtaButtonVariant.soft,
          height: AtaSizes.buttonCompact,
          loading: state.isAdding,
          onPressed: state.isFavorite || state.isAdding
              ? null
              : context.read<AddFavoriteCubit>().add,
        ),
        if (state.failure != null) ...<Widget>[
          const SizedBox(height: AtaSpacing.xs),
          InlineError(message: failureText(state.failure!, l10n)),
        ],
      ],
    );
  }
}

class _Heart extends StatelessWidget {
  const _Heart({required this.state});

  final AddFavoriteState state;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return IconButton(
      key: const ValueKey<String>('add-favorite-heart'),
      tooltip: _label(l10n, state),
      visualDensity: VisualDensity.compact,
      onPressed: state.isFavorite || state.isAdding
          ? null
          : context.read<AddFavoriteCubit>().add,
      icon: AtaIcon(
        AtaIcons.heart,
        color: state.isFavorite ? AtaColors.danger : AtaColors.muted,
        filled: state.isFavorite,
      ),
    );
  }
}
