import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/features/safety/presentation/widgets/choice_wrap.dart';
import 'package:ata_app/features/support/domain/entities/ticket.dart';
import 'package:ata_app/features/support/domain/entities/ticket_enums.dart';
import 'package:ata_app/features/support/presentation/cubit/attachments_cubit.dart';
import 'package:ata_app/features/support/presentation/cubit/fare_dispute_cubit.dart';
import 'package:ata_app/features/support/presentation/cubit/new_ticket_cubit.dart';
import 'package:ata_app/features/support/presentation/cubit/new_ticket_state.dart';
import 'package:ata_app/features/support/presentation/widgets/attachments_bar.dart';
import 'package:ata_app/features/support/presentation/widgets/dispute_section.dart';
import 'package:ata_app/features/support/presentation/widgets/support_scaffold.dart';
import 'package:ata_app/features/support/presentation/widgets/support_text.dart';
import 'package:ata_app/features/support/presentation/widgets/trip_picker.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// `/support/tickets/new?type=&tripId=&dispute=1`: type, related trip,
/// subject, details, up to five attachments and — for a payment issue — the
/// fare dispute. Opens the new thread when the API accepted it.
class NewTicketPage extends StatelessWidget {
  const NewTicketPage({
    super.key,
    required this.actor,
    this.type,
    this.tripId,
    this.dispute = false,
  });

  final TripActor actor;

  /// `type` query parameter (`trip_issue`, `payment_issue`, ...).
  final String? type;
  final String? tripId;

  /// Opens with the fare dispute switched on.
  final bool dispute;

  bool get _driver => actor == TripActor.driver;

  @override
  Widget build(BuildContext context) {
    final String subject = dispute ? context.l10n.disputeDefaultSubject : '';
    return MultiBlocProvider(
      providers: <BlocProvider<dynamic>>[
        BlocProvider<NewTicketCubit>(
          create: (_) => NewTicketCubit(
            getTrips: getIt(),
            create: getIt(),
            actor: actor,
            initialType: dispute
                ? TicketType.paymentIssue
                : TicketType.tryParse(type),
            tripId: tripId,
            initialSubject: subject,
          )..start(),
        ),
        BlocProvider<AttachmentsCubit>(
          create: (_) => AttachmentsCubit(pick: getIt(), upload: getIt()),
        ),
        BlocProvider<FareDisputeCubit>(
          create: (_) => FareDisputeCubit(enabled: dispute),
        ),
      ],
      child: BlocConsumer<NewTicketCubit, NewTicketState>(
        listenWhen: (NewTicketState p, NewTicketState c) =>
            p.created == null && c.created != null,
        listener: _onCreated,
        builder: _build,
      ),
    );
  }

  void _onCreated(BuildContext context, NewTicketState state) {
    final TicketDetail created = state.created!;
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(context.l10n.newTicketCreated(created.ticketNumber)),
      ),
    );
    context.pushReplacement(
      AppRoutes.supportTicket(created.id, driver: _driver),
    );
  }

  Widget _build(BuildContext context, NewTicketState state) {
    final AppLocalizations l10n = context.l10n;
    final NewTicketCubit cubit = context.read<NewTicketCubit>();
    final AttachmentsState files = context.watch<AttachmentsCubit>().state;
    final FareDisputeState dispute = context.watch<FareDisputeCubit>().state;
    final bool canSend =
        state.valid &&
        !state.submitting &&
        !files.uploading &&
        (!state.canDispute || dispute.valid);
    final List<TicketType> types = TicketType.values
        .where((TicketType t) => _driver ? t != TicketType.lostItem : true)
        .toList(growable: false);
    return SupportScaffold(
      actor: actor,
      eyebrow: l10n.newTicketEyebrow,
      title: l10n.newTicketTitle,
      copy: l10n.newTicketCopy,
      backTo: AppRoutes.supportRoot(driver: _driver),
      children: <Widget>[
        Text(l10n.newTicketTypeLabel, style: AtaText.label),
        const SizedBox(height: AtaSpacing.xs),
        ChoiceWrap<TicketType>(
          values: types,
          selected: state.type,
          label: (TicketType t) => SupportText.ticketType(l10n, t),
          onSelected: state.submitting ? null : cubit.selectType,
        ),
        const SizedBox(height: AtaSpacing.lg),
        TripPicker(state: state),
        if (state.canDispute) ...<Widget>[
          const SizedBox(height: AtaSpacing.lg),
          const DisputeSection(),
        ],
        const SizedBox(height: AtaSpacing.lg),
        TextFormField(
          key: const ValueKey<String>('ticket-subject'),
          initialValue: state.subject,
          onChanged: cubit.subjectChanged,
          maxLength: NewTicketState.subjectMax,
          decoration: InputDecoration(
            labelText: l10n.newTicketSubjectLabel,
            counterText: '',
          ),
        ),
        const SizedBox(height: AtaSpacing.sm),
        TextField(
          key: const ValueKey<String>('ticket-message'),
          onChanged: cubit.messageChanged,
          maxLength: NewTicketState.messageMax,
          minLines: 4,
          maxLines: 8,
          decoration: InputDecoration(
            labelText: l10n.newTicketMessageLabel,
            counterText: '',
            alignLabelWithHint: true,
          ),
        ),
        const SizedBox(height: AtaSpacing.md),
        Text(l10n.attachmentsTitle, style: AtaText.label),
        const SizedBox(height: AtaSpacing.xs),
        const AttachmentsBar(),
        if (state.failure != null) ...<Widget>[
          const SizedBox(height: AtaSpacing.md),
          InlineError(message: failureText(state.failure!, l10n)),
        ],
        const SizedBox(height: AtaSpacing.lg),
        AtaButton(
          key: const ValueKey<String>('ticket-submit'),
          label: l10n.newTicketSubmit,
          loading: state.submitting,
          onPressed: canSend
              ? () =>
                    cubit.submit(fileIds: files.fileIds, dispute: dispute.draft)
              : null,
        ),
      ],
    );
  }
}
