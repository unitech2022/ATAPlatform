import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/features/support/presentation/cubit/help_article_cubit.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// "Was this article helpful?" yes / no, then a thank-you (a "no" also
/// offers to open a ticket).
class ArticleFeedbackCard extends StatelessWidget {
  const ArticleFeedbackCard({
    super.key,
    required this.state,
    required this.driver,
  });

  final HelpArticleState state;
  final bool driver;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final HelpArticleCubit cubit = context.read<HelpArticleCubit>();
    final bool sent = state.feedback == ArticleFeedbackStatus.sent;
    final bool busy = state.feedback == ArticleFeedbackStatus.sending;
    return AtaCard(
      padding: const EdgeInsets.all(AtaSpacing.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Text(
            sent ? l10n.helpFeedbackThanks : l10n.helpFeedbackTitle,
            style: AtaText.section,
          ),
          if (sent && state.helpful == false) ...<Widget>[
            const SizedBox(height: AtaSpacing.xs),
            Text(l10n.helpFeedbackNoCopy, style: AtaText.small),
            const SizedBox(height: AtaSpacing.md),
            AtaButton(
              label: l10n.supportContactUs,
              variant: AtaButtonVariant.soft,
              height: AtaSizes.buttonCompact,
              onPressed: () =>
                  context.push(AppRoutes.newSupportTicket(driver: driver)),
            ),
          ],
          if (!sent) ...<Widget>[
            const SizedBox(height: AtaSpacing.md),
            Row(
              children: <Widget>[
                Expanded(
                  child: AtaButton(
                    key: const ValueKey<String>('feedback-yes'),
                    label: l10n.helpFeedbackYes,
                    variant: AtaButtonVariant.brand,
                    height: AtaSizes.buttonCompact,
                    loading: busy && state.helpful == true,
                    onPressed: busy
                        ? null
                        : () => cubit.sendFeedback(helpful: true),
                  ),
                ),
                const SizedBox(width: AtaSpacing.sm),
                Expanded(
                  child: AtaButton(
                    key: const ValueKey<String>('feedback-no'),
                    label: l10n.helpFeedbackNo,
                    variant: AtaButtonVariant.outline,
                    height: AtaSizes.buttonCompact,
                    loading: busy && state.helpful == false,
                    onPressed: busy
                        ? null
                        : () => cubit.sendFeedback(helpful: false),
                  ),
                ),
              ],
            ),
          ],
          if (state.feedbackFailure != null) ...<Widget>[
            const SizedBox(height: AtaSpacing.sm),
            InlineError(message: failureText(state.feedbackFailure!, l10n)),
          ],
        ],
      ),
    );
  }
}
