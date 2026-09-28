import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/dialer.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_shadows.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/features/safety/domain/entities/sos.dart';
import 'package:ata_app/features/safety/presentation/cubit/sos_cubit.dart';
import 'package:ata_app/features/safety/presentation/cubit/sos_state.dart';
import 'package:ata_app/features/safety/presentation/widgets/safety_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Status of a raised SOS: case number and status, contacts notified,
/// "call 911" and "pressed by mistake". Hidden while no SOS exists.
class SosPanel extends StatelessWidget {
  const SosPanel({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<SosCubit, SosState>(
      builder: (BuildContext context, SosState state) {
        if (!state.isVisible) return const SizedBox.shrink();
        return _Panel(state: state);
      },
    );
  }
}

class _Panel extends StatelessWidget {
  const _Panel({required this.state});

  final SosState state;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final SosCubit cubit = context.read<SosCubit>();
    final SosResult? result = state.result;
    final String title = switch (state.status) {
      SosStatus.sending => l10n.sosSending,
      SosStatus.cancelled => l10n.sosCancelledTitle,
      _ when state.failure != null => l10n.sosFailedTitle,
      _ => l10n.sosActiveTitle,
    };
    return Container(
      padding: const EdgeInsets.all(AtaSpacing.md),
      decoration: BoxDecoration(
        color: AtaColors.dangerSoft,
        borderRadius: AtaRadii.itemRadius,
        border: Border.all(color: AtaColors.danger),
        boxShadow: AtaShadows.soft,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Text(
            title,
            style: AtaText.bodyStrong.copyWith(color: AtaColors.danger),
          ),
          if (result != null) ...<Widget>[
            const SizedBox(height: AtaSpacing.xxs),
            Text(
              l10n.sosCaseLine(
                result.caseNumber,
                SafetyText.caseStatus(l10n, result.status),
              ),
              style: AtaText.small.copyWith(color: AtaColors.ink),
            ),
            if (result.contactsNotified > 0)
              Text(
                l10n.sosContactsNotified(result.contactsNotified),
                style: AtaText.caption,
              ),
            if (state.isActive)
              Text(l10n.sosSharingLocation, style: AtaText.caption),
          ],
          if (state.status == SosStatus.cancelled)
            Text(l10n.sosCancelledCopy, style: AtaText.caption),
          if (state.failure != null)
            Text(
              failureText(state.failure!, l10n),
              style: AtaText.caption.copyWith(color: AtaColors.danger),
            ),
          const SizedBox(height: AtaSpacing.sm),
          AtaButton(
            label: l10n.callEmergency(state.emergencyNumber),
            icon: AtaIcons.phone,
            variant: AtaButtonVariant.danger,
            height: AtaSizes.buttonCompact,
            onPressed: () => dialNumber(
              context,
              state.emergencyNumber,
              failedText: l10n.callFailed,
            ),
          ),
          const SizedBox(height: AtaSpacing.xs),
          if (state.isActive || state.status == SosStatus.cancelling)
            AtaButton(
              label: l10n.sosPressedByMistake,
              variant: AtaButtonVariant.outline,
              height: AtaSizes.buttonCompact,
              loading: state.status == SosStatus.cancelling,
              onPressed: state.isActive ? cubit.cancel : null,
            )
          else if (!state.isBusy)
            AtaButton(
              label: l10n.close,
              variant: AtaButtonVariant.soft,
              height: AtaSizes.buttonCompact,
              onPressed: cubit.dismiss,
            ),
        ],
      ),
    );
  }
}
