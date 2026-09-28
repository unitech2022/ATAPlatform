import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/utils/phone_number.dart';
import 'package:ata_app/features/auth/domain/entities/otp_request.dart';
import 'package:equatable/equatable.dart';

/// State of the phone-number screen.
class PhoneState extends Equatable {
  const PhoneState({
    this.digits = '',
    this.submitting = false,
    this.failure,
    this.result,
  });

  /// Up to nine local digits typed on the keypad.
  final String digits;
  final bool submitting;
  final Failure? failure;

  /// Set once the OTP was requested successfully.
  final OtpRequest? result;

  bool get isValid => PhoneNumber.isValidLocal(digits);
  bool get canSubmit => isValid && !submitting;

  PhoneState copyWith({
    String? digits,
    bool? submitting,
    Failure? failure,
    OtpRequest? result,
    bool clearFailure = false,
    bool clearResult = false,
  }) => PhoneState(
    digits: digits ?? this.digits,
    submitting: submitting ?? this.submitting,
    failure: clearFailure ? null : failure ?? this.failure,
    result: clearResult ? null : result ?? this.result,
  );

  @override
  List<Object?> get props => <Object?>[digits, submitting, failure, result];
}
