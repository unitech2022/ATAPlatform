import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/trip/domain/entities/cancellation_reason.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/domain/repositories/cancellation_repository.dart';
import 'package:equatable/equatable.dart';
import 'package:fpdart/fpdart.dart';

class CancellationReasonsParams extends Equatable {
  const CancellationReasonsParams({required this.actor, this.stage});

  final TripActor actor;

  /// `CancellationStage.apiValue`; `null` = every stage.
  final String? stage;

  @override
  List<Object?> get props => <Object?>[actor, stage];
}

/// Selectable, active reasons for the actor and stage.
class GetCancellationReasons
    implements UseCase<List<CancellationReason>, CancellationReasonsParams> {
  const GetCancellationReasons(this._repository);

  final CancellationRepository _repository;

  @override
  Future<Either<Failure, List<CancellationReason>>> call(
    CancellationReasonsParams params,
  ) => _repository.getReasons(actor: params.actor, stage: params.stage);
}
