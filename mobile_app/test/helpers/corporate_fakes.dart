import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_budget.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_invitation.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_membership.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_policy_summary.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_profile.dart';
import 'package:ata_app/features/corporate/domain/entities/cost_center.dart';
import 'package:ata_app/features/corporate/domain/repositories/corporate_repository.dart';
import 'package:fpdart/fpdart.dart';

/// An active employee of "شركة المثال" with a monthly budget.
const CorporateProfile testCorporateProfile = CorporateProfile(
  membership: CorporateMembership(
    corporateUserId: 'cu1',
    accountId: 'ca1',
    companyName: 'شركة المثال',
    employeeNumber: 'E-17',
    department: 'المبيعات',
  ),
  policy: CorporatePolicySummary(
    name: 'السياسة الافتراضية',
    maxFarePerTrip: 150,
    requirePurpose: true,
  ),
  budget: CorporateBudget(monthly: 1500, spent: 420.5, remaining: 1079.5),
  costCenters: <CostCenter>[
    CostCenter(id: 'cc1', code: 'IT-01', name: 'تقنية المعلومات'),
    CostCenter(id: 'cc2', code: 'SL-02', name: 'المبيعات'),
  ],
);

const CorporateInvitation testInvitation = CorporateInvitation(
  id: 'inv1',
  companyName: 'شركة المثال',
);

/// In-memory corporate repository; a non-member by default.
class FakeCorporateRepository implements CorporateRepository {
  CorporateProfile? profile;

  /// The profile the rider has once an invitation is accepted.
  CorporateProfile? profileOnAccept;
  List<CorporateInvitation> invitations = <CorporateInvitation>[];
  Failure? failure;
  Failure? actionFailure;
  int profileCalls = 0;
  final List<String> accepted = <String>[];
  final List<String> declined = <String>[];

  @override
  Future<Either<Failure, CorporateProfile?>> getProfile() async {
    profileCalls++;
    final Failure? error = failure;
    return error == null
        ? Right<Failure, CorporateProfile?>(profile)
        : Left<Failure, CorporateProfile?>(error);
  }

  @override
  Future<Either<Failure, List<CorporateInvitation>>> getInvitations() async {
    final Failure? error = failure;
    return error == null
        ? Right<Failure, List<CorporateInvitation>>(invitations)
        : Left<Failure, List<CorporateInvitation>>(error);
  }

  @override
  Future<Either<Failure, Unit>> acceptInvitation(String invitationId) async {
    final Failure? error = actionFailure;
    if (error != null) return Left<Failure, Unit>(error);
    accepted.add(invitationId);
    if (profileOnAccept != null) profile = profileOnAccept;
    invitations = <CorporateInvitation>[];
    return const Right<Failure, Unit>(unit);
  }

  @override
  Future<Either<Failure, Unit>> declineInvitation(String invitationId) async {
    final Failure? error = actionFailure;
    if (error != null) return Left<Failure, Unit>(error);
    declined.add(invitationId);
    invitations = <CorporateInvitation>[];
    return const Right<Failure, Unit>(unit);
  }
}
