import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/dialer.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_header.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip_chat/domain/entities/trip_message.dart';
import 'package:ata_app/features/trip_chat/presentation/cubit/masked_call_cubit.dart';
import 'package:ata_app/features/trip_chat/presentation/cubit/trip_chat_cubit.dart';
import 'package:ata_app/features/trip_chat/presentation/widgets/unread_badge.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// Masked call + chat (with unread badge) for the active trip. The call
/// dials the proxy number or falls back to the chat; the other party's
/// real number is never used (F12.4).
class TripContactActions extends StatelessWidget {
  const TripContactActions({
    super.key,
    required this.tripId,
    required this.actor,
    this.compact = false,
  });

  final String tripId;
  final TripActor actor;

  /// Round icon buttons (driver sheet) instead of labelled buttons.
  final bool compact;

  String get _chatRoute =>
      actor == TripActor.driver ? AppRoutes.driverTripChat : AppRoutes.tripChat;

  @override
  Widget build(BuildContext context) {
    return BlocProvider<MaskedCallCubit>(
      create: (_) => MaskedCallCubit(requestCall: getIt()),
      child: BlocConsumer<MaskedCallCubit, MaskedCallState>(
        listenWhen: (MaskedCallState p, MaskedCallState c) =>
            p.requestId != c.requestId,
        listener: _onCall,
        builder: (BuildContext context, MaskedCallState call) {
          final int unread = context.select(
            (TripChatCubit c) => c.state.unreadCount,
          );
          return compact
              ? _icons(context, unread)
              : _buttons(context, call, unread);
        },
      ),
    );
  }

  void _call(BuildContext context) => context.read<MaskedCallCubit>().call(
    ChatTarget(tripId: tripId, actor: actor),
  );

  Widget _buttons(BuildContext context, MaskedCallState call, int unread) {
    final AppLocalizations l10n = context.l10n;
    return Row(
      children: <Widget>[
        Expanded(
          child: AtaButton(
            label: actor == TripActor.passenger
                ? l10n.callDriver
                : l10n.callPassenger,
            icon: AtaIcons.phone,
            variant: AtaButtonVariant.outline,
            height: AtaSizes.buttonCompact,
            loading: call.loading,
            onPressed: () => _call(context),
          ),
        ),
        const SizedBox(width: AtaSpacing.sm),
        Expanded(
          child: Stack(
            clipBehavior: Clip.none,
            children: <Widget>[
              AtaButton(
                label: l10n.chatAction,
                icon: AtaIcons.bell,
                variant: AtaButtonVariant.outline,
                height: AtaSizes.buttonCompact,
                onPressed: () => context.push(_chatRoute),
              ),
              if (unread > 0)
                PositionedDirectional(
                  top: -AtaSpacing.xxs,
                  end: -AtaSpacing.xxs,
                  child: UnreadBadge(count: unread),
                ),
            ],
          ),
        ),
      ],
    );
  }

  Widget _icons(BuildContext context, int unread) {
    final AppLocalizations l10n = context.l10n;
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: <Widget>[
        Semantics(
          label: l10n.chatAction,
          child: HeaderIconButton(
            background: AtaColors.brandSoft,
            onTap: () => context.push(_chatRoute),
            badge: unread > 0 ? UnreadBadge(count: unread) : null,
            child: const AtaIcon(AtaIcons.bell, color: AtaColors.brand),
          ),
        ),
        const SizedBox(width: AtaSpacing.xs),
        Semantics(
          label: actor == TripActor.driver
              ? l10n.callPassenger
              : l10n.callDriver,
          child: HeaderIconButton(
            background: AtaColors.brandSoft,
            onTap: () => _call(context),
            child: const AtaIcon(AtaIcons.phone, color: AtaColors.brand),
          ),
        ),
      ],
    );
  }

  void _onCall(BuildContext context, MaskedCallState state) {
    final AppLocalizations l10n = context.l10n;
    final MaskedCall? call = state.call;
    final ScaffoldMessengerState messenger = ScaffoldMessenger.of(context);
    if (call != null && call.isAvailable) {
      if (call.pin != null) {
        messenger.showSnackBar(
          SnackBar(content: Text(l10n.maskedCallPin(call.pin!))),
        );
      }
      dialNumber(context, call.proxyNumber!, failedText: l10n.callFailed);
      return;
    }
    messenger.showSnackBar(SnackBar(content: Text(l10n.callUnavailable)));
    context.push(_chatRoute);
  }
}
