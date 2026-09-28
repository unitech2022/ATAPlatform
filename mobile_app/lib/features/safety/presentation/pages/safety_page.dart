import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/page_wrap.dart';
import 'package:ata_app/design/widgets/screen_title.dart';
import 'package:ata_app/features/safety/presentation/widgets/emergency_bar.dart';
import 'package:ata_app/features/safety/presentation/widgets/safety_card.dart';
import 'package:ata_app/features/safety/presentation/widgets/safety_check_prompt.dart';
import 'package:ata_app/features/trip/presentation/cubit/active_trip_cubit.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// `/safety`: trip sharing, trusted contacts, my reports, lost items and
/// the emergency bar (F12).
class SafetyPage extends StatelessWidget {
  const SafetyPage({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final bool onTrip = context.select(
      (ActiveTripCubit c) => c.state.trip?.status.hasDriver ?? false,
    );
    return PageWrap(
      children: <Widget>[
        ScreenTitle(
          eyebrow: l10n.safetyEyebrow,
          title: l10n.safetyTitle,
          copy: l10n.safetyCopy,
        ),
        const SizedBox(height: AtaSpacing.xl),
        const SafetyCheckPrompt(),
        const SizedBox(height: AtaSpacing.md),
        SafetyCard(
          icon: AtaIcons.pin,
          title: l10n.shareTripTitle,
          copy: onTrip ? l10n.shareTripCopy : l10n.shareTripOnTripOnly,
          onTap: onTrip ? () => context.go(AppRoutes.trip) : null,
        ),
        const SizedBox(height: AtaSpacing.md),
        SafetyCard(
          icon: AtaIcons.user,
          title: l10n.trustedContactsTitle,
          copy: l10n.trustedContactsCopy,
          onTap: () => context.push(AppRoutes.safetyContacts),
        ),
        const SizedBox(height: AtaSpacing.md),
        SafetyCard(
          icon: AtaIcons.shield,
          title: l10n.myReportsTitle,
          copy: l10n.myReportsCopy,
          onTap: () => context.push(AppRoutes.safetyCases),
        ),
        const SizedBox(height: AtaSpacing.md),
        SafetyCard(
          icon: AtaIcons.search,
          title: l10n.lostItemsTitle,
          copy: l10n.lostItemsCopy,
          onTap: () => context.push(AppRoutes.safetyLostItems),
        ),
        const SizedBox(height: AtaSpacing.xl),
        const EmergencyBar(),
      ],
    );
  }
}
