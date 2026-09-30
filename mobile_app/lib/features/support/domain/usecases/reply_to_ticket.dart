import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/support/domain/entities/ticket.dart';
import 'package:ata_app/features/support/domain/entities/ticket_requests.dart';
import 'package:ata_app/features/support/domain/repositories/support_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `POST /support/tickets/{id}/messages` (`409 ticket_closed`).
class ReplyToTicket implements UseCase<TicketMessage, TicketReplyRequest> {
  const ReplyToTicket(this._repository);

  final SupportRepository _repository;

  @override
  Future<Either<Failure, TicketMessage>> call(TicketReplyRequest params) =>
      _repository.replyToTicket(params);
}
