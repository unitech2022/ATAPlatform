import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/support/domain/entities/ticket.dart';
import 'package:ata_app/features/support/domain/entities/ticket_requests.dart';
import 'package:ata_app/features/support/domain/repositories/support_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `GET /support/tickets?status=&page=`.
class GetTickets implements UseCase<PageResult<TicketSummary>, TicketsQuery> {
  const GetTickets(this._repository);

  final SupportRepository _repository;

  @override
  Future<Either<Failure, PageResult<TicketSummary>>> call(
    TicketsQuery params,
  ) => _repository.getTickets(params);
}
