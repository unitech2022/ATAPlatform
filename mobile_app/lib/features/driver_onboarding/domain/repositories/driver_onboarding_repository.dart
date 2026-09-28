import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/driver_onboarding/domain/entities/driver_application.dart';
import 'package:fpdart/fpdart.dart';

/// `/driver/application`.
abstract interface class DriverOnboardingRepository {
  Future<Either<Failure, DriverApplication>> getApplication();
}
