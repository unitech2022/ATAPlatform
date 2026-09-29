import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/features/rating/domain/entities/rating_subject.dart';
import 'package:ata_app/features/rating/presentation/cubit/pending_rating_cubit.dart';
import 'package:ata_app/features/rating/presentation/cubit/pending_rating_state.dart';
import 'package:ata_app/features/rating/presentation/widgets/rating_sheet.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// "قيّم الرحلة" on the end-of-trip views: opens the rating sheet while the
/// 72 h window is open, "rated" once done (from the trip or this session).
class TripRateButton extends StatelessWidget {
  const TripRateButton({
    super.key,
    required this.trip,
    required this.rater,
    DateTime Function()? now,
  }) : _now = now ?? DateTime.now;

  final Trip trip;
  final TripActor rater;
  final DateTime Function() _now;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocSelector<PendingRatingCubit, PendingRatingState, bool>(
      selector: (PendingRatingState s) => s.isRated(trip.id),
      builder: (BuildContext context, bool ratedNow) {
        final bool rated = ratedNow || trip.rating.isRated;
        final bool open = trip.rating.canRateAt(
          _now(),
          completedAt: trip.timeline.completedAt,
        );
        if (!rated && !open) return const SizedBox.shrink();
        return AtaButton(
          key: const ValueKey<String>('trip-rate-button'),
          label: rated
              ? l10n.tripRated
              : rater == TripActor.driver
              ? l10n.ratePassenger
              : l10n.rateTrip,
          icon: rated ? AtaIcons.check : AtaIcons.star,
          variant: AtaButtonVariant.brand,
          onPressed: rated
              ? null
              : () => RatingSheet.show(context, subject: _subject()),
        );
      },
    );
  }

  RatingSubject _subject() => RatingSubject(
    tripId: trip.id,
    rater: rater,
    counterpartName: rater == TripActor.driver
        ? trip.passenger?.firstName ?? ''
        : (trip.driver?.fullName ?? '').split(' ').first,
  );
}
