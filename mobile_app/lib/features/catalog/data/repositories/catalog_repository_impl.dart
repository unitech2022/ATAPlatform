import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/catalog/data/datasources/catalog_remote_data_source.dart';
import 'package:ata_app/features/catalog/domain/entities/city.dart';
import 'package:ata_app/features/catalog/domain/entities/document_type.dart';
import 'package:ata_app/features/catalog/domain/entities/ride_category.dart';
import 'package:ata_app/features/catalog/domain/repositories/catalog_repository.dart';
import 'package:fpdart/fpdart.dart';

/// [CatalogRepository] backed by the API.
class CatalogRepositoryImpl implements CatalogRepository {
  const CatalogRepositoryImpl(this._remote);

  final CatalogRemoteDataSource _remote;

  @override
  Future<Either<Failure, List<RideCategory>>> getRideCategories() =>
      guard(_remote.rideCategories);

  @override
  Future<Either<Failure, List<DocumentType>>> getDocumentTypes() =>
      guard(_remote.documentTypes);

  @override
  Future<Either<Failure, List<City>>> getCities() => guard(_remote.cities);
}
