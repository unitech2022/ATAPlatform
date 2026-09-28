import 'package:ata_app/features/catalog/domain/entities/city.dart';

/// JSON mapping for [City].
class CityModel extends City {
  const CityModel({
    required super.id,
    required super.code,
    required super.name,
  });

  factory CityModel.fromJson(Map<String, dynamic> json) => CityModel(
    id: json['id'] as String,
    code: json['code'] as String,
    name: json['name'] as String,
  );
}
