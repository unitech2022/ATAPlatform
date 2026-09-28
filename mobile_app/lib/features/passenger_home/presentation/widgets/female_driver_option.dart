import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/ata_toggle.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/design/widgets/selectable_tile.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_cubit.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_state.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// "I prefer a female driver" tile with the "for women" tag and toggle.
class FemaleDriverOption extends StatelessWidget {
  const FemaleDriverOption({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocSelector<HomeCubit, HomeState, bool>(
      selector: (HomeState state) => state.preferFemaleDriver,
      builder: (BuildContext context, bool enabled) {
        final HomeCubit cubit = context.read<HomeCubit>();
        return SelectableTile(
          selected: enabled,
          onTap: cubit.togglePreferFemaleDriver,
          child: Row(
            children: <Widget>[
              enabled
                  ? const IconBox.brand(icon: AtaIcons.user, round: true)
                  : const IconBox.cloud(icon: AtaIcons.user, round: true),
              const SizedBox(width: AtaSpacing.sm),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: <Widget>[
                    Row(
                      children: <Widget>[
                        Text(l10n.femaleDriverTitle, style: AtaText.bodyStrong),
                        const SizedBox(width: AtaSpacing.xs),
                        AtaBadge(
                          label: l10n.femaleDriverTag,
                          background: AtaColors.white,
                        ),
                      ],
                    ),
                    const SizedBox(height: AtaSpacing.xxs),
                    Text(l10n.femaleDriverCopy, style: AtaText.caption),
                  ],
                ),
              ),
              const SizedBox(width: AtaSpacing.sm),
              AtaToggle(
                value: enabled,
                onChanged: (_) => cubit.togglePreferFemaleDriver(),
              ),
            ],
          ),
        );
      },
    );
  }
}
