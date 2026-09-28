import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_shadows.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/features/driver_dashboard/presentation/cubit/driver_tabs_cubit.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// White segmented tab bar with the active tab on `ink`.
class DriverTabs extends StatelessWidget {
  const DriverTabs({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final List<(DriverTab, String)> tabs = <(DriverTab, String)>[
      (DriverTab.overview, l10n.tabOverview),
      (DriverTab.documents, l10n.tabDocuments),
      (DriverTab.settings, l10n.tabSettings),
    ];
    return BlocBuilder<DriverTabsCubit, DriverTab>(
      builder: (BuildContext context, DriverTab current) {
        return Container(
          padding: const EdgeInsets.all(AtaSpacing.xs),
          decoration: BoxDecoration(
            color: AtaColors.white,
            borderRadius: AtaRadii.itemRadius,
            boxShadow: AtaShadows.soft,
          ),
          child: SingleChildScrollView(
            scrollDirection: Axis.horizontal,
            child: Row(
              children: <Widget>[
                for (final (DriverTab tab, String label) in tabs)
                  Material(
                    color: tab == current ? AtaColors.ink : Colors.transparent,
                    borderRadius: AtaRadii.smallRadius,
                    child: InkWell(
                      borderRadius: AtaRadii.smallRadius,
                      onTap: () => context.read<DriverTabsCubit>().select(tab),
                      child: Padding(
                        padding: const EdgeInsets.symmetric(
                          horizontal: AtaSpacing.lg,
                          vertical: AtaSpacing.sm,
                        ),
                        child: Text(
                          label,
                          style: AtaText.label.copyWith(
                            color: tab == current
                                ? AtaColors.white
                                : AtaColors.muted,
                          ),
                        ),
                      ),
                    ),
                  ),
              ],
            ),
          ),
        );
      },
    );
  }
}
