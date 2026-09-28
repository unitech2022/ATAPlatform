import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/safety/domain/entities/lost_item.dart';
import 'package:ata_app/features/safety/domain/repositories/safety_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `POST /passenger/trips/{id}/lost-items` (completed trip, 7 days).
class ReportLostItem implements UseCase<LostItemReport, LostItemDraft> {
  const ReportLostItem(this._repository);

  final SafetyRepository _repository;

  @override
  Future<Either<Failure, LostItemReport>> call(LostItemDraft params) =>
      _repository.reportLostItem(params);
}
