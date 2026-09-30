import 'package:equatable/equatable.dart';

/// `corporate` of a `Trip` (and of the receipt): who paid and why.
class TripCorporate extends Equatable {
  const TripCorporate({
    required this.companyName,
    this.purpose,
    this.costCenter,
    this.isGuest = false,
    this.guestName,
  });

  final String companyName;
  final String? purpose;

  /// Cost center code, for example `IT-01`.
  final String? costCenter;
  final bool isGuest;
  final String? guestName;

  bool get hasPurpose => purpose != null && purpose!.trim().isNotEmpty;
  bool get hasCostCenter => costCenter != null && costCenter!.isNotEmpty;

  @override
  List<Object?> get props => <Object?>[
    companyName,
    purpose,
    costCenter,
    isGuest,
    guestName,
  ];
}
