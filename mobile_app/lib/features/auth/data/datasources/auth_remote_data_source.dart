import 'package:ata_app/core/env/env.dart';
import 'package:ata_app/core/network/api_client.dart';
import 'package:ata_app/features/auth/data/models/auth_session_model.dart';
import 'package:ata_app/features/auth/data/models/otp_request_model.dart';
import 'package:ata_app/features/auth/data/models/user_model.dart';
import 'package:ata_app/features/auth/domain/entities/user_role.dart';

/// `/auth/*` and the `PATCH /me` used during onboarding.
class AuthRemoteDataSource {
  const AuthRemoteDataSource(this._api, {required this.deviceId});

  final ApiClient _api;
  final String deviceId;

  static const String _requestPath = '/auth/otp/request';
  static const String _verifyPath = '/auth/otp/verify';
  static const String _logoutPath = '/auth/logout';
  static const String _mePath = '/me';
  static const String _platform = 'android';

  Future<OtpRequestModel> requestOtp({
    required String phoneNumber,
    required UserRole role,
    required String language,
  }) async {
    final dynamic body = await _api.post(
      _requestPath,
      body: <String, dynamic>{
        'phoneNumber': phoneNumber,
        'role': role.apiValue,
        'language': language,
      },
    );
    return OtpRequestModel.fromJson(body as Map<String, dynamic>);
  }

  Future<AuthSessionModel> verifyOtp({
    required String requestId,
    required String phoneNumber,
    required String code,
    required UserRole role,
  }) async {
    final dynamic body = await _api.post(
      _verifyPath,
      body: <String, dynamic>{
        'requestId': requestId,
        'phoneNumber': phoneNumber,
        'code': code,
        'role': role.apiValue,
        'device': <String, dynamic>{
          'deviceId': deviceId,
          'platform': _platform,
          'deviceName': _platform,
          'appVersion': Env.appVersion,
        },
      },
    );
    return AuthSessionModel.fromJson(
      body as Map<String, dynamic>,
      activeRole: role,
    );
  }

  Future<AuthSessionModel> refresh({
    required String refreshToken,
    required UserRole role,
  }) async {
    final dynamic body = await _api.post(
      ApiClient.refreshPath,
      body: <String, dynamic>{'refreshToken': refreshToken},
    );
    return AuthSessionModel.fromJson(
      body as Map<String, dynamic>,
      activeRole: role,
    );
  }

  Future<void> logout({required String refreshToken}) => _api.post(
    _logoutPath,
    body: <String, dynamic>{'refreshToken': refreshToken},
  );

  Future<UserModel> completeRiderProfile({required String fullName}) async {
    final dynamic body = await _api.patch(
      _mePath,
      body: <String, dynamic>{'fullName': fullName, 'acceptTerms': true},
    );
    return UserModel.fromJson(body as Map<String, dynamic>);
  }
}
