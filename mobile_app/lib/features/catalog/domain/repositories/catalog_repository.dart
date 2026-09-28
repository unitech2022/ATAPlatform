import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/catalog/domain/entities/city.dart';
import 'package:ata_app/features/catalog/domain/entities/document_type.dart';
import 'package:ata_app/features/catalog/domain/entities/ride_category.dart';
import 'package:fpdart/fpdart.dart';

/// Public reference data (`/catalog/*`).
abstract interface class CatalogRepository {
  Future<Either<Failure, List<RideCategory>>> getRideCategories();
  Future<Either<Failure, List<DocumentType>>> getDocumentTypes();
  Future<Either<Failure, List<City>>> getCities();
}
