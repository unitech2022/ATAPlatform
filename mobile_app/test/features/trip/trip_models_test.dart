import 'package:ata_app/core/env/env.dart';
import 'package:ata_app/features/rides/data/models/trip_summary_model.dart';
import 'package:ata_app/features/rides/domain/entities/trip_summary.dart';
import 'package:ata_app/features/trip/data/models/driver_location_model.dart';
import 'package:ata_app/features/trip/data/models/estimate_model.dart';
import 'package:ata_app/features/trip/data/models/offer_model.dart';
import 'package:ata_app/features/trip/data/models/trip_model.dart';
import 'package:ata_app/features/trip/data/models/trip_request_mapper.dart';
import 'package:ata_app/features/trip/domain/entities/driver_location.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:ata_app/features/trip/domain/entities/trip_request.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stage.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/trip_fakes.dart';

const Map<String, dynamic> _tripJson = <String, dynamic>{
  'id': 't1',
  'tripNumber': 'T-20260928-00001',
  'status': 'driver_en_route',
  'bookingType': 'now',
  'scheduledAt': null,
  'rideCategory': <String, dynamic>{
    'id': 'c1',
    'code': 'economy',
    'name': 'اقتصادي',
  },
  'pickup': <String, dynamic>{
    'name': 'حي النرجس',
    'address': 'الرياض',
    'lat': 24.7136,
    'lng': 46.6753,
  },
  'dropoff': <String, dynamic>{
    'name': 'واجهة الرياض',
    'address': 'الرياض',
    'lat': 24.8433,
    'lng': 46.7275,
  },
  'stops': <Map<String, dynamic>>[
    <String, dynamic>{
      'name': 'النخيل مول',
      'address': '',
      'lat': 24.755,
      'lng': 46.626,
      'arrivedAt': null,
    },
  ],
  'paymentMethod': 'wallet',
  'pricingMode': 'offer',
  'offeredPrice': 35,
  'estimatedFare': 38,
  'finalFare': null,
  'estimatedDistanceMeters': 12000,
  'estimatedDurationSeconds': 1200,
  'driver': <String, dynamic>{
    'id': 'd1',
    'fullName': 'خالد أحمد',
    'ratingAvg': 4.9,
    'photoFileId': null,
    'phoneMasked': '05XXXX1234',
    'gender': 'male',
  },
  'vehicle': <String, dynamic>{
    'make': 'تويوتا',
    'model': 'كامري',
    'color': 'أبيض',
    'plateNumber': 'أ ب ج 2841',
  },
  'passenger': <String, dynamic>{
    'firstName': 'عبدالله',
    'phoneMasked': '05XXXX9999',
    'ratingAvg': 4.7,
  },
  'pin': '4821',
  'waitingSeconds': 0,
  'cancelledBy': null,
  'cancellationReason': null,
  'timeline': <String, dynamic>{
    'requestedAt': '2026-09-28T12:00:00Z',
    'assignedAt': '2026-09-28T12:01:00Z',
    'arrivedAt': null,
    'startedAt': null,
    'completedAt': null,
    'cancelledAt': null,
  },
  'events': <Map<String, dynamic>>[
    <String, dynamic>{
      'type': 'driver_assigned',
      'actor': 'system',
      'createdAt': '2026-09-28T12:01:00Z',
    },
  ],
};

void main() {
  test('TripModel parses the Trip object and round-trips through toJson', () {
    final TripModel trip = TripModel.fromJson(_tripJson);
    expect(trip.status, TripStage.driverEnRoute);
    expect(trip.category?.code, 'economy');
    expect(trip.pickup.point, GeoPoint.riyadh);
    expect(trip.stops.single.name, 'النخيل مول');
    expect(trip.driver?.phoneMasked, '05XXXX1234');
    expect(trip.vehicle?.plateNumber, 'أ ب ج 2841');
    expect(trip.passenger?.ratingAvg, 4.7);
    expect(trip.pin, '4821');
    expect(trip.fare, 35);
    expect(trip.timeline.assignedAt, DateTime.utc(2026, 9, 28, 12, 1));
    expect(trip.events.single.type, 'driver_assigned');
    expect(trip.status.hasDriver, isTrue);
    expect(trip.status.canCancel, isTrue);

    final TripModel again = TripModel.fromJson(trip.toJson());
    expect(again, trip);
  });

  test('TripModel tolerates a minimal / unknown payload', () {
    final TripModel trip = TripModel.fromJson(const <String, dynamic>{
      'id': 't2',
      'tripNumber': 'T-2',
      'status': 'something_new',
    });
    expect(trip.status, TripStage.unknown);
    expect(trip.driver, isNull);
    expect(trip.stops, isEmpty);
    expect(trip.pickup.name, '');
    expect(trip.fare, 0);
  });

  test('OfferModel parses the Offer object', () {
    final OfferModel offer = OfferModel.fromJson(const <String, dynamic>{
      'id': 'o1',
      'tripId': 't1',
      'pickup': <String, dynamic>{'name': 'A', 'lat': 1, 'lng': 2},
      'dropoff': <String, dynamic>{'name': 'B', 'lat': 3, 'lng': 4},
      'stops': <dynamic>[],
      'distanceToPickupMeters': 850,
      'etaSeconds': 180,
      'tripDistanceMeters': 12000,
      'passengerPrice': 38,
      'driverNetEarnings': 30.4,
      'expiresAt': '2026-09-28T12:00:20Z',
      'passenger': <String, dynamic>{'firstName': 'عبدالله', 'ratingAvg': 4.8},
    });
    expect(offer.etaMinutes, 3);
    expect(offer.driverNetEarnings, 30.4);
    expect(offer.expiresAt, DateTime.utc(2026, 9, 28, 12, 0, 20));
    expect(offer.passengerFirstName, 'عبدالله');
    expect(OfferModel.fromJson(offer.toJson()), offer);
  });

  test(
    'EstimateModel parses categories and the request mapper mirrors the doc',
    () {
      final EstimateModel estimate = EstimateModel.fromJson(
        const <String, dynamic>{
          'distanceMeters': 12000,
          'durationSeconds': 1200,
          'categories': <Map<String, dynamic>>[
            <String, dynamic>{
              'rideCategoryId': 'c1',
              'code': 'economy',
              'name': 'اقتصادي',
              'etaMinutes': 3,
              'estimatedFare': 38,
              'driverNetEarnings': 30.4,
            },
          ],
        },
      );
      expect(estimate.forCategory('c1')?.estimatedFare, 38);
      expect(estimate.forCategory('missing'), isNull);

      final Map<String, dynamic> body = TripRequestMapper.requestBody(
        testTripRequest.copyWith(
          pricingMode: PricingMode.offer,
          offeredPrice: 30,
        ),
      );
      expect(body['pricingMode'], 'offer');
      expect(body['offeredPrice'], 30);
      expect(body['bookingType'], 'now');
      expect(body.containsKey('scheduledAt'), isFalse);
      expect((body['pickup'] as Map<String, dynamic>)['lat'], 24.7136);
      expect(
        TripRequestMapper.estimateBody(
          testTripRequest,
        ).containsKey('paymentMethod'),
        isFalse,
      );
    },
  );

  test('DriverLocationModel parses the hub event and builds the PUT body', () {
    final DriverLocationModel update = DriverLocationModel.fromJson(
      const <String, dynamic>{
        'tripId': 't1',
        'lat': 24.7,
        'lng': 46.6,
        'heading': 90,
        'etaSeconds': 125,
      },
    );
    expect(update.etaMinutes, 3);
    final Map<String, dynamic> body = DriverLocationModel.positionToJson(
      const DriverPosition(point: GeoPoint.riyadh, speed: 8),
    );
    expect(body.keys, <String>['lat', 'lng', 'speed']);
  });

  test('TripSummaryModel copes with the real list fields', () {
    final TripSummaryModel summary =
        TripSummaryModel.fromJson(const <String, dynamic>{
          'id': 't1',
          'tripNumber': 'T-20260928-00001',
          'destinationName': 'واجهة الرياض',
          'pickupName': 'حي النرجس',
          'status': 'no_drivers',
          'fare': null,
          'categoryName': 'اقتصادي',
          'completedAt': null,
          'scheduledAt': null,
          'requestedAt': '2026-09-28T07:35:00Z',
          'earning': 30.4,
        });
    expect(summary.status, TripStatus.cancelled);
    expect(summary.tripNumber, 'T-20260928-00001');
    expect(summary.displayDate, DateTime.utc(2026, 9, 28, 7, 35));
    expect(summary.displayAmount, 30.4);
    expect(TripStatus.parse('in_trip'), TripStatus.active);
  });

  test('Env derives the hub URL from the API base URL', () {
    expect(
      Env.hubUrlFor('http://10.0.2.2:5000/api/v1'),
      'http://10.0.2.2:5000/hubs/trips',
    );
    expect(
      Env.hubUrlFor('https://api.ata.sa/api/v1/'),
      'https://api.ata.sa/hubs/trips',
    );
    expect(
      Env.hubUrlFor('https://api.ata.sa'),
      'https://api.ata.sa/hubs/trips',
    );
  });
}
