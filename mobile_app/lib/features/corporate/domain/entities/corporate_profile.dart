import 'package:ata_app/features/corporate/domain/entities/corporate_budget.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_membership.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_policy_summary.dart';
import 'package:ata_app/features/corporate/domain/entities/cost_center.dart';
import 'package:equatable/equatable.dart';

/// `GET /passenger/corporate`: membership, policy, budget and the cost
/// centers of the employee (`null` body = not a member).
class CorporateProfile extends Equatable {
  const CorporateProfile({
    required this.membership,
    this.policy = const CorporatePolicySummary(),
    this.budget,
    this.costCenters = const <CostCenter>[],
  });

  final CorporateMembership membership;
  final CorporatePolicySummary policy;

  /// `null` = no monthly budget.
  final CorporateBudget? budget;
  final List<CostCenter> costCenters;

  /// The corporate payment is offered only to active members
  /// (`membership.status = active`, `docs/12` §F19.5).
  bool get isActive => membership.isActive;

  double? get perTripLimit => policy.maxFarePerTrip;

  CostCenter? costCenterById(String? id) {
    for (final CostCenter center in costCenters) {
      if (center.id == id) return center;
    }
    return null;
  }

  @override
  List<Object?> get props => <Object?>[membership, policy, budget, costCenters];
}
