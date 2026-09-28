import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/trip/domain/entities/driver_location.dart';
import 'package:ata_app/features/trip/domain/repositories/trip_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `PUT /driver/location`.
class SendDriverLocation implements UseCase<Unit, DriverPosition> {
  const SendDriverLocation(this._repository);

  final TripRepository _repository;

  @override
  Future<Either<Failure, Unit>> call(DriverPosition params) =>
      _repository.sendLocation(params);
}
