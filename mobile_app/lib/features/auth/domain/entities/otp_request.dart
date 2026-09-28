import 'package:equatable/equatable.dart';

/// Result of `POST /auth/otp/request`.
class OtpRequest extends Equatable {
  const OtpRequest({
    required this.requestId,
    required this.phoneNumber,
    required this.expiresInSeconds,
    required this.resendAfterSeconds,
    this.devCode,
  });

  final String requestId;

  /// Normalized E.164 number.
  final String phoneNumber;
  final int expiresInSeconds;
  final int resendAfterSeconds;

  /// Only present when the API runs with `Otp:DevMode=true`.
  final String? devCode;

  @override
  List<Object?> get props => <Object?>[
    requestId,
    phoneNumber,
    expiresInSeconds,
    resendAfterSeconds,
    devCode,
  ];
}
