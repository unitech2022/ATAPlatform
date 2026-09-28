import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/auth/domain/entities/auth_session.dart';
import 'package:equatable/equatable.dart';

/// State of the OTP screen.
class OtpState extends Equatable {
  const OtpState({
    required this.requestId,
    required this.phoneNumber,
    this.code = '',
    this.secondsLeft = 0,
    this.verifying = false,
    this.resending = false,
    this.devCode,
    this.failure,
    this.session,
  });

  final String requestId;

  /// Normalized E.164 number the code was sent to.
  final String phoneNumber;
  final String code;
  final int secondsLeft;
  final bool verifying;
  final bool resending;
  final String? devCode;
  final Failure? failure;

  /// Set after successful verification.
  final AuthSession? session;

  static const int codeLength = 4;

  bool get isComplete => code.length == codeLength;
  bool get canVerify => isComplete && !verifying;
  bool get canResend => secondsLeft == 0 && !resending && !verifying;

  OtpState copyWith({
    String? requestId,
    String? code,
    int? secondsLeft,
    bool? verifying,
    bool? resending,
    String? devCode,
    Failure? failure,
    AuthSession? session,
    bool clearFailure = false,
  }) => OtpState(
    requestId: requestId ?? this.requestId,
    phoneNumber: phoneNumber,
    code: code ?? this.code,
    secondsLeft: secondsLeft ?? this.secondsLeft,
    verifying: verifying ?? this.verifying,
    resending: resending ?? this.resending,
    devCode: devCode ?? this.devCode,
    failure: clearFailure ? null : failure ?? this.failure,
    session: session ?? this.session,
  );

  @override
  List<Object?> get props => <Object?>[
    requestId,
    phoneNumber,
    code,
    secondsLeft,
    verifying,
    resending,
    devCode,
    failure,
    session,
  ];
}
