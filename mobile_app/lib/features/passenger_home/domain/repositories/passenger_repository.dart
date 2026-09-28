import 'package:ata_app/core/errors/failures.dart';
import 'package:fpdart/fpdart.dart';

/// `/passenger/preferences`.
abstract interface class PassengerRepository {
  Future<Either<Failure, Unit>> updatePreferences({
    bool? preferFemaleDriver,
    String? defaultPaymentMethod,
  });
}
