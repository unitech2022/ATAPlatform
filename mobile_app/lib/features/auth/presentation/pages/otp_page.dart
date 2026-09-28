import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/env/env.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/design/widgets/keypad.dart';
import 'package:ata_app/design/widgets/otp_boxes.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/features/auth/presentation/cubit/otp_cubit.dart';
import 'package:ata_app/features/auth/presentation/cubit/otp_state.dart';
import 'package:ata_app/features/auth/presentation/cubit/session_cubit.dart';
import 'package:ata_app/features/auth/presentation/pages/otp_page_args.dart';
import 'package:ata_app/features/auth/presentation/widgets/auth_panel.dart';
import 'package:ata_app/features/auth/presentation/widgets/auth_scaffold.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// 4-digit code entry with resend countdown.
class OtpPage extends StatelessWidget {
  const OtpPage({super.key, required this.args});

  final OtpPageArgs args;

  @override
  Widget build(BuildContext context) {
    return BlocProvider<OtpCubit>(
      create: (_) => OtpCubit(
        verifyOtp: getIt(),
        requestOtp: getIt(),
        request: args.request,
        role: args.role,
        language: context.localeCode,
      ),
      child: BlocListener<OtpCubit, OtpState>(
        listenWhen: (OtpState previous, OtpState current) =>
            previous.session != current.session && current.session != null,
        listener: (BuildContext context, OtpState state) =>
            context.read<SessionCubit>().signedIn(state.session!),
        child: AuthScaffold(
          onBack: () => Navigator.of(context).pop(),
          child: const _OtpPanel(),
        ),
      ),
    );
  }
}

class _OtpPanel extends StatelessWidget {
  const _OtpPanel();

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final OtpCubit cubit = context.read<OtpCubit>();
    return BlocBuilder<OtpCubit, OtpState>(
      builder: (BuildContext context, OtpState state) {
        return AuthPanel(
          centered: true,
          children: <Widget>[
            const IconBox(
              icon: AtaIcons.shield,
              size: AtaSizes.iconBoxLarge,
              iconSize: AtaSizes.iconLarge,
              radius: AtaSpacing.md,
            ),
            const SizedBox(height: AtaSpacing.xl),
            Text(
              l10n.otpTitle,
              style: AtaText.title,
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: AtaSpacing.sm),
            Text(
              l10n.otpCopy,
              style: AtaText.bodyMuted,
              textAlign: TextAlign.center,
            ),
            Text(
              state.phoneNumber,
              style: AtaText.bodyStrong,
              textDirection: TextDirection.ltr,
              textAlign: TextAlign.center,
            ),
            if (Env.showDevOtp && state.devCode != null) ...<Widget>[
              const SizedBox(height: AtaSpacing.sm),
              AtaBadge(label: l10n.otpDevHint(state.devCode!)),
            ],
            const SizedBox(height: AtaSpacing.xl),
            OtpBoxes(code: state.code),
            const SizedBox(height: AtaSpacing.xl),
            Keypad(
              onDigit: cubit.addDigit,
              onDelete: cubit.deleteDigit,
              deleteLabel: l10n.keypadDelete,
              enabled: !state.verifying,
            ),
            if (state.failure != null) ...<Widget>[
              const SizedBox(height: AtaSpacing.md),
              InlineError(message: failureText(state.failure!, l10n)),
            ],
            const SizedBox(height: AtaSpacing.xl),
            AtaButton(
              label: l10n.otpVerify,
              onPressed: state.canVerify ? cubit.verify : null,
              loading: state.verifying,
            ),
            const SizedBox(height: AtaSpacing.lg),
            TextButton(
              onPressed: state.canResend ? cubit.resend : null,
              child: Text(
                state.secondsLeft > 0
                    ? l10n.otpResendIn(state.secondsLeft)
                    : l10n.otpResend,
                style: AtaText.label.copyWith(
                  color: state.canResend ? AtaColors.brand : AtaColors.muted,
                ),
              ),
            ),
          ],
        );
      },
    );
  }
}
