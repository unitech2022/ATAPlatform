import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/trip/domain/entities/offer.dart';
import 'package:ata_app/features/trip/domain/repositories/trip_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `GET /driver/offers/active`.
class GetActiveOffer implements UseCase<Offer?, NoParams> {
  const GetActiveOffer(this._repository);

  final TripRepository _repository;

  @override
  Future<Either<Failure, Offer?>> call(NoParams params) =>
      _repository.getActiveOffer();
}
