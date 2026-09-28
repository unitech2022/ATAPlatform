import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_cubit.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_state.dart';
import 'package:ata_app/features/pricing/domain/entities/quote_category.dart';
import 'package:ata_app/features/pricing/presentation/cubit/quote_cubit.dart';
import 'package:ata_app/features/pricing/presentation/cubit/quote_state.dart';
import 'package:ata_app/features/pricing/presentation/widgets/fare_breakdown_sheet.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// "تفاصيل السعر" under the categories, or the quote failure / expiry
/// hint with a refresh action.
class FareDetailsLink extends StatelessWidget {
  const FareDetailsLink({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocBuilder<QuoteCubit, QuoteState>(
      builder: (BuildContext context, QuoteState quote) {
        if (quote.status == QuoteStatus.failure || quote.expired) {
          return _Hint(
            message: quote.expired ? l10n.quoteExpiredHint : l10n.quoteFailed,
            action: l10n.refreshQuote,
            onTap: context.read<QuoteCubit>().refresh,
          );
        }
        return BlocSelector<HomeCubit, HomeState, QuoteCategory?>(
          selector: (HomeState state) => state.quoteCategory,
          builder: (BuildContext context, QuoteCategory? category) {
            if (category == null) return const SizedBox.shrink();
            return Align(
              alignment: AlignmentDirectional.centerEnd,
              child: TextButton(
                onPressed: () => FareBreakdownSheet.show(
                  context,
                  quote: context.read<HomeCubit>().state.quote!,
                  category: category,
                ),
                child: Text(
                  l10n.fareDetails,
                  style: AtaText.label.copyWith(color: AtaColors.brand),
                ),
              ),
            );
          },
        );
      },
    );
  }
}

class _Hint extends StatelessWidget {
  const _Hint({
    required this.message,
    required this.action,
    required this.onTap,
  });

  final String message;
  final String action;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(top: AtaSpacing.xs),
      child: Row(
        children: <Widget>[
          Expanded(child: Text(message, style: AtaText.caption)),
          TextButton(
            onPressed: onTap,
            child: Text(
              action,
              style: AtaText.label.copyWith(color: AtaColors.brand),
            ),
          ),
        ],
      ),
    );
  }
}
