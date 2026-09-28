import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/widgets/failure_view.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/design/widgets/page_wrap.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/design/widgets/screen_title.dart';
import 'package:ata_app/features/payments/domain/entities/saved_card.dart';
import 'package:ata_app/features/payments/presentation/cubit/payment_methods_cubit.dart';
import 'package:ata_app/features/payments/presentation/cubit/payment_methods_state.dart';
import 'package:ata_app/features/payments/presentation/widgets/payment_text.dart';
import 'package:ata_app/features/payments/presentation/widgets/saved_card_tile.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// `/wallet/payment-methods`: saved cards, default card, delete, add.
class PaymentMethodsPage extends StatelessWidget {
  const PaymentMethodsPage({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocProvider<PaymentMethodsCubit>(
      create: (_) => PaymentMethodsCubit(
        getPaymentMethods: getIt(),
        setDefault: getIt(),
        remove: getIt(),
      )..load(),
      child: PageWrap(
        children: <Widget>[
          Align(
            alignment: AlignmentDirectional.centerStart,
            child: PillButton.back(
              label: l10n.back,
              onTap: () => context.pop(),
            ),
          ),
          const SizedBox(height: AtaSpacing.xl),
          ScreenTitle(
            eyebrow: l10n.walletEyebrow,
            title: l10n.savedCardsTitle,
            copy: l10n.savedCardsCopy,
          ),
          const SizedBox(height: AtaSpacing.xxl),
          const _CardsCard(),
        ],
      ),
    );
  }
}

class _CardsCard extends StatelessWidget {
  const _CardsCard();

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return AtaCard(
      padding: const EdgeInsets.all(AtaSpacing.md),
      child: BlocBuilder<PaymentMethodsCubit, PaymentMethodsState>(
        builder: (BuildContext context, PaymentMethodsState state) {
          final PaymentMethodsCubit cubit = context.read<PaymentMethodsCubit>();
          if (state.loading && state.cards.isEmpty) {
            return const CenteredLoader();
          }
          if (state.failure != null && state.cards.isEmpty) {
            return FailureView(failure: state.failure!, onRetry: cubit.load);
          }
          return Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: <Widget>[
              if (state.isEmpty)
                Padding(
                  padding: const EdgeInsets.all(AtaSpacing.md),
                  child: Text(
                    l10n.savedCardsEmpty,
                    style: AtaText.small,
                    textAlign: TextAlign.center,
                  ),
                ),
              for (final SavedCard card in state.cards) ...<Widget>[
                SavedCardTile(
                  card: card,
                  busy: state.busyId == card.id,
                  onMakeDefault: () => cubit.setDefault(card.id),
                  onRemove: () => _confirmRemove(context, card),
                ),
                const SizedBox(height: AtaSpacing.sm),
              ],
              if (state.actionFailure != null) ...<Widget>[
                InlineError(message: failureText(state.actionFailure!, l10n)),
                const SizedBox(height: AtaSpacing.sm),
              ],
              AtaButton(
                label: l10n.addCard,
                icon: AtaIcons.plus,
                variant: AtaButtonVariant.soft,
                onPressed: () => _addCard(context),
              ),
            ],
          );
        },
      ),
    );
  }

  Future<void> _addCard(BuildContext context) async {
    final PaymentMethodsCubit cubit = context.read<PaymentMethodsCubit>();
    final bool? added = await context.push<bool>(AppRoutes.walletAddCard);
    if (added ?? false) await cubit.load();
  }

  Future<void> _confirmRemove(BuildContext context, SavedCard card) async {
    final PaymentMethodsCubit cubit = context.read<PaymentMethodsCubit>();
    final AppLocalizations l10n = context.l10n;
    final bool? confirmed = await showDialog<bool>(
      context: context,
      builder: (BuildContext dialogContext) => AlertDialog(
        title: Text(l10n.cardRemoveTitle, style: AtaText.section),
        content: Text(
          l10n.cardRemoveCopy(PaymentText.card(l10n, card)),
          style: AtaText.body,
        ),
        actions: <Widget>[
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(false),
            child: Text(l10n.cancel, style: AtaText.labelMuted),
          ),
          TextButton(
            onPressed: () => Navigator.of(dialogContext).pop(true),
            child: Text(
              l10n.cardRemove,
              style: AtaText.label.copyWith(color: AtaColors.danger),
            ),
          ),
        ],
      ),
    );
    if (confirmed ?? false) await cubit.remove(card.id);
  }
}
