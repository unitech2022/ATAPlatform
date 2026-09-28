import 'package:equatable/equatable.dart';

/// `GET /safety/trusted-contacts` row (F12).
class TrustedContact extends Equatable {
  const TrustedContact({
    required this.id,
    required this.name,
    required this.phoneNumber,
    this.relationship,
    this.autoShare = false,
    this.notifyOnSos = true,
    this.createdAt,
  });

  /// `trusted_contacts_limit`.
  static const int maxPerUser = 5;

  final String id;
  final String name;

  /// E.164.
  final String phoneNumber;
  final String? relationship;

  /// Receives the tracking link by SMS when a driver is assigned.
  final bool autoShare;

  /// Receives an SMS when an SOS is raised.
  final bool notifyOnSos;
  final DateTime? createdAt;

  TrustedContactDraft toDraft() => TrustedContactDraft(
    name: name,
    phoneNumber: phoneNumber,
    relationship: relationship,
    autoShare: autoShare,
    notifyOnSos: notifyOnSos,
  );

  @override
  List<Object?> get props => <Object?>[
    id,
    name,
    phoneNumber,
    relationship,
    autoShare,
    notifyOnSos,
    createdAt,
  ];
}

/// Body of `POST` / `PUT /safety/trusted-contacts`.
class TrustedContactDraft extends Equatable {
  const TrustedContactDraft({
    this.name = '',
    this.phoneNumber = '',
    this.relationship,
    this.autoShare = false,
    this.notifyOnSos = true,
  });

  static const int maxNameLength = 80;
  static const int maxRelationshipLength = 40;

  final String name;
  final String phoneNumber;
  final String? relationship;
  final bool autoShare;
  final bool notifyOnSos;

  TrustedContactDraft copyWith({
    String? name,
    String? phoneNumber,
    String? relationship,
    bool? autoShare,
    bool? notifyOnSos,
  }) => TrustedContactDraft(
    name: name ?? this.name,
    phoneNumber: phoneNumber ?? this.phoneNumber,
    relationship: relationship ?? this.relationship,
    autoShare: autoShare ?? this.autoShare,
    notifyOnSos: notifyOnSos ?? this.notifyOnSos,
  );

  @override
  List<Object?> get props => <Object?>[
    name,
    phoneNumber,
    relationship,
    autoShare,
    notifyOnSos,
  ];
}
