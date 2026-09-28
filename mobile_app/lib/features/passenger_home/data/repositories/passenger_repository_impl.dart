import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/passenger_home/data/datasources/passenger_remote_data_source.dart';
import 'package:ata_app/features/passenger_home/domain/repositories/passenger_repository.dart';
import 'package:fpdart/fpdart.dart';

/// [PassengerRepository] backed by the API.
class PassengerRepositoryImpl implements PassengerRepository {
  const PassengerRepositoryImpl(this._remote);

  final PassengerRemoteDataSource _remote;

  @override
  Future<Either<Failure, Unit>> updatePreferences({
    bool? preferFemaleDriver,
    String? defaultPaymentMethod,
  }) => guard(() async {
    await _remote.updatePreferences(<String, dynamic>{
      'preferFemaleDriver': ?preferFemaleDriver,
      'defaultPaymentMethod': ?defaultPaymentMethod,
    });
    return unit;
  });
}
