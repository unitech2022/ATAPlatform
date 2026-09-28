import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip_chat/domain/entities/trip_message.dart';
import 'package:ata_app/features/trip_chat/presentation/cubit/trip_chat_cubit.dart';
import 'package:ata_app/features/trip_chat/presentation/cubit/trip_chat_state.dart';
import 'package:ata_app/features/trip_chat/presentation/widgets/chat_input_bar.dart';
import 'package:ata_app/features/trip_chat/presentation/widgets/message_bubble.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// `/trip/chat` (rider) and `/driver/trip/chat` (driver): masked chat with
/// the other party of the active trip, backed by the app-wide
/// [TripChatCubit] (opened / left by the router binding).
class TripChatPage extends StatelessWidget {
  const TripChatPage({super.key, required this.actor});

  final TripActor actor;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocBuilder<TripChatCubit, TripChatState>(
      builder: (BuildContext context, TripChatState state) {
        final List<TripMessage> messages = state.messages.reversed.toList();
        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: <Widget>[
            Padding(
              padding: const EdgeInsets.fromLTRB(
                AtaSpacing.gutter,
                AtaSpacing.md,
                AtaSpacing.gutter,
                AtaSpacing.xs,
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: <Widget>[
                  PillButton.back(
                    label: l10n.back,
                    onTap: () => context.canPop()
                        ? context.pop()
                        : context.go(
                            actor == TripActor.driver
                                ? AppRoutes.driverTrip
                                : AppRoutes.trip,
                          ),
                  ),
                  const SizedBox(height: AtaSpacing.sm),
                  Text(
                    actor == TripActor.driver
                        ? l10n.chatWithPassenger
                        : l10n.chatWithDriver,
                    style: AtaText.headline,
                  ),
                  Text(l10n.chatMaskedNote, style: AtaText.caption),
                ],
              ),
            ),
            if (state.closed || !state.isBound)
              Container(
                color: AtaColors.warningSoft,
                padding: const EdgeInsets.all(AtaSpacing.sm),
                child: Text(
                  l10n.chatClosedBanner,
                  style: AtaText.small.copyWith(color: AtaColors.warning),
                  textAlign: TextAlign.center,
                ),
              ),
            Expanded(
              child: messages.isEmpty
                  ? Center(
                      child: Text(l10n.chatEmpty, style: AtaText.bodyMuted),
                    )
                  : ListView.builder(
                      reverse: true,
                      padding: const EdgeInsets.symmetric(
                        horizontal: AtaSpacing.gutter,
                      ),
                      itemCount: messages.length,
                      itemBuilder: (BuildContext context, int i) =>
                          MessageBubble(message: messages[i]),
                    ),
            ),
            SafeArea(top: false, child: ChatInputBar(state: state)),
          ],
        );
      },
    );
  }
}
