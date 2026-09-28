/// Error raised by the network layer, mirroring the API error envelope
/// `{ "error": { "code", "message", "details" } }`.
class AppException implements Exception {
  const AppException({
    required this.code,
    required this.message,
    this.details,
    this.statusCode,
  });

  /// Machine-readable error code such as `otp_invalid`.
  final String code;

  /// Localized message coming from the API (or a generic fallback).
  final String message;

  /// Optional structured details (for example `attemptsLeft`).
  final Map<String, dynamic>? details;

  /// HTTP status code, if the error came from a response.
  final int? statusCode;

  static const String networkCode = 'network_error';
  static const String unauthorizedCode = 'unauthorized';
  static const String unexpectedCode = 'unexpected_error';

  @override
  String toString() => 'AppException($code, $message)';
}
