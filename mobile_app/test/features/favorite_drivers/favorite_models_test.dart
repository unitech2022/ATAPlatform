import 'package:ata_app/features/favorite_drivers/data/models/favorite_models.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/available_favorite.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/favorite_driver.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/favorite_params.dart';
import 'package:ata_app/features/pricing/data/models/quote_model.dart';
import 'package:ata_app/features/pricing/domain/entities/fare_breakdown.dart';
import 'package:ata_app/features/pricing/domain/entities/fare_quote.dart';
import 'package:ata_app/features/pricing/domain/entities/quote_request.dart';
import 'package:ata_app/features/trip/data/models/offer_model.dart';
import 'package:ata_app/features/trip/data/models/trip_model.dart';
import 'package:ata_app/features/trip/data/models/trip_request_mapper.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:ata_app/features/trip/domain/entities/offer.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_request.dart';
import 'package:ata_app/features/trip/domain/entities/trip_rewards.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/trip_fakes.dart';

const Map<String, dynamic> _tripJson = <String, dynamic>{
  'id': 't1',
  'tripNumber': 'T-1',
  'status': 'searching',
  'pickup': <String, dynamic>{'name': 'a', 'lat': 24.7, 'lng': 46.7},
  'dropoff': <String, dynamic>{'name': 'b', 'lat': 24.8, 'lng': 46.8},
};

void main() {
  test('FavoriteDriver parses the F16.3 list item', () {
    final FavoriteDriver d = FavoriteModels.driver(const <String, dynamic>{
      'driverId': 'd1',
      'firstName': 'محمد',
      'photoUrl': '/api/v1/passenger/favorite-drivers/d1/photo',
      'ratingAvg': 4.93,
      'vehicle': <String, dynamic>{
        'make': 'Toyota',
        'model': 'Camry',
        'color': 'أبيض',
      },
      'rideCategoryCode': 'economy',
      'tripsTogether': 6,
      'lastTripAt': '2026-09-20T10:00:00Z',
      'createdAt': '2026-08-01T10:00:00Z',
    });
    expect(d.driverId, 'd1');
    expect(d.firstName, 'محمد');
    expect(d.ratingAvg, 4.93);
    expect(d.vehicle.model, 'Camry');
    expect(d.tripsTogether, 6);
    expect(d.lastTripAt, DateTime.utc(2026, 9, 20, 10));
    expect(d.rideCategoryCode, 'economy');
  });

  test('a favourite without vehicle or dates still parses', () {
    final FavoriteDriver d = FavoriteModels.driver(const <String, dynamic>{
      'driverId': 'd2',
      'firstName': 'سالم',
    });
    expect(d.vehicle.isEmpty, isTrue);
    expect(d.lastTripAt, isNull);
    expect(d.tripsTogether, 0);
  });

  test('AvailableFavorite parses the eta and the discount (or null)', () {
    final AvailableFavorite a = FavoriteModels.available(
      const <String, dynamic>{
        'driverId': 'd1',
        'firstName': 'محمد',
        'ratingAvg': 4.9,
        'etaMinutes': 4,
        'discount': <String, dynamic>{
          'percent': 10,
          'maxAmount': 15.0,
          'stackableWithPromotions': false,
        },
      },
    );
    expect(a.etaMinutes, 4);
    expect(a.discount, const FavoriteDiscount(percent: 10, maxAmount: 15));
    final AvailableFavorite none = FavoriteModels.available(
      const <String, dynamic>{
        'driverId': 'd1',
        'firstName': 'محمد',
        'discount': null,
      },
    );
    expect(none.discount, isNull);
    expect(none.etaMinutes, isNull);
  });

  test('request bodies carry only the given id / the query values', () {
    expect(
      FavoriteModels.addBody(const AddFavoriteParams(tripId: 't1')),
      <String, dynamic>{'tripId': 't1'},
    );
    expect(
      FavoriteModels.addBody(const AddFavoriteParams(driverId: 'd1')),
      <String, dynamic>{'driverId': 'd1'},
    );
    expect(
      FavoriteModels.availableQuery(
        const AvailableFavoritesQuery(
          pickup: GeoPoint.riyadh,
          rideCategoryId: 'c1',
        ),
      ),
      <String, dynamic>{
        'lat': GeoPoint.riyadh.lat,
        'lng': GeoPoint.riyadh.lng,
        'rideCategoryId': 'c1',
      },
    );
  });

  group('Trip.favorite', () {
    Trip parse(Map<String, dynamic> favorite, {Map<String, dynamic>? driver}) =>
        TripModel.fromJson(<String, dynamic>{
          ..._tripJson,
          'favorite': favorite,
          'driver': ?driver,
        });

    test('parses status and discountApplied', () {
      final Trip trip = parse(const <String, dynamic>{
        'driverId': 'd1',
        'driverName': 'محمد',
        'status': 'requested',
        'discountApplied': false,
      });
      expect(trip.favorite?.driverName, 'محمد');
      expect(trip.favorite?.status, FavoriteStatus.requested);
      expect(trip.favorite?.isPending, isTrue);
      expect(trip.favorite?.fellBack, isFalse);
      expect(TripModel.fromJson(_tripJson).favorite, isNull);
    });

    test('rejected / expired / unavailable count as a fallback', () {
      for (final String status in <String>[
        'rejected',
        'expired',
        'unavailable',
      ]) {
        final Trip trip = parse(<String, dynamic>{
          'driverId': 'd1',
          'status': status,
        });
        expect(trip.favorite?.fellBack, isTrue, reason: status);
        expect(trip.favorite?.isPending, isFalse, reason: status);
      }
    });

    test('the heart shows for the accepted favourite or the API flag', () {
      const Map<String, dynamic> driver = <String, dynamic>{
        'id': 'd1',
        'fullName': 'محمد علي',
        'ratingAvg': 4.9,
      };
      expect(
        parse(const <String, dynamic>{
          'driverId': 'd1',
          'status': 'accepted',
          'discountApplied': true,
        }, driver: driver).hasFavoriteDriver,
        isTrue,
      );
      expect(
        parse(const <String, dynamic>{
          'driverId': 'd1',
          'status': 'requested',
        }, driver: driver).hasFavoriteDriver,
        isFalse,
      );
      expect(
        parse(const <String, dynamic>{
          'driverId': 'other',
          'status': 'accepted',
        }, driver: driver).hasFavoriteDriver,
        isFalse,
      );
      expect(
        TripModel.fromJson(const <String, dynamic>{
          ..._tripJson,
          'driver': <String, dynamic>{...driver, 'isFavorite': true},
        }).hasFavoriteDriver,
        isTrue,
      );
    });

    test('round-trips through toJson', () {
      final TripModel trip =
          parse(const <String, dynamic>{
                'driverId': 'd1',
                'driverName': 'محمد',
                'status': 'accepted',
                'discountApplied': true,
              })
              as TripModel;
      expect(TripModel.fromJson(trip.toJson()).favorite, trip.favorite);
    });
  });

  test('the offer parses isFavoriteRequest and exclusive', () {
    final Offer o = OfferModel.fromJson(const <String, dynamic>{
      'id': 'o1',
      'tripId': 't1',
      'isFavoriteRequest': true,
      'exclusive': true,
    });
    expect(o.isFavoriteRequest, isTrue);
    expect(o.exclusive, isTrue);
    expect(
      OfferModel.fromJson(const <String, dynamic>{
        'id': 'o',
        'tripId': 't',
      }).isFavoriteRequest,
      isFalse,
    );
  });

  test('quote and trip request bodies carry favoriteDriverId', () {
    expect(
      QuoteRequestMapper.body(
        const QuoteRequest(
          pickup: GeoPoint.riyadh,
          dropoff: GeoPoint.riyadh,
          favoriteDriverId: 'd1',
        ),
      )['favoriteDriverId'],
      'd1',
    );
    expect(
      QuoteRequestMapper.body(
        const QuoteRequest(pickup: GeoPoint.riyadh, dropoff: GeoPoint.riyadh),
      ).containsKey('favoriteDriverId'),
      isFalse,
    );
    expect(
      TripRequestMapper.requestBody(
        const TripRequest(
          pickup: testPickup,
          dropoff: testDropoff,
          rideCategoryId: 'c1',
          favoriteDriverId: 'd1',
        ),
      )['favoriteDriverId'],
      'd1',
    );
    expect(
      TripRequestMapper.requestBody(
        testTripRequest,
      ).containsKey('favoriteDriverId'),
      isFalse,
    );
    expect(
      TripRequestMapper.estimateBody(
        const TripRequest(
          pickup: testPickup,
          dropoff: testDropoff,
          rideCategoryId: 'c1',
          favoriteDriverId: 'd1',
        ),
      ).containsKey('favoriteDriverId'),
      isFalse,
    );
  });

  test(
    'the quote parses favoriteDiscountConditional and the favourite line',
    () {
      final FareQuote quote = QuoteModel.fromJson(const <String, dynamic>{
        'quoteId': 'q9',
        'expiresAt': '2026-09-29T12:05:00Z',
        'favoriteDiscountConditional': true,
        'categories': <Map<String, dynamic>>[
          <String, dynamic>{
            'rideCategoryId': 'c1',
            'code': 'economy',
            'name': 'اقتصادي',
            'total': 36,
            'breakdown': <String, dynamic>{
              'discount': 4,
              'discounts': <Map<String, dynamic>>[
                <String, dynamic>{
                  'source': 'favorite_driver',
                  'reference': '',
                  'label': 'خصم الكابتن المفضل',
                  'amount': 4,
                },
              ],
            },
          },
        ],
      });
      expect(quote.favoriteDiscountConditional, isTrue);
      final FareDiscount? line = quote.categories.single.breakdown.discountFrom(
        DiscountSource.favoriteDriver,
      );
      expect(line?.amount, 4);
      expect(
        quote.categories.single.breakdown.discountFrom(
          DiscountSource.promotion,
        ),
        isNull,
      );
      final FareQuote back = QuoteModel.fromJson(
        (quote as QuoteModel).toJson(),
      );
      expect(back.favoriteDiscountConditional, isTrue);
      expect(
        const QuotePromotion(
          code: 'A',
          valid: false,
          reason: 'not_stacked',
        ).isNotStacked,
        isTrue,
      );
    },
  );
}
