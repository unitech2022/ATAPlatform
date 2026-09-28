import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/core/widgets/failure_view.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/design/widgets/page_wrap.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/design/widgets/screen_title.dart';
import 'package:ata_app/features/wallet/domain/entities/wallet_summary.dart';
import 'package:ata_app/features/wallet/presentation/cubit/wallet_cubit.dart';
import 'package:ata_app/features/wallet/presentation/cubit/wallet_state.dart';
import 'package:ata_app/features/wallet/presentation/widgets/balance_card.dart';
import 'package:ata_app/features/wallet/presentation/widgets/payment_method_tile.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// Balance card, payment methods and the top-up entry point.
class WalletPage extends StatelessWidget {
  const WalletPage({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocProvider<WalletCubit>(
      create: (_) => WalletCubit(getWallet: getIt())..load(),
      child: PageWrap(
        children: <Widget>[
          ScreenTitle(
            eyebrow: l10n.walletEyebrow,
            title: l10n.walletTitle,
            copy: l10n.walletCopy,
          ),
          const SizedBox(height: AtaSpacing.xxl),
          BlocBuilder<WalletCubit, WalletState>(
            builder: (BuildContext context, WalletState state) {
              if (state.loading && state.wallet == null) {
                return const CenteredLoader();
              }
              if (state.failure != null && state.wallet == null) {
                return FailureView(
                  failure: state.failure!,
                  onRetry: context.read<WalletCubit>().load,
                );
              }
              return Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: <Widget>[
                  BalanceCard(balance: state.balance),
                  const SizedBox(height: AtaSpacing.xl),
                  _MethodsCard(state: state),
                ],
              );
            },
          ),
        ],
      ),
    );
  }
}

class _MethodsCard extends StatelessWidget {
  const _MethodsCard({required this.state});

  final WalletState state;

  static const String _cardDigits = '2841';

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final String balance = l10n.priceWithCurrency(Money.fixed(state.balance));
    final List<PaymentMethod> methods =
        state.wallet?.paymentMethods ?? const <PaymentMethod>[];
    final PaymentMethod? wallet = methods
        .where((PaymentMethod m) => m.type == 'wallet')
        .firstOrNull;
    final PaymentMethod? cash = methods
        .where((PaymentMethod m) => m.type == 'cash')
        .firstOrNull;
    return AtaCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: <Widget>[
              Text(l10n.paymentMethods, style: AtaText.section),
              PillButton(
                label: l10n.topUp,
                icon: AtaIcons.plus,
                background: AtaColors.brand,
                foreground: AtaColors.white,
                elevated: false,
                onTap: () => _openTopUp(context),
              ),
            ],
          ),
          const SizedBox(height: AtaSpacing.lg),
          PaymentMethodTile(
            title: wallet?.label ?? l10n.paymentWallet,
            subtitle: l10n.walletBalanceLine(balance),
            selected: true,
          ),
          const SizedBox(height: AtaSpacing.sm),
          PaymentMethodTile(
            title: l10n.madaCard,
            subtitle: l10n.cardEnding(_cardDigits),
            selected: false,
          ),
          const SizedBox(height: AtaSpacing.sm),
          PaymentMethodTile(
            title: cash?.label ?? l10n.payCash,
            subtitle: l10n.payCashCopy,
            selected: false,
          ),
        ],
      ),
    );
  }

  Future<void> _openTopUp(BuildContext context) async {
    final WalletCubit cubit = context.read<WalletCubit>();
    final double? newBalance = await context.push<double>(
      AppRoutes.walletTopUp,
    );
    if (newBalance != null) cubit.balanceChanged(newBalance);
  }
}
