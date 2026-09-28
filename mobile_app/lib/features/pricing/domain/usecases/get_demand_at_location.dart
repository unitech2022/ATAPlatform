import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/pricing/domain/entities/demand_level.dart';
import 'package:ata_app/features/pricing/domain/repositories/pricing_repository.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:fpdart/fpdart.dart';

/// `GET /pricing/demand?lat=&lng=`.
class GetDemandAtLocation implements UseCase<DemandLevel, GeoPoint> {
  const GetDemandAtLocation(this._repository);

  final PricingRepository _repository;

  @override
  Future<Either<Failure, DemandLevel>> call(GeoPoint params) =>
      _repository.demandAt(params);
}
