import 'package:ata_app/features/auth/domain/entities/driver_summary.dart';
import 'package:equatable/equatable.dart';

/// Review state of one uploaded document.
enum DocumentStatus {
  pending('pending'),
  verified('verified'),
  rejected('rejected');

  const DocumentStatus(this.apiValue);

  final String apiValue;

  static DocumentStatus parse(String? value) {
    for (final DocumentStatus status in values) {
      if (status.apiValue == value) return status;
    }
    return pending;
  }
}

/// An uploaded driver document.
class DriverDocument extends Equatable {
  const DriverDocument({
    required this.id,
    required this.documentTypeId,
    required this.documentTypeCode,
    required this.documentTypeName,
    required this.status,
    this.expiresAt,
    this.reviewNote,
    this.fileName,
    this.uploadedAt,
  });

  final String id;
  final String documentTypeId;
  final String documentTypeCode;
  final String documentTypeName;
  final DocumentStatus status;
  final DateTime? expiresAt;
  final String? reviewNote;
  final String? fileName;
  final DateTime? uploadedAt;

  @override
  List<Object?> get props => <Object?>[
    id,
    documentTypeId,
    documentTypeCode,
    documentTypeName,
    status,
    expiresAt,
    reviewNote,
    fileName,
    uploadedAt,
  ];
}

/// A document the application still needs (or already has).
class RequiredDocument extends Equatable {
  const RequiredDocument({
    required this.documentTypeId,
    required this.code,
    required this.name,
    required this.isRequired,
    required this.uploaded,
  });

  final String documentTypeId;
  final String code;
  final String name;
  final bool isRequired;
  final bool uploaded;

  @override
  List<Object?> get props => <Object?>[
    documentTypeId,
    code,
    name,
    isRequired,
    uploaded,
  ];
}

/// The driver's vehicle.
class Vehicle extends Equatable {
  const Vehicle({
    required this.id,
    required this.make,
    required this.model,
    required this.year,
    required this.color,
    required this.plateNumber,
    required this.seats,
  });

  final String id;
  final String make;
  final String model;
  final int year;
  final String color;
  final String plateNumber;
  final int seats;

  @override
  List<Object?> get props => <Object?>[
    id,
    make,
    model,
    year,
    color,
    plateNumber,
    seats,
  ];
}

/// Completion flags of the application.
class ApplicationSteps extends Equatable {
  const ApplicationSteps({
    required this.profileComplete,
    required this.vehicleComplete,
    required this.documentsComplete,
    required this.canSubmit,
  });

  final bool profileComplete;
  final bool vehicleComplete;
  final bool documentsComplete;
  final bool canSubmit;

  @override
  List<Object?> get props => <Object?>[
    profileComplete,
    vehicleComplete,
    documentsComplete,
    canSubmit,
  ];
}

/// `GET /driver/application`.
class DriverApplication extends Equatable {
  const DriverApplication({
    required this.applicationNumber,
    required this.status,
    required this.documents,
    required this.requiredDocuments,
    required this.steps,
    this.rejectionReason,
    this.fullName,
    this.vehicle,
  });

  final String applicationNumber;
  final DriverApplicationStatus status;
  final String? rejectionReason;
  final String? fullName;
  final Vehicle? vehicle;
  final List<DriverDocument> documents;
  final List<RequiredDocument> requiredDocuments;
  final ApplicationSteps steps;

  /// Latest uploaded document for a type, if any.
  DriverDocument? documentFor(String documentTypeId) {
    for (final DriverDocument document in documents) {
      if (document.documentTypeId == documentTypeId) return document;
    }
    return null;
  }

  @override
  List<Object?> get props => <Object?>[
    applicationNumber,
    status,
    rejectionReason,
    fullName,
    vehicle,
    documents,
    requiredDocuments,
    steps,
  ];
}
