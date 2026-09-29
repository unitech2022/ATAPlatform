import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/available_favorite.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/favorite_driver.dart';
import 'package:ata_app/features/favorite_drivers/domain/entities/favorite_params.dart';
import 'package:ata_app/features/favorite_drivers/domain/usecases/add_favorite_driver.dart';
import 'package:ata_app/features/favorite_drivers/domain/usecases/get_available_favorites.dart';
import 'package:ata_app/features/favorite_drivers/domain/usecases/get_favorite_drivers.dart';
import 'package:ata_app/features/favorite_drivers/domain/usecases/remove_favorite_driver.dart';
import 'package:ata_app/features/favorite_drivers/presentation/cubit/add_favorite_cubit.dart';
import 'package:ata_app/features/favorite_drivers/presentation/cubit/add_favorite_state.dart';
import 'package:ata_app/features/favorite_drivers/presentation/cubit/available_favorites_cubit.dart';
import 'package:ata_app/features/favorite_drivers/presentation/cubit/available_favorites_state.dart';
import 'package:ata_app/features/favorite_drivers/presentation/cubit/favorite_drivers_cubit.dart';
import 'package:ata_app/features/favorite_drivers/presentation/cubit/favorite_drivers_state.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:mocktail/mocktail.dart';

import '../../helpers/favorites_fakes.dart';

class _MockGet extends Mock implements GetFavoriteDrivers {}

class _MockRemove extends Mock implements RemoveFavoriteDriver {}

class _MockAvailable extends Mock implements GetAvailableFavorites {}

class _MockAdd extends Mock implements AddFavoriteDriver {}

const FavoriteDriver _second = FavoriteDriver(
  driverId: 'd2',
  firstName: 'سالم',
);

const ServerFailure _notEligible = ServerFailure(
  code: 'favorite_not_eligible',
  message: '',
  statusCode: 422,
);

void main() {
  setUpAll(() {
    registerFallbackValue(const NoParams());
    registerFallbackValue(
      const AvailableFavoritesQuery(pickup: GeoPoint.riyadh),
    );
    registerFallbackValue(const AddFavoriteParams(tripId: 't'));
  });

  group('FavoriteDriversCubit', () {
    late _MockGet get;
    late _MockRemove remove;

    setUp(() {
      get = _MockGet();
      remove = _MockRemove();
      when(() => get(any())).thenAnswer(
        (_) async => const Right<Failure, List<FavoriteDriver>>(
          <FavoriteDriver>[testFavoriteDriver, _second],
        ),
      );
      when(
        () => remove(any()),
      ).thenAnswer((_) async => const Right<Failure, Unit>(unit));
    });

    FavoriteDriversCubit build() =>
        FavoriteDriversCubit(getFavorites: get, removeFavorite: remove);

    blocTest<FavoriteDriversCubit, FavoriteDriversState>(
      'loads the list',
      build: build,
      act: (FavoriteDriversCubit c) => c.load(),
      verify: (FavoriteDriversCubit c) {
        expect(c.state.items.length, 2);
        expect(c.state.loaded, isTrue);
        expect(c.state.isEmpty, isFalse);
      },
    );

    blocTest<FavoriteDriversCubit, FavoriteDriversState>(
      'an empty list is the empty state',
      setUp: () => when(() => get(any())).thenAnswer(
        (_) async =>
            const Right<Failure, List<FavoriteDriver>>(<FavoriteDriver>[]),
      ),
      build: build,
      act: (FavoriteDriversCubit c) => c.load(),
      verify: (FavoriteDriversCubit c) => expect(c.state.isEmpty, isTrue),
    );

    blocTest<FavoriteDriversCubit, FavoriteDriversState>(
      'a failed load keeps the failure and is not the empty state',
      setUp: () => when(() => get(any())).thenAnswer(
        (_) async => const Left<Failure, List<FavoriteDriver>>(
          NetworkFailure(message: 'offline'),
        ),
      ),
      build: build,
      act: (FavoriteDriversCubit c) => c.load(),
      verify: (FavoriteDriversCubit c) {
        expect(c.state.failure, isA<NetworkFailure>());
        expect(c.state.isEmpty, isFalse);
        expect(c.state.loaded, isFalse);
      },
    );

    blocTest<FavoriteDriversCubit, FavoriteDriversState>(
      'remove drops the driver and records who was removed',
      build: build,
      act: (FavoriteDriversCubit c) async {
        await c.load();
        await c.remove(testFavoriteDriver);
      },
      verify: (FavoriteDriversCubit c) {
        verify(() => remove('d1')).called(1);
        expect(c.state.items, <FavoriteDriver>[_second]);
        expect(c.state.lastRemoved, testFavoriteDriver);
        expect(c.state.removingId, isNull);
      },
    );

    blocTest<FavoriteDriversCubit, FavoriteDriversState>(
      'a failed removal keeps the list and shows the failure',
      setUp: () => when(() => remove(any())).thenAnswer(
        (_) async =>
            const Left<Failure, Unit>(NetworkFailure(message: 'offline')),
      ),
      build: build,
      act: (FavoriteDriversCubit c) async {
        await c.load();
        await c.remove(testFavoriteDriver);
      },
      verify: (FavoriteDriversCubit c) {
        expect(c.state.items.length, 2);
        expect(c.state.failure, isNotNull);
        expect(c.state.removingId, isNull);
        expect(c.state.lastRemoved, isNull);
      },
    );
  });

  group('AddFavoriteDriver use case', () {
    test('needs exactly one of driverId / tripId', () async {
      final FakeFavoriteDriversRepository repo =
          FakeFavoriteDriversRepository();
      final AddFavoriteDriver add = AddFavoriteDriver(repo);
      expect((await add(const AddFavoriteParams())).isLeft(), isTrue);
      expect(
        (await add(
          const AddFavoriteParams(driverId: 'd', tripId: 't'),
        )).isLeft(),
        isTrue,
      );
      expect(repo.added, isEmpty);
      expect(
        (await add(const AddFavoriteParams(tripId: 't1'))).isRight(),
        isTrue,
      );
      expect(repo.added.single.tripId, 't1');
    });
  });

  group('AddFavoriteCubit', () {
    late _MockAdd add;

    setUp(() {
      add = _MockAdd();
      when(() => add(any())).thenAnswer(
        (_) async => const Right<Failure, FavoriteDriver>(testFavoriteDriver),
      );
    });

    AddFavoriteCubit build() =>
        AddFavoriteCubit(tripId: 't1', addFavorite: add);

    blocTest<AddFavoriteCubit, AddFavoriteState>(
      'adds by the trip id',
      build: build,
      act: (AddFavoriteCubit c) => c.add(),
      expect: () => <AddFavoriteState>[
        const AddFavoriteState(status: AddFavoriteStatus.adding),
        const AddFavoriteState(status: AddFavoriteStatus.added),
      ],
      verify: (_) {
        final AddFavoriteParams sent =
            verify(() => add(captureAny())).captured.single
                as AddFavoriteParams;
        expect(sent.tripId, 't1');
        expect(sent.driverId, isNull);
      },
    );

    blocTest<AddFavoriteCubit, AddFavoriteState>(
      '409 favorite_exists counts as already a favourite',
      setUp: () => when(() => add(any())).thenAnswer(
        (_) async => const Left<Failure, FavoriteDriver>(
          ServerFailure(code: 'favorite_exists', message: '', statusCode: 409),
        ),
      ),
      build: build,
      act: (AddFavoriteCubit c) => c.add(),
      verify: (AddFavoriteCubit c) {
        expect(c.state.status, AddFavoriteStatus.alreadyFavorite);
        expect(c.state.isFavorite, isTrue);
      },
    );

    blocTest<AddFavoriteCubit, AddFavoriteState>(
      'favorite_not_eligible is a failure and can be retried',
      setUp: () => when(() => add(any())).thenAnswer(
        (_) async => const Left<Failure, FavoriteDriver>(_notEligible),
      ),
      build: build,
      act: (AddFavoriteCubit c) async {
        await c.add();
        await c.add();
      },
      verify: (AddFavoriteCubit c) {
        expect(c.state.status, AddFavoriteStatus.failed);
        expect(c.state.failure, _notEligible);
        verify(() => add(any())).called(2);
      },
    );

    blocTest<AddFavoriteCubit, AddFavoriteState>(
      'does nothing once added',
      build: build,
      act: (AddFavoriteCubit c) async {
        await c.add();
        await c.add();
      },
      verify: (_) => verify(() => add(any())).called(1),
    );
  });

  group('AvailableFavoritesCubit', () {
    late _MockAvailable available;

    setUp(() {
      available = _MockAvailable();
      when(() => available(any())).thenAnswer(
        (_) async => const Right<Failure, List<AvailableFavorite>>(
          <AvailableFavorite>[testAvailableFavorite],
        ),
      );
    });

    AvailableFavoritesCubit build({Stream<void> Function(Duration)? ticker}) =>
        AvailableFavoritesCubit(
          getAvailable: available,
          debounce: const Duration(milliseconds: 5),
          ticker: ticker ?? (_) => const Stream<void>.empty(),
        );

    blocTest<AvailableFavoritesCubit, AvailableFavoritesState>(
      'looks the favourites up for the pickup and category after the debounce',
      build: build,
      act: (AvailableFavoritesCubit c) =>
          c.watch(GeoPoint.riyadh, rideCategoryId: 'c1'),
      wait: const Duration(milliseconds: 40),
      verify: (AvailableFavoritesCubit c) {
        final AvailableFavoritesQuery q =
            verify(() => available(captureAny())).captured.single
                as AvailableFavoritesQuery;
        expect(q.rideCategoryId, 'c1');
        expect(q.pickup, GeoPoint.riyadh);
        expect(c.state.items, <AvailableFavorite>[testAvailableFavorite]);
        expect(c.state.byId('d1'), testAvailableFavorite);
        expect(c.state.byId('zz'), isNull);
        expect(c.state.status, AvailableFavoritesStatus.ready);
      },
    );

    blocTest<AvailableFavoritesCubit, AvailableFavoritesState>(
      'rapid changes are debounced into one request; same input is ignored',
      build: build,
      act: (AvailableFavoritesCubit c) async {
        c
          ..watch(GeoPoint.riyadh)
          ..watch(GeoPoint.riyadh, rideCategoryId: 'c1')
          ..watch(GeoPoint.riyadh, rideCategoryId: 'c1');
        await Future<void>.delayed(const Duration(milliseconds: 40));
        c.watch(GeoPoint.riyadh, rideCategoryId: 'c1');
      },
      wait: const Duration(milliseconds: 40),
      verify: (_) => verify(() => available(any())).called(1),
    );

    blocTest<AvailableFavoritesCubit, AvailableFavoritesState>(
      'refreshes on every tick while watching',
      build: () => build(
        ticker: (_) =>
            Stream<void>.periodic(const Duration(milliseconds: 20)).take(2),
      ),
      act: (AvailableFavoritesCubit c) => c.watch(GeoPoint.riyadh),
      wait: const Duration(milliseconds: 120),
      verify: (_) =>
          verify(() => available(any())).called(greaterThanOrEqualTo(3)),
    );

    blocTest<AvailableFavoritesCubit, AvailableFavoritesState>(
      'a failed lookup clears the list and can be retried with the same input',
      setUp: () => when(() => available(any())).thenAnswer(
        (_) async => const Left<Failure, List<AvailableFavorite>>(
          NetworkFailure(message: 'offline'),
        ),
      ),
      build: build,
      act: (AvailableFavoritesCubit c) async {
        c.watch(GeoPoint.riyadh);
        await Future<void>.delayed(const Duration(milliseconds: 30));
        c.watch(GeoPoint.riyadh);
      },
      wait: const Duration(milliseconds: 40),
      verify: (AvailableFavoritesCubit c) {
        expect(c.state.status, AvailableFavoritesStatus.failure);
        expect(c.state.items, isEmpty);
        verify(() => available(any())).called(2);
      },
    );
  });
}
