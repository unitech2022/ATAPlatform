import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/catalog/domain/entities/ride_category.dart';
import 'package:ata_app/features/passenger_home/domain/entities/fare_estimate.dart';
import 'package:ata_app/features/passenger_home/domain/entities/ride_time.dart';
import 'package:equatable/equatable.dart';

/// State of the rider home sheet.
class HomeState extends Equatable {
  const HomeState({
    this.categories = const <RideCategory>[],
    this.loadingCategories = false,
    this.categoriesFailure,
    this.selectedCategoryId,
    this.stops = const <String>[],
    this.rideTime = RideTime.now,
    this.preferFemaleDriver = false,
    this.payment = PaymentOption.cash,
    this.offeredPrice,
    this.estimate = const FareEstimate(price: 0, etaMinutes: 0),
  });

  final List<RideCategory> categories;
  final bool loadingCategories;
  final Failure? categoriesFailure;
  final String? selectedCategoryId;
  final List<String> stops;
  final RideTime rideTime;
  final bool preferFemaleDriver;
  final PaymentOption payment;

  /// Optional price proposed by the rider (`pricingMode: offer`).
  final double? offeredPrice;
  final FareEstimate estimate;

  RideCategory? get selectedCategory {
    for (final RideCategory category in categories) {
      if (category.id == selectedCategoryId) return category;
    }
    return categories.isEmpty ? null : categories.first;
  }

  int get maxStops => selectedCategory?.maxStops ?? 0;
  bool get canAddStop => stops.length < maxStops;
  bool get canRequest => selectedCategory != null;
  bool get hasOfferedPrice => offeredPrice != null;

  HomeState copyWith({
    List<RideCategory>? categories,
    bool? loadingCategories,
    Failure? categoriesFailure,
    String? selectedCategoryId,
    List<String>? stops,
    RideTime? rideTime,
    bool? preferFemaleDriver,
    PaymentOption? payment,
    double? offeredPrice,
    FareEstimate? estimate,
    bool clearFailure = false,
    bool clearOfferedPrice = false,
  }) => HomeState(
    categories: categories ?? this.categories,
    loadingCategories: loadingCategories ?? this.loadingCategories,
    categoriesFailure: clearFailure
        ? null
        : categoriesFailure ?? this.categoriesFailure,
    selectedCategoryId: selectedCategoryId ?? this.selectedCategoryId,
    stops: stops ?? this.stops,
    rideTime: rideTime ?? this.rideTime,
    preferFemaleDriver: preferFemaleDriver ?? this.preferFemaleDriver,
    payment: payment ?? this.payment,
    offeredPrice: clearOfferedPrice ? null : offeredPrice ?? this.offeredPrice,
    estimate: estimate ?? this.estimate,
  );

  @override
  List<Object?> get props => <Object?>[
    categories,
    loadingCategories,
    categoriesFailure,
    selectedCategoryId,
    stops,
    rideTime,
    preferFemaleDriver,
    payment,
    offeredPrice,
    estimate,
  ];
}
