import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

/// "Back to settings" pill used by account sub-panels.
class BackToSettings extends StatelessWidget {
  const BackToSettings({super.key});

  @override
  Widget build(BuildContext context) {
    return Align(
      alignment: AlignmentDirectional.centerStart,
      child: PillButton.back(
        label: context.l10n.backToSettings,
        onTap: () => context.go(AppRoutes.account),
      ),
    );
  }
}
