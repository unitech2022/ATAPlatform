import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/page_wrap.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/design/widgets/screen_title.dart';
import 'package:ata_app/features/driver_wallet/presentation/widgets/driver_subpage.dart';
import 'package:ata_app/features/rating/domain/entities/pending_rating.dart';
import 'package:ata_app/features/rating/domain/entities/rating_subject.dart';
import 'package:ata_app/features/rating/presentation/cubit/pending_rating_cubit.dart';
import 'package:ata_app/features/rating/presentation/cubit/rating_cubit.dart';
import 'package:ata_app/features/rating/presentation/cubit/rating_state.dart';
import 'package:ata_app/features/rating/presentation/widgets/rating_form.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// `/rate/:tripId` (rider) and `/driver/rate/:tripId` (driver), opened by
/// the `rating.reminder` push (`ata://rate/{tripId}`).
class RateTripPage extends StatelessWidget {
  const RateTripPage({super.key, required this.tripId, required this.rater});

  final String tripId;
  final TripActor rater;

  String get _home =>
      rater == TripActor.driver ? AppRoutes.driver : AppRoutes.home;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final PendingRatingCubit pending = context.read<PendingRatingCubit>();
    final Widget form = BlocProvider<RatingCubit>(
      create: (_) => RatingCubit(
        subject: RatingSubject(
          tripId: tripId,
          rater: rater,
          counterpartName: _nameFrom(pending.state.pending),
        ),
        getTags: getIt(),
        submitRating: getIt(),
        addFavorite: getIt(),
      )..loadTags(),
      child: BlocListener<RatingCubit, RatingState>(
        listenWhen: (RatingState p, RatingState c) =>
            !p.isFinished && c.isFinished,
        listener: (_, _) => pending.markRated(tripId),
        child: AtaCard(
          padding: const EdgeInsets.all(AtaSpacing.lg),
          child: RatingForm(onDone: () => context.go(_home)),
        ),
      ),
    );
    if (rater == TripActor.driver) {
      return DriverSubpage.content(
        eyebrow: l10n.driverAccountEyebrow,
        title: l10n.rateTrip,
        copy: l10n.rateTripCopy,
        children: <Widget>[form],
      );
    }
    return PageWrap(
      children: <Widget>[
        Align(
          alignment: AlignmentDirectional.centerStart,
          child: PillButton.back(
            label: l10n.back,
            onTap: () => context.canPop() ? context.pop() : context.go(_home),
          ),
        ),
        const SizedBox(height: AtaSpacing.xl),
        ScreenTitle(
          eyebrow: l10n.ridesEyebrow,
          title: l10n.rateTrip,
          copy: l10n.rateTripCopy,
        ),
        const SizedBox(height: AtaSpacing.xl),
        form,
      ],
    );
  }

  String _nameFrom(List<PendingRating> pending) {
    for (final PendingRating p in pending) {
      if (p.tripId == tripId) return p.counterpartName;
    }
    return '';
  }
}
