import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/dialer.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_shadows.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/features/safety/domain/entities/safety_alert.dart';
import 'package:ata_app/features/safety/domain/entities/sos.dart';
import 'package:ata_app/features/safety/presentation/cubit/safety_check_cubit.dart';
import 'package:ata_app/features/safety/presentation/cubit/safety_check_state.dart';
import 'package:ata_app/features/safety/presentation/widgets/safety_text.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// "هل أنت بخير؟" card: I'm OK / I need help, countdown to `respondBy`,
/// then the outcome. Hidden while no check is pending.
class SafetyCheckPrompt extends StatelessWidget {
  const SafetyCheckPrompt({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<SafetyCheckCubit, SafetyCheckState>(
      builder: (BuildContext context, SafetyCheckState state) {
        final SafetyAlert? alert = state.alert;
        if (!state.isShown || alert == null) return const SizedBox.shrink();
        return _Card(state: state, alert: alert);
      },
    );
  }
}

class _Card extends StatelessWidget {
  const _Card({required this.state, required this.alert});

  final SafetyCheckState state;
  final SafetyAlert alert;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final SafetyCheckCubit cubit = context.read<SafetyCheckCubit>();
    final bool needHelp = state.response == SafetyCheckResponse.needHelp;
    final String? outcome = switch (state.status) {
      SafetyCheckStatus.answered =>
        needHelp ? l10n.safetyCheckHelpSent : l10n.safetyCheckOkThanks,
      SafetyCheckStatus.expired => l10n.safetyCheckExpired,
      _ => null,
    };
    return Container(
      padding: const EdgeInsets.all(AtaSpacing.md),
      decoration: BoxDecoration(
        color: AtaColors.white,
        borderRadius: AtaRadii.itemRadius,
        border: Border.all(color: AtaColors.warning),
        boxShadow: AtaShadows.panel,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Text(l10n.safetyCheckTitle, style: AtaText.section),
          const SizedBox(height: AtaSpacing.xxs),
          Text(
            outcome ?? SafetyText.alertCopy(l10n, alert.type),
            style: AtaText.small,
          ),
          if (outcome == null && state.secondsLeft != null)
            Text(
              l10n.safetyCheckCountdown(state.secondsLeft!),
              style: AtaText.caption.copyWith(color: AtaColors.warning),
            ),
          if (state.failure != null)
            Text(
              failureText(state.failure!, l10n),
              style: AtaText.caption.copyWith(color: AtaColors.danger),
            ),
          const SizedBox(height: AtaSpacing.sm),
          if (outcome == null)
            Row(
              children: <Widget>[
                Expanded(
                  child: AtaButton(
                    label: l10n.safetyCheckOk,
                    variant: AtaButtonVariant.brand,
                    height: AtaSizes.buttonCompact,
                    loading:
                        state.status == SafetyCheckStatus.responding &&
                        !needHelp,
                    onPressed: state.canRespond
                        ? () => cubit.respond(SafetyCheckResponse.ok)
                        : null,
                  ),
                ),
                const SizedBox(width: AtaSpacing.sm),
                Expanded(
                  child: AtaButton(
                    label: l10n.safetyCheckHelp,
                    variant: AtaButtonVariant.danger,
                    height: AtaSizes.buttonCompact,
                    loading:
                        state.status == SafetyCheckStatus.responding &&
                        needHelp,
                    onPressed: state.canRespond
                        ? () => cubit.respond(SafetyCheckResponse.needHelp)
                        : null,
                  ),
                ),
              ],
            )
          else ...<Widget>[
            if (needHelp ||
                state.status == SafetyCheckStatus.expired) ...<Widget>[
              AtaButton(
                label: l10n.callEmergency(SosResult.defaultEmergencyNumber),
                variant: AtaButtonVariant.danger,
                height: AtaSizes.buttonCompact,
                onPressed: () => dialNumber(
                  context,
                  SosResult.defaultEmergencyNumber,
                  failedText: l10n.callFailed,
                ),
              ),
              const SizedBox(height: AtaSpacing.xs),
            ],
            AtaButton(
              label: l10n.close,
              variant: AtaButtonVariant.soft,
              height: AtaSizes.buttonCompact,
              onPressed: cubit.dismiss,
            ),
          ],
        ],
      ),
    );
  }
}
