import 'package:ata_app/core/localization/corporate_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/selectable_tile.dart';
import 'package:ata_app/design/widgets/sheet_handle.dart';
import 'package:ata_app/features/catalog/domain/entities/ride_category.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_profile.dart';
import 'package:ata_app/features/corporate/domain/entities/policy_violation.dart';
import 'package:ata_app/features/corporate/presentation/cubit/corporate_payment_cubit.dart';
import 'package:ata_app/features/corporate/presentation/cubit/corporate_payment_state.dart';
import 'package:ata_app/features/passenger_home/domain/entities/ride_time.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_cubit.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_state.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/payment_row.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Payment-method chooser: cash, wallet, card and, for an active company
/// member, "حساب الشركة" with the company, the remaining monthly budget and
/// the per-trip limit. The company option is disabled with the reason when
/// the policy does not allow the current category / time.
class PaymentMethodSheet extends StatelessWidget {
  const PaymentMethodSheet({super.key});

  static Future<void> show(BuildContext context) {
    final HomeCubit home = context.read<HomeCubit>();
    final CorporatePaymentCubit corporate = context
        .read<CorporatePaymentCubit>();
    return showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      builder: (_) => MultiBlocProvider(
        providers: <BlocProvider<dynamic>>[
          BlocProvider<HomeCubit>.value(value: home),
          BlocProvider<CorporatePaymentCubit>.value(value: corporate),
        ],
        child: const PaymentMethodSheet(),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocBuilder<CorporatePaymentCubit, CorporatePaymentState>(
      builder: (BuildContext context, CorporatePaymentState corporate) =>
          BlocBuilder<HomeCubit, HomeState>(
            builder: (BuildContext context, HomeState home) => SafeArea(
              child: SingleChildScrollView(
                padding: const EdgeInsets.all(AtaSpacing.lg),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: <Widget>[
                    const SheetHandle(),
                    Text(l10n.paymentMethod, style: AtaText.section),
                    const SizedBox(height: AtaSpacing.md),
                    for (final PaymentOption option in _options(corporate))
                      Padding(
                        padding: const EdgeInsets.only(bottom: AtaSpacing.xs),
                        child: option == PaymentOption.corporate
                            ? _CorporateTile(home: home, corporate: corporate)
                            : _Tile(option: option, home: home),
                      ),
                  ],
                ),
              ),
            ),
          ),
    );
  }

  static List<PaymentOption> _options(CorporatePaymentState corporate) =>
      PaymentOption.values
          .where(
            (PaymentOption o) =>
                o != PaymentOption.corporate || corporate.available,
          )
          .toList(growable: false);
}

void _select(BuildContext context, PaymentOption option) {
  context.read<HomeCubit>().selectPayment(option);
  Navigator.of(context).pop();
}

class _Tile extends StatelessWidget {
  const _Tile({required this.option, required this.home});

  final PaymentOption option;
  final HomeState home;

  @override
  Widget build(BuildContext context) {
    final bool selected = option == home.payment;
    return SelectableTile(
      selected: selected,
      onTap: () => _select(context, option),
      child: Row(
        children: <Widget>[
          Expanded(
            child: Text(
              paymentLabel(context.l10n, option),
              style: AtaText.bodyStrong,
            ),
          ),
          RadioDot(selected: selected),
        ],
      ),
    );
  }
}

class _CorporateTile extends StatelessWidget {
  const _CorporateTile({required this.home, required this.corporate});

  final HomeState home;
  final CorporatePaymentState corporate;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final CorporateProfile profile = corporate.profile!;
    final bool selected = home.payment == PaymentOption.corporate;
    final double? remaining = corporate.eligibility.remainingBudget;
    final String? reason = corporate.usable
        ? null
        : _reason(l10n, corporate, home.categories);
    return Opacity(
      opacity: corporate.usable ? 1 : 0.6,
      child: SelectableTile(
        key: const ValueKey<String>('payment-corporate'),
        selected: selected,
        onTap: corporate.usable
            ? () => _select(context, PaymentOption.corporate)
            : null,
        child: Row(
          children: <Widget>[
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: <Widget>[
                  Text(l10n.paymentCorporate, style: AtaText.bodyStrong),
                  Text(profile.membership.companyName, style: AtaText.caption),
                  if (remaining != null)
                    Text(
                      l10n.corpOptionBudget(
                        CorporateText.money(l10n, remaining),
                      ),
                      style: AtaText.caption,
                    ),
                  if (profile.perTripLimit != null)
                    Text(
                      l10n.corpOptionTripLimit(
                        CorporateText.money(l10n, profile.perTripLimit!),
                      ),
                      style: AtaText.caption,
                    ),
                  if (reason != null)
                    Text(
                      l10n.corpOptionUnavailable(reason),
                      key: const ValueKey<String>('payment-corporate-reason'),
                      style: AtaText.caption.copyWith(color: AtaColors.danger),
                    ),
                ],
              ),
            ),
            RadioDot(selected: selected),
          ],
        ),
      ),
    );
  }

  /// The first violated rule with the options the company allows.
  static String _reason(
    AppLocalizations l10n,
    CorporatePaymentState corporate,
    List<RideCategory> categories,
  ) {
    final PolicyViolation v = corporate.eligibility.violations.first;
    final String? allowed = CorporateText.allowedOptions(
      l10n,
      v,
      categoryName: CorporateText.categoryNames(categories),
    );
    final String message = CorporateText.violation(l10n, v);
    return allowed == null ? message : '$message ($allowed)';
  }
}
