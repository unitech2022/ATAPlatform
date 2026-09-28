import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_header.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/ata_logo.dart';
import 'package:ata_app/design/widgets/icon_box.dart';
import 'package:ata_app/features/auth/presentation/cubit/session_cubit.dart';
import 'package:ata_app/features/auth/presentation/cubit/session_state.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Logo + "driver portal" on the start side, clock and profile pill on the end.
class DriverHeader extends StatelessWidget {
  const DriverHeader({super.key});

  @override
  Widget build(BuildContext context) {
    final String? name = context.select(
      (SessionCubit cubit) => cubit.state.session?.user.fullName,
    );
    return AtaHeader(
      leading: <Widget>[
        const AtaLogo(),
        Text(context.l10n.driverPortal, style: AtaText.label),
      ],
      trailing: <Widget>[
        HeaderIconButton(
          onTap: () {},
          badge: Container(
            width: AtaSizes.badgeDot,
            height: AtaSizes.badgeDot,
            decoration: const BoxDecoration(
              color: AtaColors.brand,
              shape: BoxShape.circle,
            ),
          ),
          child: const AtaIcon(AtaIcons.clock, color: AtaColors.ink),
        ),
        Container(
          padding: const EdgeInsetsDirectional.only(
            start: AtaSpacing.xs,
            end: AtaSpacing.md,
            top: AtaSpacing.xs,
            bottom: AtaSpacing.xs,
          ),
          decoration: const BoxDecoration(
            color: AtaColors.ink,
            borderRadius: AtaRadii.pillRadius,
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: <Widget>[
              const IconBox.brand(
                icon: AtaIcons.user,
                size: AtaSizes.iconBoxSmall - AtaSpacing.xs,
                iconSize: AtaSizes.iconSmall,
                round: true,
              ),
              if (name != null) ...<Widget>[
                const SizedBox(width: AtaSpacing.xs),
                Text(
                  name,
                  style: AtaText.label.copyWith(color: AtaColors.white),
                ),
              ],
            ],
          ),
        ),
      ],
    );
  }
}

/// Convenience selector for the session's driver status.
extension DriverSessionX on BuildContext {
  SessionState get sessionState => read<SessionCubit>().state;
}
