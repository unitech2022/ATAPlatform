import 'package:equatable/equatable.dart';

/// What the corporate form of the request sheet contributes to the trip
/// request: the purpose, the cost center and whether the corporate payment
/// may be requested right now (eligible and required fields filled).
class CorporateBooking extends Equatable {
  const CorporateBooking({
    this.tripPurpose,
    this.costCenterId,
    this.ready = false,
  });

  final String? tripPurpose;
  final String? costCenterId;
  final bool ready;

  /// Re-quote key: the purpose text itself is not sent on every keystroke,
  /// only whether there is one.
  String get quoteKey =>
      '${tripPurpose != null && tripPurpose!.trim().isNotEmpty}|'
      '${costCenterId ?? ''}';

  @override
  List<Object?> get props => <Object?>[tripPurpose, costCenterId, ready];
}
