import 'package:ata_app/features/auth/data/models/driver_summary_model.dart';
import 'package:ata_app/features/auth/data/models/user_model.dart';
import 'package:ata_app/features/auth/domain/entities/auth_session.dart';
import 'package:ata_app/features/auth/domain/entities/user_role.dart';

/// JSON mapping for the `AuthResponse` contract.
class AuthSessionModel extends AuthSession {
  const AuthSessionModel({
    required super.accessToken,
    required super.refreshToken,
    required super.isNewUser,
    required super.user,
    required super.activeRole,
    super.driver,
  });

  factory AuthSessionModel.fromJson(
    Map<String, dynamic> json, {
    required UserRole activeRole,
  }) {
    final Map<String, dynamic>? driver =
        json['driver'] as Map<String, dynamic>?;
    return AuthSessionModel(
      accessToken: json['accessToken'] as String,
      refreshToken: json['refreshToken'] as String,
      isNewUser: json['isNewUser'] as bool? ?? false,
      user: UserModel.fromJson(json['user'] as Map<String, dynamic>),
      activeRole: activeRole,
      driver: driver == null ? null : DriverSummaryModel.fromJson(driver),
    );
  }
}
