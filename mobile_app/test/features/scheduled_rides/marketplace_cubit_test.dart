import 'dart:async';

import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/scheduled_rides/domain/entities/marketplace_trip.dart';
import 'package:ata_app/features/scheduled_rides/domain/usecases/get_marketplace_trips.dart';
import 'package:ata_app/features/scheduled_rides/domain/usecases/reserve_scheduled_trip.dart';
import 'package:ata_app/features/scheduled_rides/presentation/cubit/marketplace_cubit.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/scheduling_fakes.dart';

void main() {
  late FakeScheduledRepository repository;
  late StreamController<void> ticks;

  setUp(() {
    repository = FakeScheduledRepository()
      ..market = pageOf<MarketplaceTrip>(<MarketplaceTrip>[
        marketTrip('m1'),
        marketTrip('m2', airport: true),
      ]);
    ticks = StreamController<void>.broadcast();
  });
  tearDown(() => ticks.close());

  MarketplaceCubit build() => MarketplaceCubit(
    getMarketplace: GetMarketplaceTrips(repository),
    reserve: ReserveScheduledTrip(repository),
    ticker: (Duration _) => ticks.stream,
    now: () => schedulingNow,
  );

  const GeoPoint here = GeoPoint(lat: 24.8, lng: 46.7);

  test('loads the upcoming requests around the driver position', () async {
    final MarketplaceCubit cubit = build();
    await cubit.load(position: here);
    expect(cubit.state.trips.map((MarketplaceTrip t) => t.tripId), <String>[
      'm1',
      'm2',
    ]);
    expect(cubit.state.trips.last.isAirport, isTrue);
    expect(repository.marketQueries.single.position, here);
    expect(repository.marketQueries.single.from, isNull);
    expect(cubit.state.loaded, isTrue);
    await cubit.close();
  });

  test('the day filter asks for that local day only', () async {
    final MarketplaceCubit cubit = build();
    await cubit.load(position: here);
    expect(cubit.state.days, hasLength(7));
    expect(cubit.state.days.first, DateTime(2026, 9, 27));

    await cubit.selectDay(DateTime(2026, 9, 29));
    final query = repository.marketQueries.last;
    expect(query.from, DateTime(2026, 9, 29));
    expect(query.to, DateTime(2026, 9, 30));
    expect(query.position, here);

    await cubit.selectDay(null);
    expect(repository.marketQueries.last.from, isNull);
    await cubit.close();
  });

  test('refreshes every 60 seconds without the loading state', () async {
    final MarketplaceCubit cubit = build();
    await cubit.load(position: here);
    repository.market = pageOf<MarketplaceTrip>(<MarketplaceTrip>[
      marketTrip('m1'),
      marketTrip('m2'),
      marketTrip('m3'),
    ]);
    ticks.add(null);
    await Future<void>.delayed(Duration.zero);
    await Future<void>.delayed(Duration.zero);
    expect(cubit.state.trips, hasLength(3));
    expect(cubit.state.loading, isFalse);
    await cubit.close();
  });

  test('loadMore appends the next page', () async {
    repository.market = pageOf<MarketplaceTrip>(<MarketplaceTrip>[
      marketTrip('m1'),
    ], total: 2);
    final MarketplaceCubit cubit = build();
    await cubit.load(position: here);
    expect(cubit.state.hasMore, isTrue);

    repository.market = pageOf<MarketplaceTrip>(
      <MarketplaceTrip>[marketTrip('m2')],
      page: 2,
      total: 2,
    );
    await cubit.loadMore();
    expect(cubit.state.trips.map((MarketplaceTrip t) => t.tripId), <String>[
      'm1',
      'm2',
    ]);
    expect(repository.marketQueries.last.page, 2);
    expect(cubit.state.hasMore, isFalse);
    await cubit.close();
  });

  group('reserve', () {
    test('a reserved trip leaves the list and is reported', () async {
      final MarketplaceCubit cubit = build();
      await cubit.load(position: here);
      await cubit.reserve('m1');
      expect(repository.reserved, <String>['m1']);
      expect(cubit.state.trips.map((MarketplaceTrip t) => t.tripId), <String>[
        'm2',
      ]);
      expect(cubit.state.reserved?.tripId, 'm1');
      expect(cubit.state.isReserving, isFalse);

      cubit.clearNotice();
      expect(cubit.state.reserved, isNull);
      await cubit.close();
    });

    test('reservation_taken removes the trip and explains why', () async {
      repository.reserveFailure = const ServerFailure(
        code: 'reservation_taken',
        message: '',
        statusCode: 409,
      );
      final MarketplaceCubit cubit = build();
      await cubit.load(position: here);
      await cubit.reserve('m1');
      expect(cubit.state.trips.map((MarketplaceTrip t) => t.tripId), <String>[
        'm2',
      ]);
      expect(cubit.state.actionFailure?.code, 'reservation_taken');
      expect(cubit.state.reserved, isNull);
      await cubit.close();
    });

    test('a conflict keeps the trip listed', () async {
      repository.reserveFailure = const ServerFailure(
        code: 'reservation_conflict',
        message: '',
        details: <String, dynamic>{'conflictingTripId': 'x'},
      );
      final MarketplaceCubit cubit = build();
      await cubit.load(position: here);
      await cubit.reserve('m1');
      expect(cubit.state.trips, hasLength(2));
      expect(cubit.state.actionFailure?.code, 'reservation_conflict');
      await cubit.close();
    });

    test('only one reservation runs at a time', () async {
      final MarketplaceCubit cubit = build();
      await cubit.load(position: here);
      final Future<void> first = cubit.reserve('m1');
      await cubit.reserve('m2');
      await first;
      expect(repository.reserved, <String>['m1']);
      await cubit.close();
    });
  });
}
