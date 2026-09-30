import 'package:ata_app/features/scheduled_rides/domain/entities/marketplace_trip.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/reservation.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/scheduling_rules.dart';
import 'package:ata_app/features/trip/data/models/json_readers.dart';
import 'package:ata_app/features/trip/data/models/trip_model.dart';
import 'package:ata_app/features/trip/data/models/trip_stop_model.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:ata_app/features/trip/domain/entities/trip_scheduling.dart';

/// JSON mapping of the scheduled-ride endpoints (`docs/11` §F17.4).
abstract final class ScheduledModels {
  static SchedulingRules rules(Map<String, dynamic> json) {
    const SchedulingRules d = SchedulingRules.fallback;
    final Object? offsets = json['reminderOffsets'];
    return SchedulingRules(
      maxDaysAhead:
          JsonReaders.optionalInteger(json, 'maxDaysAhead') ?? d.maxDaysAhead,
      minLeadMinutes:
          JsonReaders.optionalInteger(json, 'minLeadMinutes') ??
          d.minLeadMinutes,
      minScheduledAt: JsonReaders.date(json, 'minScheduledAt'),
      maxScheduledAt: JsonReaders.date(json, 'maxScheduledAt'),
      freeCancelMinutesBefore:
          JsonReaders.optionalInteger(json, 'freeCancelMinutesBefore') ??
          d.freeCancelMinutesBefore,
      lateCancelFee: JsonReaders.optionalNumber(json, 'lateCancelFee'),
      reminderOffsets: offsets is List<dynamic>
          ? offsets.whereType<num>().map((num n) => n.toInt()).toList()
          : d.reminderOffsets,
    );
  }

  static MarketplaceTrip marketplace(Map<String, dynamic> json) {
    final Map<String, dynamic>? category = JsonReaders.object(
      json,
      'rideCategory',
    );
    final Map<String, dynamic>? approx = JsonReaders.object(
      json,
      'pickupApprox',
    );
    return MarketplaceTrip(
      tripId: JsonReaders.string(json, 'tripId'),
      scheduledAt: JsonReaders.date(json, 'scheduledAt') ?? DateTime.now(),
      categoryCode: category == null
          ? ''
          : JsonReaders.string(category, 'code'),
      categoryName: category == null
          ? ''
          : JsonReaders.string(category, 'name'),
      pickupArea: JsonReaders.string(json, 'pickupArea'),
      dropoffArea: JsonReaders.string(json, 'dropoffArea'),
      pickupApprox: approx == null
          ? null
          : GeoPoint(
              lat: JsonReaders.number(approx, 'lat'),
              lng: JsonReaders.number(approx, 'lng'),
            ),
      distanceToPickupKm: JsonReaders.number(json, 'distanceToPickupKm'),
      tripDistanceMeters: JsonReaders.integer(json, 'tripDistanceMeters'),
      estimatedFare: JsonReaders.number(json, 'estimatedFare'),
      driverNetEarnings: JsonReaders.number(json, 'driverNetEarnings'),
      isAirport: json['isAirport'] == true,
      isFavoriteRequest: json['isFavoriteRequest'] == true,
      exclusiveUntil: JsonReaders.date(json, 'exclusiveUntil'),
    );
  }

  static Reservation reservation(Map<String, dynamic> json) {
    final Map<String, dynamic>? pickup = JsonReaders.object(json, 'pickup');
    final Map<String, dynamic>? dropoff = JsonReaders.object(json, 'dropoff');
    return Reservation(
      id: JsonReaders.string(json, 'id'),
      tripId: JsonReaders.string(json, 'tripId'),
      status: ReservationStatus.parse(
        JsonReaders.optionalString(json, 'status'),
      ),
      source: JsonReaders.optionalString(json, 'source') ?? 'marketplace',
      scheduledAt: JsonReaders.date(json, 'scheduledAt') ?? DateTime.now(),
      pickup: pickup == null ? null : TripStopModel.fromJson(pickup),
      dropoff: dropoff == null ? null : TripStopModel.fromJson(dropoff),
      passengerFirstName: JsonReaders.string(json, 'passengerFirstName'),
      estimatedFare: JsonReaders.number(json, 'estimatedFare'),
      driverNetEarnings: JsonReaders.number(json, 'driverNetEarnings'),
      confirmDeadline: JsonReaders.date(json, 'confirmDeadline'),
      finalConfirmDeadline: JsonReaders.date(json, 'finalConfirmDeadline'),
      freeReleaseUntil: JsonReaders.date(json, 'freeReleaseUntil'),
      reservedAt: JsonReaders.date(json, 'reservedAt'),
      penaltyPoints: JsonReaders.integer(json, 'penaltyPoints'),
    );
  }

  /// The final confirmation answers with the reservation plus `trip`; the
  /// first one with the bare reservation.
  static ConfirmResult confirmResult(Map<String, dynamic> json) {
    final Map<String, dynamic>? trip = JsonReaders.object(json, 'trip');
    final Map<String, dynamic>? nested = JsonReaders.object(
      json,
      'reservation',
    );
    return ConfirmResult(
      reservation: reservation(nested ?? json),
      trip: trip == null ? null : TripModel.fromJson(trip),
    );
  }
}
