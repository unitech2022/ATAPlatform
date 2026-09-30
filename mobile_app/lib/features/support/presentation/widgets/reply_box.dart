import 'package:ata_app/app/router/app_routes.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/features/support/presentation/cubit/attachments_cubit.dart';
import 'package:ata_app/features/support/presentation/cubit/ticket_detail_cubit.dart';
import 'package:ata_app/features/support/presentation/cubit/ticket_detail_state.dart';
import 'package:ata_app/features/support/presentation/widgets/attachments_bar.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:go_router/go_router.dart';

/// The reply box at the bottom of a thread: attachments, the text field and
/// send. A closed ticket replaces it with a "create a new ticket" call to
/// action (`409 ticket_closed`).
class ReplyBox extends StatelessWidget {
  const ReplyBox({super.key, required this.state, required this.driver});

  final TicketDetailState state;
  final bool driver;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    if (!state.canReply) {
      return _closed(context, l10n);
    }
    final TicketDetailCubit cubit = context.read<TicketDetailCubit>();
    final AttachmentsCubit files = context.read<AttachmentsCubit>();
    final bool uploading = context.select<AttachmentsCubit, bool>(
      (AttachmentsCubit c) => c.state.uploading,
    );
    return Padding(
      padding: const EdgeInsets.all(AtaSpacing.md),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          if (state.sendFailure != null) ...<Widget>[
            InlineError(message: failureText(state.sendFailure!, l10n)),
            const SizedBox(height: AtaSpacing.xs),
          ],
          const AttachmentsBar(showHint: false),
          const SizedBox(height: AtaSpacing.xs),
          Row(
            children: <Widget>[
              Expanded(
                child: TextField(
                  key: ValueKey<int>(state.draftVersion),
                  onChanged: cubit.draftChanged,
                  maxLength: TicketDetailState.maxLength,
                  minLines: 1,
                  maxLines: 4,
                  style: AtaText.body,
                  decoration: InputDecoration(
                    hintText: l10n.threadReplyHint,
                    counterText: '',
                  ),
                ),
              ),
              const SizedBox(width: AtaSpacing.xs),
              IconButton.filled(
                key: const ValueKey<String>('reply-send'),
                tooltip: l10n.threadSend,
                onPressed: state.canSend && !uploading
                    ? () async {
                        final List<String> ids = files.state.fileIds;
                        if (await cubit.send(fileIds: ids)) files.clear();
                      }
                    : null,
                style: IconButton.styleFrom(
                  backgroundColor: AtaColors.brand,
                  disabledBackgroundColor: AtaColors.line,
                ),
                icon: const AtaIcon(AtaIcons.arrow, color: AtaColors.white),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _closed(BuildContext context, AppLocalizations l10n) {
    return Container(
      color: AtaColors.warningSoft,
      padding: const EdgeInsets.all(AtaSpacing.md),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: <Widget>[
          Text(l10n.threadClosedTitle, style: AtaText.bodyStrong),
          Text(l10n.threadClosedCopy, style: AtaText.small),
          const SizedBox(height: AtaSpacing.sm),
          AtaButton(
            key: const ValueKey<String>('create-new-ticket'),
            label: l10n.threadCreateNew,
            variant: AtaButtonVariant.brand,
            height: AtaSizes.buttonCompact,
            onPressed: () =>
                context.push(AppRoutes.newSupportTicket(driver: driver)),
          ),
        ],
      ),
    );
  }
}
