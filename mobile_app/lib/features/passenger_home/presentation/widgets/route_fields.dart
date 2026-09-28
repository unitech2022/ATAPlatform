import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_shadows.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_cubit.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_state.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Pickup row, extra stops, destination row and the "add stop" action.
class RouteFields extends StatelessWidget {
  const RouteFields({super.key});

  static const double _dotSize = 12;
  static const double _stopBadge = 20;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final List<String> options = <String>[
      l10n.stopOption1,
      l10n.stopOption2,
      l10n.stopOption3,
    ];
    return BlocBuilder<HomeCubit, HomeState>(
      buildWhen: (HomeState p, HomeState c) =>
          p.stops != c.stops || p.maxStops != c.maxStops,
      builder: (BuildContext context, HomeState state) {
        final HomeCubit cubit = context.read<HomeCubit>();
        final String? nextStop = options
            .where((String option) => !state.stops.contains(option))
            .firstOrNull;
        final bool canAdd = state.canAddStop && nextStop != null;
        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: <Widget>[
            _RouteRow(
              background: AtaColors.cloud,
              marker: Container(
                width: _dotSize,
                height: _dotSize,
                decoration: BoxDecoration(
                  color: AtaColors.white,
                  shape: BoxShape.circle,
                  border: Border.all(color: AtaColors.brand, width: 4),
                ),
              ),
              label: l10n.pickupLabel,
              value: l10n.pickupCurrent,
              trailing: const AtaIcon(
                AtaIcons.location,
                color: AtaColors.brand,
              ),
            ),
            for (int i = 0; i < state.stops.length; i++) ...<Widget>[
              const SizedBox(height: AtaSpacing.xs),
              _RouteRow(
                background: AtaColors.white,
                borderColor: AtaColors.line,
                marker: Container(
                  width: _stopBadge,
                  height: _stopBadge,
                  alignment: Alignment.center,
                  decoration: const BoxDecoration(
                    color: AtaColors.brandSoft,
                    shape: BoxShape.circle,
                  ),
                  child: Text(
                    '${i + 1}',
                    style: AtaText.captionStrong.copyWith(
                      color: AtaColors.brand,
                    ),
                  ),
                ),
                label: l10n.stopLabel,
                value: state.stops[i],
                trailing: TextButton(
                  style: TextButton.styleFrom(
                    backgroundColor: AtaColors.dangerSoft,
                    padding: const EdgeInsets.symmetric(
                      horizontal: AtaSpacing.xs,
                    ),
                    minimumSize: Size.zero,
                    tapTargetSize: MaterialTapTargetSize.shrinkWrap,
                  ),
                  onPressed: () => cubit.removeStop(i),
                  child: Text(
                    l10n.removeStop,
                    style: AtaText.captionStrong.copyWith(
                      color: AtaColors.danger,
                    ),
                  ),
                ),
              ),
            ],
            const SizedBox(height: AtaSpacing.xs),
            _RouteRow(
              background: AtaColors.white,
              borderColor: AtaColors.brand,
              borderWidth: AtaSizes.borderThick,
              shadow: AtaShadows.brand,
              marker: Container(
                width: _dotSize,
                height: _dotSize,
                decoration: const BoxDecoration(
                  color: AtaColors.ink,
                  borderRadius: BorderRadius.all(Radius.circular(2)),
                ),
              ),
              label: l10n.destinationLabel,
              value: l10n.destinationDefault,
              trailing: const AtaIcon(AtaIcons.search, color: AtaColors.muted),
            ),
            const SizedBox(height: AtaSpacing.sm),
            _AddStopButton(
              enabled: canAdd,
              label: state.canAddStop ? l10n.addStop : l10n.maxStopsReached,
              onTap: canAdd ? () => cubit.addStop(nextStop) : null,
            ),
          ],
        );
      },
    );
  }
}

class _RouteRow extends StatelessWidget {
  const _RouteRow({
    required this.background,
    required this.marker,
    required this.label,
    required this.value,
    required this.trailing,
    this.borderColor,
    this.borderWidth = AtaSizes.borderThin,
    this.shadow,
  });

  final Color background;
  final Widget marker;
  final String label;
  final String value;
  final Widget trailing;
  final Color? borderColor;
  final double borderWidth;
  final List<BoxShadow>? shadow;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(AtaSpacing.md),
      decoration: BoxDecoration(
        color: background,
        borderRadius: AtaRadii.itemRadius,
        border: borderColor == null
            ? null
            : Border.all(color: borderColor!, width: borderWidth),
        boxShadow: shadow,
      ),
      child: Row(
        children: <Widget>[
          marker,
          const SizedBox(width: AtaSpacing.sm),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: <Widget>[
                Text(label, style: AtaText.caption),
                Text(
                  value,
                  style: AtaText.bodyStrong,
                  overflow: TextOverflow.ellipsis,
                ),
              ],
            ),
          ),
          trailing,
        ],
      ),
    );
  }
}

class _AddStopButton extends StatelessWidget {
  const _AddStopButton({
    required this.enabled,
    required this.label,
    required this.onTap,
  });

  final bool enabled;
  final String label;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final Color color = enabled ? AtaColors.brand : AtaColors.muted;
    return Material(
      type: MaterialType.transparency,
      child: InkWell(
        onTap: onTap,
        borderRadius: AtaRadii.smallRadius,
        child: Container(
          padding: const EdgeInsets.symmetric(vertical: AtaSpacing.sm),
          decoration: BoxDecoration(
            borderRadius: AtaRadii.smallRadius,
            border: Border.all(
              color: enabled ? AtaColors.brand : AtaColors.line,
            ),
          ),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: <Widget>[
              AtaIcon(AtaIcons.plus, size: AtaSizes.iconSmall, color: color),
              const SizedBox(width: AtaSpacing.xs),
              Text(label, style: AtaText.label.copyWith(color: color)),
            ],
          ),
        ),
      ),
    );
  }
}
