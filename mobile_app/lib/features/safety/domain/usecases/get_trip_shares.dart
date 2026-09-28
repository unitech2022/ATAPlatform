import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/safety/domain/entities/trip_share.dart';
import 'package:ata_app/features/safety/domain/repositories/safety_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `GET /safety/trips/{tripId}/shares`.
class GetTripShares implements UseCase<List<TripShare>, String> {
  const GetTripShares(this._repository);

  final SafetyRepository _repository;

  @override
  Future<Either<Failure, List<TripShare>>> call(String params) =>
      _repository.getTripShares(params);
}
