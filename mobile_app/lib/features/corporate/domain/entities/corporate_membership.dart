import 'package:equatable/equatable.dart';

/// Role of an employee inside a company account (`docs/12` §F19.1).
enum CorporateRole {
  employee('employee'),
  admin('corporate_admin');

  const CorporateRole(this.apiValue);

  final String apiValue;

  static CorporateRole parse(String? value) => CorporateRole.values.firstWhere(
    (CorporateRole r) => r.apiValue == value,
    orElse: () => CorporateRole.employee,
  );
}

/// `corporate_users.status`.
enum CorporateMemberStatus {
  invited('invited'),
  active('active'),
  disabled('disabled');

  const CorporateMemberStatus(this.apiValue);

  final String apiValue;

  /// Unknown values are treated as `disabled`: never offer the corporate
  /// payment on an unrecognised status.
  static CorporateMemberStatus parse(String? value) =>
      CorporateMemberStatus.values.firstWhere(
        (CorporateMemberStatus s) => s.apiValue == value,
        orElse: () => CorporateMemberStatus.disabled,
      );
}

/// `membership` of `GET /passenger/corporate`.
class CorporateMembership extends Equatable {
  const CorporateMembership({
    required this.corporateUserId,
    required this.accountId,
    required this.companyName,
    this.role = CorporateRole.employee,
    this.employeeNumber,
    this.department,
    this.status = CorporateMemberStatus.active,
  });

  final String corporateUserId;
  final String accountId;
  final String companyName;
  final CorporateRole role;
  final String? employeeNumber;
  final String? department;
  final CorporateMemberStatus status;

  bool get isActive => status == CorporateMemberStatus.active;
  bool get isAdmin => role == CorporateRole.admin;

  @override
  List<Object?> get props => <Object?>[
    corporateUserId,
    accountId,
    companyName,
    role,
    employeeNumber,
    department,
    status,
  ];
}
