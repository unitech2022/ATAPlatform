import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/widgets/failure_view.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/features/support/domain/entities/help.dart';
import 'package:ata_app/features/support/presentation/cubit/help_center_cubit.dart';
import 'package:ata_app/features/support/presentation/cubit/help_center_state.dart';
import 'package:ata_app/features/support/presentation/cubit/tickets_cubit.dart';
import 'package:ata_app/features/support/presentation/widgets/help_article_tile.dart';
import 'package:ata_app/features/support/presentation/widgets/help_category_tile.dart';
import 'package:ata_app/features/support/presentation/widgets/help_search_field.dart';
import 'package:ata_app/features/support/presentation/widgets/support_entry_card.dart';
import 'package:ata_app/features/support/presentation/widgets/support_scaffold.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// `/support` (rider) and `/driver/support` (driver): the help center —
/// search, topics filtered by audience, "تذاكري" with its unread badge and
/// "تواصل معنا".
class SupportPage extends StatelessWidget {
  const SupportPage({super.key, required this.actor});

  final TripActor actor;

  bool get _driver => actor == TripActor.driver;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return MultiBlocProvider(
      providers: <BlocProvider<dynamic>>[
        BlocProvider<HelpCenterCubit>(
          create: (_) => HelpCenterCubit(
            getCategories: getIt(),
            searchArticles: getIt(),
            audience: _driver ? 'driver' : 'passenger',
          )..load(),
        ),
        BlocProvider<TicketsCubit>(
          create: (_) =>
              TicketsCubit(getTickets: getIt(), watch: getIt())..start(),
        ),
      ],
      child: BlocBuilder<HelpCenterCubit, HelpCenterState>(
        builder: (BuildContext context, HelpCenterState state) =>
            SupportScaffold(
              actor: actor,
              eyebrow: l10n.supportEyebrow,
              title: l10n.supportTitle,
              copy: l10n.supportCopy,
              backTo: _driver ? AppRoutes.driver : AppRoutes.account,
              children: <Widget>[
                HelpSearchField(
                  version: state.resetVersion,
                  onChanged: context.read<HelpCenterCubit>().queryChanged,
                ),
                const SizedBox(height: AtaSpacing.lg),
                if (state.browsing)
                  _Browse(state: state, driver: _driver)
                else
                  _Results(state: state, driver: _driver),
              ],
            ),
      ),
    );
  }
}

class _Browse extends StatelessWidget {
  const _Browse({required this.state, required this.driver});

  final HelpCenterState state;
  final bool driver;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final int unread = context.select<TicketsCubit, int>(
      (TicketsCubit c) => c.state.unreadTotal,
    );
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        SupportEntryCard(
          icon: AtaIcons.document,
          title: l10n.supportMyTickets,
          copy: l10n.supportMyTicketsCopy,
          badge: unread > 0 ? l10n.supportUnreadBadge(unread) : null,
          onTap: () => context.push(AppRoutes.supportTickets(driver: driver)),
        ),
        const SizedBox(height: AtaSpacing.sm),
        SupportEntryCard(
          icon: AtaIcons.phone,
          title: l10n.supportContactUs,
          copy: l10n.supportContactUsCopy,
          onTap: () => context.push(AppRoutes.newSupportTicket(driver: driver)),
        ),
        const SizedBox(height: AtaSpacing.xl),
        Text(l10n.helpTopicsTitle, style: AtaText.section),
        const SizedBox(height: AtaSpacing.sm),
        if (state.loading && state.categories.isEmpty)
          const CenteredLoader()
        else if (state.failure != null && state.categories.isEmpty)
          FailureView(
            failure: state.failure!,
            onRetry: context.read<HelpCenterCubit>().load,
          )
        else
          for (final HelpCategory c in state.categories)
            HelpCategoryTile(
              category: c,
              onTap: () => context.read<HelpCenterCubit>().selectCategory(c.id),
            ),
      ],
    );
  }
}

class _Results extends StatelessWidget {
  const _Results({required this.state, required this.driver});

  final HelpCenterState state;
  final bool driver;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final HelpCenterCubit cubit = context.read<HelpCenterCubit>();
    final String? category = state.category?.name;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        Row(
          children: <Widget>[
            if (category != null)
              Expanded(child: Text(category, style: AtaText.section))
            else
              const Spacer(),
            PillButton(
              label: l10n.helpAllTopics,
              onTap: cubit.reset,
              background: AtaColors.brandSoft,
              foreground: AtaColors.brand,
              elevated: false,
            ),
          ],
        ),
        const SizedBox(height: AtaSpacing.sm),
        for (final HelpArticleSummary a in state.articles)
          HelpArticleTile(
            article: a,
            onTap: () =>
                context.push(AppRoutes.supportArticle(a.slug, driver: driver)),
          ),
        if (state.searching)
          const CenteredLoader()
        else if (state.searchFailure != null)
          FailureView(failure: state.searchFailure!, onRetry: cubit.retry)
        else if (state.articles.isEmpty)
          _NoResults(driver: driver)
        else if (state.hasMore)
          AtaButton(
            label: l10n.helpLoadMore,
            variant: AtaButtonVariant.outline,
            height: AtaSizes.buttonCompact,
            onPressed: cubit.loadMore,
          ),
      ],
    );
  }
}

class _NoResults extends StatelessWidget {
  const _NoResults({required this.driver});

  final bool driver;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        Text(l10n.helpNoResults, style: AtaText.bodyStrong),
        Text(l10n.helpNoResultsCopy, style: AtaText.small),
        const SizedBox(height: AtaSpacing.md),
        AtaButton(
          label: l10n.supportContactUs,
          variant: AtaButtonVariant.soft,
          height: AtaSizes.buttonCompact,
          onPressed: () =>
              context.push(AppRoutes.newSupportTicket(driver: driver)),
        ),
      ],
    );
  }
}
