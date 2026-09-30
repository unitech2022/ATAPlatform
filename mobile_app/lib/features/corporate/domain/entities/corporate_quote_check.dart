import 'package:ata_app/features/corporate/domain/entities/policy_violation.dart';
import 'package:equatable/equatable.dart';

/// `corporate: { allowed, violations, remainingBudget }` of a
/// `POST /pricing/quote` priced with `paymentMethod: corporate`.
class CorporateQuoteCheck extends Equatable {
  const CorporateQuoteCheck({
    required this.allowed,
    this.violations = const <PolicyViolation>[],
    this.remainingBudget,
  });

  final bool allowed;
  final List<PolicyViolation> violations;
  final double? remainingBudget;

  @override
  List<Object?> get props => <Object?>[allowed, violations, remainingBudget];
}
