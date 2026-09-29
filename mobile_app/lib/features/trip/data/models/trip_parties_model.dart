import 'package:ata_app/features/trip/data/models/json_readers.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_parties.dart';

/// JSON mapping for [TripDriver].
class TripDriverModel extends TripDriver {
  const TripDriverModel({
    required super.id,
    required super.fullName,
    required super.ratingAvg,
    super.photoFileId,
    super.phoneMasked,
    super.gender,
    super.isFavorite,
  });

  factory TripDriverModel.fromJson(Map<String, dynamic> json) =>
      TripDriverModel(
        id: JsonReaders.string(json, 'id'),
        fullName: JsonReaders.string(json, 'fullName'),
        ratingAvg: JsonReaders.number(json, 'ratingAvg'),
        photoFileId: JsonReaders.optionalString(json, 'photoFileId'),
        phoneMasked: JsonReaders.optionalString(json, 'phoneMasked'),
        gender: JsonReaders.optionalString(json, 'gender'),
        isFavorite: json['isFavorite'] == true,
      );

  Map<String, dynamic> toJson() => <String, dynamic>{
    'id': id,
    'fullName': fullName,
    'ratingAvg': ratingAvg,
    'photoFileId': photoFileId,
    'phoneMasked': phoneMasked,
    'gender': gender,
    'isFavorite': isFavorite,
  };
}

/// JSON mapping for [TripVehicle].
class TripVehicleModel extends TripVehicle {
  const TripVehicleModel({
    required super.make,
    required super.model,
    required super.color,
    required super.plateNumber,
  });

  factory TripVehicleModel.fromJson(Map<String, dynamic> json) =>
      TripVehicleModel(
        make: JsonReaders.string(json, 'make'),
        model: JsonReaders.string(json, 'model'),
        color: JsonReaders.string(json, 'color'),
        plateNumber: JsonReaders.string(json, 'plateNumber'),
      );

  Map<String, dynamic> toJson() => <String, dynamic>{
    'make': make,
    'model': model,
    'color': color,
    'plateNumber': plateNumber,
  };
}

/// JSON mapping for [TripPassenger].
class TripPassengerModel extends TripPassenger {
  const TripPassengerModel({
    required super.firstName,
    super.phoneMasked,
    super.ratingAvg,
  });

  factory TripPassengerModel.fromJson(Map<String, dynamic> json) =>
      TripPassengerModel(
        firstName: JsonReaders.string(json, 'firstName'),
        phoneMasked: JsonReaders.optionalString(json, 'phoneMasked'),
        ratingAvg: JsonReaders.optionalNumber(json, 'ratingAvg'),
      );

  Map<String, dynamic> toJson() => <String, dynamic>{
    'firstName': firstName,
    'phoneMasked': phoneMasked,
    'ratingAvg': ratingAvg,
  };
}

/// JSON mapping for [TripCategory].
class TripCategoryModel extends TripCategory {
  const TripCategoryModel({
    required super.id,
    required super.code,
    required super.name,
  });

  factory TripCategoryModel.fromJson(Map<String, dynamic> json) =>
      TripCategoryModel(
        id: JsonReaders.string(json, 'id'),
        code: JsonReaders.string(json, 'code'),
        name: JsonReaders.string(json, 'name'),
      );

  Map<String, dynamic> toJson() => <String, dynamic>{
    'id': id,
    'code': code,
    'name': name,
  };
}
