import 'package:equatable/equatable.dart';

/// A document kind drivers must upload.
class DocumentType extends Equatable {
  const DocumentType({
    required this.id,
    required this.code,
    required this.name,
    required this.appliesTo,
    required this.isRequired,
    required this.requiresExpiry,
  });

  final String id;
  final String code;
  final String name;

  /// `driver` or `vehicle`.
  final String appliesTo;
  final bool isRequired;
  final bool requiresExpiry;

  @override
  List<Object?> get props => <Object?>[
    id,
    code,
    name,
    appliesTo,
    isRequired,
    requiresExpiry,
  ];
}
