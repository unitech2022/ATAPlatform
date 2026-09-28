import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/core/utils/date_text.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/features/trip_chat/domain/entities/trip_message.dart';
import 'package:ata_app/features/trip_chat/presentation/cubit/trip_chat_cubit.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// One chat message: mine (brand, end), theirs (cloud, start), system
/// (centered caption); pending / failed (tap to retry) / read footers.
class MessageBubble extends StatelessWidget {
  const MessageBubble({super.key, required this.message});

  final TripMessage message;

  static const double _maxWidthFactor = 0.78;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    if (message.isSystem) {
      return Padding(
        padding: const EdgeInsets.symmetric(vertical: AtaSpacing.xs),
        child: Text(
          message.body,
          style: AtaText.caption,
          textAlign: TextAlign.center,
        ),
      );
    }
    final bool mine = message.isMine;
    final bool failed = message.delivery == MessageDelivery.failed;
    final DateTime? at = message.createdAt;
    final String footer = switch (message.delivery) {
      MessageDelivery.pending => l10n.messageSending,
      MessageDelivery.failed => l10n.messageFailed,
      MessageDelivery.sent =>
        mine && message.readAt != null
            ? l10n.messageRead
            : at == null
            ? ''
            : DateText.dayAndTime(at, context.localeCode),
    };
    return Align(
      alignment: mine
          ? AlignmentDirectional.centerEnd
          : AlignmentDirectional.centerStart,
      child: GestureDetector(
        onTap: failed
            ? () => context.read<TripChatCubit>().retry(message)
            : null,
        child: ConstrainedBox(
          constraints: BoxConstraints(
            maxWidth: MediaQuery.sizeOf(context).width * _maxWidthFactor,
          ),
          child: Container(
            margin: const EdgeInsets.symmetric(vertical: AtaSpacing.xxs),
            padding: const EdgeInsets.symmetric(
              horizontal: AtaSpacing.md,
              vertical: AtaSpacing.sm,
            ),
            decoration: BoxDecoration(
              color: failed
                  ? AtaColors.dangerSoft
                  : mine
                  ? AtaColors.brand
                  : AtaColors.cloud,
              borderRadius: AtaRadii.itemRadius,
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: <Widget>[
                Text(
                  message.body,
                  style: AtaText.body.copyWith(
                    color: mine && !failed ? AtaColors.white : AtaColors.ink,
                  ),
                ),
                if (footer.isNotEmpty)
                  Text(
                    footer,
                    style: AtaText.caption.copyWith(
                      color: failed
                          ? AtaColors.danger
                          : mine
                          ? AtaColors.white80
                          : AtaColors.muted,
                    ),
                  ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
