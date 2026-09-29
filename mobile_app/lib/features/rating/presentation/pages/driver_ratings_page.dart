import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/widgets/failure_view.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/features/driver_wallet/presentation/widgets/driver_subpage.dart';
import 'package:ata_app/features/rating/presentation/cubit/rating_summary_cubit.dart';
import 'package:ata_app/features/rating/presentation/cubit/rating_summary_state.dart';
import 'package:ata_app/features/rating/presentation/widgets/rating_summary_view.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// `/driver/ratings`: the driver's own rating summary (anonymous).
class DriverRatingsPage extends StatelessWidget {
  const DriverRatingsPage({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocProvider<RatingSummaryCubit>(
      create: (_) =>
          RatingSummaryCubit(getSummary: getIt(), role: TripActor.driver)
            ..load(),
      child: DriverSubpage.content(
        eyebrow: l10n.driverAccountEyebrow,
        title: l10n.driverRatingsTitle,
        copy: l10n.driverRatingsCopy,
        children: <Widget>[
          BlocBuilder<RatingSummaryCubit, RatingSummaryState>(
            builder: (BuildContext context, RatingSummaryState state) {
              if (state.summary != null) {
                return RatingSummaryView(summary: state.summary!);
              }
              if (state.failure != null) {
                return FailureView(
                  failure: state.failure!,
                  onRetry: context.read<RatingSummaryCubit>().load,
                );
              }
              return const CenteredLoader();
            },
          ),
        ],
      ),
    );
  }
}
