import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_shadows.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_toggle.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/design/widgets/screen_title.dart';
import 'package:ata_app/features/auth/presentation/cubit/session_cubit.dart';
import 'package:ata_app/features/driver_dashboard/presentation/cubit/online_status_cubit.dart';
import 'package:ata_app/features/driver_dashboard/presentation/cubit/online_status_state.dart';
import 'package:ata_app/features/driver_dashboard/presentation/widgets/debt_block_card.dart';
import 'package:ata_app/features/trip/presentation/widgets/location_notice.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Greeting and the online / offline toggle button.
class DriverHero extends StatelessWidget {
  const DriverHero({super.key});

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final String? firstName = context.select(
      (SessionCubit cubit) => cubit.state.session?.user.firstName,
    );
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: <Widget>[
        ScreenTitle(
          eyebrow: l10n.driverAccountEyebrow,
          title: l10n.driverWelcome(firstName ?? l10n.driverGuestName),
          copy: l10n.driverDashboardCopy,
        ),
        const SizedBox(height: AtaSpacing.lg),
        BlocBuilder<OnlineStatusCubit, OnlineStatusState>(
          builder: (BuildContext context, OnlineStatusState state) {
            final OnlineStatusCubit cubit = context.read<OnlineStatusCubit>();
            return Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: <Widget>[
                Material(
                  color: state.isOnline ? AtaColors.brand : AtaColors.white,
                  borderRadius: AtaRadii.itemRadius,
                  child: InkWell(
                    borderRadius: AtaRadii.itemRadius,
                    onTap: state.updating ? null : cubit.toggle,
                    child: Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: AtaSpacing.lg,
                        vertical: AtaSpacing.sm,
                      ),
                      decoration: BoxDecoration(
                        borderRadius: AtaRadii.itemRadius,
                        boxShadow: AtaShadows.soft,
                      ),
                      child: Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: <Widget>[
                          Text(
                            state.isOnline
                                ? l10n.onlineLabel
                                : l10n.offlineLabel,
                            style: AtaText.bodyStrong.copyWith(
                              color: state.isOnline
                                  ? AtaColors.white
                                  : AtaColors.muted,
                            ),
                          ),
                          AtaToggle(
                            value: state.isOnline,
                            onDark: true,
                            onChanged: state.updating
                                ? null
                                : (_) => cubit.toggle(),
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
                if (state.debtBlock != null) ...<Widget>[
                  const SizedBox(height: AtaSpacing.sm),
                  DebtBlockCard(block: state.debtBlock!),
                ] else if (state.failure != null) ...<Widget>[
                  const SizedBox(height: AtaSpacing.sm),
                  InlineError(message: failureText(state.failure!, l10n)),
                ],
                if (state.isOnline) const LocationNotice(),
              ],
            );
          },
        ),
      ],
    );
  }
}
