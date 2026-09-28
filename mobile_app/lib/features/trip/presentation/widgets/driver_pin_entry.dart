import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/keypad.dart';
import 'package:ata_app/design/widgets/otp_boxes.dart';
import 'package:ata_app/features/trip/presentation/cubit/driver_trip_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/driver_trip_state.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Passenger PIN entry for the driver: 4 boxes, keypad and verify.
class DriverPinEntry extends StatelessWidget {
  const DriverPinEntry({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final DriverTripCubit cubit = context.read<DriverTripCubit>();
    return BlocBuilder<DriverTripCubit, DriverTripState>(
      buildWhen: (DriverTripState p, DriverTripState c) =>
          p.pin != c.pin || p.busy != c.busy,
      builder: (BuildContext context, DriverTripState state) {
        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: <Widget>[
            Text(
              l10n.enterPinTitle,
              style: AtaText.section,
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: AtaSpacing.xxs),
            Text(
              l10n.enterPinCopy,
              style: AtaText.small,
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: AtaSpacing.md),
            OtpBoxes(code: state.pin, length: DriverTripState.pinLength),
            const SizedBox(height: AtaSpacing.md),
            Keypad(
              onDigit: cubit.addPinDigit,
              onDelete: cubit.deletePinDigit,
              deleteLabel: l10n.keypadDelete,
              enabled: !state.busy,
            ),
            const SizedBox(height: AtaSpacing.md),
            AtaButton(
              label: l10n.verifyPinAction,
              loading: state.busy,
              onPressed: state.isPinComplete ? cubit.submitPin : null,
            ),
          ],
        );
      },
    );
  }
}
