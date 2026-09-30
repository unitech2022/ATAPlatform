import 'dart:typed_data';

import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/support/domain/usecases/download_support_file.dart';
import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// State of `AttachmentFileCubit`.
class AttachmentFileState extends Equatable {
  const AttachmentFileState({this.bytes, this.loading = false, this.failure});

  final Uint8List? bytes;
  final bool loading;
  final Failure? failure;

  @override
  List<Object?> get props => <Object?>[bytes?.length, loading, failure];
}

/// Fetches one attachment with the session token (`GET /files/{id}`); the
/// file endpoint is authenticated, so `Image.network` cannot be used.
class AttachmentFileCubit extends Cubit<AttachmentFileState> {
  AttachmentFileCubit({required this._download, required this.fileId})
    : super(const AttachmentFileState());

  final DownloadSupportFile _download;
  final String fileId;

  Future<void> load() async {
    if (state.loading || state.bytes != null) return;
    emit(const AttachmentFileState(loading: true));
    final result = await _download(fileId);
    if (isClosed) return;
    emit(
      result.fold(
        (Failure f) => AttachmentFileState(failure: f),
        (Uint8List b) => AttachmentFileState(bytes: b),
      ),
    );
  }
}
