import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/payments/data/datasources/payments_remote_data_source.dart';
import 'package:ata_app/features/payments/domain/entities/payment_action.dart';
import 'package:ata_app/features/payments/domain/entities/receipt.dart';
import 'package:ata_app/features/payments/domain/entities/saved_card.dart';
import 'package:ata_app/features/payments/domain/repositories/payments_repository.dart';
import 'package:fpdart/fpdart.dart';

/// [PaymentsRepository] backed by the API.
class PaymentsRepositoryImpl implements PaymentsRepository {
  const PaymentsRepositoryImpl(this._remote);

  final PaymentsRemoteDataSource _remote;

  @override
  Future<Either<Failure, List<SavedCard>>> getPaymentMethods() =>
      guard(_remote.paymentMethods);

  @override
  Future<Either<Failure, AddCardResult>> addPaymentMethod({
    required String token,
    bool setDefault = false,
  }) => guard(
    () => _remote.addPaymentMethod(token: token, setDefault: setDefault),
  );

  @override
  Future<Either<Failure, SavedCard>> setDefault(String id) =>
      guard(() => _remote.setDefault(id));

  @override
  Future<Either<Failure, Unit>> remove(String id) => guard(() async {
    await _remote.remove(id);
    return unit;
  });

  @override
  Future<Either<Failure, Receipt>> getTripReceipt(String tripId) =>
      guard(() => _remote.receipt(tripId));
}
