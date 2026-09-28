import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/storage/token_storage.dart';
import 'package:ata_app/features/auth/data/datasources/auth_local_data_source.dart';
import 'package:ata_app/features/auth/data/datasources/auth_remote_data_source.dart';
import 'package:ata_app/features/auth/data/models/auth_session_model.dart';
import 'package:ata_app/features/auth/data/models/user_model.dart';
import 'package:ata_app/features/auth/domain/entities/auth_session.dart';
import 'package:ata_app/features/auth/domain/entities/otp_request.dart';
import 'package:ata_app/features/auth/domain/entities/user.dart';
import 'package:ata_app/features/auth/domain/entities/user_role.dart';
import 'package:ata_app/features/auth/domain/repositories/auth_repository.dart';
import 'package:fpdart/fpdart.dart';

/// [AuthRepository] backed by the API and local storage.
class AuthRepositoryImpl implements AuthRepository {
  const AuthRepositoryImpl({required this._remote, required this._local});

  final AuthRemoteDataSource _remote;
  final AuthLocalDataSource _local;

  @override
  Future<Either<Failure, OtpRequest>> requestOtp({
    required String phoneNumber,
    required UserRole role,
    required String language,
  }) => guard(
    () => _remote.requestOtp(
      phoneNumber: phoneNumber,
      role: role,
      language: language,
    ),
  );

  @override
  Future<Either<Failure, AuthSession>> verifyOtp({
    required String requestId,
    required String phoneNumber,
    required String code,
    required UserRole role,
  }) => guard(() async {
    final AuthSessionModel session = await _remote.verifyOtp(
      requestId: requestId,
      phoneNumber: phoneNumber,
      code: code,
      role: role,
    );
    await _persist(session);
    return session;
  });

  @override
  Future<Either<Failure, AuthSession?>> restoreSession() async {
    final StoredTokens? tokens = await _local.readTokens();
    final UserRole? role = _local.readActiveRole();
    if (tokens == null || role == null) {
      return const Right<Failure, AuthSession?>(null);
    }

    final Either<Failure, AuthSessionModel> refreshed = await guard(
      () => _remote.refresh(refreshToken: tokens.refreshToken, role: role),
    );
    return refreshed.fold(
      (Failure failure) async {
        // Offline: keep the cached user so the app still opens.
        final UserModel? cached = _local.readCachedUser();
        if (failure is NetworkFailure && cached != null) {
          return Right<Failure, AuthSession?>(
            AuthSession(
              accessToken: tokens.accessToken,
              refreshToken: tokens.refreshToken,
              isNewUser: false,
              user: cached,
              activeRole: role,
            ),
          );
        }
        await _local.clear();
        return const Right<Failure, AuthSession?>(null);
      },
      (AuthSessionModel session) async {
        await _persist(session);
        return Right<Failure, AuthSession?>(session);
      },
    );
  }

  @override
  Future<Either<Failure, User>> completeRiderProfile({
    required String fullName,
  }) => guard(() async {
    final UserModel user = await _remote.completeRiderProfile(
      fullName: fullName,
    );
    await _local.saveUser(user);
    return user;
  });

  @override
  Future<Either<Failure, Unit>> logout() async {
    final StoredTokens? tokens = await _local.readTokens();
    if (tokens != null) {
      // Best effort: local sign-out succeeds even if the API is unreachable.
      await guard(() => _remote.logout(refreshToken: tokens.refreshToken));
    }
    await _local.clear();
    return const Right<Failure, Unit>(unit);
  }

  Future<void> _persist(AuthSessionModel session) => _local.saveSession(
    tokens: StoredTokens(
      accessToken: session.accessToken,
      refreshToken: session.refreshToken,
    ),
    role: session.activeRole,
    user: UserModel.fromEntity(session.user),
  );
}
