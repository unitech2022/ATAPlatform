import 'package:ata_app/core/storage/token_storage.dart';
import 'package:dio/dio.dart';

/// Adds the Bearer token and transparently refreshes it once on 401.
class AuthInterceptor extends QueuedInterceptor {
  AuthInterceptor({
    required this._dio,
    required this._tokens,
    required this._refresh,
    required this._onSessionExpired,
  });

  static const String authPrefix = '/auth/';
  static const String _retriedFlag = 'ata.retried';

  final Dio _dio;
  final TokenStorage _tokens;
  final Future<StoredTokens?> Function(String refreshToken) _refresh;
  final void Function() _onSessionExpired;

  @override
  Future<void> onRequest(
    RequestOptions options,
    RequestInterceptorHandler handler,
  ) async {
    final StoredTokens? stored = await _tokens.read();
    if (stored != null) {
      options.headers['Authorization'] = 'Bearer ${stored.accessToken}';
    }
    handler.next(options);
  }

  @override
  Future<void> onError(
    DioException err,
    ErrorInterceptorHandler handler,
  ) async {
    final RequestOptions request = err.requestOptions;
    final bool isAuthCall = request.path.contains(authPrefix);
    final bool retried = request.extra[_retriedFlag] == true;
    if (err.response?.statusCode != 401 || isAuthCall || retried) {
      return handler.next(err);
    }
    final StoredTokens? stored = await _tokens.read();
    if (stored == null) return handler.next(err);

    StoredTokens? fresh;
    try {
      fresh = await _refresh(stored.refreshToken);
    } on Object {
      fresh = null;
    }
    if (fresh == null) {
      await _tokens.clear();
      _onSessionExpired();
      return handler.next(err);
    }
    await _tokens.save(fresh);
    request.extra[_retriedFlag] = true;
    request.headers['Authorization'] = 'Bearer ${fresh.accessToken}';
    try {
      final Response<dynamic> response = await _dio.fetch<dynamic>(request);
      handler.resolve(response);
    } on DioException catch (retryError) {
      handler.next(retryError);
    }
  }
}
