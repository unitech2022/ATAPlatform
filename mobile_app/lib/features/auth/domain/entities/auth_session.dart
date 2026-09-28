import 'package:ata_app/features/auth/domain/entities/driver_summary.dart';
import 'package:ata_app/features/auth/domain/entities/user.dart';
import 'package:ata_app/features/auth/domain/entities/user_role.dart';
import 'package:equatable/equatable.dart';

/// A signed-in session: tokens, the user, the optional driver summary and
/// the role the user chose at sign-in.
class AuthSession extends Equatable {
  const AuthSession({
    required this.accessToken,
    required this.refreshToken,
    required this.isNewUser,
    required this.user,
    required this.activeRole,
    this.driver,
  });

  final String accessToken;
  final String refreshToken;
  final bool isNewUser;
  final User user;
  final UserRole activeRole;
  final DriverSummary? driver;

  bool get isDriver => activeRole == UserRole.driver;
  bool get isDriverApproved => driver?.applicationStatus.isApproved ?? false;

  AuthSession copyWith({
    User? user,
    DriverSummary? driver,
    UserRole? activeRole,
  }) => AuthSession(
    accessToken: accessToken,
    refreshToken: refreshToken,
    isNewUser: isNewUser,
    user: user ?? this.user,
    activeRole: activeRole ?? this.activeRole,
    driver: driver ?? this.driver,
  );

  @override
  List<Object?> get props => <Object?>[
    accessToken,
    refreshToken,
    isNewUser,
    user,
    activeRole,
    driver,
  ];
}
