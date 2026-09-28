import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/payments/domain/entities/payment_action.dart';
import 'package:ata_app/features/payments/domain/entities/receipt.dart';
import 'package:ata_app/features/payments/domain/entities/saved_card.dart';
import 'package:ata_app/features/payments/domain/repositories/payments_repository.dart';
import 'package:fpdart/fpdart.dart';

const SavedCard testMada = SavedCard(
  id: 'pm1',
  brand: 'mada',
  last4: '4201',
  expiryMonth: 8,
  expiryYear: 2029,
  isDefault: true,
);

const SavedCard testVisa = SavedCard(
  id: 'pm2',
  brand: 'visa',
  last4: '1111',
  expiryMonth: 1,
  expiryYear: 2030,
);

/// Records every token sent to the API.
class FakePaymentsRepository implements PaymentsRepository {
  final List<String> tokens = <String>[];
  PaymentAction? nextAction;
  Failure? failure;
  Receipt? receipt;

  @override
  Future<Either<Failure, AddCardResult>> addPaymentMethod({
    required String token,
    bool setDefault = false,
  }) async {
    tokens.add(token);
    final Failure? f = failure;
    if (f != null) return Left<Failure, AddCardResult>(f);
    return Right<Failure, AddCardResult>(
      AddCardResult(card: testMada, action: nextAction),
    );
  }

  @override
  Future<Either<Failure, List<SavedCard>>> getPaymentMethods() async =>
      const Right<Failure, List<SavedCard>>(<SavedCard>[testMada, testVisa]);

  @override
  Future<Either<Failure, Receipt>> getTripReceipt(String tripId) async {
    final Receipt? value = receipt;
    if (value != null) return Right<Failure, Receipt>(value);
    return Left<Failure, Receipt>(
      failure ?? const ServerFailure(code: 'conflict', message: ''),
    );
  }

  @override
  Future<Either<Failure, Unit>> remove(String id) async =>
      const Right<Failure, Unit>(unit);

  @override
  Future<Either<Failure, SavedCard>> setDefault(String id) async =>
      const Right<Failure, SavedCard>(testVisa);
}
