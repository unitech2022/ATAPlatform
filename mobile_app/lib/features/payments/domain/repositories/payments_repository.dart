import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/payments/domain/entities/payment_action.dart';
import 'package:ata_app/features/payments/domain/entities/receipt.dart';
import 'package:ata_app/features/payments/domain/entities/saved_card.dart';
import 'package:fpdart/fpdart.dart';

/// `/passenger/payment-methods*` and `/passenger/trips/{id}/receipt`.
abstract interface class PaymentsRepository {
  Future<Either<Failure, List<SavedCard>>> getPaymentMethods();

  /// Saves a tokenised card. Only the gateway [token] is sent.
  Future<Either<Failure, AddCardResult>> addPaymentMethod({
    required String token,
    bool setDefault = false,
  });

  Future<Either<Failure, SavedCard>> setDefault(String id);
  Future<Either<Failure, Unit>> remove(String id);
  Future<Either<Failure, Receipt>> getTripReceipt(String tripId);
}
