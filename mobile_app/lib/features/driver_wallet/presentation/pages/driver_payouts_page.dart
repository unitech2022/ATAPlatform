import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/widgets/failure_view.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/payout.dart';
import 'package:ata_app/features/driver_wallet/presentation/cubit/payout_summary_cubit.dart';
import 'package:ata_app/features/driver_wallet/presentation/cubit/payout_summary_state.dart';
import 'package:ata_app/features/driver_wallet/presentation/cubit/payouts_cubit.dart';
import 'package:ata_app/features/driver_wallet/presentation/cubit/payouts_state.dart';
import 'package:ata_app/features/driver_wallet/presentation/widgets/cash_debt_card.dart';
import 'package:ata_app/features/driver_wallet/presentation/widgets/driver_subpage.dart';
import 'package:ata_app/features/driver_wallet/presentation/widgets/payout_tile.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// `/driver/payouts`: cash debt, "request a transfer" and the history.
class DriverPayoutsPage extends StatelessWidget {
  const DriverPayoutsPage({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return MultiBlocProvider(
      providers: <BlocProvider<dynamic>>[
        BlocProvider<PayoutSummaryCubit>(
          create: (_) => PayoutSummaryCubit(getSummary: getIt())..load(),
        ),
        BlocProvider<PayoutsCubit>(
          create: (_) =>
              PayoutsCubit(getPayouts: getIt(), cancelPayout: getIt())..load(),
        ),
      ],
      child: DriverSubpage.content(
        eyebrow: l10n.driverWalletEyebrow,
        title: l10n.payoutsTitle,
        copy: l10n.payoutsCopy,
        children: <Widget>[
          BlocBuilder<PayoutSummaryCubit, PayoutSummaryState>(
            builder: (BuildContext context, PayoutSummaryState state) =>
                (state.summary?.hasCashDebt ?? false)
                ? Padding(
                    padding: const EdgeInsets.only(bottom: AtaSpacing.md),
                    child: CashDebtCard(summary: state.summary!),
                  )
                : const SizedBox.shrink(),
          ),
          AtaButton(
            label: l10n.requestPayout,
            icon: AtaIcons.plus,
            onPressed: () => _request(context),
          ),
          const SizedBox(height: AtaSpacing.xl),
          const _History(),
        ],
      ),
    );
  }

  Future<void> _request(BuildContext context) async {
    final PayoutsCubit payouts = context.read<PayoutsCubit>();
    final PayoutSummaryCubit summary = context.read<PayoutSummaryCubit>();
    final bool? created = await context.push<bool>(
      AppRoutes.driverPayoutRequest,
    );
    if (created ?? false) {
      await Future.wait(<Future<void>>[payouts.load(), summary.load()]);
    }
  }
}

class _History extends StatelessWidget {
  const _History();

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return AtaCard(
      padding: const EdgeInsets.all(AtaSpacing.md),
      child: BlocBuilder<PayoutsCubit, PayoutsState>(
        builder: (BuildContext context, PayoutsState state) {
          final PayoutsCubit cubit = context.read<PayoutsCubit>();
          return Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: <Widget>[
              Text(l10n.payoutHistory, style: AtaText.section),
              const SizedBox(height: AtaSpacing.md),
              if (state.loading && state.payouts.isEmpty)
                const CenteredLoader()
              else if (state.failure != null && state.payouts.isEmpty)
                FailureView(failure: state.failure!, onRetry: cubit.load)
              else if (state.isEmpty)
                Text(
                  l10n.payoutsEmpty,
                  style: AtaText.small,
                  textAlign: TextAlign.center,
                ),
              for (final Payout payout in state.payouts) ...<Widget>[
                PayoutTile(
                  payout: payout,
                  cancelling: state.cancellingId == payout.id,
                  onCancel: () => cubit.cancel(payout.id),
                ),
                const SizedBox(height: AtaSpacing.sm),
              ],
              if (state.actionFailure != null)
                InlineError(message: failureText(state.actionFailure!, l10n)),
            ],
          );
        },
      ),
    );
  }
}
