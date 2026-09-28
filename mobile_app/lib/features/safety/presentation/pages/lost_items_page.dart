import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/widgets/failure_view.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/features/safety/domain/entities/lost_item.dart';
import 'package:ata_app/features/safety/presentation/cubit/lost_items_cubit.dart';
import 'package:ata_app/features/safety/presentation/cubit/safety_list_state.dart';
import 'package:ata_app/features/safety/presentation/widgets/lost_item_tile.dart';
import 'package:ata_app/features/safety/presentation/widgets/safety_subpage.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// `/safety/lost-items`: the passenger's lost item reports and statuses.
class LostItemsPage extends StatelessWidget {
  const LostItemsPage({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocProvider<LostItemsCubit>(
      create: (_) => LostItemsCubit(getMine: getIt())..load(),
      child: BlocBuilder<LostItemsCubit, SafetyListState<LostItemReport>>(
        builder: (BuildContext context, SafetyListState<LostItemReport> s) =>
            SafetySubpage(
              title: l10n.lostItemsTitle,
              copy: l10n.lostItemsPageCopy,
              children: <Widget>[
                if (s.loading && s.items.isEmpty)
                  const CenteredLoader()
                else if (s.failure != null && s.items.isEmpty)
                  FailureView(
                    failure: s.failure!,
                    onRetry: context.read<LostItemsCubit>().load,
                  )
                else if (s.isEmpty)
                  Text(l10n.noLostItems, style: AtaText.bodyMuted)
                else
                  for (final LostItemReport r in s.items)
                    LostItemTile(report: r),
              ],
            ),
      ),
    );
  }
}
