import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/trip/domain/entities/offer.dart';
import 'package:ata_app/features/trip/domain/entities/trip.dart';
import 'package:equatable/equatable.dart';

/// Lifecycle of the offer sheet.
enum DriverOfferStatus {
  idle,
  listening,
  pending,
  accepting,
  rejecting,
  accepted,
  expired,
}

/// State of [DriverOfferCubit].
class DriverOfferState extends Equatable {
  const DriverOfferState({
    this.status = DriverOfferStatus.idle,
    this.offer,
    this.secondsLeft = 0,
    this.trip,
    this.failure,
  });

  final DriverOfferStatus status;
  final Offer? offer;
  final int secondsLeft;

  /// The trip created by a successful accept.
  final Trip? trip;
  final Failure? failure;

  bool get hasOffer => offer != null;
  bool get isListening => status != DriverOfferStatus.idle;
  bool get isBusy =>
      status == DriverOfferStatus.accepting ||
      status == DriverOfferStatus.rejecting;

  DriverOfferState copyWith({
    DriverOfferStatus? status,
    Offer? offer,
    int? secondsLeft,
    Trip? trip,
    Failure? failure,
    bool clearOffer = false,
    bool clearTrip = false,
    bool clearFailure = false,
  }) => DriverOfferState(
    status: status ?? this.status,
    offer: clearOffer ? null : offer ?? this.offer,
    secondsLeft: secondsLeft ?? this.secondsLeft,
    trip: clearTrip ? null : trip ?? this.trip,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[
    status,
    offer,
    secondsLeft,
    trip,
    failure,
  ];
}
