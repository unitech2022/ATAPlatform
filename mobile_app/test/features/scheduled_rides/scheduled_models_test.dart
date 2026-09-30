import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/features/airport/data/models/airport_models.dart';
import 'package:ata_app/features/airport/domain/entities/airport.dart';
import 'package:ata_app/features/airport/domain/entities/airport_queue_status.dart';
import 'package:ata_app/features/scheduled_rides/data/datasources/scheduled_remote_data_source.dart';
import 'package:ata_app/features/scheduled_rides/data/models/scheduled_models.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/marketplace_trip.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/reservation.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/scheduling_rules.dart';
import 'package:ata_app/features/trip/data/models/offer_model.dart';
import 'package:ata_app/features/trip/data/models/trip_model.dart';
import 'package:ata_app/features/trip/domain/entities/trip_airport.dart';
import 'package:ata_app/features/trip/domain/entities/trip_scheduling.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stage.dart';
import 'package:flutter_test/flutter_test.dart';

const Map<String, dynamic> _stop = <String, dynamic>{
  'name': 'حي الملقا',
  'address': 'الرياض',
  'lat': 24.8,
  'lng': 46.6,
};

/// A scheduled `Trip` as documented in `docs/11` §F17.4 / §F17.8.
Map<String, dynamic> _tripJson() => <String, dynamic>{
  'id': 't1',
  'tripNumber': 'T-20260927-00007',
  'status': 'scheduled',
  'bookingType': 'scheduled',
  'scheduledAt': '2026-09-29T12:30:00Z',
  'rideCategory': <String, dynamic>{
    'id': 'c1',
    'code': 'economy',
    'name': 'اقتصادي',
  },
  'pickup': _stop,
  'dropoff': _stop,
  'estimatedFare': 95.0,
  'scheduling': <String, dynamic>{
    'freeCancelUntil': '2026-09-29T11:30:00Z',
    'searchStartsAt': '2026-09-29T12:20:00Z',
    'reservation': <String, dynamic>{
      'status': 'confirmed',
      'driverFirstName': 'محمد',
      'driverPhotoUrl': 'https://x/y.jpg',
      'ratingAvg': 4.9,
      'vehicle': <String, dynamic>{
        'make': 'Toyota',
        'model': 'Camry',
        'color': 'أبيض',
        'plateNumber': 'أ ب ج 2841',
      },
      'reservedAt': '2026-09-27T08:00:00Z',
    },
  },
  'airport': <String, dynamic>{
    'code': 'RUH',
    'direction': 'pickup',
    'zoneName': 'منطقة الالتقاط 3',
    'terminalCode': 'T1',
    'flightNumber': 'SV1020',
    'freeWaitingMinutes': 15,
  },
};

void main() {
  group('Trip (F17 fields)', () {
    test('parses status scheduled, scheduling and airport', () {
      final TripModel trip = TripModel.fromJson(_tripJson());
      expect(trip.status, TripStage.scheduled);
      expect(trip.isScheduled, isTrue);
      expect(trip.scheduledAt, DateTime.utc(2026, 9, 29, 12, 30));
      final TripScheduling scheduling = trip.scheduling!;
      expect(scheduling.freeCancelUntil, DateTime.utc(2026, 9, 29, 11, 30));
      expect(scheduling.searchStartsAt, DateTime.utc(2026, 9, 29, 12, 20));
      final TripReservationInfo reservation = scheduling.reservation!;
      expect(reservation.status, ReservationStatus.confirmed);
      expect(reservation.isConfirmed, isTrue);
      expect(reservation.driverFirstName, 'محمد');
      expect(reservation.ratingAvg, 4.9);
      expect(reservation.vehicle?.plateNumber, 'أ ب ج 2841');
      final TripAirport airport = trip.airport!;
      expect(airport.code, 'RUH');
      expect(airport.direction, AirportDirection.pickup);
      expect(airport.zoneName, 'منطقة الالتقاط 3');
      expect(airport.terminalCode, 'T1');
      expect(airport.flightNumber, 'SV1020');
      expect(airport.freeWaitingMinutes, 15);
    });

    test('scheduling.reservation may be null and airport absent', () {
      final Map<String, dynamic> json = _tripJson()
        ..['scheduling'] = <String, dynamic>{
          'freeCancelUntil': '2026-09-29T11:30:00Z',
          'reservation': null,
        }
        ..remove('airport');
      final TripModel trip = TripModel.fromJson(json);
      expect(trip.scheduling?.reservation, isNull);
      expect(trip.airport, isNull);
      expect(
        TripModel.fromJson(const <String, dynamic>{'id': 't'}).scheduling,
        isNull,
      );
    });

    test('an unknown airport direction is ignored', () {
      final Map<String, dynamic> json = _tripJson()
        ..['airport'] = <String, dynamic>{'code': 'RUH', 'direction': 'x'};
      expect(TripModel.fromJson(json).airport, isNull);
    });

    test('survives a toJson / fromJson round trip', () {
      final TripModel first = TripModel.fromJson(_tripJson());
      final TripModel again = TripModel.fromJson(first.toJson());
      expect(again.scheduling, first.scheduling);
      expect(again.airport, first.airport);
      expect(again.status, TripStage.scheduled);
    });

    test('reservation statuses parse, unknown ones are safe', () {
      expect(ReservationStatus.parse('no_show'), ReservationStatus.noShow);
      expect(ReservationStatus.parse('weird'), ReservationStatus.unknown);
      expect(ReservationStatus.parse(null), ReservationStatus.unknown);
      expect(ReservationStatus.reserved.isActive, isTrue);
      expect(ReservationStatus.released.isActive, isFalse);
    });

    test('the offer carries the airport without a flight number', () {
      final OfferModel offer = OfferModel.fromJson(const <String, dynamic>{
        'id': 'o1',
        'tripId': 't1',
        'pickup': _stop,
        'dropoff': _stop,
        'expiresAt': '2026-09-27T10:00:20Z',
        'scheduledAt': '2026-09-27T10:30:00Z',
        'airport': <String, dynamic>{
          'code': 'RUH',
          'direction': 'dropoff',
          'terminalCode': 'T2',
        },
      });
      expect(offer.airport?.direction, AirportDirection.dropoff);
      expect(offer.airport?.flightNumber, isNull);
      expect(offer.scheduledAt, DateTime.utc(2026, 9, 27, 10, 30));
      expect(offer.toJson()['airport'], isNotNull);
    });
  });

  group('scheduling endpoints', () {
    test('rules', () {
      final SchedulingRules rules = ScheduledModels.rules(<String, dynamic>{
        'maxDaysAhead': 7,
        'minLeadMinutes': 30,
        'minScheduledAt': '2026-09-27T10:30:00Z',
        'maxScheduledAt': '2026-10-04T10:00:00Z',
        'freeCancelMinutesBefore': 60,
        'lateCancelFee': 10.0,
        'reminderOffsets': <int>[1440, 60, 15],
      });
      expect(rules.maxDaysAhead, 7);
      expect(rules.minLeadMinutes, 30);
      expect(rules.minScheduledAt, DateTime.utc(2026, 9, 27, 10, 30));
      expect(rules.freeCancelMinutesBefore, 60);
      expect(rules.lateCancelFee, 10);
      expect(rules.reminderOffsets, <int>[1440, 60, 15]);
      final DateTime now = DateTime.utc(2026, 9, 27, 10);
      expect(rules.minAt(now), DateTime.utc(2026, 9, 27, 10, 30));
      expect(rules.maxAt(now), DateTime.utc(2026, 10, 4, 10));
      expect(
        rules.freeCancelUntil(DateTime.utc(2026, 9, 29, 12)),
        DateTime.utc(2026, 9, 29, 11),
      );
    });

    test('missing rule fields fall back to the seeded defaults', () {
      final SchedulingRules rules = ScheduledModels.rules(
        const <String, dynamic>{},
      );
      expect(rules, SchedulingRules.fallback);
      expect(rules.lateCancelFee, isNull);
    });

    test('marketplace item', () {
      final MarketplaceTrip trip = ScheduledModels.marketplace(
        <String, dynamic>{
          'tripId': 'u1',
          'scheduledAt': '2026-09-28T09:00:00Z',
          'rideCategory': <String, dynamic>{
            'code': 'economy',
            'name': 'اقتصادي',
          },
          'pickupArea': 'حي الملقا',
          'pickupApprox': <String, dynamic>{'lat': 24.812, 'lng': 46.611},
          'dropoffArea': 'مطار الملك خالد',
          'distanceToPickupKm': 8.4,
          'tripDistanceMeters': 32000,
          'estimatedFare': 95.0,
          'driverNetEarnings': 76.0,
          'isAirport': true,
          'isFavoriteRequest': true,
          'exclusiveUntil': '2026-09-27T10:30:00Z',
        },
      );
      expect(trip.tripId, 'u1');
      expect(trip.categoryName, 'اقتصادي');
      expect(trip.pickupApprox?.lat, 24.812);
      expect(trip.distanceToPickupKm, 8.4);
      expect(trip.tripDistanceMeters, 32000);
      expect(trip.driverNetEarnings, 76);
      expect(trip.isAirport, isTrue);
      expect(trip.isFavoriteRequest, isTrue);
      expect(trip.exclusiveUntil, DateTime.utc(2026, 9, 27, 10, 30));
    });

    test('reservation', () {
      final Reservation r = ScheduledModels.reservation(<String, dynamic>{
        'id': 'r1',
        'tripId': 't1',
        'status': 'confirmed',
        'source': 'favorite',
        'scheduledAt': '2026-09-28T09:00:00Z',
        'pickup': _stop,
        'dropoff': _stop,
        'passengerFirstName': 'سارة',
        'estimatedFare': 95.0,
        'driverNetEarnings': 76.0,
        'confirmDeadline': null,
        'finalConfirmDeadline': '2026-09-28T08:50:00Z',
        'freeReleaseUntil': '2026-09-28T07:00:00Z',
        'reservedAt': '2026-09-27T09:00:00Z',
        'penaltyPoints': 3,
      });
      expect(r.status, ReservationStatus.confirmed);
      expect(r.source, 'favorite');
      expect(r.pickup?.name, 'حي الملقا');
      expect(r.passengerFirstName, 'سارة');
      expect(r.confirmDeadline, isNull);
      expect(r.finalConfirmDeadline, DateTime.utc(2026, 9, 28, 8, 50));
      expect(r.freeReleaseUntil, DateTime.utc(2026, 9, 28, 7));
      expect(r.penaltyPoints, 3);
      expect(
        r.pendingAt(DateTime.utc(2026, 9, 28, 8, 45)),
        ConfirmationKind.finalStep,
      );
      expect(r.pendingAt(DateTime.utc(2026, 9, 28, 8, 50)), isNull);
    });

    test('the final confirmation also returns the trip', () {
      final ConfirmResult result = ScheduledModels.confirmResult(
        <String, dynamic>{
          'reservation': <String, dynamic>{
            'id': 'r1',
            'tripId': 't1',
            'status': 'assigned',
            'scheduledAt': '2026-09-28T09:00:00Z',
          },
          'trip': <String, dynamic>{
            'id': 't1',
            'tripNumber': 'T-1',
            'status': 'driver_assigned',
            'pickup': _stop,
            'dropoff': _stop,
          },
        },
      );
      expect(result.reservation.status, ReservationStatus.assigned);
      expect(result.trip?.status, TripStage.driverAssigned);
    });

    test('the API answers the confirmation with the reservation and its '
        'trip in one object', () {
      final ConfirmResult result = ScheduledModels.confirmResult(
        <String, dynamic>{
          'id': 'r1',
          'tripId': 't1',
          'status': 'assigned',
          'scheduledAt': '2026-09-28T09:00:00Z',
          'trip': <String, dynamic>{
            'id': 't1',
            'tripNumber': 'T-1',
            'status': 'driver_assigned',
            'pickup': _stop,
            'dropoff': _stop,
          },
        },
      );
      expect(result.reservation.status, ReservationStatus.assigned);
      expect(result.trip?.status, TripStage.driverAssigned);
    });

    test('the first confirmation is the bare reservation', () {
      final ConfirmResult result =
          ScheduledModels.confirmResult(<String, dynamic>{
            'id': 'r1',
            'tripId': 't1',
            'status': 'confirmed',
            'scheduledAt': '2026-09-28T09:00:00Z',
          });
      expect(result.reservation.status, ReservationStatus.confirmed);
      expect(result.trip, isNull);
    });

    test('lists accept an array or a page', () {
      final PageResult<Reservation> page = ScheduledRemoteDataSource.parsePage(
        <String, dynamic>{
          'items': <Map<String, dynamic>>[
            <String, dynamic>{
              'id': 'r1',
              'tripId': 't1',
              'status': 'reserved',
              'scheduledAt': '2026-09-28T09:00:00Z',
            },
          ],
          'page': 1,
          'pageSize': 1,
          'total': 3,
        },
        ScheduledModels.reservation,
      );
      expect(page.items, hasLength(1));
      expect(page.hasMore, isTrue);
      final PageResult<Reservation> bare = ScheduledRemoteDataSource.parsePage(
        <Map<String, dynamic>>[
          <String, dynamic>{
            'id': 'r1',
            'tripId': 't1',
            'status': 'reserved',
            'scheduledAt': '2026-09-28T09:00:00Z',
          },
        ],
        ScheduledModels.reservation,
      );
      expect(bare.items, hasLength(1));
      expect(bare.hasMore, isFalse);
    });
  });

  group('airport endpoints', () {
    test('the catalog airport with terminals and pickup zones', () {
      final Airport airport = AirportModels.airport(<String, dynamic>{
        'id': 'a1',
        'code': 'RUH',
        'name': 'مطار الملك خالد الدولي',
        'lat': 24.9576,
        'lng': 46.6988,
        'terminals': <Map<String, dynamic>>[
          <String, dynamic>{
            'id': 'te1',
            'code': 'T1',
            'terminalCode': 'T1',
            'name': 'صالة 1',
          },
        ],
        'pickupZones': <Map<String, dynamic>>[
          <String, dynamic>{
            'id': 'z1',
            'code': 'T1-P1',
            'terminalCode': 'T1',
            'name': 'منطقة الالتقاط 1',
            'lat': 24.96,
            'lng': 46.7,
            'instructions': 'عند البوابة 3',
            'freeWaitingMinutes': 15,
          },
        ],
      });
      expect(airport.code, 'RUH');
      expect(airport.terminals.single.terminalCode, 'T1');
      final AirportZone zone = airport.pickupZones.single;
      expect(zone.point?.lat, 24.96);
      expect(zone.instructions, 'عند البوابة 3');
      expect(zone.effectiveFreeWaitingMinutes, 15);
      expect(airport.zonesOf('T1'), hasLength(1));
      expect(airport.zonesOf('T2'), isEmpty);
      expect(airport.requiresPickupZone, isTrue);
    });

    test('resolve answers null outside an airport', () {
      expect(AirportModels.resolution(null), isNull);
      expect(
        AirportModels.resolution(<String, dynamic>{'airport': null}),
        isNull,
      );
      final AirportResolution? resolution = AirportModels.resolution(
        <String, dynamic>{
          'airport': <String, dynamic>{
            'id': 'a1',
            'code': 'RUH',
            'name': 'مطار',
          },
          'requiresPickupZone': true,
          'pickupZones': <Map<String, dynamic>>[
            <String, dynamic>{'id': 'z1', 'name': 'منطقة 1'},
          ],
        },
      );
      expect(resolution?.code, 'RUH');
      expect(resolution?.requiresPickupZone, isTrue);
      expect(resolution?.pickupZones.single.point, isNull);
      expect(
        resolution?.pickupZones.single.effectiveFreeWaitingMinutes,
        AirportZone.defaultFreeWaitingMinutes,
      );
    });

    test('the driver queue status, in and out of the queue', () {
      final AirportQueueStatus inQueue = AirportModels.queue(<String, dynamic>{
        'inQueue': true,
        'airport': <String, dynamic>{'id': 'a1', 'code': 'RUH', 'name': 'مطار'},
        'position': 7,
        'total': 23,
        'enteredAt': '2026-09-27T10:00:00Z',
        'estimatedWaitMinutes': 25,
      });
      expect(inQueue.inQueue, isTrue);
      expect(inQueue.position, 7);
      expect(inQueue.total, 23);
      expect(inQueue.estimatedWaitMinutes, 25);
      expect(inQueue.relevantAirport?.code, 'RUH');

      final AirportQueueStatus out = AirportModels.queue(<String, dynamic>{
        'inQueue': false,
        'eligibleAirport': <String, dynamic>{
          'id': 'a1',
          'code': 'RUH',
          'name': 'مطار',
        },
      });
      expect(out.inQueue, isFalse);
      expect(out.isRelevant, isTrue);
      expect(out.relevantAirport?.code, 'RUH');
      expect(
        AirportModels.queue(<String, dynamic>{'inQueue': false}).isRelevant,
        isFalse,
      );
    });

    test('the AirportQueueUpdated event', () {
      final AirportQueuePosition update = AirportModels.queuePosition(
        <String, dynamic>{
          'position': 4,
          'total': 19,
          'estimatedWaitMinutes': null,
        },
      );
      expect(update.position, 4);
      expect(update.total, 19);
      expect(update.estimatedWaitMinutes, isNull);
    });
  });
}
