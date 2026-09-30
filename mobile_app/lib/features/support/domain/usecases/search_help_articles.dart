import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/support/domain/entities/help.dart';
import 'package:ata_app/features/support/domain/repositories/support_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `GET /help/articles?categoryId&q&audience&page` (published only).
class SearchHelpArticles
    implements UseCase<PageResult<HelpArticleSummary>, HelpQuery> {
  const SearchHelpArticles(this._repository);

  final SupportRepository _repository;

  @override
  Future<Either<Failure, PageResult<HelpArticleSummary>>> call(
    HelpQuery params,
  ) => _repository.searchHelpArticles(params);
}
