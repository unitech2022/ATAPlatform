import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:equatable/equatable.dart';

/// Query of `GET /driver/scheduled/marketplace`.
class MarketplaceQuery extends Equatable {
  const MarketplaceQuery({
    required this.position,
    this.from,
    this.to,
    this.page = 1,
  });

  final GeoPoint position;
  final DateTime? from;
  final DateTime? to;
  final int page;

  @override
  List<Object?> get props => <Object?>[position, from, to, page];
}

/// `status` of `GET /driver/scheduled`.
enum ReservationList {
  active('active'),
  history('history');

  const ReservationList(this.apiValue);

  final String apiValue;
}

/// Query of `GET /driver/scheduled`.
class ReservationsQuery extends Equatable {
  const ReservationsQuery({this.list = ReservationList.active, this.page = 1});

  final ReservationList list;
  final int page;

  @override
  List<Object?> get props => <Object?>[list, page];
}

/// Body of `POST /driver/scheduled/{tripId}/release`.
class ReleaseParams extends Equatable {
  const ReleaseParams({required this.tripId, this.reason});

  final String tripId;
  final String? reason;

  @override
  List<Object?> get props => <Object?>[tripId, reason];
}
