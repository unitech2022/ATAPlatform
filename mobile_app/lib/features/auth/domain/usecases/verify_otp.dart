import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/auth/domain/entities/auth_session.dart';
import 'package:ata_app/features/auth/domain/entities/user_role.dart';
import 'package:ata_app/features/auth/domain/repositories/auth_repository.dart';
import 'package:equatable/equatable.dart';
import 'package:fpdart/fpdart.dart';

/// Input of [VerifyOtp].
class VerifyOtpParams extends Equatable {
  const VerifyOtpParams({
    required this.requestId,
    required this.phoneNumber,
    required this.code,
    required this.role,
  });

  final String requestId;
  final String phoneNumber;
  final String code;
  final UserRole role;

  @override
  List<Object?> get props => <Object?>[requestId, phoneNumber, code, role];
}

/// Exchanges the code for a session.
class VerifyOtp implements UseCase<AuthSession, VerifyOtpParams> {
  const VerifyOtp(this._repository);

  final AuthRepository _repository;

  @override
  Future<Either<Failure, AuthSession>> call(VerifyOtpParams params) =>
      _repository.verifyOtp(
        requestId: params.requestId,
        phoneNumber: params.phoneNumber,
        code: params.code,
        role: params.role,
      );
}
