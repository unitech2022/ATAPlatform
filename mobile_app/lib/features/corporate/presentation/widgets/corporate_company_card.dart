import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/corporate_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_profile.dart';
import 'package:ata_app/features/corporate/presentation/cubit/corporate_invitations_cubit.dart';
import 'package:ata_app/features/corporate/presentation/cubit/corporate_invitations_state.dart';
import 'package:ata_app/features/corporate/presentation/cubit/corporate_membership_cubit.dart';
import 'package:ata_app/features/corporate/presentation/cubit/corporate_membership_state.dart';
import 'package:ata_app/features/corporate/presentation/widgets/corporate_budget_bar.dart';
import 'package:ata_app/features/corporate/presentation/widgets/invitation_prompt_card.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// "شركتي" on the account page: company name, role and the monthly budget
/// used / limit for a member; the invitation prompt while a company invite
/// is pending; nothing for everyone else.
class CorporateCompanyCard extends StatelessWidget {
  const CorporateCompanyCard({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocBuilder<CorporateMembershipCubit, CorporateMembershipState>(
      builder: (BuildContext context, CorporateMembershipState membership) {
        final CorporateProfile? profile = membership.profile;
        if (profile != null) return _Spaced(child: _Card(profile: profile));
        return BlocBuilder<
          CorporateInvitationsCubit,
          CorporateInvitationsState
        >(
          builder: (BuildContext context, CorporateInvitationsState invites) =>
              invites.hasInvitations
              ? const _Spaced(child: InvitationPromptCard())
              : const SizedBox.shrink(),
        );
      },
    );
  }
}

/// Space below the card (only when there is a card).
class _Spaced extends StatelessWidget {
  const _Spaced({required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context) => Padding(
    padding: const EdgeInsets.only(bottom: AtaSpacing.xl),
    child: child,
  );
}

class _Card extends StatelessWidget {
  const _Card({required this.profile});

  final CorporateProfile profile;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return AtaCard(
      key: const ValueKey<String>('company-card'),
      padding: const EdgeInsets.all(AtaSpacing.lg),
      onTap: () => context.go(AppRoutes.accountCorporate),
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
                    Text(l10n.corpMyCompany, style: AtaText.caption),
                    Text(
                      profile.membership.companyName,
                      style: AtaText.bodyStrong,
                    ),
                    Text(
                      CorporateText.role(l10n, profile.membership.role),
                      style: AtaText.caption,
                    ),
                  ],
                ),
              ),
            ],
          ),
          if (profile.isActive && profile.budget != null) ...<Widget>[
            const SizedBox(height: AtaSpacing.md),
            CorporateBudgetBar(budget: profile.budget),
          ],
        ],
      ),
    );
  }
}
