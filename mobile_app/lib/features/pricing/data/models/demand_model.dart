import 'package:ata_app/features/pricing/domain/entities/demand_level.dart';
import 'package:ata_app/features/trip/data/models/json_readers.dart';

/// JSON mapping for [DemandLevel] (`{ code, name, multiplier, color }`).
class DemandModel extends DemandLevel {
  const DemandModel({
    required super.code,
    required super.name,
    required super.multiplier,
    super.color,
  });

  factory DemandModel.fromJson(Map<String, dynamic> json) => DemandModel(
    code: DemandCode.fromApi(JsonReaders.string(json, 'code')),
    name: JsonReaders.string(json, 'name'),
    multiplier: JsonReaders.optionalNumber(json, 'multiplier') ?? 1,
    color: JsonReaders.string(json, 'color'),
  );

  Map<String, dynamic> toJson() => <String, dynamic>{
    'code': code.apiValue,
    'name': name,
    'multiplier': multiplier,
    'color': color,
  };
}
