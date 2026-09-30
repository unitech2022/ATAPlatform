import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/support/domain/entities/ticket_requests.dart';
import 'package:ata_app/features/support/domain/repositories/support_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `POST /help/articles/{id}/feedback` (once per article per day).
class SendArticleFeedback implements UseCase<Unit, ArticleFeedback> {
  const SendArticleFeedback(this._repository);

  final SupportRepository _repository;

  @override
  Future<Either<Failure, Unit>> call(ArticleFeedback params) =>
      _repository.sendArticleFeedback(params);
}
