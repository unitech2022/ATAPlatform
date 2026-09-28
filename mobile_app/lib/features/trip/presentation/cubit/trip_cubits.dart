import 'dart:async';

import 'package:ata_app/core/di/injector.dart';
import 'package:ata_app/features/auth/presentation/cubit/session_cubit.dart';
import 'package:ata_app/features/auth/presentation/cubit/session_state.dart';
import 'package:ata_app/features/trip/data/datasources/trip_watcher.dart';
import 'package:ata_app/features/trip/presentation/cubit/active_trip_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/driver_offer_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/driver_trip_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/driver_trip_state.dart';
import 'package:ata_app/features/trip/presentation/cubit/location_stream_cubit.dart';
import 'package:ata_app/features/trip/presentation/cubit/trip_presence.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// The app-wide trip cubits and their binding to the session: the passenger
/// feed runs for onboarded riders, the driver feed for approved drivers, and
/// everything stops on sign-out.
class TripCubits {
  TripCubits({
    required this.activeTrip,
    required this.driverTrip,
    required this.driverOffer,
    required this.location,
  });

  factory TripCubits.fromInjector() => TripCubits(
    activeTrip: ActiveTripCubit(
      watchActiveTrip: getIt(),
      watchDriverLocation: getIt(),
      getTrip: getIt(),
      cancelTrip: getIt(),
    ),
    driverTrip: DriverTripCubit(
      watchActiveTrip: getIt(),
      advanceTrip: getIt(),
      verifyPin: getIt(),
      cancelTrip: getIt(),
    ),
    driverOffer: DriverOfferCubit(
      watchOffers: getIt(),
      acceptOffer: getIt(),
      rejectOffer: getIt(),
    ),
    location: LocationStreamCubit(
      requestAccess: getIt(),
      watchPosition: getIt(),
      sendLocation: getIt(),
    ),
  );

  final ActiveTripCubit activeTrip;
  final DriverTripCubit driverTrip;
  final DriverOfferCubit driverOffer;
  final LocationStreamCubit location;

  StreamSubscription<SessionState>? _session;
  StreamSubscription<DriverTripState>? _driverTripWatch;

  TripPresence get presence => TripPresence.of(
    activeTrip: activeTrip.state,
    driverTrip: driverTrip.state,
    driverOffer: driverOffer.state,
  );

  /// Emits whenever a state relevant to routing changes.
  Stream<Object?> get changes => mergeStreams<Object?>(<Stream<Object?>>[
    activeTrip.stream,
    driverTrip.stream,
    driverOffer.stream,
  ]);

  /// Makes the four cubits available below [child].
  Widget provide({required Widget child}) => MultiBlocProvider(
    providers: <BlocProvider<dynamic>>[
      BlocProvider<ActiveTripCubit>.value(value: activeTrip),
      BlocProvider<DriverTripCubit>.value(value: driverTrip),
      BlocProvider<DriverOfferCubit>.value(value: driverOffer),
      BlocProvider<LocationStreamCubit>.value(value: location),
    ],
    child: child,
  );

  /// Follows the session: applies the current state and every change. Also
  /// keeps the location stream running while a driver trip is in progress.
  void bindSession(SessionCubit session) {
    _session?.cancel();
    syncWithSession(session.state);
    _session = session.stream.listen(syncWithSession);
    _driverTripWatch ??= driverTrip.stream.listen((DriverTripState state) {
      if (state.hasTrip && !state.trip!.status.isTerminal) location.start();
    });
  }

  Future<void> syncWithSession(SessionState session) async {
    final bool rider =
        session.isAuthenticated &&
        !session.needsRiderOnboarding &&
        !session.session!.isDriver;
    final bool driver =
        session.isAuthenticated && session.session!.isDriverApproved;
    if (rider) {
      activeTrip.start();
    } else {
      await activeTrip.stop();
    }
    if (driver) {
      driverTrip.start();
    } else {
      await driverTrip.stop();
      await driverOffer.stop();
      await location.stop();
    }
  }

  Future<void> close() async {
    await _session?.cancel();
    await _driverTripWatch?.cancel();
    await Future.wait(<Future<void>>[
      activeTrip.close(),
      driverTrip.close(),
      driverOffer.close(),
      location.close(),
    ]);
  }
}
