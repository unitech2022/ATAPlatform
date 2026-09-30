import 'dart:typed_data';

import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/support/domain/repositories/support_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `GET /files/{id}` with the session token (attachments).
class DownloadSupportFile implements UseCase<Uint8List, String> {
  const DownloadSupportFile(this._repository);

  final SupportRepository _repository;

  @override
  Future<Either<Failure, Uint8List>> call(String params) =>
      _repository.downloadFile(params);
}
