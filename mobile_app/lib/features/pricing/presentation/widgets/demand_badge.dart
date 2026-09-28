import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/money.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/features/pricing/domain/entities/demand_level.dart';
import 'package:ata_app/features/pricing/presentation/cubit/demand_cubit.dart';
import 'package:ata_app/features/pricing/presentation/cubit/demand_state.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Palette tokens for a demand tier: normal → brand, moderate → ink,
/// high → warning, very high → danger.
class DemandPalette {
  const DemandPalette({required this.foreground, required this.background});

  final Color foreground;
  final Color background;

  static DemandPalette of(DemandCode code) => switch (code) {
    DemandCode.normal => const DemandPalette(
      foreground: AtaColors.brand,
      background: AtaColors.brandSoft,
    ),
    DemandCode.moderate => const DemandPalette(
      foreground: AtaColors.ink,
      background: AtaColors.cloud,
    ),
    DemandCode.high => const DemandPalette(
      foreground: AtaColors.warning,
      background: AtaColors.warningSoft,
    ),
    DemandCode.veryHigh => const DemandPalette(
      foreground: AtaColors.danger,
      background: AtaColors.dangerSoft,
    ),
  };
}

/// Localized copy for demand levels.
abstract final class DemandText {
  /// "الطلب مرتفع الآن ×1.5".
  static String badge(AppLocalizations l10n, DemandLevel level) =>
      l10n.demandBadge(name(l10n, level.code), Money.compact(level.multiplier));

  static String name(AppLocalizations l10n, DemandCode code) => switch (code) {
    DemandCode.normal => l10n.demandNormal,
    DemandCode.moderate => l10n.demandModerate,
    DemandCode.high => l10n.demandHigh,
    DemandCode.veryHigh => l10n.demandVeryHigh,
  };
}

/// Coloured pill under the sheet title, shown above the normal level only.
class DemandBadge extends StatelessWidget {
  const DemandBadge({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocSelector<DemandCubit, DemandState, DemandLevel?>(
      selector: (DemandState state) => state.showBadge ? state.level : null,
      builder: (BuildContext context, DemandLevel? level) {
        if (level == null) return const SizedBox.shrink();
        return DemandChip(level: level);
      },
    );
  }
}

/// Stateless chip for a known level (reused by the breakdown sheet).
class DemandChip extends StatelessWidget {
  const DemandChip({super.key, required this.level});

  final DemandLevel level;

  @override
  Widget build(BuildContext context) {
    final DemandPalette palette = DemandPalette.of(level.code);
    return Align(
      alignment: AlignmentDirectional.centerStart,
      child: AtaBadge(
        label: DemandText.badge(context.l10n, level),
        background: palette.background,
        foreground: palette.foreground,
      ),
    );
  }
}
