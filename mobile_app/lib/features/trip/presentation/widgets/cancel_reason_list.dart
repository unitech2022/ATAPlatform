import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/design/widgets/selectable_tile.dart';
import 'package:ata_app/features/trip/domain/entities/cancellation_reason.dart';
import 'package:ata_app/features/trip/presentation/cubit/cancel_flow_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/cancel_flow_state.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Radio list of the API reasons with excused / emergency badges.
class CancelReasonList extends StatelessWidget {
  const CancelReasonList({super.key, required this.state});

  final CancelFlowState state;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final CancelFlowCubit cubit = context.read<CancelFlowCubit>();
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        for (final CancellationReason reason in state.reasons) ...<Widget>[
          SelectableTile(
            selected: state.selected?.code == reason.code,
            onTap: state.isBusy ? null : () => cubit.select(reason),
            child: Row(
              children: <Widget>[
                RadioDot(selected: state.selected?.code == reason.code),
                const SizedBox(width: AtaSpacing.sm),
                Expanded(child: Text(reason.name, style: AtaText.bodyStrong)),
                if (reason.isEmergency)
                  AtaBadge(
                    label: l10n.reasonEmergencyBadge,
                    background: AtaColors.dangerSoft,
                    foreground: AtaColors.danger,
                  )
                else if (reason.isExcusable)
                  AtaBadge(
                    label: l10n.reasonExcusableBadge,
                    background: AtaColors.cloud,
                    foreground: AtaColors.muted,
                  ),
              ],
            ),
          ),
          const SizedBox(height: AtaSpacing.xs),
        ],
      ],
    );
  }
}
