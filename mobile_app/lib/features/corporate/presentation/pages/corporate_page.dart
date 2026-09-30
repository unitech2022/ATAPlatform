import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/widgets/failure_view.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_card.dart';
import 'package:ata_app/design/widgets/page_wrap.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/design/widgets/screen_title.dart';
import 'package:ata_app/features/catalog/domain/entities/ride_category.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_profile.dart';
import 'package:ata_app/features/corporate/presentation/cubit/corporate_catalog_cubit.dart';
import 'package:ata_app/features/corporate/presentation/cubit/corporate_invitations_cubit.dart';
import 'package:ata_app/features/corporate/presentation/cubit/corporate_membership_cubit.dart';
import 'package:ata_app/features/corporate/presentation/cubit/corporate_membership_state.dart';
import 'package:ata_app/features/corporate/presentation/widgets/corporate_details_card.dart';
import 'package:ata_app/features/corporate/presentation/widgets/corporate_policy_card.dart';
import 'package:ata_app/features/corporate/presentation/widgets/invitations_section.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// `/account/corporate` (`ata://corporate/invitations`): the company
/// membership, policy and monthly budget, and the pending invitations to
/// accept or decline. Leaving a company is not offered: the API has no
/// employee endpoint for it (`docs/12` §F19.4).
class CorporatePage extends StatelessWidget {
  const CorporatePage({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocProvider<CorporateCatalogCubit>(
      create: (_) => CorporateCatalogCubit(getCategories: getIt())..load(),
      child: PageWrap(
        children: <Widget>[
          Align(
            alignment: AlignmentDirectional.centerStart,
            child: PillButton.back(
              label: l10n.back,
              onTap: () => context.canPop()
                  ? context.pop()
                  : context.go(AppRoutes.account),
            ),
          ),
          const SizedBox(height: AtaSpacing.xl),
          ScreenTitle(
            eyebrow: l10n.corpSectionTitle,
            title: l10n.corpMyCompany,
            copy: l10n.corpMyCompanyCopy,
          ),
          const SizedBox(height: AtaSpacing.xl),
          const InvitationsSection(),
          const _Membership(),
        ],
      ),
    );
  }
}

class _Membership extends StatelessWidget {
  const _Membership();

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocBuilder<CorporateMembershipCubit, CorporateMembershipState>(
      builder: (BuildContext context, CorporateMembershipState state) {
        final CorporateProfile? profile = state.profile;
        if (profile != null) return _Profile(profile: profile);
        if (state.isLoading) {
          return const Center(child: CircularProgressIndicator());
        }
        if (state.failure != null) {
          return FailureView(
            failure: state.failure!,
            onRetry: () {
              context.read<CorporateMembershipCubit>().refresh();
              context.read<CorporateInvitationsCubit>().load();
            },
          );
        }
        return AtaCard(
          key: const ValueKey<String>('corporate-none'),
          child: Column(
            children: <Widget>[
              Text(
                l10n.corpNoMembership,
                style: AtaText.section,
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: AtaSpacing.xs),
              Text(
                l10n.corpNoMembershipCopy,
                style: AtaText.bodyMuted,
                textAlign: TextAlign.center,
              ),
            ],
          ),
        );
      },
    );
  }
}

class _Profile extends StatelessWidget {
  const _Profile({required this.profile});

  final CorporateProfile profile;

  @override
  Widget build(BuildContext context) {
    final List<RideCategory> categories = context
        .watch<CorporateCatalogCubit>()
        .state;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        CorporateDetailsCard(profile: profile),
        if (profile.isActive) ...<Widget>[
          const SizedBox(height: AtaSpacing.md),
          CorporatePolicyCard(policy: profile.policy, categories: categories),
        ],
      ],
    );
  }
}
