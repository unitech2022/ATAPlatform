import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/trip/domain/entities/cancel_preview.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/domain/repositories/cancellation_repository.dart';
import 'package:equatable/equatable.dart';
import 'package:fpdart/fpdart.dart';

class PreviewCancellationParams extends Equatable {
  const PreviewCancellationParams({
    required this.tripId,
    required this.actor,
    this.reasonCode,
  });

  final String tripId;
  final TripActor actor;
  final String? reasonCode;

  @override
  List<Object?> get props => <Object?>[tripId, actor, reasonCode];
}

/// Fee / points the cancellation would cost, before confirming.
class PreviewCancellation
    implements UseCase<CancelPreview, PreviewCancellationParams> {
  const PreviewCancellation(this._repository);

  final CancellationRepository _repository;

  @override
  Future<Either<Failure, CancelPreview>> call(
    PreviewCancellationParams params,
  ) => _repository.preview(
    tripId: params.tripId,
    actor: params.actor,
    reasonCode: params.reasonCode,
  );
}
