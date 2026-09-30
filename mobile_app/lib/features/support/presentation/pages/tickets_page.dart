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
import 'package:ata_app/features/support/domain/entities/ticket.dart';
import 'package:ata_app/features/support/domain/entities/ticket_enums.dart';
import 'package:ata_app/features/support/presentation/cubit/tickets_cubit.dart';
import 'package:ata_app/features/support/presentation/cubit/tickets_state.dart';
import 'package:ata_app/features/support/presentation/widgets/support_scaffold.dart';
import 'package:ata_app/features/support/presentation/widgets/ticket_tile.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// `/support/tickets`: "تذاكري" with the open / closed tabs, status chips
/// and unread badges.
class TicketsPage extends StatelessWidget {
  const TicketsPage({super.key, required this.actor});

  final TripActor actor;

  bool get _driver => actor == TripActor.driver;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocProvider<TicketsCubit>(
      create: (_) => TicketsCubit(getTickets: getIt(), watch: getIt())..start(),
      child: BlocBuilder<TicketsCubit, TicketsState>(
        builder: (BuildContext context, TicketsState state) => SupportScaffold(
          actor: actor,
          eyebrow: l10n.ticketsEyebrow,
          title: l10n.ticketsTitle,
          copy: l10n.ticketsCopy,
          backTo: AppRoutes.supportRoot(driver: _driver),
          children: <Widget>[
            _Tabs(filter: state.filter),
            const SizedBox(height: AtaSpacing.md),
            AtaButton(
              label: l10n.ticketsNew,
              icon: AtaIcons.plus,
              variant: AtaButtonVariant.brand,
              height: AtaSizes.buttonCompact,
              onPressed: () =>
                  _open(context, AppRoutes.newSupportTicket(driver: _driver)),
            ),
            const SizedBox(height: AtaSpacing.lg),
            ..._list(context, state),
          ],
        ),
      ),
    );
  }

  List<Widget> _list(BuildContext context, TicketsState state) {
    final AppLocalizations l10n = context.l10n;
    final TicketsCubit cubit = context.read<TicketsCubit>();
    if (state.loading && state.tickets.isEmpty) {
      return const <Widget>[CenteredLoader()];
    }
    if (state.failure != null && state.tickets.isEmpty) {
      return <Widget>[
        FailureView(failure: state.failure!, onRetry: cubit.refresh),
      ];
    }
    if (state.isEmpty) {
      return <Widget>[
        Text(l10n.ticketsEmpty, style: AtaText.bodyStrong),
        Text(l10n.ticketsEmptyCopy, style: AtaText.small),
      ];
    }
    return <Widget>[
      for (final TicketSummary t in state.tickets)
        TicketTile(
          ticket: t,
          onTap: () async {
            cubit.markRead(t.id);
            await _open(
              context,
              AppRoutes.supportTicket(t.id, driver: _driver),
            );
            cubit.refresh(silent: true);
          },
        ),
      if (state.loadingMore)
        const CenteredLoader()
      else if (state.hasMore)
        AtaButton(
          label: l10n.helpLoadMore,
          variant: AtaButtonVariant.outline,
          height: AtaSizes.buttonCompact,
          onPressed: cubit.loadMore,
        ),
    ];
  }

  Future<void> _open(BuildContext context, String route) =>
      context.push<Object?>(route);
}

class _Tabs extends StatelessWidget {
  const _Tabs({required this.filter});

  final TicketFilter filter;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final TicketsCubit cubit = context.read<TicketsCubit>();
    Widget tab(TicketFilter value, String label) {
      final bool selected = filter == value;
      return PillButton(
        label: label,
        onTap: () => cubit.setFilter(value),
        background: selected ? AtaColors.ink : AtaColors.white,
        foreground: selected ? AtaColors.white : AtaColors.ink,
        bordered: !selected,
        elevated: false,
      );
    }

    return Row(
      children: <Widget>[
        tab(TicketFilter.open, l10n.ticketsTabOpen),
        const SizedBox(width: AtaSpacing.xs),
        tab(TicketFilter.closed, l10n.ticketsTabClosed),
      ],
    );
  }
}
