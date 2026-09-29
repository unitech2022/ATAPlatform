import 'dart:async';

import 'package:ata_app/features/auth/presentation/cubit/session_cubit.dart';
import 'package:ata_app/features/auth/presentation/cubit/session_state.dart';
import 'package:ata_app/features/rating/presentation/cubit/pending_rating_cubit.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/entities/trip_stage.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/presentation/cubit/trip_cubits.dart';

/// Binds the app-wide [PendingRatingCubit] (F15) to the session (riders and
/// approved drivers load their pending ratings on sign-in / app start) and
/// reloads it whenever the rider's or the driver's trip completes.
class RatingPromptBinder {
  RatingPromptBinder(this._pending);

  final PendingRatingCubit _pending;
  final List<StreamSubscription<Object?>> _subscriptions =
      <StreamSubscription<Object?>>[];
  final Set<String> _completed = <String>{};

  void bind({required SessionCubit session, required TripCubits trips}) {
    _sync(session.state);
    _subscriptions
      ..add(session.stream.listen(_sync))
      ..add(trips.activeTrip.stream.listen((s) => _onTrip(s.trip)))
      ..add(trips.driverTrip.stream.listen((s) => _onTrip(s.trip)));
  }

  void _sync(SessionState session) {
    if (!session.isAuthenticated || session.needsRiderOnboarding) {
      _pending.stop();
      return;
    }
    final bool driver = session.session!.isDriver;
    if (driver && !session.session!.isDriverApproved) {
      _pending.stop();
      return;
    }
    _pending.start(driver ? TripActor.driver : TripActor.passenger);
  }

  void _onTrip(Trip? trip) {
    if (trip == null || trip.status != TripStage.completed) return;
    if (_completed.add(trip.id)) _pending.refresh();
  }

  Future<void> close() async {
    for (final StreamSubscription<Object?> s in _subscriptions) {
      await s.cancel();
    }
  }
}
