import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/core/widgets/failure_view.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/features/driver_wallet/domain/entities/payout_summary.dart';
import 'package:ata_app/features/driver_wallet/presentation/cubit/payout_request_cubit.dart';
import 'package:ata_app/features/driver_wallet/presentation/cubit/payout_request_state.dart';
import 'package:ata_app/features/driver_wallet/presentation/widgets/driver_subpage.dart';
import 'package:ata_app/features/driver_wallet/presentation/widgets/driver_wallet_text.dart';
import 'package:ata_app/features/driver_wallet/presentation/widgets/payout_request_form.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// `/driver/payouts/request`: amount (validated against the minimum and
/// the available balance) to the masked IBAN. Pops `true` once created.
class PayoutRequestPage extends StatelessWidget {
  const PayoutRequestPage({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocProvider<PayoutRequestCubit>(
      create: (_) =>
          PayoutRequestCubit(getSummary: getIt(), request: getIt())..load(),
      child: DriverSubpage.content(
        eyebrow: l10n.driverWalletEyebrow,
        title: l10n.requestPayout,
        copy: l10n.payoutRequestCopy,
        children: <Widget>[
          BlocBuilder<PayoutRequestCubit, PayoutRequestState>(
            builder: (BuildContext context, PayoutRequestState state) {
              final PayoutSummary? summary = state.summary;
              if (state.loading && summary == null) {
                return const CenteredLoader();
              }
              if (summary == null) {
                return state.failure == null
                    ? const SizedBox.shrink()
                    : FailureView(
                        failure: state.failure!,
                        onRetry: context.read<PayoutRequestCubit>().load,
                      );
              }
              if (state.payout != null) return const _SuccessView();
              return _FormBody(state: state, summary: summary);
            },
          ),
        ],
      ),
    );
  }
}

class _FormBody extends StatelessWidget {
  const _FormBody({required this.state, required this.summary});

  final PayoutRequestState state;
  final PayoutSummary summary;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final String? blocked = summary.canRequest
        ? null
        : DriverWalletText.reason(l10n, summary.reason) ??
              l10n.payoutUnavailable;
    return AtaCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          PayoutRequestForm(state: state, summary: summary),
          if (blocked != null) ...<Widget>[
            const SizedBox(height: AtaSpacing.md),
            InlineError(message: blocked),
          ],
          if (state.failure != null) ...<Widget>[
            const SizedBox(height: AtaSpacing.md),
            InlineError(message: failureText(state.failure!, l10n)),
          ],
          const SizedBox(height: AtaSpacing.xl),
          AtaButton(
            label: l10n.confirmPayout(
              l10n.priceWithCurrency(Money.compact(state.amount ?? 0)),
            ),
            loading: state.submitting,
            onPressed: state.canSubmit
                ? context.read<PayoutRequestCubit>().submit
                : null,
          ),
        ],
      ),
    );
  }
}

class _SuccessView extends StatelessWidget {
  const _SuccessView();

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return AtaCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Text(
            l10n.payoutRequestedTitle,
            style: AtaText.headline.copyWith(color: AtaColors.brand),
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: AtaSpacing.xs),
          Text(
            l10n.payoutRequestedCopy,
            style: AtaText.bodyMuted,
            textAlign: TextAlign.center,
          ),
          const SizedBox(height: AtaSpacing.xl),
          AtaButton(label: l10n.done, onPressed: () => context.pop(true)),
        ],
      ),
    );
  }
}
