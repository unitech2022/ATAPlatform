import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/support/domain/entities/help.dart';
import 'package:ata_app/features/support/domain/repositories/support_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `GET /help/articles/{slug}` (Markdown body).
class GetHelpArticle implements UseCase<HelpArticle, String> {
  const GetHelpArticle(this._repository);

  final SupportRepository _repository;

  @override
  Future<Either<Failure, HelpArticle>> call(String params) =>
      _repository.getHelpArticle(params);
}
