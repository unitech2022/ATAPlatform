import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/support/domain/entities/ticket.dart';
import 'package:ata_app/features/support/domain/repositories/support_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `GET /support/tickets/{id}` (also marks the ticket read).
class GetTicket implements UseCase<TicketDetail, String> {
  const GetTicket(this._repository);

  final SupportRepository _repository;

  @override
  Future<Either<Failure, TicketDetail>> call(String params) =>
      _repository.getTicket(params);
}
