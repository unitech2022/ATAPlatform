import 'package:ata_app/core/network/api_client.dart';
import 'package:ata_app/features/catalog/data/models/city_model.dart';
import 'package:ata_app/features/catalog/data/models/document_type_model.dart';
import 'package:ata_app/features/catalog/data/models/ride_category_model.dart';

/// `/catalog/*` endpoints.
class CatalogRemoteDataSource {
  const CatalogRemoteDataSource(this._api);

  final ApiClient _api;

  static const String _categoriesPath = '/catalog/ride-categories';
  static const String _documentTypesPath = '/catalog/document-types';
  static const String _citiesPath = '/catalog/cities';

  Future<List<RideCategoryModel>> rideCategories() async =>
      _list(await _api.get(_categoriesPath), RideCategoryModel.fromJson);

  Future<List<DocumentTypeModel>> documentTypes() async =>
      _list(await _api.get(_documentTypesPath), DocumentTypeModel.fromJson);

  Future<List<CityModel>> cities() async =>
      _list(await _api.get(_citiesPath), CityModel.fromJson);

  List<T> _list<T>(dynamic body, T Function(Map<String, dynamic>) parse) =>
      (body as List<dynamic>)
          .map((dynamic e) => parse(e as Map<String, dynamic>))
          .toList(growable: false);
}
