import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/trip/data/datasources/trip_realtime_data_source.dart';
import 'package:ata_app/features/trip/data/datasources/trip_remote_data_source.dart';
import 'package:ata_app/features/trip/data/datasources/trip_watcher.dart';
import 'package:ata_app/features/trip/data/models/driver_location_model.dart';
import 'package:ata_app/features/trip/data/models/trip_request_mapper.dart';
import 'package:ata_app/features/trip/domain/entities/driver_location.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:ata_app/features/trip/domain/entities/offer.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_estimate.dart';
import 'package:ata_app/features/trip/domain/entities/trip_request.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/domain/repositories/trip_repository.dart';
import 'package:fpdart/fpdart.dart';

/// [TripRepository] backed by the REST API and the SignalR hub.
class TripRepositoryImpl implements TripRepository {
  TripRepositoryImpl({
    required this._remote,
    required this._realtime,
    this.pollInterval = const Duration(seconds: 5),
  });

  final TripRemoteDataSource _remote;
  final TripRealtimeDataSource _realtime;
  final Duration pollInterval;

  String? _lastOfferId;

  @override
  Future<Either<Failure, TripEstimate>> estimate(TripRequest request) =>
      guard(() => _remote.estimate(TripRequestMapper.estimateBody(request)));

  @override
  Future<Either<Failure, Trip>> requestTrip(TripRequest request) =>
      guard(() => _remote.requestTrip(TripRequestMapper.requestBody(request)));

  @override
  Future<Either<Failure, Trip?>> getActiveTrip(TripActor actor) =>
      guard(() => _activeTrip(actor));

  @override
  Future<Either<Failure, Trip>> getTrip(String tripId) =>
      guard(() => _remote.passengerTrip(tripId));

  @override
  Future<Either<Failure, Trip>> cancelTrip({
    required String tripId,
    required TripActor actor,
    required String reasonCode,
    String? note,
    double? expectedFee,
    int? expectedPenaltyPoints,
  }) => guard(() {
    final Map<String, dynamic> body = <String, dynamic>{
      'reasonCode': reasonCode,
      'note': ?note,
      'expectedFee': ?expectedFee,
      'expectedPenaltyPoints': ?expectedPenaltyPoints,
    };
    return actor == TripActor.passenger
        ? _remote.passengerCancel(tripId, body)
        : _remote.driverTripAction(
            tripId,
            TripRemoteDataSource.cancelAction,
            body: body,
          );
  });

  @override
  Stream<Trip?> watchActiveTrip(TripActor actor) => TripWatcher<Trip>(
    realtime: _realtime.tripUpdated,
    poll: () => _activeTrip(actor),
    interval: pollInterval,
    isRealtimeConnected: () => _realtime.isConnected,
    onListen: _realtime.acquire,
    onCancel: _realtime.release,
  ).watch();

  @override
  Stream<DriverLocationUpdate> watchDriverLocation() =>
      _realtime.driverLocation;

  @override
  Future<Either<Failure, Unit>> sendLocation(DriverPosition position) => guard(
    () async {
      await _remote.sendLocation(DriverLocationModel.positionToJson(position));
      return unit;
    },
  );

  @override
  Future<Either<Failure, Offer?>> getActiveOffer() =>
      guard(_remote.activeOffer);

  @override
  Stream<Offer?> watchOffers() => TripWatcher<Offer>(
    realtime: mergeStreams<Offer?>(<Stream<Offer?>>[
      _realtime.offerReceived.map((Offer offer) {
        _lastOfferId = offer.id;
        return offer;
      }),
      _realtime.offerExpired
          .where((String id) => id == _lastOfferId)
          .map((_) => null),
    ]),
    poll: () async {
      final Offer? offer = await _remote.activeOffer();
      _lastOfferId = offer?.id ?? _lastOfferId;
      return offer;
    },
    interval: pollInterval,
    isRealtimeConnected: () => _realtime.isConnected,
    onListen: _realtime.acquire,
    onCancel: _realtime.release,
  ).watch();

  @override
  Future<Either<Failure, Trip>> acceptOffer(String offerId) =>
      guard(() => _remote.acceptOffer(offerId));

  @override
  Future<Either<Failure, Unit>> rejectOffer(String offerId, {String? reason}) =>
      guard(() async {
        await _remote.rejectOffer(offerId, <String, dynamic>{
          'reasonCode': ?reason,
        });
        return unit;
      });

  @override
  Future<Either<Failure, Trip>> advance({
    required String tripId,
    required TripStep step,
    GeoPoint? at,
  }) => guard(
    () => _remote.driverTripAction(
      tripId,
      step.apiPath,
      body: step == TripStep.complete
          ? <String, dynamic>{'finalLat': ?at?.lat, 'finalLng': ?at?.lng}
          : null,
    ),
  );

  @override
  Future<Either<Failure, Trip>> verifyPin({
    required String tripId,
    required String pin,
  }) => guard(
    () => _remote.driverTripAction(
      tripId,
      TripRemoteDataSource.verifyPinAction,
      body: <String, dynamic>{'pin': pin},
    ),
  );

  Future<Trip?> _activeTrip(TripActor actor) => actor == TripActor.passenger
      ? _remote.passengerActiveTrip()
      : _remote.driverActiveTrip();
}
