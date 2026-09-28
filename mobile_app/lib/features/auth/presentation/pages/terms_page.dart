import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/design/widgets/screen_title.dart';
import 'package:ata_app/design/widgets/selectable_tile.dart';
import 'package:ata_app/features/auth/presentation/cubit/session_cubit.dart';
import 'package:ata_app/features/auth/presentation/cubit/terms_cubit.dart';
import 'package:ata_app/features/auth/presentation/cubit/terms_state.dart';
import 'package:ata_app/features/auth/presentation/widgets/auth_panel.dart';
import 'package:ata_app/features/auth/presentation/widgets/auth_scaffold.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// First-time rider: full name and terms acceptance.
class TermsPage extends StatelessWidget {
  const TermsPage({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocProvider<TermsCubit>(
      create: (_) => TermsCubit(completeProfile: getIt()),
      child: BlocListener<TermsCubit, TermsState>(
        listenWhen: (TermsState previous, TermsState current) =>
            previous.user != current.user && current.user != null,
        listener: (BuildContext context, TermsState state) =>
            context.read<SessionCubit>().updateUser(state.user!),
        child: AuthScaffold(
          onBack: () => context.read<SessionCubit>().signOut(),
          child: const _TermsPanel(),
        ),
      ),
    );
  }
}

class _TermsPanel extends StatelessWidget {
  const _TermsPanel();

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final TermsCubit cubit = context.read<TermsCubit>();
    return BlocBuilder<TermsCubit, TermsState>(
      builder: (BuildContext context, TermsState state) {
        return AuthPanel(
          children: <Widget>[
            const IconBox(
              icon: AtaIcons.user,
              size: AtaSizes.iconBoxLarge,
              iconSize: AtaSizes.iconLarge,
              radius: AtaSpacing.md,
            ),
            const SizedBox(height: AtaSpacing.xl),
            ScreenTitle(
              eyebrow: l10n.termsEyebrow,
              title: l10n.termsTitle,
              copy: l10n.termsCopy,
            ),
            const SizedBox(height: AtaSpacing.xl),
            Text(l10n.fullNameLabel, style: AtaText.label),
            const SizedBox(height: AtaSpacing.xs),
            TextField(
              onChanged: cubit.nameChanged,
              enabled: !state.submitting,
              textInputAction: TextInputAction.done,
              textCapitalization: TextCapitalization.words,
              style: AtaText.bodyStrong,
              decoration: InputDecoration(hintText: l10n.fullNameHint),
            ),
            const SizedBox(height: AtaSpacing.md),
            SelectableTile(
              selected: state.accepted,
              onTap: () => cubit.acceptedChanged(!state.accepted),
              child: Row(
                children: <Widget>[
                  _CheckCircle(checked: state.accepted),
                  const SizedBox(width: AtaSpacing.sm),
                  Expanded(
                    child: Text(l10n.acceptTermsLabel, style: AtaText.label),
                  ),
                ],
              ),
            ),
            if (state.failure != null) ...<Widget>[
              const SizedBox(height: AtaSpacing.md),
              InlineError(message: failureText(state.failure!, l10n)),
            ],
            const SizedBox(height: AtaSpacing.xl),
            AtaButton(
              label: l10n.termsContinue,
              onPressed: state.canSubmit ? cubit.submit : null,
              loading: state.submitting,
            ),
          ],
        );
      },
    );
  }
}

/// Round check indicator (brand when checked).
class _CheckCircle extends StatelessWidget {
  const _CheckCircle({required this.checked});

  final bool checked;

  static const double _size = 24;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: _size,
      height: _size,
      alignment: Alignment.center,
      decoration: BoxDecoration(
        color: checked ? AtaColors.brand : AtaColors.white,
        shape: BoxShape.circle,
        border: Border.all(
          color: checked ? AtaColors.brand : AtaColors.line,
          width: AtaSizes.borderThick,
        ),
      ),
      child: checked
          ? const AtaIcon(
              AtaIcons.check,
              size: AtaSizes.iconSmall,
              color: AtaColors.white,
            )
          : null,
    );
  }
}
