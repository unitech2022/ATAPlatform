import 'package:ata_app/features/corporate/domain/entities/corporate_membership.dart';
import 'package:equatable/equatable.dart';

/// A pending company invitation (`GET /passenger/corporate/invitations`).
class CorporateInvitation extends Equatable {
  const CorporateInvitation({
    required this.id,
    required this.companyName,
    this.role = CorporateRole.employee,
    this.expiresAt,
  });

  final String id;
  final String companyName;
  final CorporateRole role;
  final DateTime? expiresAt;

  bool isExpiredAt(DateTime now) =>
      expiresAt != null && !now.isBefore(expiresAt!);

  @override
  List<Object?> get props => <Object?>[id, companyName, role, expiresAt];
}
