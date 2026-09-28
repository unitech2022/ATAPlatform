import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/pill.dart';
import 'package:ata_app/features/trip_chat/domain/entities/trip_message.dart';
import 'package:ata_app/features/trip_chat/presentation/cubit/trip_chat_cubit.dart';
import 'package:ata_app/features/trip_chat/presentation/cubit/trip_chat_state.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Horizontal quick replies + the text field and send button.
class ChatInputBar extends StatelessWidget {
  const ChatInputBar({super.key, required this.state});

  final TripChatState state;

  static const double _repliesHeight = 44;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final TripChatCubit cubit = context.read<TripChatCubit>();
    final bool enabled = state.isBound && !state.closed;
    return Column(
      mainAxisSize: MainAxisSize.min,
      children: <Widget>[
        if (enabled && state.quickReplies.isNotEmpty)
          SizedBox(
            height: _repliesHeight,
            child: ListView.separated(
              scrollDirection: Axis.horizontal,
              padding: const EdgeInsets.symmetric(horizontal: AtaSpacing.md),
              itemCount: state.quickReplies.length,
              separatorBuilder: (_, _) => const SizedBox(width: AtaSpacing.xs),
              itemBuilder: (BuildContext context, int i) {
                final QuickReply reply = state.quickReplies[i];
                return PillButton(
                  label: reply.text,
                  onTap: () => cubit.sendQuickReply(reply),
                  background: AtaColors.brandSoft,
                  foreground: AtaColors.brand,
                );
              },
            ),
          ),
        Padding(
          padding: const EdgeInsets.all(AtaSpacing.md),
          child: Row(
            children: <Widget>[
              Expanded(
                child: TextField(
                  key: ValueKey<int>(state.draftVersion),
                  enabled: enabled,
                  onChanged: cubit.draftChanged,
                  onSubmitted: (_) => cubit.send(),
                  maxLength: TripChatState.maxLength,
                  minLines: 1,
                  maxLines: 3,
                  textInputAction: TextInputAction.send,
                  style: AtaText.body,
                  decoration: InputDecoration(
                    hintText: l10n.chatInputHint,
                    counterText: '',
                  ),
                ),
              ),
              const SizedBox(width: AtaSpacing.xs),
              IconButton.filled(
                tooltip: l10n.chatSend,
                onPressed: state.canSend ? cubit.send : null,
                style: IconButton.styleFrom(
                  backgroundColor: AtaColors.brand,
                  disabledBackgroundColor: AtaColors.line,
                ),
                icon: const AtaIcon(AtaIcons.arrow, color: AtaColors.white),
              ),
            ],
          ),
        ),
      ],
    );
  }
}
