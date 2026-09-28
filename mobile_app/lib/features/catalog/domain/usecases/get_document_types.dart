import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/catalog/domain/entities/document_type.dart';
import 'package:ata_app/features/catalog/domain/repositories/catalog_repository.dart';
import 'package:fpdart/fpdart.dart';

/// Loads the document types drivers must provide.
class GetDocumentTypes implements UseCase<List<DocumentType>, NoParams> {
  const GetDocumentTypes(this._repository);

  final CatalogRepository _repository;

  @override
  Future<Either<Failure, List<DocumentType>>> call(NoParams params) =>
      _repository.getDocumentTypes();
}
