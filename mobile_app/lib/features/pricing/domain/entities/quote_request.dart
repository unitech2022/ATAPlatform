import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:equatable/equatable.dart';

/// Body of `POST /pricing/quote`. Every category is priced, so the selected
/// category is optional.
class QuoteRequest extends Equatable {
  const QuoteRequest({
    required this.pickup,
    required this.dropoff,
    this.stops = const <GeoPoint>[],
    this.rideCategoryId,
    this.bookingType = 'now',
    this.scheduledAt,
  });

  final GeoPoint pickup;
  final GeoPoint dropoff;
  final List<GeoPoint> stops;
  final String? rideCategoryId;
  final String bookingType;
  final DateTime? scheduledAt;

  @override
  List<Object?> get props => <Object?>[
    pickup,
    dropoff,
    stops,
    rideCategoryId,
    bookingType,
    scheduledAt,
  ];
}
