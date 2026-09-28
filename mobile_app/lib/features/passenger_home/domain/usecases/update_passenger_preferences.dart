import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/passenger_home/domain/repositories/passenger_repository.dart';
import 'package:equatable/equatable.dart';
import 'package:fpdart/fpdart.dart';

/// Input of [UpdatePassengerPreferences]; `null` fields are left unchanged.
class PassengerPreferencesParams extends Equatable {
  const PassengerPreferencesParams({
    this.preferFemaleDriver,
    this.defaultPaymentMethod,
  });

  final bool? preferFemaleDriver;
  final String? defaultPaymentMethod;

  @override
  List<Object?> get props => <Object?>[
    preferFemaleDriver,
    defaultPaymentMethod,
  ];
}

/// Persists rider preferences (`PATCH /passenger/preferences`).
class UpdatePassengerPreferences
    implements UseCase<Unit, PassengerPreferencesParams> {
  const UpdatePassengerPreferences(this._repository);

  final PassengerRepository _repository;

  @override
  Future<Either<Failure, Unit>> call(PassengerPreferencesParams params) =>
      _repository.updatePreferences(
        preferFemaleDriver: params.preferFemaleDriver,
        defaultPaymentMethod: params.defaultPaymentMethod,
      );
}
