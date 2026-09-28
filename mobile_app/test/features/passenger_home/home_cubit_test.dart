import 'package:ata_app/features/catalog/domain/usecases/get_ride_categories.dart';
import 'package:ata_app/features/passenger_home/domain/entities/ride_time.dart';
import 'package:ata_app/features/passenger_home/domain/usecases/update_passenger_preferences.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_cubit.dart';
import 'package:ata_app/features/passenger_home/presentation/cubit/home_state.dart';
import 'package:bloc_test/bloc_test.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/fakes.dart';

void main() {
  late FakePassengerRepository passengerRepository;

  setUp(() => passengerRepository = FakePassengerRepository());

  HomeCubit build() => HomeCubit(
    getRideCategories: GetRideCategories(FakeCatalogRepository()),
    updatePreferences: UpdatePassengerPreferences(passengerRepository),
  );

  blocTest<HomeCubit, HomeState>(
    'loadCategories selects the first category and computes the estimate',
    build: build,
    act: (HomeCubit cubit) => cubit.loadCategories(),
    verify: (HomeCubit cubit) {
      expect(cubit.state.categories.length, 2);
      expect(cubit.state.selectedCategory?.code, 'economy');
      expect(cubit.state.estimate.price, 38);
      expect(cubit.state.estimate.etaMinutes, 2);
    },
  );

  blocTest<HomeCubit, HomeState>(
    'stops are limited by the category and add a surcharge',
    build: build,
    act: (HomeCubit cubit) async {
      await cubit.loadCategories();
      cubit
        ..addStop('النخيل مول')
        ..addStop('برج المملكة')
        ..addStop('حديقة الملك عبدالله');
    },
    verify: (HomeCubit cubit) {
      expect(cubit.state.stops.length, 2);
      expect(cubit.state.canAddStop, isFalse);
      expect(cubit.state.estimate.price, 38 + 2 * 12);
    },
  );

  blocTest<HomeCubit, HomeState>(
    'switching to a category with fewer stops trims the list',
    build: build,
    act: (HomeCubit cubit) async {
      await cubit.loadCategories();
      cubit
        ..addStop('a')
        ..addStop('b')
        ..selectCategory('c2');
    },
    verify: (HomeCubit cubit) {
      expect(cubit.state.selectedCategory?.code, 'family');
      expect(cubit.state.stops, <String>['a']);
      expect(cubit.state.estimate.price, 54 + 12);
    },
  );

  blocTest<HomeCubit, HomeState>(
    'the price offer starts from the estimate, steps and clears',
    build: build,
    act: (HomeCubit cubit) async {
      await cubit.loadCategories();
      cubit
        ..toggleOfferedPrice()
        ..adjustOfferedPrice(-HomeCubit.offeredPriceStep)
        ..adjustOfferedPrice(-100);
      expect(cubit.state.offeredPrice, HomeCubit.minOfferedPrice);
      cubit.toggleOfferedPrice();
    },
    verify: (HomeCubit cubit) {
      expect(cubit.state.hasOfferedPrice, isFalse);
      expect(cubit.state.canRequest, isTrue);
    },
  );

  blocTest<HomeCubit, HomeState>(
    'female driver preference is persisted and options are local',
    build: build,
    act: (HomeCubit cubit) async {
      await cubit.togglePreferFemaleDriver();
      cubit
        ..selectRideTime(RideTime.scheduled)
        ..selectPayment(PaymentOption.wallet);
    },
    verify: (HomeCubit cubit) {
      expect(cubit.state.preferFemaleDriver, isTrue);
      expect(passengerRepository.lastPreferFemale, isTrue);
      expect(cubit.state.rideTime, RideTime.scheduled);
      expect(cubit.state.payment, PaymentOption.wallet);
    },
  );
}
