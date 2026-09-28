import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/auth/domain/entities/auth_session.dart';
import 'package:ata_app/features/auth/domain/entities/otp_request.dart';
import 'package:ata_app/features/auth/domain/entities/user.dart';
import 'package:ata_app/features/auth/domain/entities/user_role.dart';
import 'package:fpdart/fpdart.dart';

/// Authentication and session persistence.
abstract interface class AuthRepository {
  Future<Either<Failure, OtpRequest>> requestOtp({
    required String phoneNumber,
    required UserRole role,
    required String language,
  });

  Future<Either<Failure, AuthSession>> verifyOtp({
    required String requestId,
    required String phoneNumber,
    required String code,
    required UserRole role,
  });

  /// Returns the persisted session (refreshing tokens) or `null`.
  Future<Either<Failure, AuthSession?>> restoreSession();

  /// First-time rider: sets the name and accepts the terms.
  Future<Either<Failure, User>> completeRiderProfile({
    required String fullName,
  });

  Future<Either<Failure, Unit>> logout();
}
