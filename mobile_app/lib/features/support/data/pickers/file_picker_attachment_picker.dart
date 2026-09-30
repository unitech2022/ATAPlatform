import 'package:ata_app/features/support/domain/entities/attachments.dart';
import 'package:ata_app/features/support/domain/repositories/support_repository.dart';
import 'package:file_picker/file_picker.dart';
import 'package:flutter/foundation.dart';

/// [AttachmentPicker] over the native file chooser (`file_picker`), limited
/// to jpg / png / pdf. Mobile and desktop return a path, the web the bytes.
class FilePickerAttachmentPicker implements AttachmentPicker {
  const FilePickerAttachmentPicker();

  @override
  Future<List<PickedAttachment>> pick({required int maxCount}) async {
    final FilePickerResult? result = await FilePicker.pickFiles(
      type: FileType.custom,
      allowedExtensions: AttachmentRules.extensions,
      allowMultiple: maxCount > 1,
      withData: kIsWeb,
    );
    if (result == null) return const <PickedAttachment>[];
    return result.files
        .take(maxCount)
        .map(
          (PlatformFile f) => PickedAttachment(
            name: f.name,
            sizeBytes: f.size,
            path: kIsWeb ? null : f.path,
            bytes: f.bytes,
          ),
        )
        .toList(growable: false);
  }
}
