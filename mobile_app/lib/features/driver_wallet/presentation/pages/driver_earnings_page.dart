import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/widgets/failure_view.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/design/widgets/selectable_tile.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/statement_period.dart';
import 'package:ata_app/features/driver_wallet/presentation/cubit/earnings_statement_cubit.dart';
import 'package:ata_app/features/driver_wallet/presentation/cubit/earnings_statement_state.dart';
import 'package:ata_app/features/driver_wallet/presentation/widgets/driver_subpage.dart';
import 'package:ata_app/features/driver_wallet/presentation/widgets/driver_wallet_text.dart';
import 'package:ata_app/features/driver_wallet/presentation/widgets/statement_totals_card.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// `/driver/earnings`: statement with the today / week / month filter.
class DriverEarningsPage extends StatelessWidget {
  const DriverEarningsPage({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocProvider<EarningsStatementCubit>(
      create: (_) => EarningsStatementCubit(getStatement: getIt())..load(),
      child: DriverSubpage.content(
        eyebrow: l10n.driverWalletEyebrow,
        title: l10n.earningsStatementTitle,
        copy: l10n.earningsStatementCopy,
        children: <Widget>[
          BlocBuilder<EarningsStatementCubit, EarningsStatementState>(
            builder: (BuildContext context, EarningsStatementState state) {
              final EarningsStatementCubit cubit = context
                  .read<EarningsStatementCubit>();
              return Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: <Widget>[
                  Row(
                    children: <Widget>[
                      for (final StatementPeriod p
                          in StatementPeriod.values) ...<Widget>[
                        Expanded(
                          child: SelectableTile(
                            selected: state.period == p,
                            onTap: () => cubit.select(p),
                            padding: const EdgeInsets.symmetric(
                              vertical: AtaSpacing.sm,
                            ),
                            child: Text(
                              DriverWalletText.period(l10n, p),
                              textAlign: TextAlign.center,
                              style: AtaText.label.copyWith(
                                color: state.period == p
                                    ? AtaColors.brand
                                    : AtaColors.ink,
                              ),
                            ),
                          ),
                        ),
                        if (p != StatementPeriod.values.last)
                          const SizedBox(width: AtaSpacing.sm),
                      ],
                    ],
                  ),
                  const SizedBox(height: AtaSpacing.xl),
                  if (state.loading)
                    const CenteredLoader()
                  else if (state.failure != null)
                    FailureView(failure: state.failure!, onRetry: cubit.load)
                  else if (state.statement != null)
                    StatementTotalsCard(statement: state.statement!),
                ],
              );
            },
          ),
        ],
      ),
    );
  }
}
