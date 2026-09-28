import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/features/driver_dashboard/presentation/cubit/online_status_cubit.dart';
import 'package:ata_app/features/driver_dashboard/presentation/cubit/online_status_state.dart';
import 'package:ata_app/features/driver_wallet/presentation/cubit/payout_summary_cubit.dart';
import 'package:ata_app/features/wallet/presentation/widgets/outstanding_balance_banner.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// "You can't go online": the cash debt exceeds the limit; settle it.
class DebtBlockCard extends StatelessWidget {
  const DebtBlockCard({super.key, required this.block});

  final CashDebtBlock block;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final double? limit = block.limit;
    return OutstandingBalanceBanner(
      title: l10n.cantGoOnlineTitle,
      message: limit == null
          ? l10n.cashDebtLimitError
          : '${l10n.cashDebtLimitError} · '
                '${l10n.cashDebtLimit(l10n.priceWithCurrency(Money.compact(limit)))}',
      amount: block.cashDebt,
      actionLabel: l10n.settleDebt,
      onAction: () => _settle(context),
    );
  }

  /// Opens the driver top-up, then re-reads the status and the debt.
  Future<void> _settle(BuildContext context) async {
    final OnlineStatusCubit status = context.read<OnlineStatusCubit>();
    final PayoutSummaryCubit summary = context.read<PayoutSummaryCubit>();
    await context.push<double>(AppRoutes.driverTopUpFor(block.cashDebt));
    await Future.wait(<Future<void>>[status.load(), summary.load()]);
  }
}
