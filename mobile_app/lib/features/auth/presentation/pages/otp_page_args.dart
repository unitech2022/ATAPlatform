import 'package:ata_app/features/auth/domain/entities/otp_request.dart';
import 'package:ata_app/features/auth/domain/entities/user_role.dart';

/// Data the phone screen hands to the OTP screen.
class OtpPageArgs {
  const OtpPageArgs({required this.request, required this.role});

  final OtpRequest request;
  final UserRole role;
}
