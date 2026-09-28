import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:equatable/equatable.dart';

/// Body of `POST /safety/sos`.
class SosRequest extends Equatable {
  const SosRequest({
    required this.point,
    this.tripId,
    this.role,
    this.accuracy,
    this.note,
    this.notifyTrustedContacts = true,
  });

  final GeoPoint point;
  final String? tripId;

  /// `passenger` / `driver` when not a party of [tripId].
  final String? role;
  final double? accuracy;
  final String? note;
  final bool notifyTrustedContacts;

  @override
  List<Object?> get props => <Object?>[
    point,
    tripId,
    role,
    accuracy,
    note,
    notifyTrustedContacts,
  ];
}

/// `201` / `200` of `POST /safety/sos`.
class SosResult extends Equatable {
  const SosResult({
    required this.caseId,
    required this.caseNumber,
    this.status = 'open',
    this.emergencyNumber = defaultEmergencyNumber,
    this.contactsNotified = 0,
  });

  /// `Safety:EmergencyNumber`.
  static const String defaultEmergencyNumber = '911';

  final String caseId;
  final String caseNumber;
  final String status;
  final String emergencyNumber;
  final int contactsNotified;

  SosResult withStatus(String status) => SosResult(
    caseId: caseId,
    caseNumber: caseNumber,
    status: status,
    emergencyNumber: emergencyNumber,
    contactsNotified: contactsNotified,
  );

  @override
  List<Object?> get props => <Object?>[
    caseId,
    caseNumber,
    status,
    emergencyNumber,
    contactsNotified,
  ];
}

/// `reason` of `POST /safety/sos/{caseId}/cancel`.
enum SosCancelReason {
  accidental('accidental'),
  resolved('resolved');

  const SosCancelReason(this.apiValue);

  final String apiValue;
}
