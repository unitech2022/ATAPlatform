import 'package:equatable/equatable.dart';

/// Which notification categories the user wants.
class NotificationPreferences extends Equatable {
  const NotificationPreferences({
    this.trips = true,
    this.wallet = true,
    this.safety = true,
    this.offers = true,
  });

  final bool trips;
  final bool wallet;
  final bool safety;
  final bool offers;

  NotificationPreferences copyWith({
    bool? trips,
    bool? wallet,
    bool? safety,
    bool? offers,
  }) => NotificationPreferences(
    trips: trips ?? this.trips,
    wallet: wallet ?? this.wallet,
    safety: safety ?? this.safety,
    offers: offers ?? this.offers,
  );

  @override
  List<Object?> get props => <Object?>[trips, wallet, safety, offers];
}
