import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/features/payments/domain/entities/saved_card.dart';
import 'package:ata_app/features/payments/presentation/widgets/payment_text.dart';
import 'package:ata_app/features/wallet/presentation/cubit/top_up_cubit.dart';
import 'package:ata_app/features/wallet/presentation/cubit/top_up_state.dart';
import 'package:ata_app/features/wallet/presentation/widgets/payment_method_tile.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// Saved cards plus the sandbox option; riders can add a card inline.
class TopUpSourcePicker extends StatelessWidget {
  const TopUpSourcePicker({
    super.key,
    required this.state,
    required this.allowAddCard,
  });

  final TopUpState state;
  final bool allowAddCard;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final TopUpCubit cubit = context.read<TopUpCubit>();
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        Text(l10n.topUpSource, style: AtaText.label),
        const SizedBox(height: AtaSpacing.sm),
        for (final SavedCard card in state.cards) ...<Widget>[
          PaymentMethodTile(
            title: PaymentText.card(l10n, card),
            subtitle: l10n.cardExpiry(card.expiryLabel),
            selected: state.cardId == card.id,
            onTap: () => cubit.selectCard(card.id),
          ),
          const SizedBox(height: AtaSpacing.xs),
        ],
        PaymentMethodTile(
          title: l10n.sandboxMethod,
          subtitle: l10n.sandboxCopy,
          selected: !state.usesCard,
          onTap: () => cubit.selectCard(null),
        ),
        if (allowAddCard)
          Align(
            alignment: AlignmentDirectional.centerStart,
            child: TextButton(
              onPressed: () => _addCard(context),
              child: Text(
                l10n.addCard,
                style: AtaText.captionStrong.copyWith(color: AtaColors.brand),
              ),
            ),
          ),
      ],
    );
  }

  Future<void> _addCard(BuildContext context) async {
    final TopUpCubit cubit = context.read<TopUpCubit>();
    final bool? added = await context.push<bool>(AppRoutes.walletAddCard);
    if (added ?? false) await cubit.loadCards();
  }
}
