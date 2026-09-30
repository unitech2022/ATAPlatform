import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/support/domain/entities/help.dart';
import 'package:ata_app/features/support/domain/repositories/support_repository.dart';
import 'package:fpdart/fpdart.dart';

/// `GET /help/categories?audience=`.
class GetHelpCategories implements UseCase<List<HelpCategory>, String> {
  const GetHelpCategories(this._repository);

  final SupportRepository _repository;

  @override
  Future<Either<Failure, List<HelpCategory>>> call(String params) =>
      _repository.getHelpCategories(params);
}
