import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/features/passenger_home/domain/entities/ride_time.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_cubit.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_state.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// "Now" / "Schedule" segmented pills.
class TimePills extends StatelessWidget {
  const TimePills({super.key});

  @override
  Widget build(BuildContext context) {
    return BlocSelector<HomeCubit, HomeState, RideTime>(
      selector: (HomeState state) => state.rideTime,
      builder: (BuildContext context, RideTime selected) {
        final HomeCubit cubit = context.read<HomeCubit>();
        return Row(
          children: <Widget>[
            Expanded(
              child: _Pill(
                label: context.l10n.timeNow,
                active: selected == RideTime.now,
                onTap: () => cubit.selectRideTime(RideTime.now),
              ),
            ),
            const SizedBox(width: AtaSpacing.xs),
            Expanded(
              child: _Pill(
                label: context.l10n.timeSchedule,
                active: selected == RideTime.scheduled,
                onTap: () {
                  cubit.selectRideTime(RideTime.scheduled);
                  ScaffoldMessenger.of(context)
                    ..hideCurrentSnackBar()
                    ..showSnackBar(
                      SnackBar(content: Text(context.l10n.scheduleComingSoon)),
                    );
                },
              ),
            ),
          ],
        );
      },
    );
  }
}

class _Pill extends StatelessWidget {
  const _Pill({required this.label, required this.active, required this.onTap});

  final String label;
  final bool active;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final Color color = active ? AtaColors.brand : AtaColors.muted;
    return Material(
      color: active ? AtaColors.brandSoft : AtaColors.cloud,
      borderRadius: AtaRadii.smallRadius,
      child: InkWell(
        onTap: onTap,
        borderRadius: AtaRadii.smallRadius,
        child: Padding(
          padding: const EdgeInsets.symmetric(vertical: AtaSpacing.sm),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: <Widget>[
              AtaIcon(AtaIcons.clock, size: AtaSizes.iconSmall, color: color),
              const SizedBox(width: AtaSpacing.xs),
              Text(label, style: AtaText.label.copyWith(color: color)),
            ],
          ),
        ),
      ),
    );
  }
}
