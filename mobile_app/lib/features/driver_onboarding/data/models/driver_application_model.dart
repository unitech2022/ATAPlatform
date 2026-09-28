import 'package:ata_app/features/auth/domain/entities/driver_summary.dart';
import 'package:ata_app/features/driver_onboarding/domain/entities/driver_application.dart';

/// JSON mapping for [DriverApplication] and its nested objects.
class DriverApplicationModel extends DriverApplication {
  const DriverApplicationModel({
    required super.applicationNumber,
    required super.status,
    required super.documents,
    required super.requiredDocuments,
    required super.steps,
    super.rejectionReason,
    super.fullName,
    super.vehicle,
  });

  factory DriverApplicationModel.fromJson(Map<String, dynamic> json) {
    final Map<String, dynamic>? profile =
        json['profile'] as Map<String, dynamic>?;
    final Map<String, dynamic>? vehicle =
        json['vehicle'] as Map<String, dynamic>?;
    final Map<String, dynamic> steps =
        json['steps'] as Map<String, dynamic>? ?? const <String, dynamic>{};
    return DriverApplicationModel(
      applicationNumber: json['applicationNumber'] as String? ?? '',
      status: DriverApplicationStatus.parse(json['status'] as String?),
      rejectionReason: json['rejectionReason'] as String?,
      fullName: profile?['fullName'] as String?,
      vehicle: vehicle == null ? null : _vehicle(vehicle),
      documents: _list(json['documents'], _document),
      requiredDocuments: _list(json['requiredDocuments'], _required),
      steps: ApplicationSteps(
        profileComplete: steps['profileComplete'] as bool? ?? false,
        vehicleComplete: steps['vehicleComplete'] as bool? ?? false,
        documentsComplete: steps['documentsComplete'] as bool? ?? false,
        canSubmit: steps['canSubmit'] as bool? ?? false,
      ),
    );
  }

  static List<T> _list<T>(
    dynamic raw,
    T Function(Map<String, dynamic>) parse,
  ) => (raw as List<dynamic>? ?? const <dynamic>[])
      .map((dynamic e) => parse(e as Map<String, dynamic>))
      .toList(growable: false);

  static DriverDocument _document(Map<String, dynamic> json) => DriverDocument(
    id: json['id'] as String,
    documentTypeId: json['documentTypeId'] as String? ?? '',
    documentTypeCode: json['documentTypeCode'] as String? ?? '',
    documentTypeName: json['documentTypeName'] as String? ?? '',
    status: DocumentStatus.parse(json['status'] as String?),
    expiresAt: DateTime.tryParse(json['expiresAt'] as String? ?? ''),
    reviewNote: json['reviewNote'] as String?,
    fileName: json['fileName'] as String?,
    uploadedAt: DateTime.tryParse(json['uploadedAt'] as String? ?? ''),
  );

  static RequiredDocument _required(Map<String, dynamic> json) =>
      RequiredDocument(
        documentTypeId: json['documentTypeId'] as String? ?? '',
        code: json['code'] as String? ?? '',
        name: json['name'] as String? ?? '',
        isRequired: json['isRequired'] as bool? ?? true,
        uploaded: json['uploaded'] as bool? ?? false,
      );

  static Vehicle _vehicle(Map<String, dynamic> json) => Vehicle(
    id: json['id'] as String? ?? '',
    make: json['make'] as String? ?? '',
    model: json['model'] as String? ?? '',
    year: (json['year'] as num?)?.toInt() ?? 0,
    color: json['color'] as String? ?? '',
    plateNumber: json['plateNumber'] as String? ?? '',
    seats: (json['seats'] as num?)?.toInt() ?? 4,
  );
}
