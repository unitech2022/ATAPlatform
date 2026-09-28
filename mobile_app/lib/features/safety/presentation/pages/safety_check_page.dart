import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/features/safety/presentation/cubit/safety_check_cubit.dart';
import 'package:ata_app/features/safety/presentation/cubit/safety_check_state.dart';
import 'package:ata_app/features/safety/presentation/widgets/safety_check_prompt.dart';
import 'package:ata_app/features/safety/presentation/widgets/safety_subpage.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// `/safety/check/:alertId` ("هل أنت بخير؟" opened from a push or link).
/// The app-wide [SafetyCheckCubit] was pointed at the alert by the deep
/// link binding (`SafetyCubits`).
class SafetyCheckPage extends StatelessWidget {
  const SafetyCheckPage({super.key, required this.alertId});

  final String alertId;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final bool shown = context.select(
      (SafetyCheckCubit c) =>
          c.state.isShown && c.state.alert?.id == alertId ||
          c.state.status == SafetyCheckStatus.answered,
    );
    return SafetySubpage(
      title: l10n.safetyCheckTitle,
      children: <Widget>[
        if (shown)
          const SafetyCheckPrompt()
        else
          Text(l10n.noPendingSafetyCheck, style: AtaText.bodyMuted),
      ],
    );
  }
}
