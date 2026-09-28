import 'package:ata_app/features/auth/domain/entities/otp_request.dart';

/// JSON mapping for [OtpRequest].
class OtpRequestModel extends OtpRequest {
  const OtpRequestModel({
    required super.requestId,
    required super.phoneNumber,
    required super.expiresInSeconds,
    required super.resendAfterSeconds,
    super.devCode,
  });

  static const int defaultExpiry = 300;
  static const int defaultResend = 60;

  factory OtpRequestModel.fromJson(Map<String, dynamic> json) =>
      OtpRequestModel(
        requestId: json['requestId'] as String,
        phoneNumber: json['phoneNumber'] as String,
        expiresInSeconds:
            (json['expiresInSeconds'] as num?)?.toInt() ?? defaultExpiry,
        resendAfterSeconds:
            (json['resendAfterSeconds'] as num?)?.toInt() ?? defaultResend,
        devCode: json['devCode'] as String?,
      );
}
