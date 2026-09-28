import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_shadows.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/design/widgets/page_wrap.dart';
import 'package:ata_app/features/account/presentation/cubit/delete_account_cubit.dart';
import 'package:ata_app/features/account/presentation/cubit/delete_account_state.dart';
import 'package:ata_app/features/auth/presentation/cubit/session_cubit.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// Confirmation before `DELETE /me`.
class DeleteAccountPage extends StatelessWidget {
  const DeleteAccountPage({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocProvider<DeleteAccountCubit>(
      create: (_) => DeleteAccountCubit(deleteAccount: getIt()),
      child: BlocListener<DeleteAccountCubit, DeleteAccountState>(
        listenWhen: (DeleteAccountState p, DeleteAccountState c) =>
            !p.deleted && c.deleted,
        listener: (BuildContext context, _) =>
            context.read<SessionCubit>().expire(),
        child: const PageWrap(center: true, children: <Widget>[_DeletePanel()]),
      ),
    );
  }
}

class _DeletePanel extends StatelessWidget {
  const _DeletePanel();

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocBuilder<DeleteAccountCubit, DeleteAccountState>(
      builder: (BuildContext context, DeleteAccountState state) {
        return AtaCard(
          shadow: AtaShadows.panel,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: <Widget>[
              const Center(
                child: IconBox(
                  icon: AtaIcons.document,
                  size: AtaSizes.iconBoxHero,
                  iconSize: AtaSizes.iconHero,
                  background: AtaColors.dangerSoft,
                  foreground: AtaColors.danger,
                  round: true,
                ),
              ),
              const SizedBox(height: AtaSpacing.xl),
              Text(
                l10n.deleteTitle,
                style: AtaText.title,
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: AtaSpacing.md),
              Text(
                l10n.deleteCopy,
                style: AtaText.bodyMuted,
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: AtaSpacing.xl),
              Container(
                padding: const EdgeInsets.all(AtaSpacing.md),
                decoration: const BoxDecoration(
                  color: AtaColors.dangerSoft,
                  borderRadius: AtaRadii.itemRadius,
                ),
                child: Text(
                  l10n.deleteWarning,
                  style: AtaText.label.copyWith(color: AtaColors.danger),
                  textAlign: TextAlign.center,
                ),
              ),
              if (state.failure != null) ...<Widget>[
                const SizedBox(height: AtaSpacing.md),
                InlineError(message: failureText(state.failure!, l10n)),
              ],
              const SizedBox(height: AtaSpacing.xl),
              AtaButton(
                label: l10n.confirmDelete,
                variant: AtaButtonVariant.danger,
                onPressed: state.submitting
                    ? null
                    : context.read<DeleteAccountCubit>().confirm,
                loading: state.submitting,
              ),
              const SizedBox(height: AtaSpacing.sm),
              AtaButton(
                label: l10n.cancel,
                variant: AtaButtonVariant.soft,
                onPressed: () => context.go(AppRoutes.account),
              ),
            ],
          ),
        );
      },
    );
  }
}
