import 'package:ata_app/features/trip/domain/entities/trip_cancellation.dart';
import 'package:ata_app/features/trip/domain/entities/trip_parties.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stage.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stop.dart';
import 'package:ata_app/features/trip/domain/entities/trip_timeline.dart';
import 'package:equatable/equatable.dart';

/// The `rideCategory` of a trip.
class TripCategory extends Equatable {
  const TripCategory({
    required this.id,
    required this.code,
    required this.name,
  });

  final String id;
  final String code;
  final String name;

  @override
  List<Object?> get props => <Object?>[id, code, name];
}

/// The `Trip` object of the API (passenger and driver views).
class Trip extends Equatable {
  const Trip({
    required this.id,
    required this.tripNumber,
    required this.status,
    required this.pickup,
    required this.dropoff,
    this.bookingType = 'now',
    this.scheduledAt,
    this.category,
    this.stops = const <TripStop>[],
    this.paymentMethod = 'cash',
    this.pricingMode = 'fixed',
    this.offeredPrice,
    this.estimatedFare = 0,
    this.finalFare,
    this.estimatedDistanceMeters = 0,
    this.estimatedDurationSeconds = 0,
    this.finalDistanceMeters,
    this.finalDurationSeconds,
    this.preferFemaleDriver = false,
    this.driver,
    this.vehicle,
    this.passenger,
    this.pin,
    this.waitingSeconds = 0,
    this.cancelledBy,
    this.cancellationReason,
    this.timeline = const TripTimeline(),
    this.events = const <TripEvent>[],
    this.collectCashAmount,
    this.cancellation,
  });

  /// Trip event recorded when a card capture failed and the fare moved to
  /// cash (`docs/08` §F11.4).
  static const String paymentFallbackEvent = 'payment_fallback_cash';

  final String id;
  final String tripNumber;
  final TripStage status;
  final String bookingType;
  final DateTime? scheduledAt;
  final TripCategory? category;
  final TripStop pickup;
  final TripStop dropoff;
  final List<TripStop> stops;
  final String paymentMethod;
  final String pricingMode;
  final double? offeredPrice;
  final double estimatedFare;
  final double? finalFare;
  final int estimatedDistanceMeters;
  final int estimatedDurationSeconds;
  final int? finalDistanceMeters;
  final int? finalDurationSeconds;
  final bool preferFemaleDriver;
  final TripDriver? driver;
  final TripVehicle? vehicle;
  final TripPassenger? passenger;
  final String? pin;
  final int waitingSeconds;
  final String? cancelledBy;
  final String? cancellationReason;
  final TripTimeline timeline;
  final List<TripEvent> events;

  /// Driver view: cash to collect after completion (includes a failed card
  /// capture).
  final double? collectCashAmount;

  /// Stage, reason, fee / compensation of a cancelled trip (F14).
  final TripCancellation? cancellation;

  bool get isScheduled => bookingType == 'scheduled';

  /// The card could not be charged and the trip is paid in cash.
  bool get paymentFellBackToCash =>
      events.any((TripEvent e) => e.type == paymentFallbackEvent);

  /// Final fare when known, otherwise the estimate (or the offered price).
  double get fare => finalFare ?? offeredPrice ?? estimatedFare;
  int get distanceMeters => finalDistanceMeters ?? estimatedDistanceMeters;
  int get durationSeconds => finalDurationSeconds ?? estimatedDurationSeconds;
  int get etaMinutes => (estimatedDurationSeconds / 60).ceil();

  Trip copyWith({TripStage? status, String? pin}) => Trip(
    id: id,
    tripNumber: tripNumber,
    status: status ?? this.status,
    pickup: pickup,
    dropoff: dropoff,
    bookingType: bookingType,
    scheduledAt: scheduledAt,
    category: category,
    stops: stops,
    paymentMethod: paymentMethod,
    pricingMode: pricingMode,
    offeredPrice: offeredPrice,
    estimatedFare: estimatedFare,
    finalFare: finalFare,
    estimatedDistanceMeters: estimatedDistanceMeters,
    estimatedDurationSeconds: estimatedDurationSeconds,
    finalDistanceMeters: finalDistanceMeters,
    finalDurationSeconds: finalDurationSeconds,
    preferFemaleDriver: preferFemaleDriver,
    driver: driver,
    vehicle: vehicle,
    passenger: passenger,
    pin: pin ?? this.pin,
    waitingSeconds: waitingSeconds,
    cancelledBy: cancelledBy,
    cancellationReason: cancellationReason,
    timeline: timeline,
    events: events,
    collectCashAmount: collectCashAmount,
    cancellation: cancellation,
  );

  @override
  List<Object?> get props => <Object?>[
    id,
    tripNumber,
    status,
    bookingType,
    scheduledAt,
    category,
    pickup,
    dropoff,
    stops,
    paymentMethod,
    pricingMode,
    offeredPrice,
    estimatedFare,
    finalFare,
    estimatedDistanceMeters,
    estimatedDurationSeconds,
    finalDistanceMeters,
    finalDurationSeconds,
    preferFemaleDriver,
    driver,
    vehicle,
    passenger,
    pin,
    waitingSeconds,
    cancelledBy,
    cancellationReason,
    timeline,
    events,
    collectCashAmount,
    cancellation,
  ];
}
