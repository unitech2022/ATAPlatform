import 'package:ata_app/core/localization/corporate_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_invitation.dart';
import 'package:ata_app/features/corporate/presentation/cubit/corporate_invitations_cubit.dart';
import 'package:ata_app/features/corporate/presentation/cubit/corporate_invitations_state.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// One pending invitation: company, role, expiry and accept / decline.
class InvitationTile extends StatelessWidget {
  const InvitationTile({super.key, required this.invitation});

  final CorporateInvitation invitation;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final CorporateInvitationsCubit cubit = context
        .read<CorporateInvitationsCubit>();
    return BlocBuilder<CorporateInvitationsCubit, CorporateInvitationsState>(
      builder: (BuildContext context, CorporateInvitationsState state) {
        final bool busy = state.busyId == invitation.id;
        final bool expired = invitation.isExpiredAt(DateTime.now());
        return AtaCard(
          key: ValueKey<String>('invitation-${invitation.id}'),
          padding: const EdgeInsets.all(AtaSpacing.lg),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: <Widget>[
              Row(
                children: <Widget>[
                  const IconBox(icon: AtaIcons.building),
                  const SizedBox(width: AtaSpacing.md),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: <Widget>[
                        Text(
                          l10n.corpInvitationFrom(invitation.companyName),
                          style: AtaText.bodyStrong,
                        ),
                        Text(
                          l10n.corpInvitationRole(
                            CorporateText.role(l10n, invitation.role),
                          ),
                          style: AtaText.caption,
                        ),
                        if (invitation.expiresAt != null)
                          Text(
                            expired
                                ? l10n.corpInvitationExpired
                                : l10n.corpInvitationExpires(
                                    DateText.longDate(
                                      invitation.expiresAt!,
                                      context.localeCode,
                                    ),
                                  ),
                            style: AtaText.caption,
                          ),
                      ],
                    ),
                  ),
                ],
              ),
              const SizedBox(height: AtaSpacing.md),
              Row(
                children: <Widget>[
                  Expanded(
                    child: AtaButton(
                      key: ValueKey<String>('accept-${invitation.id}'),
                      label: l10n.corpAccept,
                      variant: AtaButtonVariant.brand,
                      height: AtaSizes.buttonCompact,
                      loading: busy,
                      onPressed: state.isBusy || expired
                          ? null
                          : () => cubit.accept(invitation.id),
                    ),
                  ),
                  const SizedBox(width: AtaSpacing.sm),
                  Expanded(
                    child: AtaButton(
                      key: ValueKey<String>('decline-${invitation.id}'),
                      label: l10n.corpDecline,
                      variant: AtaButtonVariant.outline,
                      height: AtaSizes.buttonCompact,
                      onPressed: state.isBusy
                          ? null
                          : () => cubit.decline(invitation.id),
                    ),
                  ),
                ],
              ),
            ],
          ),
        );
      },
    );
  }
}
