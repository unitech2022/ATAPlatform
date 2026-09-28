import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/safety/domain/entities/lost_item.dart';
import 'package:ata_app/features/safety/domain/repositories/safety_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `GET /passenger/lost-items?page=`.
class GetMyLostItems implements UseCase<PageResult<LostItemReport>, int> {
  const GetMyLostItems(this._repository);

  final SafetyRepository _repository;

  @override
  Future<Either<Failure, PageResult<LostItemReport>>> call(int params) =>
      _repository.getMyLostItems(page: params);
}
