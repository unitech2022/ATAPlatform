import 'dart:typed_data';

import 'package:equatable/equatable.dart';

/// Limits of `POST /support/attachments` (`docs/11` §F18.2: jpg / png / pdf,
/// at most 10 MB and 5 per message).
abstract final class AttachmentRules {
  static const int maxCount = 5;
  static const int maxBytes = 10 * 1024 * 1024;
  static const List<String> extensions = <String>['jpg', 'jpeg', 'png', 'pdf'];

  static const Map<String, String> _types = <String, String>{
    'jpg': 'image/jpeg',
    'jpeg': 'image/jpeg',
    'png': 'image/png',
    'pdf': 'application/pdf',
  };

  /// MIME type from the file name, or `null` when not supported.
  static String? contentTypeOf(String fileName) {
    final int dot = fileName.lastIndexOf('.');
    if (dot < 0) return null;
    return _types[fileName.substring(dot + 1).toLowerCase()];
  }
}

/// A file chosen on the device, not uploaded yet (a [path] on mobile and
/// desktop, [bytes] on the web).
class PickedAttachment extends Equatable {
  const PickedAttachment({
    required this.name,
    required this.sizeBytes,
    this.path,
    this.bytes,
  });

  final String name;
  final int sizeBytes;
  final String? path;
  final Uint8List? bytes;

  String? get contentType => AttachmentRules.contentTypeOf(name);

  @override
  List<Object?> get props => <Object?>[name, sizeBytes, path];
}

/// `201` of `POST /support/attachments`.
class UploadedAttachment extends Equatable {
  const UploadedAttachment({
    required this.fileId,
    this.fileName = '',
    this.contentType = '',
    this.sizeBytes = 0,
  });

  final String fileId;
  final String fileName;
  final String contentType;
  final int sizeBytes;

  @override
  List<Object?> get props => <Object?>[
    fileId,
    fileName,
    contentType,
    sizeBytes,
  ];
}
