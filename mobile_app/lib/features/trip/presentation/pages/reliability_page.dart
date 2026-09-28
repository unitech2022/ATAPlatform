import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/widgets/page_wrap.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/design/widgets/screen_title.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/presentation/widgets/reliability_details.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

/// Rider `/account/reliability` (F14).
class ReliabilityPage extends StatelessWidget {
  const ReliabilityPage({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return PageWrap(
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
          eyebrow: l10n.accountEyebrow,
          title: l10n.reliabilityTitle,
          copy: l10n.reliabilityCopy,
        ),
        const SizedBox(height: AtaSpacing.xl),
        const ReliabilityDetails(role: TripActor.passenger),
      ],
    );
  }
}
