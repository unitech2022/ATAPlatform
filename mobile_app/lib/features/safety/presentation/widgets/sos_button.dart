import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_shadows.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/features/safety/presentation/cubit/sos_cubit.dart';
import 'package:ata_app/features/safety/presentation/cubit/sos_hold_cubit.dart';
import 'package:ata_app/features/safety/presentation/cubit/sos_state.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Red SOS button: press and hold (progress ring driven by
/// [SosHoldCubit]) to raise the SOS through the app-wide [SosCubit].
class SosButton extends StatelessWidget {
  const SosButton({
    super.key,
    this.tripId,
    this.role,
    this.fallback,
    this.size = defaultSize,
    this.showHint = true,
  });

  static const double defaultSize = 64;
  static const double _ringStroke = 4;

  final String? tripId;

  /// `passenger` / `driver`.
  final String? role;

  /// Used when the device cannot be located in time.
  final GeoPoint? fallback;
  final double size;
  final bool showHint;

  @override
  Widget build(BuildContext context) {
    return BlocProvider<SosHoldCubit>(
      create: (_) => SosHoldCubit(),
      child: BlocListener<SosHoldCubit, SosHoldState>(
        listenWhen: (SosHoldState p, SosHoldState c) =>
            !p.confirmed && c.confirmed,
        listener: (BuildContext context, _) {
          context.read<SosCubit>().trigger(
            tripId: tripId,
            role: role,
            fallback: fallback,
          );
          context.read<SosHoldCubit>().reset();
        },
        child: BlocBuilder<SosCubit, SosState>(
          buildWhen: (SosState p, SosState c) => p.status != c.status,
          builder: (BuildContext context, SosState sos) {
            final bool disabled = sos.isActive || sos.isBusy;
            return BlocBuilder<SosHoldCubit, SosHoldState>(
              builder: (BuildContext context, SosHoldState hold) =>
                  _button(context, hold, disabled),
            );
          },
        ),
      ),
    );
  }

  Widget _button(BuildContext context, SosHoldState hold, bool disabled) {
    final AppLocalizations l10n = context.l10n;
    final SosHoldCubit cubit = context.read<SosHoldCubit>();
    return Semantics(
      button: true,
      label: l10n.sosSemantics,
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: <Widget>[
          GestureDetector(
            onTapDown: disabled ? null : (_) => cubit.press(),
            onTapUp: disabled ? null : (_) => cubit.release(),
            onTapCancel: disabled ? null : cubit.release,
            child: SizedBox.square(
              dimension: size,
              child: Stack(
                alignment: Alignment.center,
                children: <Widget>[
                  Container(
                    decoration: BoxDecoration(
                      color: disabled ? AtaColors.muted : AtaColors.danger,
                      shape: BoxShape.circle,
                      boxShadow: AtaShadows.soft,
                    ),
                  ),
                  SizedBox.square(
                    dimension: size,
                    child: CircularProgressIndicator(
                      value: hold.progress,
                      strokeWidth: _ringStroke,
                      color: AtaColors.white,
                      backgroundColor: AtaColors.white25,
                    ),
                  ),
                  Text(
                    l10n.sosLabel,
                    style: AtaText.label.copyWith(color: AtaColors.white),
                  ),
                ],
              ),
            ),
          ),
          if (showHint) ...<Widget>[
            const SizedBox(height: AtaSpacing.xxs),
            Text(
              hold.holding ? l10n.sosKeepHolding : l10n.sosHoldHint,
              style: AtaText.caption.copyWith(color: AtaColors.danger),
            ),
          ],
        ],
      ),
    );
  }
}
