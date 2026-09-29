import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/design/widgets/sheet_handle.dart';
import 'package:ata_app/features/promotions/domain/entities/promo_validation.dart';
import 'package:ata_app/features/promotions/presentation/cubit/promo_code_cubit.dart';
import 'package:ata_app/features/promotions/presentation/cubit/promo_code_state.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// "كود الخصم" input sheet: type a code and validate it for the current
/// trip draft; closes itself once the code is applied.
class PromoCodeSheet extends StatelessWidget {
  const PromoCodeSheet({super.key, required this.draft});

  /// Reads the current draft (`quoteId`, category, payment, booking type).
  final PromoValidationParams Function() draft;

  static Future<void> show(
    BuildContext context, {
    required PromoCodeCubit cubit,
    required PromoValidationParams Function() draft,
  }) => showModalBottomSheet<void>(
    context: context,
    isScrollControlled: true,
    builder: (_) => BlocProvider<PromoCodeCubit>.value(
      value: cubit,
      child: PromoCodeSheet(draft: draft),
    ),
  );

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final PromoCodeCubit cubit = context.read<PromoCodeCubit>();
    return BlocConsumer<PromoCodeCubit, PromoCodeState>(
      listenWhen: (PromoCodeState p, PromoCodeState c) =>
          p.isValidating && c.status == PromoCodeStatus.applied,
      listener: (BuildContext context, _) => Navigator.of(context).pop(),
      builder: (BuildContext context, PromoCodeState state) => Padding(
        padding: EdgeInsets.fromLTRB(
          AtaSpacing.lg,
          AtaSpacing.lg,
          AtaSpacing.lg,
          AtaSpacing.lg +
              MediaQuery.viewInsetsOf(context).bottom +
              MediaQuery.paddingOf(context).bottom,
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: <Widget>[
            const SheetHandle(),
            Text(l10n.promoCodeTitle, style: AtaText.section),
            const SizedBox(height: AtaSpacing.xxs),
            Text(l10n.promoCodeCopy, style: AtaText.small),
            const SizedBox(height: AtaSpacing.md),
            TextFormField(
              key: ValueKey<String>('promo-input-${state.inputVersion}'),
              initialValue: state.input,
              autofocus: true,
              enabled: !state.isValidating,
              onChanged: cubit.inputChanged,
              onFieldSubmitted: (_) => _validate(cubit, state),
              textCapitalization: TextCapitalization.characters,
              textDirection: TextDirection.ltr,
              maxLength: _maxLength,
              style: AtaText.bodyStrong,
              decoration: InputDecoration(
                hintText: l10n.promoCodeHint,
                counterText: '',
              ),
            ),
            if (state.failure != null) ...<Widget>[
              const SizedBox(height: AtaSpacing.sm),
              InlineError(message: failureText(state.failure!, l10n)),
            ],
            const SizedBox(height: AtaSpacing.md),
            AtaButton(
              label: l10n.promoApply,
              variant: AtaButtonVariant.brand,
              loading: state.isValidating,
              onPressed: state.canValidate
                  ? () => _validate(cubit, state)
                  : null,
            ),
          ],
        ),
      ),
    );
  }

  static const int _maxLength = 24;

  void _validate(PromoCodeCubit cubit, PromoCodeState state) {
    if (state.canValidate) cubit.validate(draft());
  }
}
