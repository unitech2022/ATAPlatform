import 'package:ata_app/features/corporate/data/models/corporate_check_model.dart';
import 'package:ata_app/features/trip/data/models/cancellation_models.dart';
import 'package:ata_app/features/trip/data/models/json_readers.dart';
import 'package:ata_app/features/trip/data/models/trip_parties_model.dart';
import 'package:ata_app/features/trip/data/models/trip_rewards_model.dart';
import 'package:ata_app/features/trip/data/models/trip_scheduling_model.dart';
import 'package:ata_app/features/trip/data/models/trip_stop_model.dart';
import 'package:ata_app/features/trip/data/models/trip_timeline_model.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stage.dart';

/// JSON mapping for the `Trip` response object.
class TripModel extends Trip {
  const TripModel({
    required super.id,
    required super.tripNumber,
    required super.status,
    required super.pickup,
    required super.dropoff,
    super.bookingType,
    super.scheduledAt,
    super.category,
    super.stops,
    super.paymentMethod,
    super.pricingMode,
    super.offeredPrice,
    super.estimatedFare,
    super.finalFare,
    super.estimatedDistanceMeters,
    super.estimatedDurationSeconds,
    super.finalDistanceMeters,
    super.finalDurationSeconds,
    super.preferFemaleDriver,
    super.driver,
    super.vehicle,
    super.passenger,
    super.pin,
    super.waitingSeconds,
    super.cancelledBy,
    super.cancellationReason,
    super.timeline,
    super.events,
    super.collectCashAmount,
    super.cancellation,
    super.rating,
    super.promotion,
    super.favorite,
    super.scheduling,
    super.airport,
    super.corporate,
  });

  factory TripModel.fromJson(Map<String, dynamic> json) {
    final Map<String, dynamic>? category = JsonReaders.object(
      json,
      'rideCategory',
    );
    final Map<String, dynamic>? driver = JsonReaders.object(json, 'driver');
    final Map<String, dynamic>? vehicle = JsonReaders.object(json, 'vehicle');
    final Map<String, dynamic>? passenger = JsonReaders.object(
      json,
      'passenger',
    );
    final Map<String, dynamic>? timeline = JsonReaders.object(json, 'timeline');
    final Map<String, dynamic>? cancellation = JsonReaders.object(
      json,
      'cancellation',
    );
    return TripModel(
      id: JsonReaders.string(json, 'id'),
      tripNumber: JsonReaders.string(json, 'tripNumber'),
      status: TripStage.parse(JsonReaders.optionalString(json, 'status')),
      bookingType: JsonReaders.optionalString(json, 'bookingType') ?? 'now',
      scheduledAt: JsonReaders.date(json, 'scheduledAt'),
      category: category == null ? null : TripCategoryModel.fromJson(category),
      pickup: TripStopModel.fromJson(
        JsonReaders.object(json, 'pickup') ?? const <String, dynamic>{},
      ),
      dropoff: TripStopModel.fromJson(
        JsonReaders.object(json, 'dropoff') ?? const <String, dynamic>{},
      ),
      stops: TripStopModel.listFromJson(json, 'stops'),
      paymentMethod:
          JsonReaders.optionalString(json, 'paymentMethod') ?? 'cash',
      pricingMode: JsonReaders.optionalString(json, 'pricingMode') ?? 'fixed',
      offeredPrice: JsonReaders.optionalNumber(json, 'offeredPrice'),
      estimatedFare: JsonReaders.number(json, 'estimatedFare'),
      finalFare: JsonReaders.optionalNumber(json, 'finalFare'),
      estimatedDistanceMeters: JsonReaders.integer(
        json,
        'estimatedDistanceMeters',
      ),
      estimatedDurationSeconds: JsonReaders.integer(
        json,
        'estimatedDurationSeconds',
      ),
      finalDistanceMeters: JsonReaders.optionalInteger(
        json,
        'finalDistanceMeters',
      ),
      finalDurationSeconds: JsonReaders.optionalInteger(
        json,
        'finalDurationSeconds',
      ),
      preferFemaleDriver: json['preferFemaleDriver'] as bool? ?? false,
      driver: driver == null ? null : TripDriverModel.fromJson(driver),
      vehicle: vehicle == null ? null : TripVehicleModel.fromJson(vehicle),
      passenger: passenger == null
          ? null
          : TripPassengerModel.fromJson(passenger),
      pin: JsonReaders.optionalString(json, 'pin'),
      waitingSeconds: JsonReaders.integer(json, 'waitingSeconds'),
      cancelledBy: JsonReaders.optionalString(json, 'cancelledBy'),
      cancellationReason: JsonReaders.optionalString(
        json,
        'cancellationReason',
      ),
      timeline: timeline == null
          ? const TripTimelineModel()
          : TripTimelineModel.fromJson(timeline),
      events: JsonReaders.objects(
        json,
        'events',
      ).map(TripEventModel.fromJson).toList(growable: false),
      collectCashAmount: JsonReaders.optionalNumber(json, 'collectCashAmount'),
      cancellation: cancellation == null
          ? null
          : TripCancellationModel.fromJson(cancellation),
      rating: TripRewardsModel.rating(json),
      promotion: TripRewardsModel.promotion(json),
      favorite: TripRewardsModel.favorite(json),
      scheduling: TripSchedulingModel.scheduling(json),
      airport: TripSchedulingModel.airport(json),
      corporate: CorporateCheckModel.trip(json),
    );
  }

  Map<String, dynamic> toJson() => <String, dynamic>{
    'id': id,
    'tripNumber': tripNumber,
    'status': status.apiValue,
    'bookingType': bookingType,
    'scheduledAt': scheduledAt?.toIso8601String(),
    'rideCategory': category == null
        ? null
        : <String, dynamic>{
            'id': category!.id,
            'code': category!.code,
            'name': category!.name,
          },
    'pickup': TripStopModel.fromEntity(pickup).toJson(),
    'dropoff': TripStopModel.fromEntity(dropoff).toJson(),
    'stops': stops
        .map((stop) => TripStopModel.fromEntity(stop).toJson())
        .toList(growable: false),
    'paymentMethod': paymentMethod,
    'pricingMode': pricingMode,
    'offeredPrice': offeredPrice,
    'estimatedFare': estimatedFare,
    'finalFare': finalFare,
    'estimatedDistanceMeters': estimatedDistanceMeters,
    'estimatedDurationSeconds': estimatedDurationSeconds,
    'finalDistanceMeters': finalDistanceMeters,
    'finalDurationSeconds': finalDurationSeconds,
    'preferFemaleDriver': preferFemaleDriver,
    'driver': driver == null
        ? null
        : <String, dynamic>{
            'id': driver!.id,
            'fullName': driver!.fullName,
            'ratingAvg': driver!.ratingAvg,
            'photoFileId': driver!.photoFileId,
            'phoneMasked': driver!.phoneMasked,
            'gender': driver!.gender,
            'isFavorite': driver!.isFavorite,
          },
    'vehicle': vehicle == null
        ? null
        : <String, dynamic>{
            'make': vehicle!.make,
            'model': vehicle!.model,
            'color': vehicle!.color,
            'plateNumber': vehicle!.plateNumber,
          },
    'passenger': passenger == null
        ? null
        : <String, dynamic>{
            'firstName': passenger!.firstName,
            'phoneMasked': passenger!.phoneMasked,
            'ratingAvg': passenger!.ratingAvg,
          },
    'pin': pin,
    'waitingSeconds': waitingSeconds,
    'cancelledBy': cancelledBy,
    'cancellationReason': cancellationReason,
    'timeline': <String, dynamic>{
      'requestedAt': timeline.requestedAt?.toIso8601String(),
      'assignedAt': timeline.assignedAt?.toIso8601String(),
      'arrivedAt': timeline.arrivedAt?.toIso8601String(),
      'startedAt': timeline.startedAt?.toIso8601String(),
      'completedAt': timeline.completedAt?.toIso8601String(),
      'cancelledAt': timeline.cancelledAt?.toIso8601String(),
    },
    'events': events
        .map(
          (event) => <String, dynamic>{
            'type': event.type,
            'actor': event.actor,
            'createdAt': event.createdAt?.toIso8601String(),
          },
        )
        .toList(growable: false),
    'collectCashAmount': collectCashAmount,
    'cancellation': cancellation == null
        ? null
        : TripCancellationModel.toJson(cancellation!),
    ...TripRewardsModel.toJson(rating, promotion),
    'favorite': TripRewardsModel.favoriteJson(favorite),
    'scheduling': TripSchedulingModel.schedulingJson(scheduling),
    'airport': TripSchedulingModel.airportJson(airport),
    'corporate': CorporateCheckModel.tripJson(corporate),
  };
}
