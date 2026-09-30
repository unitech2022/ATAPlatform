import 'dart:convert';

import 'package:ata_app/core/errors/app_exception.dart';
import 'package:dio/dio.dart';

/// Turns every [DioException] into an [AppException] carrying the API's
/// error envelope when present.
class ErrorInterceptor extends Interceptor {
  const ErrorInterceptor();

  /// Empty messages are replaced by localized text in the presentation layer
  /// (see `failureText`).
  static const String _noMessage = '';

  @override
  void onError(DioException err, ErrorInterceptorHandler handler) {
    handler.reject(err.copyWith(error: toAppException(err)));
  }

  AppException toAppException(DioException err) {
    final Object? existing = err.error;
    if (existing is AppException) return existing;
    final Response<dynamic>? response = err.response;
    if (response == null) {
      return const AppException(
        code: AppException.networkCode,
        message: _noMessage,
      );
    }
    final Map<String, dynamic>? envelope = _envelope(response.data);
    final int status = response.statusCode ?? 0;
    return AppException(
      code:
          envelope?['code'] as String? ??
          (status == 401 ? AppException.unauthorizedCode : 'http_$status'),
      message: envelope?['message'] as String? ?? _noMessage,
      details: envelope?['details'] as Map<String, dynamic>?,
      statusCode: status,
    );
  }

  Map<String, dynamic>? _envelope(dynamic data) {
    if (data is List<int>) {
      // Binary downloads (`responseType: bytes`) carry the error as JSON bytes.
      try {
        return _envelope(jsonDecode(utf8.decode(data)));
      } on Object {
        return null;
      }
    }
    if (data is Map<String, dynamic>) {
      final Object? error = data['error'];
      if (error is Map<String, dynamic>) return error;
    }
    return null;
  }
}
