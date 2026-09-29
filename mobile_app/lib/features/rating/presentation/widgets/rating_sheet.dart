import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/widgets/sheet_handle.dart';
import 'package:ata_app/features/rating/domain/entities/rating_subject.dart';
import 'package:ata_app/features/rating/presentation/cubit/pending_rating_cubit.dart';
import 'package:ata_app/features/rating/presentation/cubit/rating_cubit.dart';
import 'package:ata_app/features/rating/presentation/widgets/rating_form.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Bottom sheet with the [RatingForm] of the [RatingCubit] above it.
class RatingSheet extends StatelessWidget {
  const RatingSheet({super.key});

  /// Opens the sheet for [subject]; resolves to `true` when nothing is left
  /// to rate (sent, already rated or window closed) and records it in the
  /// app-wide [PendingRatingCubit].
  static Future<bool> show(
    BuildContext context, {
    required RatingSubject subject,
  }) async {
    final PendingRatingCubit pending = context.read<PendingRatingCubit>();
    final RatingCubit cubit = RatingCubit(
      subject: subject,
      getTags: getIt(),
      submitRating: getIt(),
    )..loadTags();
    await showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      builder: (_) => BlocProvider<RatingCubit>.value(
        value: cubit,
        child: const RatingSheet(),
      ),
    );
    final bool finished = cubit.state.isFinished;
    await cubit.close();
    if (finished) pending.markRated(subject.tripId);
    return finished;
  }

  @override
  Widget build(BuildContext context) {
    final EdgeInsets insets = MediaQuery.viewInsetsOf(context);
    return SingleChildScrollView(
      padding: EdgeInsets.fromLTRB(
        AtaSpacing.lg,
        AtaSpacing.lg,
        AtaSpacing.lg,
        AtaSpacing.lg + insets.bottom + MediaQuery.paddingOf(context).bottom,
      ),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          const SheetHandle(),
          RatingForm(onDone: () => Navigator.of(context).pop()),
        ],
      ),
    );
  }
}
