import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/airport/domain/entities/airport_selection.dart';
import 'package:ata_app/features/catalog/domain/usecases/get_ride_categories.dart';
import 'package:ata_app/features/passenger_home/domain/entities/ride_time.dart';
import 'package:ata_app/features/passenger_home/domain/usecases/update_passenger_preferences.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_cubit.dart';
import 'package:ata_app/features/passenger_home/presentation/widgets/trip_request_builder.dart';
import 'package:ata_app/features/pricing/data/models/quote_model.dart';
import 'package:ata_app/features/trip/data/models/trip_request_mapper.dart';
import 'package:ata_app/features/trip/domain/entities/trip_airport.dart';
import 'package:ata_app/features/trip/domain/entities/trip_places.dart';
import 'package:ata_app/features/trip/domain/entities/trip_request.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stage.dart';
import 'package:ata_app/features/trip/domain/usecases/cancel_trip.dart';
import 'package:ata_app/features/trip/domain/usecases/estimate_trip.dart';
import 'package:ata_app/features/trip/domain/usecases/request_trip.dart';
import 'package:ata_app/features/trip/presentation/cubit/trip_request_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/trip_request_state.dart';
import 'package:ata_app/l10n/generated/app_localizations_ar.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/fakes.dart';
import '../../helpers/favorites_fakes.dart';
import '../../helpers/scheduling_fakes.dart';
import '../../helpers/trip_fakes.dart';

void main() {
  final AppLocalizationsAr l10n = AppLocalizationsAr();
  final DateTime at = DateTime.utc(2026, 9, 29, 15, 30);

  HomeCubit home() => HomeCubit(
    getRideCategories: GetRideCategories(FakeCatalogRepository()),
    updatePreferences: UpdatePassengerPreferences(FakePassengerRepository()),
  );

  group('HomeState', () {
    test(
      'scheduling applies the confirmed time and going back clears it',
      () async {
        final HomeCubit cubit = home();
        await cubit.loadCategories();
        expect(cubit.state.canRequest, isTrue);

        cubit.applyScheduledAt(at);
        expect(cubit.state.rideTime, RideTime.scheduled);
        expect(cubit.state.scheduledAt, at);
        expect(cubit.state.isScheduled, isTrue);
        expect(cubit.state.canRequest, isTrue);

        cubit.applyScheduledAt(null);
        expect(cubit.state.rideTime, RideTime.now);
        expect(cubit.state.scheduledAt, isNull);

        cubit
          ..applyScheduledAt(at)
          ..selectRideTime(RideTime.now);
        expect(cubit.state.scheduledAt, isNull);
      },
    );

    test(
      'a scheduled request without a confirmed time cannot be sent',
      () async {
        final HomeCubit cubit = home();
        await cubit.loadCategories();
        cubit.selectRideTime(RideTime.scheduled);
        expect(cubit.state.canRequest, isFalse);
        // …and is priced as an immediate ride until a time is confirmed.
        expect(buildQuoteRequest(cubit.state, l10n).bookingType, 'now');
      },
    );
  });

  group('scheduled booking request', () {
    test(
      'the trip request carries bookingType scheduled and scheduledAt',
      () async {
        final HomeCubit cubit = home();
        await cubit.loadCategories();
        cubit.applyScheduledAt(at);
        final TripRequest request = buildTripRequest(cubit.state, l10n);
        expect(request.bookingType, 'scheduled');
        expect(request.scheduledAt, at);
        expect(request.rideCategoryId, 'c1');

        final Map<String, dynamic> body = TripRequestMapper.requestBody(
          request,
        );
        expect(body['bookingType'], 'scheduled');
        // Always sent in UTC so the API compares the same instant.
        expect(body['scheduledAt'], '2026-09-29T15:30:00.000Z');
      },
    );

    test('a local time is converted to UTC', () async {
      final DateTime local = DateTime(2026, 9, 29, 15, 30);
      final Map<String, dynamic> body = TripRequestMapper.requestBody(
        testTripRequest.copyWith(),
      );
      expect(body.containsKey('scheduledAt'), isFalse);
      final TripRequest request = TripRequest(
        pickup: testPickup,
        dropoff: testDropoff,
        rideCategoryId: 'c1',
        bookingType: 'scheduled',
        scheduledAt: local,
      );
      expect(
        DateTime.parse(
          TripRequestMapper.requestBody(request)['scheduledAt'] as String,
        ),
        local.toUtc(),
      );
    });

    test('the quote is priced for the scheduled time', () async {
      final HomeCubit cubit = home();
      await cubit.loadCategories();
      cubit.applyScheduledAt(at);
      final quote = buildQuoteRequest(cubit.state, l10n);
      expect(quote.bookingType, 'scheduled');
      expect(quote.scheduledAt, at);
      final Map<String, dynamic> body = QuoteRequestMapper.body(quote);
      expect(body['bookingType'], 'scheduled');
      expect(body['scheduledAt'], '2026-09-29T15:30:00.000Z');
    });

    test('an immediate request has no scheduledAt', () async {
      final HomeCubit cubit = home();
      await cubit.loadCategories();
      expect(buildTripRequest(cubit.state, l10n).scheduledAt, isNull);
      expect(
        QuoteRequestMapper.body(
          buildQuoteRequest(cubit.state, l10n),
        ).containsKey('scheduledAt'),
        isFalse,
      );
    });

    test(
      'promo code and favourite driver still travel with the booking',
      () async {
        final HomeCubit cubit = home();
        await cubit.loadCategories();
        cubit
          ..applyScheduledAt(at)
          ..applyPromoCode('ATA10')
          ..toggleFavorite(testAvailableFavorite);
        final TripRequest request = buildTripRequest(cubit.state, l10n);
        expect(request.promoCode, 'ATA10');
        expect(request.favoriteDriverId, 'd1');
        expect(buildQuoteRequest(cubit.state, l10n).favoriteDriverId, 'd1');
        expect(request.bookingType, 'scheduled');
        expect(buildQuoteRequest(cubit.state, l10n).promoCode, 'ATA10');
        expect(buildPromoContext(cubit.state).bookingType, 'scheduled');
      },
    );
  });

  group('airport request', () {
    test('an airport pickup replaces the pickup and sends the zone', () async {
      final HomeCubit cubit = home();
      await cubit.loadCategories();
      cubit.applyAirport(
        AirportSelection(
          airport: testAirport,
          direction: AirportDirection.pickup,
          zone: testAirport.pickupZones.first,
          terminalCode: 'T1',
          flightNumber: 'SV1020',
        ),
      );
      expect(cubit.state.pickupPoint.lat, 24.9601);
      expect(cubit.state.dropoffPoint, TripPlaces.defaultDestination);

      final TripRequest request = buildTripRequest(cubit.state, l10n);
      expect(request.pickup.point.lat, 24.9601);
      expect(request.pickup.name, contains('مطار الملك خالد'));
      expect(request.airportPickupZoneId, 'z1');
      expect(request.airportTerminalCode, isNull);
      expect(request.flightNumber, 'SV1020');

      final Map<String, dynamic> body = TripRequestMapper.requestBody(request);
      expect(body['airportPickupZoneId'], 'z1');
      expect(body['flightNumber'], 'SV1020');
      expect(body.containsKey('airportTerminalCode'), isFalse);

      final quoteBody = QuoteRequestMapper.body(
        buildQuoteRequest(cubit.state, l10n),
      );
      expect(quoteBody['airportPickupZoneId'], 'z1');
      expect((quoteBody['pickup'] as Map<String, dynamic>)['lat'], 24.9601);
    });

    test('an airport dropoff sends the terminal only', () async {
      final HomeCubit cubit = home();
      await cubit.loadCategories();
      cubit.applyAirport(
        const AirportSelection(
          airport: testAirport,
          direction: AirportDirection.dropoff,
          terminalCode: 'T1',
        ),
      );
      expect(cubit.state.pickupPoint, TripPlaces.currentLocation);
      expect(cubit.state.dropoffPoint, testAirport.point);
      final TripRequest request = buildTripRequest(cubit.state, l10n);
      expect(request.dropoff.name, testAirport.name);
      expect(request.airportPickupZoneId, isNull);
      expect(request.airportTerminalCode, 'T1');
      expect(request.flightNumber, isNull);
    });

    test('an airport pickup without its zone blocks the request', () async {
      final HomeCubit cubit = home();
      await cubit.loadCategories();
      cubit.applyAirport(
        const AirportSelection(
          airport: testAirport,
          direction: AirportDirection.pickup,
        ),
      );
      expect(cubit.state.canRequest, isFalse);
      cubit.applyAirport(null);
      expect(cubit.state.canRequest, isTrue);
      expect(cubit.state.pickupPoint, TripPlaces.currentLocation);
    });

    test('an invalid flight number blocks the request', () async {
      final HomeCubit cubit = home();
      await cubit.loadCategories();
      cubit.applyAirport(
        const AirportSelection(
          airport: testAirport,
          direction: AirportDirection.dropoff,
          flightNumber: 'not a flight',
        ),
      );
      expect(cubit.state.canRequest, isFalse);
    });
  });

  group('TripRequestCubit', () {
    late FakeTripRepository trips;

    setUp(() => trips = FakeTripRepository());

    TripRequestCubit build() => TripRequestCubit(
      estimateTrip: EstimateTrip(trips),
      requestTrip: RequestTrip(trips),
      cancelTrip: CancelTrip(trips),
    );

    test('a scheduled booking is "scheduled", not "searching"', () async {
      trips.requestResult = scheduledTripAt(at);
      final TripRequestCubit cubit = build();
      await cubit.request(
        TripRequest(
          pickup: testPickup,
          dropoff: testDropoff,
          rideCategoryId: 'c1',
          bookingType: 'scheduled',
          scheduledAt: at,
          quoteId: 'q1',
        ),
      );
      expect(cubit.state.status, TripRequestStatus.scheduled);
      expect(cubit.state.isScheduled, isTrue);
      expect(cubit.state.isSearching, isFalse);
      expect(cubit.state.trip?.status, TripStage.scheduled);
      expect(trips.requests.single.bookingType, 'scheduled');

      // No second request while the booking is being handed over.
      await cubit.request(testTripRequest);
      expect(trips.requests, hasLength(1));
    });

    test('an immediate trip is still "searching"', () async {
      final TripRequestCubit cubit = build();
      await cubit.request(testTripRequest.copyWith(quoteId: 'q1'));
      expect(cubit.state.isSearching, isTrue);
    });

    test('window / lead errors are recognised', () async {
      trips.requestFailure = const ServerFailure(
        code: 'schedule_lead_too_short',
        message: '',
        details: <String, dynamic>{'minScheduledAt': '2026-09-27T11:00:00Z'},
        statusCode: 422,
      );
      final TripRequestCubit cubit = build();
      await cubit.request(testTripRequest.copyWith(quoteId: 'q1'));
      expect(cubit.state.isScheduleRejected, isTrue);
    });
  });
}
