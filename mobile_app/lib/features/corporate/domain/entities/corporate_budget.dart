import 'package:equatable/equatable.dart';

/// `budget` of `GET /passenger/corporate` (the employee's monthly budget).
class CorporateBudget extends Equatable {
  const CorporateBudget({
    required this.monthly,
    required this.spent,
    required this.remaining,
  });

  final double monthly;
  final double spent;
  final double remaining;

  /// Share of the budget already used, `0..1`.
  double get usedFraction =>
      monthly <= 0 ? 0 : (spent / monthly).clamp(0, 1).toDouble();

  bool get isExhausted => remaining <= 0;

  @override
  List<Object?> get props => <Object?>[monthly, spent, remaining];
}
