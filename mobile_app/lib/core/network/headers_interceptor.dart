import 'package:ata_app/core/storage/preferences_storage.dart';
import 'package:dio/dio.dart';

/// Adds `Accept-Language` and `X-Device-Id` to every request.
class HeadersInterceptor extends Interceptor {
  const HeadersInterceptor(this._prefs);

  static const String acceptLanguage = 'Accept-Language';
  static const String deviceId = 'X-Device-Id';

  final PreferencesStorage _prefs;

  @override
  void onRequest(RequestOptions options, RequestInterceptorHandler handler) {
    options.headers[acceptLanguage] = _prefs.localeCode;
    options.headers[deviceId] = _prefs.deviceId;
    handler.next(options);
  }
}
