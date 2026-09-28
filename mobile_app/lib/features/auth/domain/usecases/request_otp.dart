import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/auth/domain/entities/otp_request.dart';
import 'package:ata_app/features/auth/domain/entities/user_role.dart';
import 'package:ata_app/features/auth/domain/repositories/auth_repository.dart';
import 'package:equatable/equatable.dart';
import 'package:fpdart/fpdart.dart';

/// Input of [RequestOtp].
class RequestOtpParams extends Equatable {
  const RequestOtpParams({
    required this.phoneNumber,
    required this.role,
    required this.language,
  });

  final String phoneNumber;
  final UserRole role;
  final String language;

  @override
  List<Object?> get props => <Object?>[phoneNumber, role, language];
}

/// Sends a one-time code to the given phone number.
class RequestOtp implements UseCase<OtpRequest, RequestOtpParams> {
  const RequestOtp(this._repository);

  final AuthRepository _repository;

  @override
  Future<Either<Failure, OtpRequest>> call(RequestOtpParams params) =>
      _repository.requestOtp(
        phoneNumber: params.phoneNumber,
        role: params.role,
        language: params.language,
      );
}
