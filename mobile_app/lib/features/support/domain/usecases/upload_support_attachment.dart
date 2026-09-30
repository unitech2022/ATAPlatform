import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/support/domain/entities/attachments.dart';
import 'package:ata_app/features/support/domain/repositories/support_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `POST /support/attachments` (multipart): jpg / png / pdf up to 10 MB.
class UploadSupportAttachment
    implements UseCase<UploadedAttachment, PickedAttachment> {
  const UploadSupportAttachment(this._repository);

  final SupportRepository _repository;

  @override
  Future<Either<Failure, UploadedAttachment>> call(PickedAttachment params) =>
      _repository.uploadAttachment(params);
}
