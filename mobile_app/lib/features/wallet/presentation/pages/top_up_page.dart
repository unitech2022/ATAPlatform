import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/design/tokens/ata_shadows.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/page_wrap.dart';
import 'package:ata_app/features/payments/presentation/widgets/payment_action_view.dart';
import 'package:ata_app/features/wallet/domain/entities/top_up_params.dart';
import 'package:ata_app/features/wallet/presentation/cubit/top_up_cubit.dart';
import 'package:ata_app/features/wallet/presentation/cubit/top_up_state.dart';
import 'package:ata_app/features/wallet/presentation/widgets/top_up_choose_view.dart';
import 'package:ata_app/features/wallet/presentation/widgets/top_up_success_view.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Choose an amount and a source (saved card or sandbox), confirm, then see
/// the success screen (or the 3-D Secure hand-off). Pops with the new
/// balance. The driver variant settles the cash debt (`kind=driver`).
class TopUpPage extends StatelessWidget {
  const TopUpPage({
    super.key,
    this.kind = WalletKind.passenger,
    this.initialAmount,
  });

  final WalletKind kind;
  final double? initialAmount;

  bool get _isDriver => kind == WalletKind.driver;

  @override
  Widget build(BuildContext context) {
    return BlocProvider<TopUpCubit>(
      create: (_) => TopUpCubit(
        topUpWallet: getIt(),
        topUpWithMethod: getIt(),
        // Saved cards belong to the passenger account.
        getPaymentMethods: _isDriver ? null : getIt(),
        kind: kind,
        initialAmount: initialAmount,
      )..loadCards(),
      child: PageWrap(
        center: true,
        bottomPadding: _isDriver ? AtaSpacing.xxxl : PageWrap.navClearance,
        children: <Widget>[
          AtaCard(
            shadow: AtaShadows.panel,
            child: BlocBuilder<TopUpCubit, TopUpState>(
              builder: (BuildContext context, TopUpState state) =>
                  switch (state.step) {
                    TopUpStep.success => TopUpSuccessView(state: state),
                    TopUpStep.action => PaymentActionView(
                      action: state.action!,
                      onDone: () => Navigator.of(context).pop(),
                    ),
                    TopUpStep.choose => TopUpChooseView(
                      state: state,
                      isDriver: _isDriver,
                    ),
                  },
            ),
          ),
        ],
      ),
    );
  }
}
