import 'package:equatable/equatable.dart';

/// The authenticated account (`user` object of the API contract).
class User extends Equatable {
  const User({
    required this.id,
    required this.phoneNumber,
    required this.language,
    required this.gender,
    required this.roles,
    this.fullName,
    this.termsAcceptedAt,
    this.createdAt,
  });

  final String id;
  final String phoneNumber;
  final String? fullName;
  final String language;
  final String gender;
  final List<String> roles;
  final DateTime? termsAcceptedAt;
  final DateTime? createdAt;

  bool get hasAcceptedTerms => termsAcceptedAt != null;
  bool get hasName => fullName != null && fullName!.trim().isNotEmpty;

  /// First word of the full name, for greetings.
  String? get firstName => hasName ? fullName!.trim().split(' ').first : null;

  User copyWith({
    String? fullName,
    String? language,
    DateTime? termsAcceptedAt,
  }) => User(
    id: id,
    phoneNumber: phoneNumber,
    fullName: fullName ?? this.fullName,
    language: language ?? this.language,
    gender: gender,
    roles: roles,
    termsAcceptedAt: termsAcceptedAt ?? this.termsAcceptedAt,
    createdAt: createdAt,
  );

  @override
  List<Object?> get props => <Object?>[
    id,
    phoneNumber,
    fullName,
    language,
    gender,
    roles,
    termsAcceptedAt,
    createdAt,
  ];
}
