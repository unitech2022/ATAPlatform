import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/core/widgets/failure_view.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/features/support/domain/entities/help.dart';
import 'package:ata_app/features/support/presentation/cubit/help_article_cubit.dart';
import 'package:ata_app/features/support/presentation/widgets/article_feedback_card.dart';
import 'package:ata_app/features/support/presentation/widgets/markdown_view.dart';
import 'package:ata_app/features/support/presentation/widgets/support_scaffold.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// `/support/articles/:slug`: one help article rendered from Markdown, its
/// related articles, the helpful yes / no feedback and the "open a ticket"
/// invitation.
class HelpArticlePage extends StatelessWidget {
  const HelpArticlePage({super.key, required this.slug, required this.actor});

  final String slug;
  final TripActor actor;

  bool get _driver => actor == TripActor.driver;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocProvider<HelpArticleCubit>(
      create: (_) => HelpArticleCubit(
        getArticle: getIt(),
        sendFeedback: getIt(),
        slug: slug,
      )..load(),
      child: BlocBuilder<HelpArticleCubit, HelpArticleState>(
        builder: (BuildContext context, HelpArticleState state) {
          final HelpArticle? article = state.article;
          return SupportScaffold(
            actor: actor,
            eyebrow: article?.categoryName ?? l10n.supportEyebrow,
            title: article?.title ?? l10n.supportTitle,
            copy: article?.updatedAt == null
                ? null
                : l10n.helpArticleUpdated(
                    DateText.longDate(article!.updatedAt!, context.localeCode),
                  ),
            backTo: AppRoutes.supportRoot(driver: _driver),
            children: <Widget>[
              if (article == null && state.failure == null)
                const CenteredLoader()
              else if (article == null)
                FailureView(
                  failure: state.failure!,
                  onRetry: context.read<HelpArticleCubit>().load,
                )
              else
                ..._body(context, article, state),
            ],
          );
        },
      ),
    );
  }

  List<Widget> _body(
    BuildContext context,
    HelpArticle article,
    HelpArticleState state,
  ) {
    final AppLocalizations l10n = context.l10n;
    return <Widget>[
      AtaCard(
        padding: const EdgeInsets.all(AtaSpacing.lg),
        child: MarkdownView(source: article.body),
      ),
      if (article.related.isNotEmpty) ...<Widget>[
        const SizedBox(height: AtaSpacing.lg),
        Text(l10n.helpRelatedTitle, style: AtaText.section),
        const SizedBox(height: AtaSpacing.xs),
        for (final RelatedArticle r in article.related)
          InkWell(
            onTap: () =>
                context.push(AppRoutes.supportArticle(r.slug, driver: _driver)),
            child: Padding(
              padding: const EdgeInsets.symmetric(vertical: AtaSpacing.xs),
              child: Text(
                r.title,
                style: AtaText.label.copyWith(color: AtaColors.brand),
              ),
            ),
          ),
      ],
      const SizedBox(height: AtaSpacing.lg),
      ArticleFeedbackCard(state: state, driver: _driver),
      const SizedBox(height: AtaSpacing.lg),
      Text(l10n.helpStillNeedTitle, style: AtaText.section),
      Text(l10n.helpStillNeedCopy, style: AtaText.small),
      const SizedBox(height: AtaSpacing.sm),
      AtaButton(
        label: l10n.ticketsNew,
        variant: AtaButtonVariant.soft,
        height: AtaSizes.buttonCompact,
        onPressed: () =>
            context.push(AppRoutes.newSupportTicket(driver: _driver)),
      ),
    ];
  }
}
