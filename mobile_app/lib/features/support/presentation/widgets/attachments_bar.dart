import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_button.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/design/widgets/inline_error.dart';
import 'package:ata_app/features/support/presentation/cubit/attachments_cubit.dart';
import 'package:ata_app/l10n/generated/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// The attachments of a message being written: chosen files with their
/// upload status (tap a failed one to retry, × removes) and the add button.
class AttachmentsBar extends StatelessWidget {
  const AttachmentsBar({super.key, this.showHint = true});

  final bool showHint;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    return BlocBuilder<AttachmentsCubit, AttachmentsState>(
      builder: (BuildContext context, AttachmentsState state) {
        final AttachmentsCubit cubit = context.read<AttachmentsCubit>();
        return Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: <Widget>[
            for (final AttachmentItem item in state.items)
              _Row(item: item, cubit: cubit),
            if (state.failure != null) ...<Widget>[
              InlineError(message: failureText(state.failure!, l10n)),
              const SizedBox(height: AtaSpacing.xs),
            ],
            AtaButton(
              key: const ValueKey<String>('add-attachment'),
              label: l10n.attachmentsAdd,
              icon: AtaIcons.upload,
              variant: AtaButtonVariant.outline,
              height: AtaSizes.buttonCompact,
              onPressed: state.canAdd ? cubit.pick : null,
            ),
            if (showHint)
              Padding(
                padding: const EdgeInsets.only(top: AtaSpacing.xxs),
                child: Text(l10n.attachmentsHint, style: AtaText.caption),
              ),
          ],
        );
      },
    );
  }
}

class _Row extends StatelessWidget {
  const _Row({required this.item, required this.cubit});

  final AttachmentItem item;
  final AttachmentsCubit cubit;

  @override
  Widget build(BuildContext context) {
    final AppLocalizations l10n = context.l10n;
    final bool failed = item.status == AttachmentStatus.failed;
    return Container(
      margin: const EdgeInsets.only(bottom: AtaSpacing.xs),
      padding: const EdgeInsets.symmetric(
        horizontal: AtaSpacing.sm,
        vertical: AtaSpacing.xs,
      ),
      decoration: BoxDecoration(
        color: failed ? AtaColors.dangerSoft : AtaColors.cloud,
        borderRadius: AtaRadii.smallRadius,
      ),
      child: Row(
        children: <Widget>[
          Expanded(
            child: InkWell(
              onTap: failed ? () => cubit.retry(item.localId) : null,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: <Widget>[
                  Text(
                    item.file.name,
                    style: AtaText.label,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                  if (item.status == AttachmentStatus.uploading)
                    Text(l10n.attachmentUploading, style: AtaText.caption)
                  else if (failed)
                    Text(
                      l10n.attachmentFailedRetry,
                      style: AtaText.caption.copyWith(color: AtaColors.danger),
                    ),
                ],
              ),
            ),
          ),
          IconButton(
            tooltip: l10n.attachmentRemove,
            onPressed: () => cubit.remove(item.localId),
            icon: const Icon(Icons.close, size: AtaSizes.iconDefault),
          ),
        ],
      ),
    );
  }
}
