import 'package:equatable/equatable.dart';

/// A page of results as returned by list endpoints
/// (`{ items, page, pageSize, total }`).
class PageResult<T> extends Equatable {
  const PageResult({
    required this.items,
    required this.page,
    required this.pageSize,
    required this.total,
  });

  factory PageResult.fromJson(
    Map<String, dynamic> json,
    T Function(Map<String, dynamic> item) parseItem,
  ) {
    final List<dynamic> rawItems = json['items'] as List<dynamic>? ?? const [];
    return PageResult<T>(
      items: rawItems
          .map((dynamic e) => parseItem(e as Map<String, dynamic>))
          .toList(growable: false),
      page: (json['page'] as num?)?.toInt() ?? 1,
      pageSize: (json['pageSize'] as num?)?.toInt() ?? rawItems.length,
      total: (json['total'] as num?)?.toInt() ?? rawItems.length,
    );
  }

  /// Tolerant reader: a bare array, or the `{ items, page, pageSize, total }`
  /// envelope. Anything else is an empty page.
  factory PageResult.fromAny(
    Object? body,
    T Function(Map<String, dynamic> item) parseItem,
  ) {
    if (body is Map<String, dynamic>) {
      return PageResult.fromJson(body, parseItem);
    }
    if (body is! List<dynamic>) return PageResult<T>.empty();
    final List<T> items = body
        .whereType<Map<String, dynamic>>()
        .map(parseItem)
        .toList(growable: false);
    return PageResult<T>(
      items: items,
      page: 1,
      pageSize: items.length,
      total: items.length,
    );
  }

  const PageResult.empty()
    : items = const [],
      page = 1,
      pageSize = 0,
      total = 0;

  final List<T> items;
  final int page;
  final int pageSize;
  final int total;

  bool get isEmpty => items.isEmpty;
  bool get hasMore => page * pageSize < total;

  @override
  List<Object?> get props => <Object?>[items, page, pageSize, total];
}
