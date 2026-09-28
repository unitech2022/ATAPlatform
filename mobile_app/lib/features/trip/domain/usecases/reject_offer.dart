import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/trip/domain/repositories/trip_repository.dart';
import 'package:equatable/equatable.dart';
import 'package:fpdart/fpdart.dart';

class RejectOfferParams extends Equatable {
  const RejectOfferParams({required this.offerId, this.reason});

  final String offerId;
  final String? reason;

  @override
  List<Object?> get props => <Object?>[offerId, reason];
}

/// `POST /driver/offers/{id}/reject`.
class RejectOffer implements UseCase<Unit, RejectOfferParams> {
  const RejectOffer(this._repository);

  final TripRepository _repository;

  @override
  Future<Either<Failure, Unit>> call(RejectOfferParams params) =>
      _repository.rejectOffer(params.offerId, reason: params.reason);
}
