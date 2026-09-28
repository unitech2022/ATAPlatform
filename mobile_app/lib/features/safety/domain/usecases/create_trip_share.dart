import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/safety/domain/entities/trip_share.dart';
import 'package:ata_app/features/safety/domain/repositories/safety_repository.dart';
import 'package:equatable/equatable.dart';
import 'package:fpdart/fpdart.dart';

class CreateTripShareParams extends Equatable {
  const CreateTripShareParams({
    required this.tripId,
    this.channel = ShareChannel.link,
    this.contactIds = const <String>[],
  });

  final String tripId;
  final ShareChannel channel;

  /// Trusted contacts that receive an SMS (`channel: sms`).
  final List<String> contactIds;

  @override
  List<Object?> get props => <Object?>[tripId, channel, contactIds];
}

/// `POST /safety/trips/{tripId}/shares` (one link, or one per contact).
class CreateTripShare
    implements UseCase<List<TripShare>, CreateTripShareParams> {
  const CreateTripShare(this._repository);

  final SafetyRepository _repository;

  @override
  Future<Either<Failure, List<TripShare>>> call(CreateTripShareParams params) =>
      _repository.createTripShare(
        tripId: params.tripId,
        channel: params.channel,
        contactIds: params.contactIds,
      );
}
