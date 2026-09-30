import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/core/localization/l10n_extension.dart';
import 'package:ata_app/design/tokens/ata_colors.dart';
import 'package:ata_app/design/tokens/ata_radii.dart';
import 'package:ata_app/design/tokens/ata_spacing.dart';
import 'package:ata_app/design/tokens/ata_text.dart';
import 'package:ata_app/design/widgets/ata_icon.dart';
import 'package:ata_app/design/widgets/ata_icon_data.dart';
import 'package:ata_app/features/support/domain/entities/ticket.dart';
import 'package:ata_app/features/support/presentation/cubit/attachment_file_cubit.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:share_plus/share_plus.dart';

/// An attachment of a message. The file endpoint needs the session token, so
/// the bytes come from [AttachmentFileCubit]: images are shown inline (tap
/// to enlarge), PDFs are fetched on tap and handed to the share sheet.
class AttachmentView extends StatelessWidget {
  const AttachmentView({super.key, required this.attachment});

  final TicketAttachment attachment;

  static const double _thumb = 160;

  @override
  Widget build(BuildContext context) {
    return BlocProvider<AttachmentFileCubit>(
      create: (_) {
        final AttachmentFileCubit cubit = AttachmentFileCubit(
          download: getIt(),
          fileId: attachment.fileId,
        );
        if (attachment.isImage) cubit.load();
        return cubit;
      },
      child: BlocConsumer<AttachmentFileCubit, AttachmentFileState>(
        listenWhen: (AttachmentFileState p, AttachmentFileState c) =>
            !attachment.isImage && p.bytes == null && c.bytes != null,
        listener: (BuildContext context, AttachmentFileState state) =>
            _share(state),
        builder: (BuildContext context, AttachmentFileState state) =>
            attachment.isImage ? _image(context, state) : _file(context, state),
      ),
    );
  }

  Widget _image(BuildContext context, AttachmentFileState state) {
    final bytes = state.bytes;
    return ClipRRect(
      borderRadius: AtaRadii.smallRadius,
      child: SizedBox(
        width: _thumb,
        height: _thumb,
        child: bytes == null
            ? ColoredBox(
                color: AtaColors.cloud,
                child: Center(
                  child: state.failure != null
                      ? IconButton(
                          onPressed: context.read<AttachmentFileCubit>().load,
                          icon: const Icon(Icons.refresh),
                        )
                      : const CircularProgressIndicator(color: AtaColors.brand),
                ),
              )
            : GestureDetector(
                onTap: () => showDialog<void>(
                  context: context,
                  builder: (_) => Dialog(
                    child: InteractiveViewer(child: Image.memory(bytes)),
                  ),
                ),
                child: Image.memory(
                  bytes,
                  fit: BoxFit.cover,
                  errorBuilder: (_, _, _) => const Center(
                    child: Icon(Icons.broken_image, color: AtaColors.muted),
                  ),
                ),
              ),
      ),
    );
  }

  Widget _file(BuildContext context, AttachmentFileState state) {
    final AttachmentFileCubit cubit = context.read<AttachmentFileCubit>();
    return InkWell(
      onTap: state.loading
          ? null
          : () => state.bytes == null ? cubit.load() : _share(state),
      borderRadius: AtaRadii.smallRadius,
      child: Container(
        padding: const EdgeInsets.all(AtaSpacing.sm),
        decoration: const BoxDecoration(
          color: AtaColors.white,
          borderRadius: AtaRadii.smallRadius,
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: <Widget>[
            const AtaIcon(AtaIcons.document, color: AtaColors.ink),
            const SizedBox(width: AtaSpacing.xs),
            Flexible(
              child: Text(
                state.loading
                    ? context.l10n.threadAttachmentLoading
                    : attachment.fileName.isEmpty
                    ? context.l10n.threadAttachmentOpen
                    : attachment.fileName,
                style: AtaText.label,
                overflow: TextOverflow.ellipsis,
              ),
            ),
          ],
        ),
      ),
    );
  }

  void _share(AttachmentFileState state) {
    final bytes = state.bytes;
    if (bytes == null) return;
    // The share sheet may be unavailable (web, tests): nothing to report.
    SharePlus.instance
        .share(
          ShareParams(
            files: <XFile>[
              XFile.fromData(
                bytes,
                name: attachment.fileName,
                mimeType: attachment.contentType,
              ),
            ],
            fileNameOverrides: <String>[attachment.fileName],
          ),
        )
        .then<void>((_) {}, onError: (Object _) {});
  }
}
