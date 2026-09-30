import 'dart:async';

import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/features/support/domain/entities/attachments.dart';
import 'package:ata_app/features/support/domain/usecases/pick_support_attachments.dart';
import 'package:ata_app/features/support/domain/usecases/upload_support_attachment.dart';
import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Upload progress of one attachment.
enum AttachmentStatus { uploading, uploaded, failed }

/// A chosen file and its upload result.
class AttachmentItem extends Equatable {
  const AttachmentItem({
    required this.localId,
    required this.file,
    this.status = AttachmentStatus.uploading,
    this.fileId,
    this.failure,
  });

  final int localId;
  final PickedAttachment file;
  final AttachmentStatus status;
  final String? fileId;
  final Failure? failure;

  AttachmentItem copyWith({
    AttachmentStatus? status,
    String? fileId,
    Failure? failure,
  }) => AttachmentItem(
    localId: localId,
    file: file,
    status: status ?? this.status,
    fileId: fileId ?? this.fileId,
    failure: status == AttachmentStatus.failed ? failure : null,
  );

  @override
  List<Object?> get props => <Object?>[localId, file, status, fileId, failure];
}

/// State of `AttachmentsCubit`.
class AttachmentsState extends Equatable {
  const AttachmentsState({this.items = const <AttachmentItem>[], this.failure});

  final List<AttachmentItem> items;

  /// Why the last pick was (partly) refused: `attachment_limit`,
  /// `unsupported_file_type` or `file_too_large`.
  final Failure? failure;

  bool get uploading =>
      items.any((AttachmentItem i) => i.status == AttachmentStatus.uploading);

  bool get hasFailed =>
      items.any((AttachmentItem i) => i.status == AttachmentStatus.failed);

  bool get canAdd => items.length < AttachmentRules.maxCount;

  /// Ids to send with the message (uploaded files only).
  List<String> get fileIds => items
      .where((AttachmentItem i) => i.fileId != null)
      .map((AttachmentItem i) => i.fileId!)
      .toList(growable: false);

  @override
  List<Object?> get props => <Object?>[items, failure];
}

/// Files attached to a new ticket or a reply: picks up to five jpg / png /
/// pdf files (10 MB each) and uploads them at once
/// (`POST /support/attachments`), so sending only needs the `fileIds`.
class AttachmentsCubit extends Cubit<AttachmentsState> {
  AttachmentsCubit({required this._pick, required this._upload})
    : super(const AttachmentsState());

  static const String limitCode = ErrorCodes.attachmentLimit;
  static const String typeCode = ErrorCodes.unsupportedFileType;
  static const String sizeCode = ErrorCodes.fileTooLarge;

  final PickSupportAttachments _pick;
  final UploadSupportAttachment _upload;

  int _seq = 0;

  Future<void> pick() async {
    final int room = AttachmentRules.maxCount - state.items.length;
    if (room <= 0) return _refuse(limitCode);
    final result = await _pick(room);
    if (isClosed) return;
    final List<PickedAttachment> files = result.fold((Failure f) {
      emit(AttachmentsState(items: state.items, failure: f));
      return const <PickedAttachment>[];
    }, (List<PickedAttachment> f) => f);
    if (files.isEmpty) return;
    String? refused = files.length > room ? limitCode : null;
    final List<AttachmentItem> added = <AttachmentItem>[];
    for (final PickedAttachment file in files.take(room)) {
      if (file.contentType == null) {
        refused ??= typeCode;
      } else if (file.sizeBytes > AttachmentRules.maxBytes) {
        refused ??= sizeCode;
      } else {
        added.add(AttachmentItem(localId: _seq++, file: file));
      }
    }
    emit(
      AttachmentsState(
        items: <AttachmentItem>[...state.items, ...added],
        failure: refused == null ? null : _local(refused),
      ),
    );
    for (final AttachmentItem item in added) {
      unawaited(_send(item));
    }
  }

  void remove(int localId) => emit(
    AttachmentsState(
      items: state.items
          .where((AttachmentItem i) => i.localId != localId)
          .toList(growable: false),
    ),
  );

  Future<void> retry(int localId) async {
    final AttachmentItem? item = state.items
        .where((AttachmentItem i) => i.localId == localId)
        .firstOrNull;
    if (item == null || item.status != AttachmentStatus.failed) return;
    _replace(item.copyWith(status: AttachmentStatus.uploading));
    await _send(item);
  }

  /// Forgets everything (after a message was sent).
  void clear() => emit(const AttachmentsState());

  Future<void> _send(AttachmentItem item) async {
    final result = await _upload(item.file);
    if (isClosed) return;
    final bool stillThere = state.items.any(
      (AttachmentItem i) => i.localId == item.localId,
    );
    if (!stillThere) return;
    _replace(
      result.fold(
        (Failure f) =>
            item.copyWith(status: AttachmentStatus.failed, failure: f),
        (UploadedAttachment u) =>
            item.copyWith(status: AttachmentStatus.uploaded, fileId: u.fileId),
      ),
    );
  }

  void _replace(AttachmentItem next) => emit(
    AttachmentsState(
      items: state.items
          .map((AttachmentItem i) => i.localId == next.localId ? next : i)
          .toList(growable: false),
      failure: state.failure,
    ),
  );

  void _refuse(String code) =>
      emit(AttachmentsState(items: state.items, failure: _local(code)));

  static Failure _local(String code) => ServerFailure(code: code, message: '');
}
