import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/features/trip/presentation/cubit/location_stream_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/location_stream_state.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Explains a denied / disabled location permission with a retry.
class LocationNotice extends StatelessWidget {
  const LocationNotice({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocBuilder<LocationStreamCubit, LocationStreamState>(
      buildWhen: (LocationStreamState p, LocationStreamState c) =>
          p.status != c.status,
      builder: (BuildContext context, LocationStreamState state) {
        final String? message = switch (state.status) {
          LocationStreamStatus.denied => l10n.locationDenied,
          LocationStreamStatus.deniedForever => l10n.locationDeniedForever,
          LocationStreamStatus.serviceDisabled => l10n.locationServiceDisabled,
          _ => null,
        };
        if (message == null) return const SizedBox.shrink();
        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: <Widget>[
            const SizedBox(height: AtaSpacing.sm),
            InlineError(message: message),
            const SizedBox(height: AtaSpacing.xs),
            AtaButton(
              label: l10n.retry,
              variant: AtaButtonVariant.outline,
              height: AtaSizes.buttonCompact,
              onPressed: () {
                final LocationStreamCubit cubit = context
                    .read<LocationStreamCubit>();
                cubit.stop().then((_) => cubit.start());
              },
            ),
          ],
        );
      },
    );
  }
}
