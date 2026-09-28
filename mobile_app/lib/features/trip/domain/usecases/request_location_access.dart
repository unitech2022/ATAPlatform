import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/trip/domain/entities/driver_location.dart';
import 'package:ata_app/features/trip/domain/repositories/location_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Asks the OS for the foreground location permission.
class RequestLocationAccess implements UseCase<LocationAccess, NoParams> {
  const RequestLocationAccess(this._repository);

  final LocationRepository _repository;

  @override
  Future<Either<Failure, LocationAccess>> call(NoParams params) =>
      guard(_repository.requestAccess);
}
