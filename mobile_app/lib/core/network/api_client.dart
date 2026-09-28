import 'package:ata_app/core/errors/app_exception.dart';
import 'package:ata_app/core/network/auth_interceptor.dart';
import 'package:ata_app/core/network/error_interceptor.dart';
import 'package:ata_app/core/network/headers_interceptor.dart';
import 'package:ata_app/core/storage/preferences_storage.dart';
import 'package:ata_app/core/storage/token_storage.dart';
import 'package:dio/dio.dart';

/// Thin wrapper over [Dio] configured for the ATA API.
///
/// Every call returns the decoded JSON body and throws [AppException]
/// on failure.
class ApiClient {
  ApiClient({
    required String baseUrl,
    required TokenStorage tokens,
    required PreferencesStorage prefs,
    required void Function() onSessionExpired,
    Dio? dio,
  }) : _dio = dio ?? Dio() {
    _dio.options = BaseOptions(
      baseUrl: baseUrl,
      connectTimeout: _timeout,
      receiveTimeout: _timeout,
      headers: <String, dynamic>{'Accept': 'application/json'},
    );
    _dio.interceptors.addAll(<Interceptor>[
      HeadersInterceptor(prefs),
      AuthInterceptor(
        dio: _dio,
        tokens: tokens,
        refresh: _refreshTokens,
        onSessionExpired: onSessionExpired,
      ),
      const ErrorInterceptor(),
    ]);
  }

  static const Duration _timeout = Duration(seconds: 20);
  static const String refreshPath = '/auth/refresh';
  static const String idempotencyHeader = 'Idempotency-Key';

  final Dio _dio;

  /// Exposed for tests that need to attach an adapter.
  Dio get dio => _dio;

  Future<StoredTokens?> _refreshTokens(String refreshToken) async {
    final Dio bare = Dio(BaseOptions(baseUrl: _dio.options.baseUrl));
    final Response<dynamic> response = await bare.post<dynamic>(
      refreshPath,
      data: <String, dynamic>{'refreshToken': refreshToken},
    );
    final Map<String, dynamic> body = response.data as Map<String, dynamic>;
    return StoredTokens(
      accessToken: body['accessToken'] as String,
      refreshToken: body['refreshToken'] as String,
    );
  }

  Future<dynamic> get(String path, {Map<String, dynamic>? query}) =>
      _run(() => _dio.get<dynamic>(path, queryParameters: query));

  Future<dynamic> post(
    String path, {
    Object? body,
    Map<String, dynamic>? headers,
  }) => _run(
    () => _dio.post<dynamic>(
      path,
      data: body,
      options: Options(headers: headers),
    ),
  );

  Future<dynamic> put(String path, {Object? body}) =>
      _run(() => _dio.put<dynamic>(path, data: body));

  Future<dynamic> patch(String path, {Object? body}) =>
      _run(() => _dio.patch<dynamic>(path, data: body));

  Future<dynamic> delete(String path, {Object? body}) =>
      _run(() => _dio.delete<dynamic>(path, data: body));

  Future<dynamic> _run(Future<Response<dynamic>> Function() call) async {
    try {
      final Response<dynamic> response = await call();
      return response.data;
    } on DioException catch (error) {
      final Object? mapped = error.error;
      if (mapped is AppException) throw mapped;
      throw AppException(
        code: AppException.unexpectedCode,
        message: error.message ?? error.toString(),
      );
    }
  }
}
