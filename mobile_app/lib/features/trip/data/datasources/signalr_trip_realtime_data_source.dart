import 'dart:async';

import 'package:ata_app/core/storage/token_storage.dart';
import 'package:ata_app/features/trip/data/datasources/trip_realtime_data_source.dart';
import 'package:ata_app/features/trip/data/models/driver_location_model.dart';
import 'package:ata_app/features/trip/data/models/offer_model.dart';
import 'package:ata_app/features/trip/data/models/trip_model.dart';
import 'package:signalr_netcore/signalr_client.dart';

/// Builds a hub connection for [url]; swapped in tests.
typedef HubConnectionFactory =
    HubConnection Function(String url, HttpConnectionOptions options);

HubConnection _defaultHubFactory(String url, HttpConnectionOptions options) =>
    HubConnectionBuilder()
        .withUrl(url, options: options)
        .withAutomaticReconnect(retryDelays: _retryDelaysMs)
        .build();

const List<int> _retryDelaysMs = <int>[0, 2000, 5000, 10000, 20000];

/// [TripRealtimeDataSource] over `signalr_netcore`, connecting to
/// `<hubUrl>?access_token=<jwt>` with automatic reconnect. When the built-in
/// retry policy gives up, a new connection is attempted every
/// [reconnectDelay] while at least one watcher is still attached.
class SignalRTripRealtimeDataSource implements TripRealtimeDataSource {
  SignalRTripRealtimeDataSource({
    required this._hubUrl,
    required this._tokens,
    this._factory = _defaultHubFactory,
    this.reconnectDelay = const Duration(seconds: 5),
  });

  static const String tokenParam = 'access_token';

  final String _hubUrl;
  final TokenStorage _tokens;
  final HubConnectionFactory _factory;
  final Duration reconnectDelay;

  final StreamController<TripModel> _trips =
      StreamController<TripModel>.broadcast();
  final StreamController<DriverLocationModel> _locations =
      StreamController<DriverLocationModel>.broadcast();
  final StreamController<OfferModel> _offers =
      StreamController<OfferModel>.broadcast();
  final StreamController<String> _expired =
      StreamController<String>.broadcast();
  final StreamController<Map<String, dynamic>> _safetyChecks =
      StreamController<Map<String, dynamic>>.broadcast();
  final StreamController<Map<String, dynamic>> _messages =
      StreamController<Map<String, dynamic>>.broadcast();
  final StreamController<Map<String, dynamic>> _messagesRead =
      StreamController<Map<String, dynamic>>.broadcast();

  final StreamController<Map<String, dynamic>> _airportQueue =
      StreamController<Map<String, dynamic>>.broadcast();

  final StreamController<Map<String, dynamic>> _support =
      StreamController<Map<String, dynamic>>.broadcast();

  HubConnection? _hub;
  int _refCount = 0;
  bool _connecting = false;
  Timer? _retry;

  @override
  Stream<TripModel> get tripUpdated => _trips.stream;
  @override
  Stream<DriverLocationModel> get driverLocation => _locations.stream;
  @override
  Stream<OfferModel> get offerReceived => _offers.stream;
  @override
  Stream<String> get offerExpired => _expired.stream;

  @override
  Stream<Map<String, dynamic>> get safetyCheck => _safetyChecks.stream;
  @override
  Stream<Map<String, dynamic>> get tripMessage => _messages.stream;
  @override
  Stream<Map<String, dynamic>> get tripMessagesRead => _messagesRead.stream;

  @override
  Stream<Map<String, dynamic>> get airportQueueUpdated => _airportQueue.stream;

  @override
  Stream<Map<String, dynamic>> get supportTicketUpdated => _support.stream;

  @override
  bool get isConnected => _hub?.state == HubConnectionState.Connected;

  @override
  Future<void> acquire() async {
    _refCount++;
    if (_refCount == 1) await _connect();
  }

  @override
  Future<void> release() async {
    if (_refCount == 0) return;
    _refCount--;
    if (_refCount == 0) await _disconnect();
  }

  Future<void> _connect() async {
    if (_connecting || _refCount == 0 || isConnected) return;
    _connecting = true;
    _retry?.cancel();
    try {
      final String? token = (await _tokens.read())?.accessToken;
      final Uri uri = Uri.parse(_hubUrl).replace(
        queryParameters: <String, String>{
          ...Uri.parse(_hubUrl).queryParameters,
          tokenParam: ?token,
        },
      );
      final HubConnection hub = _factory(
        uri.toString(),
        HttpConnectionOptions(
          accessTokenFactory: () async =>
              (await _tokens.read())?.accessToken ?? '',
          requestTimeout: 15000,
        ),
      );
      _register(hub);
      _hub = hub;
      await hub.start();
    } on Object {
      _scheduleReconnect();
    } finally {
      _connecting = false;
    }
  }

  void _register(HubConnection hub) {
    hub
      ..on(TripHubEvents.tripUpdated, (List<Object?>? args) {
        final Map<String, dynamic>? json = _firstObject(args);
        if (json != null) _trips.add(TripModel.fromJson(json));
      })
      ..on(TripHubEvents.driverLocation, (List<Object?>? args) {
        final Map<String, dynamic>? json = _firstObject(args);
        if (json != null) _locations.add(DriverLocationModel.fromJson(json));
      })
      ..on(TripHubEvents.offerReceived, (List<Object?>? args) {
        final Map<String, dynamic>? json = _firstObject(args);
        if (json != null) _offers.add(OfferModel.fromJson(json));
      })
      ..on(TripHubEvents.offerExpired, (List<Object?>? args) {
        final Object? first = args?.firstOrNull;
        final Object? id = first is Map<dynamic, dynamic>
            ? first['offerId']
            : first;
        if (id != null) _expired.add(id.toString());
      })
      ..on(TripHubEvents.safetyCheck, (List<Object?>? args) {
        final Map<String, dynamic>? json = _firstObject(args);
        if (json != null) _safetyChecks.add(json);
      })
      ..on(TripHubEvents.tripMessage, (List<Object?>? args) {
        final Map<String, dynamic>? json = _firstObject(args);
        if (json != null) _messages.add(json);
      })
      ..on(TripHubEvents.tripMessagesRead, (List<Object?>? args) {
        final Map<String, dynamic>? json = _firstObject(args);
        if (json != null) _messagesRead.add(json);
      })
      ..on(TripHubEvents.airportQueueUpdated, (List<Object?>? args) {
        final Map<String, dynamic>? json = _firstObject(args);
        if (json != null) _airportQueue.add(json);
      })
      ..on(TripHubEvents.supportTicketUpdated, (List<Object?>? args) {
        final Map<String, dynamic>? json = _firstObject(args);
        if (json != null) _support.add(json);
      })
      ..onclose(({Exception? error}) => _scheduleReconnect());
  }

  Map<String, dynamic>? _firstObject(List<Object?>? args) {
    final Object? first = args?.firstOrNull;
    if (first is Map<String, dynamic>) return first;
    if (first is Map<dynamic, dynamic>) {
      return first.map(
        (dynamic key, dynamic value) =>
            MapEntry<String, dynamic>('$key', value),
      );
    }
    return null;
  }

  void _scheduleReconnect() {
    if (_refCount == 0) return;
    _retry?.cancel();
    _retry = Timer(reconnectDelay, () {
      if (_refCount > 0 && !isConnected) _connect();
    });
  }

  Future<void> _disconnect() async {
    _retry?.cancel();
    final HubConnection? hub = _hub;
    _hub = null;
    if (hub != null) {
      try {
        await hub.stop();
      } on Object {
        // Already closed.
      }
    }
  }

  Future<void> dispose() async {
    _refCount = 0;
    await _disconnect();
    await Future.wait(<Future<void>>[
      _trips.close(),
      _locations.close(),
      _offers.close(),
      _expired.close(),
      _safetyChecks.close(),
      _messages.close(),
      _messagesRead.close(),
      _airportQueue.close(),
    ]);
  }
}
