import 'dart:async';

import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/utils/countdown.dart';
import 'package:ata_app/features/trip/domain/entities/offer.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:ata_app/features/trip/domain/usecases/accept_offer.dart';
import 'package:ata_app/features/trip/domain/usecases/reject_offer.dart';
import 'package:ata_app/features/trip/domain/usecases/watch_offers.dart';
import 'package:ata_app/features/trip/presentation/cubit/driver_offer_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Listens to dispatch offers while the driver is online and runs the
/// 20-second countdown of the offer sheet.
class DriverOfferCubit extends Cubit<DriverOfferState> {
  DriverOfferCubit({
    required this._watchOffers,
    required this._acceptOffer,
    required this._rejectOffer,
    this._countdown = secondsCountdown,
    DateTime Function()? now,
  }) : _now = now ?? DateTime.now,
       super(const DriverOfferState());

  /// `Matching:OfferTimeoutSeconds`.
  static const int offerSeconds = 20;

  final WatchOffers _watchOffers;
  final AcceptOffer _acceptOffer;
  final RejectOffer _rejectOffer;
  final Countdown _countdown;
  final DateTime Function() _now;

  StreamSubscription<Offer?>? _offers;
  StreamSubscription<int>? _timer;

  /// Starts listening (call when the driver goes online). Idempotent.
  void start() {
    if (_offers != null) return;
    emit(state.copyWith(status: DriverOfferStatus.listening));
    _offers = _watchOffers().listen(_onOffer);
  }

  Future<void> stop() async {
    await _offers?.cancel();
    await _timer?.cancel();
    _offers = null;
    _timer = null;
    if (!isClosed) emit(const DriverOfferState());
  }

  Future<void> accept() async {
    final Offer? offer = state.offer;
    if (offer == null || state.status != DriverOfferStatus.pending) return;
    emit(
      state.copyWith(status: DriverOfferStatus.accepting, clearFailure: true),
    );
    final result = await _acceptOffer(offer.id);
    result.fold(
      (failure) => emit(
        failure.code == ErrorCodes.offerExpired
            ? state.copyWith(
                status: DriverOfferStatus.expired,
                clearOffer: true,
                secondsLeft: 0,
                failure: failure,
              )
            : state.copyWith(
                status: DriverOfferStatus.pending,
                failure: failure,
              ),
      ),
      (Trip trip) {
        _timer?.cancel();
        emit(
          state.copyWith(
            status: DriverOfferStatus.accepted,
            trip: trip,
            clearOffer: true,
            secondsLeft: 0,
          ),
        );
      },
    );
  }

  Future<void> reject({String? reason}) async {
    final Offer? offer = state.offer;
    if (offer == null || state.status != DriverOfferStatus.pending) return;
    _timer?.cancel();
    emit(state.copyWith(status: DriverOfferStatus.rejecting));
    await _rejectOffer(RejectOfferParams(offerId: offer.id, reason: reason));
    if (!isClosed) {
      emit(
        state.copyWith(
          status: DriverOfferStatus.listening,
          clearOffer: true,
          secondsLeft: 0,
        ),
      );
    }
  }

  /// The page consumed the accepted trip / expiry notice.
  void acknowledge() {
    if (state.status == DriverOfferStatus.accepted ||
        state.status == DriverOfferStatus.expired) {
      emit(
        state.copyWith(
          status: DriverOfferStatus.listening,
          clearTrip: true,
          clearFailure: true,
        ),
      );
    }
  }

  void _onOffer(Offer? offer) {
    if (isClosed) return;
    if (offer == null) {
      if (state.hasOffer && state.status == DriverOfferStatus.pending) {
        _expire();
      }
      return;
    }
    if (state.offer?.id == offer.id || state.isBusy) return;
    final int seconds = _secondsFor(offer);
    emit(
      state.copyWith(
        status: DriverOfferStatus.pending,
        offer: offer,
        secondsLeft: seconds,
        clearTrip: true,
        clearFailure: true,
      ),
    );
    _startCountdown(seconds);
  }

  int _secondsFor(Offer offer) {
    final int remaining = offer.expiresAt.difference(_now()).inSeconds;
    return remaining <= 0 || remaining > offerSeconds
        ? offerSeconds
        : remaining;
  }

  void _startCountdown(int seconds) {
    _timer?.cancel();
    _timer = _countdown(seconds).listen((int left) {
      if (isClosed || state.status != DriverOfferStatus.pending) return;
      emit(state.copyWith(secondsLeft: left));
      if (left <= 0) _expire();
    });
  }

  void _expire() {
    _timer?.cancel();
    emit(
      state.copyWith(
        status: DriverOfferStatus.expired,
        clearOffer: true,
        secondsLeft: 0,
      ),
    );
  }

  @override
  Future<void> close() async {
    await _offers?.cancel();
    await _timer?.cancel();
    return super.close();
  }
}
