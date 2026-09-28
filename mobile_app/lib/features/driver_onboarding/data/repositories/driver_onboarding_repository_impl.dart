import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/driver_onboarding/data/datasources/driver_onboarding_remote_data_source.dart';
import 'package:ata_app/features/driver_onboarding/domain/entities/driver_application.dart';
import 'package:ata_app/features/driver_onboarding/domain/repositories/driver_onboarding_repository.dart';
import 'package:fpdart/fpdart.dart';

/// [DriverOnboardingRepository] backed by the API.
class DriverOnboardingRepositoryImpl implements DriverOnboardingRepository {
  const DriverOnboardingRepositoryImpl(this._remote);

  final DriverOnboardingRemoteDataSource _remote;

  @override
  Future<Either<Failure, DriverApplication>> getApplication() =>
      guard(_remote.application);
}
