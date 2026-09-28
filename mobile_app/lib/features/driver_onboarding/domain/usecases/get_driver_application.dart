import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/driver_onboarding/domain/entities/driver_application.dart';
import 'package:ata_app/features/driver_onboarding/domain/repositories/driver_onboarding_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Loads the driver application with documents and steps.
class GetDriverApplication implements UseCase<DriverApplication, NoParams> {
  const GetDriverApplication(this._repository);

  final DriverOnboardingRepository _repository;

  @override
  Future<Either<Failure, DriverApplication>> call(NoParams params) =>
      _repository.getApplication();
}
