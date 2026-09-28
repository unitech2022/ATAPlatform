import 'package:ata_app/features/catalog/domain/entities/document_type.dart';

/// JSON mapping for [DocumentType].
class DocumentTypeModel extends DocumentType {
  const DocumentTypeModel({
    required super.id,
    required super.code,
    required super.name,
    required super.appliesTo,
    required super.isRequired,
    required super.requiresExpiry,
  });

  factory DocumentTypeModel.fromJson(Map<String, dynamic> json) =>
      DocumentTypeModel(
        id: json['id'] as String,
        code: json['code'] as String,
        name: json['name'] as String,
        appliesTo: json['appliesTo'] as String? ?? 'driver',
        isRequired: json['isRequired'] as bool? ?? true,
        requiresExpiry: json['requiresExpiry'] as bool? ?? false,
      );
}
