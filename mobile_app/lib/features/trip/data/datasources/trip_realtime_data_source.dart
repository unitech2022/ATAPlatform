import 'package:ata_app/features/trip/data/models/driver_location_model.dart';
import 'package:ata_app/features/trip/data/models/offer_model.dart';
import 'package:ata_app/features/trip/data/models/trip_model.dart';

/// The `/hubs/trips` SignalR feed.
///
/// The connection is reference counted: every watcher calls [acquire] when
/// it starts listening and [release] when it stops; the socket is opened on
/// the first acquire and closed after the last release.
abstract interface class TripRealtimeDataSource {
  /// `TripUpdated(trip)` for both roles.
  Stream<TripModel> get tripUpdated;

  /// `DriverLocation({tripId, lat, lng, heading, etaSeconds})` (passenger).
  Stream<DriverLocationModel> get driverLocation;

  /// `OfferReceived(offer)` (driver).
  Stream<OfferModel> get offerReceived;

  /// `OfferExpired(offerId)` (driver).
  Stream<String> get offerExpired;

  /// True while the hub socket is connected (polling can back off).
  bool get isConnected;

  Future<void> acquire();
  Future<void> release();
}

/// Hub method names as sent by the API.
abstract final class TripHubEvents {
  static const String tripUpdated = 'TripUpdated';
  static const String driverLocation = 'DriverLocation';
  static const String offerReceived = 'OfferReceived';
  static const String offerExpired = 'OfferExpired';
}
