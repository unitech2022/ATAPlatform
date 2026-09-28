import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_shadows.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/design/widgets/page_wrap.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/features/payments/presentation/cubit/add_card_cubit.dart';
import 'package:ata_app/features/payments/presentation/cubit/add_card_state.dart';
import 'package:ata_app/features/payments/presentation/widgets/card_form.dart';
import 'package:ata_app/features/payments/presentation/widgets/payment_action_view.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// `/wallet/payment-methods/add`: validates the card on the device,
/// tokenises it and saves only the token. Pops `true` once saved.
class AddCardPage extends StatelessWidget {
  const AddCardPage({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocProvider<AddCardCubit>(
      create: (_) => AddCardCubit(addPaymentMethod: getIt()),
      child: BlocListener<AddCardCubit, AddCardState>(
        listenWhen: (AddCardState p, AddCardState c) =>
            p.status != c.status && c.status == AddCardStatus.saved,
        listener: (BuildContext context, _) => context.pop(true),
        child: PageWrap(
          children: <Widget>[
            AtaCard(
              shadow: AtaShadows.panel,
              child: BlocBuilder<AddCardCubit, AddCardState>(
                builder: (BuildContext context, AddCardState state) =>
                    state.status == AddCardStatus.requiresAction
                    ? PaymentActionView(
                        action: state.action!,
                        onDone: () => context.pop(true),
                      )
                    : _FormView(state: state),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _FormView extends StatelessWidget {
  const _FormView({required this.state});

  final AddCardState state;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        Align(
          alignment: AlignmentDirectional.centerStart,
          child: PillButton.back(
            label: l10n.back,
            background: AtaColors.cloud,
            elevated: false,
            onTap: () => context.pop(),
          ),
        ),
        const SizedBox(height: AtaSpacing.xl),
        Text(l10n.addCardTitle, style: AtaText.title),
        const SizedBox(height: AtaSpacing.xs),
        Text(l10n.addCardCopy, style: AtaText.bodyMuted),
        const SizedBox(height: AtaSpacing.xl),
        CardForm(state: state),
        const SizedBox(height: AtaSpacing.sm),
        Text(l10n.sandboxCardsHint, style: AtaText.caption),
        if (state.failure != null) ...<Widget>[
          const SizedBox(height: AtaSpacing.md),
          InlineError(message: failureText(state.failure!, l10n)),
        ],
        const SizedBox(height: AtaSpacing.xl),
        AtaButton(
          label: l10n.saveCard,
          loading: state.submitting,
          onPressed: state.submitting
              ? null
              : context.read<AddCardCubit>().submit,
        ),
      ],
    );
  }
}
