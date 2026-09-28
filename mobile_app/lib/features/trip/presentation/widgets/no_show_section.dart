import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/presentation/cubit/driver_trip_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/location_stream_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/no_show_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/no_show_state.dart';
import 'package:ata_app/features/trip/presentation/widgets/trip_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// "Passenger didn't show" on the driver waiting screen: disabled with a
/// countdown until the policy wait has passed, then confirm → result.
class NoShowSection extends StatelessWidget {
  const NoShowSection({super.key, required this.trip});

  final Trip trip;

  @override
  Widget build(BuildContext context) {
    return BlocProvider<NoShowCubit>(
      create: (_) => NoShowCubit(markNoShow: getIt())..start(trip),
      child: BlocConsumer<NoShowCubit, NoShowState>(
        listenWhen: (NoShowState p, NoShowState c) =>
            p.result == null && c.result != null,
        listener: (BuildContext context, NoShowState state) =>
            context.read<DriverTripCubit>().adopt(state.result!),
        builder: (BuildContext context, NoShowState state) {
          final AppLocalizations l10n = context.l10n;
          return Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: <Widget>[
              Text(
                state.secondsRemaining > 0
                    ? l10n.noShowAvailableIn(
                        TripText.clock(state.secondsRemaining),
                      )
                    : l10n.noShowAvailableNow,
                style: AtaText.caption,
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: AtaSpacing.xs),
              AtaButton(
                label: l10n.noShowAction,
                variant: AtaButtonVariant.outline,
                height: AtaSizes.buttonCompact,
                loading: state.busy,
                onPressed: state.canMarkNoShow ? () => _confirm(context) : null,
              ),
              if (state.failure != null) ...<Widget>[
                const SizedBox(height: AtaSpacing.xs),
                InlineError(message: failureText(state.failure!, l10n)),
              ],
            ],
          );
        },
      ),
    );
  }

  Future<void> _confirm(BuildContext context) async {
    final AppLocalizations l10n = context.l10n;
    final NoShowCubit cubit = context.read<NoShowCubit>();
    final LocationStreamCubit location = context.read<LocationStreamCubit>();
    final bool? ok = await showDialog<bool>(
      context: context,
      builder: (BuildContext dialog) => AlertDialog(
        title: Text(l10n.noShowConfirmTitle),
        content: Text(l10n.noShowConfirmCopy),
        actions: <Widget>[
          TextButton(
            onPressed: () => Navigator.of(dialog).pop(false),
            child: Text(l10n.keepWaiting),
          ),
          TextButton(
            onPressed: () => Navigator.of(dialog).pop(true),
            child: Text(l10n.noShowAction),
          ),
        ],
      ),
    );
    if (ok ?? false) {
      await cubit.markNoShow(at: location.state.position?.point);
    }
  }
}
