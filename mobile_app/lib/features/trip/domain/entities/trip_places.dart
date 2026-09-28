import 'package:ata_app/features/trip/domain/entities/geo_point.dart';

/// Fixed Riyadh coordinates for the Step-1 places of the home sheet (real
/// place search arrives with the Maps integration).
abstract final class TripPlaces {
  /// "موقعك الحالي" until the passenger position is read from the device.
  static const GeoPoint currentLocation = GeoPoint.riyadh;

  /// "واجهة الرياض".
  static const GeoPoint defaultDestination = GeoPoint(
    lat: 24.8433,
    lng: 46.7275,
  );

  /// Coordinates of the three stop options, in the order of the l10n keys
  /// `stopOption1..3`.
  static const List<GeoPoint> stopOptions = <GeoPoint>[
    GeoPoint(lat: 24.7550, lng: 46.6260),
    GeoPoint(lat: 24.7112, lng: 46.6745),
    GeoPoint(lat: 24.6786, lng: 46.7357),
  ];
}
