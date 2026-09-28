import 'package:ata_app/features/auth/domain/entities/user.dart';

/// JSON mapping for [User].
class UserModel extends User {
  const UserModel({
    required super.id,
    required super.phoneNumber,
    required super.language,
    required super.gender,
    required super.roles,
    super.fullName,
    super.termsAcceptedAt,
    super.createdAt,
  });

  factory UserModel.fromJson(Map<String, dynamic> json) => UserModel(
    id: json['id'] as String,
    phoneNumber: json['phoneNumber'] as String,
    fullName: json['fullName'] as String?,
    language: json['language'] as String? ?? 'ar',
    gender: json['gender'] as String? ?? 'unknown',
    roles: (json['roles'] as List<dynamic>? ?? const <dynamic>[])
        .map((dynamic e) => e as String)
        .toList(growable: false),
    termsAcceptedAt: DateTime.tryParse(
      json['termsAcceptedAt'] as String? ?? '',
    ),
    createdAt: DateTime.tryParse(json['createdAt'] as String? ?? ''),
  );

  factory UserModel.fromEntity(User user) => UserModel(
    id: user.id,
    phoneNumber: user.phoneNumber,
    fullName: user.fullName,
    language: user.language,
    gender: user.gender,
    roles: user.roles,
    termsAcceptedAt: user.termsAcceptedAt,
    createdAt: user.createdAt,
  );

  Map<String, dynamic> toJson() => <String, dynamic>{
    'id': id,
    'phoneNumber': phoneNumber,
    'fullName': fullName,
    'language': language,
    'gender': gender,
    'roles': roles,
    'termsAcceptedAt': termsAcceptedAt?.toIso8601String(),
    'createdAt': createdAt?.toIso8601String(),
  };
}
