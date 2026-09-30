import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/support/domain/entities/ticket_requests.dart';
import 'package:ata_app/features/support/domain/repositories/support_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `POST /support/tickets/{id}/csat` (once, after resolved / closed).
class RateTicket implements UseCase<Unit, TicketRatingRequest> {
  const RateTicket(this._repository);

  final SupportRepository _repository;

  @override
  Future<Either<Failure, Unit>> call(TicketRatingRequest params) =>
      _repository.rateTicket(params);
}
