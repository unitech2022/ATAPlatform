import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/widgets/failure_view.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/features/driver_wallet/presentation/widgets/driver_subpage.dart';
import 'package:ata_app/features/safety/domain/entities/lost_item.dart';
import 'package:ata_app/features/safety/presentation/cubit/lost_items_cubit.dart';
import 'package:ata_app/features/safety/presentation/cubit/safety_list_state.dart';
import 'package:ata_app/features/safety/presentation/widgets/lost_item_tile.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// `/driver/lost-items` and `/driver/lost-items/:reportId`: items reported
/// by passengers (no passenger phone), answered found / not found.
class DriverLostItemsPage extends StatelessWidget {
  const DriverLostItemsPage({super.key, this.reportId});

  /// Highlighted report (from `ata://driver/lost-items/{id}`).
  final String? reportId;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocProvider<DriverLostItemsCubit>(
      create: (_) =>
          DriverLostItemsCubit(getItems: getIt(), respond: getIt())..load(),
      child: BlocBuilder<DriverLostItemsCubit, SafetyListState<LostItemReport>>(
        builder: (BuildContext context, SafetyListState<LostItemReport> s) {
          final DriverLostItemsCubit cubit = context
              .read<DriverLostItemsCubit>();
          return DriverSubpage.content(
            eyebrow: l10n.driverAccountEyebrow,
            title: l10n.driverLostItemsTitle,
            copy: l10n.driverLostItemsCopy,
            children: <Widget>[
              if (s.loading && s.items.isEmpty)
                const CenteredLoader()
              else if (s.failure != null && s.items.isEmpty)
                FailureView(failure: s.failure!, onRetry: cubit.load)
              else if (s.isEmpty)
                Text(l10n.noLostItems, style: AtaText.bodyMuted)
              else
                for (final LostItemReport r in s.items)
                  LostItemTile(
                    report: r,
                    highlighted: r.id == reportId,
                    actions: r.awaitsDriver
                        ? _Answer(report: r, busy: s.busyId == r.id)
                        : null,
                  ),
              if (s.failure != null && s.items.isNotEmpty)
                InlineError(message: failureText(s.failure!, l10n)),
            ],
          );
        },
      ),
    );
  }
}

class _Answer extends StatelessWidget {
  const _Answer({required this.report, required this.busy});

  final LostItemReport report;
  final bool busy;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final DriverLostItemsCubit cubit = context.read<DriverLostItemsCubit>();
    return Row(
      children: <Widget>[
        Expanded(
          child: AtaButton(
            label: l10n.lostItemFound,
            variant: AtaButtonVariant.brand,
            height: AtaSizes.buttonCompact,
            loading: busy,
            onPressed: busy
                ? null
                : () => cubit.respond(report.id, found: true),
          ),
        ),
        const SizedBox(width: AtaSpacing.sm),
        Expanded(
          child: AtaButton(
            label: l10n.lostItemNotFound,
            variant: AtaButtonVariant.outline,
            height: AtaSizes.buttonCompact,
            onPressed: busy
                ? null
                : () => cubit.respond(report.id, found: false),
          ),
        ),
      ],
    );
  }
}
