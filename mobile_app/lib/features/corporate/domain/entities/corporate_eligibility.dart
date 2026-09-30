import 'package:ata_app/features/corporate/domain/entities/policy_violation.dart';
import 'package:equatable/equatable.dart';

/// Whether the corporate payment can be used for the current draft: the
/// device pre-check of the policy merged with the API's own evaluation.
class CorporateEligibility extends Equatable {
  const CorporateEligibility({
    this.violations = const <PolicyViolation>[],
    this.remainingBudget,
  });

  final List<PolicyViolation> violations;

  /// From the quote when available, otherwise the profile budget.
  final double? remainingBudget;

  bool get eligible => violations.isEmpty;

  @override
  List<Object?> get props => <Object?>[violations, remainingBudget];
}
