import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:equatable/equatable.dart';

/// Body of `POST /passenger/favorite-drivers`: exactly one of the two ids.
class AddFavoriteParams extends Equatable {
  const AddFavoriteParams({this.driverId, this.tripId});

  final String? driverId;
  final String? tripId;

  @override
  List<Object?> get props => <Object?>[driverId, tripId];
}

/// Query of `GET /passenger/favorite-drivers/available`.
class AvailableFavoritesQuery extends Equatable {
  const AvailableFavoritesQuery({required this.pickup, this.rideCategoryId});

  final GeoPoint pickup;
  final String? rideCategoryId;

  @override
  List<Object?> get props => <Object?>[pickup, rideCategoryId];
}
