import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/widgets/failure_view.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/features/support/domain/entities/ticket.dart';
import 'package:ata_app/features/support/presentation/cubit/attachments_cubit.dart';
import 'package:ata_app/features/support/presentation/cubit/csat_cubit.dart';
import 'package:ata_app/features/support/presentation/cubit/ticket_detail_cubit.dart';
import 'package:ata_app/features/support/presentation/cubit/ticket_detail_state.dart';
import 'package:ata_app/features/support/presentation/widgets/csat_card.dart';
import 'package:ata_app/features/support/presentation/widgets/dispute_card.dart';
import 'package:ata_app/features/support/presentation/widgets/reply_box.dart';
import 'package:ata_app/features/support/presentation/widgets/ticket_message_bubble.dart';
import 'package:ata_app/features/support/presentation/widgets/ticket_status_banner.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// `/support/tickets/:ticketId` (`ata://support/tickets/{id}`): a
/// chat-style thread with the agent replies, attachments, the status banner,
/// the fare dispute card, the reply box and the one-time CSAT prompt.
class TicketThreadPage extends StatelessWidget {
  const TicketThreadPage({
    super.key,
    required this.ticketId,
    required this.actor,
  });

  final String ticketId;
  final TripActor actor;

  bool get _driver => actor == TripActor.driver;

  @override
  Widget build(BuildContext context) {
    final Widget body = MultiBlocProvider(
      providers: <BlocProvider<dynamic>>[
        BlocProvider<TicketDetailCubit>(
          create: (_) => TicketDetailCubit(
            getTicket: getIt(),
            reply: getIt(),
            watch: getIt(),
            ticketId: ticketId,
          )..start(),
        ),
        BlocProvider<AttachmentsCubit>(
          create: (_) => AttachmentsCubit(pick: getIt(), upload: getIt()),
        ),
        BlocProvider<CsatCubit>(
          create: (_) => CsatCubit(rate: getIt(), ticketId: ticketId),
        ),
      ],
      child: BlocBuilder<TicketDetailCubit, TicketDetailState>(builder: _build),
    );
    return _driver ? Scaffold(body: SafeArea(child: body)) : body;
  }

  Widget _build(BuildContext context, TicketDetailState state) {
    final AppLocalizations l10n = context.l10n;
    final TicketDetail? detail = state.detail;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        _Header(detail: detail, driver: _driver),
        if (detail != null) TicketStatusBanner(status: detail.status),
        Expanded(child: _content(context, state, detail, l10n)),
        if (detail != null)
          SafeArea(
            top: false,
            child: ReplyBox(state: state, driver: _driver),
          ),
      ],
    );
  }

  Widget _content(
    BuildContext context,
    TicketDetailState state,
    TicketDetail? detail,
    AppLocalizations l10n,
  ) {
    if (detail == null) {
      if (state.failure == null) return const CenteredLoader();
      return Padding(
        padding: const EdgeInsets.all(AtaSpacing.gutter),
        child: FailureView(
          failure: state.failure!,
          onRetry: context.read<TicketDetailCubit>().refresh,
        ),
      );
    }
    // Bottom-up (reverse list): prompt, newest message … oldest, dispute,
    // trip — so the newest message is always in view.
    final List<Widget> items = <Widget>[
      if (detail.ratable || detail.csatScore != null)
        Padding(
          padding: const EdgeInsets.only(top: AtaSpacing.sm),
          child: CsatCard(ratedScore: detail.csatScore),
        ),
      for (final TicketMessage m in detail.messages.reversed)
        TicketMessageBubble(message: m),
      if (detail.dispute != null)
        Padding(
          padding: const EdgeInsets.only(bottom: AtaSpacing.sm),
          child: DisputeCard(dispute: detail.dispute!),
        ),
      if (detail.trip != null && detail.trip!.tripNumber.isNotEmpty)
        Padding(
          padding: const EdgeInsets.only(bottom: AtaSpacing.sm),
          child: Text(
            l10n.threadRelatedTrip(detail.trip!.tripNumber),
            style: AtaText.caption,
          ),
        ),
    ];
    return ListView.builder(
      reverse: true,
      padding: const EdgeInsets.symmetric(horizontal: AtaSpacing.gutter),
      itemCount: items.length,
      itemBuilder: (BuildContext context, int i) => items[i],
    );
  }
}

class _Header extends StatelessWidget {
  const _Header({required this.detail, required this.driver});

  final TicketDetail? detail;
  final bool driver;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return Padding(
      padding: const EdgeInsets.fromLTRB(
        AtaSpacing.gutter,
        AtaSpacing.md,
        AtaSpacing.gutter,
        AtaSpacing.xs,
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: <Widget>[
          PillButton.back(
            label: l10n.back,
            onTap: () => context.canPop()
                ? context.pop()
                : context.go(AppRoutes.supportTickets(driver: driver)),
          ),
          if (detail != null) ...<Widget>[
            const SizedBox(height: AtaSpacing.sm),
            Text(detail!.subject, style: AtaText.headline),
            Text(detail!.ticketNumber, style: AtaText.caption),
          ],
        ],
      ),
    );
  }
}
