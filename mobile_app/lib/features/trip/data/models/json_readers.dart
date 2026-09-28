/// Small helpers for tolerant JSON parsing.
abstract final class JsonReaders {
  static String string(Map<String, dynamic> json, String key) =>
      json[key]?.toString() ?? '';

  static String? optionalString(Map<String, dynamic> json, String key) =>
      json[key]?.toString();

  static double number(Map<String, dynamic> json, String key) =>
      (json[key] as num?)?.toDouble() ?? 0;

  static double? optionalNumber(Map<String, dynamic> json, String key) =>
      (json[key] as num?)?.toDouble();

  static int integer(Map<String, dynamic> json, String key) =>
      (json[key] as num?)?.toInt() ?? 0;

  static int? optionalInteger(Map<String, dynamic> json, String key) =>
      (json[key] as num?)?.toInt();

  static DateTime? date(Map<String, dynamic> json, String key) =>
      DateTime.tryParse(json[key]?.toString() ?? '');

  static Map<String, dynamic>? object(Map<String, dynamic> json, String key) {
    final Object? value = json[key];
    return value is Map<String, dynamic> ? value : null;
  }

  static List<Map<String, dynamic>> objects(
    Map<String, dynamic> json,
    String key,
  ) {
    final Object? value = json[key];
    if (value is! List<dynamic>) return const <Map<String, dynamic>>[];
    return value.whereType<Map<String, dynamic>>().toList(growable: false);
  }
}
