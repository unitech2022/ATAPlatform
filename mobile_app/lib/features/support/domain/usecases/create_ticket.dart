import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/support/domain/entities/ticket.dart';
import 'package:ata_app/features/support/domain/entities/ticket_requests.dart';
import 'package:ata_app/features/support/domain/repositories/support_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `POST /support/tickets` (with `dispute` for a fare dispute).
class CreateTicket implements UseCase<TicketDetail, NewTicketRequest> {
  const CreateTicket(this._repository);

  final SupportRepository _repository;

  @override
  Future<Either<Failure, TicketDetail>> call(NewTicketRequest params) =>
      _repository.createTicket(params);
}
