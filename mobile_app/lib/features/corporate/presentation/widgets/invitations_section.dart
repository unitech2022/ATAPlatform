import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_invitation.dart';
import 'package:ata_app/features/corporate/presentation/cubit/corporate_invitations_cubit.dart';
import 'package:ata_app/features/corporate/presentation/cubit/corporate_invitations_state.dart';
import 'package:ata_app/features/corporate/presentation/widgets/invitation_tile.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// "دعوات الشركات": the pending invitations and a snackbar with the result
/// of accepting or declining one.
class InvitationsSection extends StatelessWidget {
  const InvitationsSection({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocConsumer<CorporateInvitationsCubit, CorporateInvitationsState>(
      listenWhen: (CorporateInvitationsState p, CorporateInvitationsState c) =>
          c.outcome != InvitationOutcome.none && p.outcome != c.outcome,
      listener: (BuildContext context, CorporateInvitationsState state) {
        ScaffoldMessenger.maybeOf(context)?.showSnackBar(
          SnackBar(
            content: Text(
              state.outcome == InvitationOutcome.accepted
                  ? l10n.corpInvitationAccepted(state.outcomeCompany ?? '')
                  : l10n.corpInvitationDeclined,
            ),
          ),
        );
        context.read<CorporateInvitationsCubit>().outcomeShown();
      },
      builder: (BuildContext context, CorporateInvitationsState state) {
        final Failure? failure = state.actionFailure;
        if (!state.hasInvitations && failure == null) {
          return const SizedBox.shrink();
        }
        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: <Widget>[
            Text(l10n.corpInvitationsTitle, style: AtaText.section),
            const SizedBox(height: AtaSpacing.sm),
            if (failure != null) ...<Widget>[
              InlineError(message: failureText(failure, l10n)),
              const SizedBox(height: AtaSpacing.sm),
            ],
            for (final CorporateInvitation i in state.invitations) ...<Widget>[
              InvitationTile(invitation: i),
              const SizedBox(height: AtaSpacing.md),
            ],
          ],
        );
      },
    );
  }
}
