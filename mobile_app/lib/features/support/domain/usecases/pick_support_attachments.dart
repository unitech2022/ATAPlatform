import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/support/domain/entities/attachments.dart';
import 'package:ata_app/features/support/domain/repositories/support_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Opens the file chooser for at most `maxCount` jpg / png / pdf files.
class PickSupportAttachments implements UseCase<List<PickedAttachment>, int> {
  const PickSupportAttachments(this._picker);

  final AttachmentPicker _picker;

  @override
  Future<Either<Failure, List<PickedAttachment>>> call(int params) =>
      guard(() => _picker.pick(maxCount: params));
}
