import 'package:equatable/equatable.dart';

/// `GET /help/categories` row.
class HelpCategory extends Equatable {
  const HelpCategory({
    required this.id,
    required this.code,
    required this.name,
    this.icon = '',
    this.articlesCount = 0,
  });

  final String id;
  final String code;
  final String name;

  /// Icon key chosen by the admin (`car`, `wallet`, `shield`, ...).
  final String icon;
  final int articlesCount;

  @override
  List<Object?> get props => <Object?>[id, code, name, icon, articlesCount];
}

/// Row of `GET /help/articles` (excerpt without Markdown).
class HelpArticleSummary extends Equatable {
  const HelpArticleSummary({
    required this.id,
    required this.slug,
    required this.title,
    this.excerpt = '',
    this.categoryId,
    this.updatedAt,
  });

  final String id;
  final String slug;
  final String title;
  final String excerpt;
  final String? categoryId;
  final DateTime? updatedAt;

  @override
  List<Object?> get props => <Object?>[
    id,
    slug,
    title,
    excerpt,
    categoryId,
    updatedAt,
  ];
}

/// `related` item of an article.
class RelatedArticle extends Equatable {
  const RelatedArticle({required this.slug, required this.title});

  final String slug;
  final String title;

  @override
  List<Object?> get props => <Object?>[slug, title];
}

/// `GET /help/articles/{slug}`; [body] is Markdown.
class HelpArticle extends Equatable {
  const HelpArticle({
    required this.id,
    required this.slug,
    required this.title,
    required this.body,
    this.categoryName,
    this.tags = const <String>[],
    this.updatedAt,
    this.related = const <RelatedArticle>[],
  });

  final String id;
  final String slug;
  final String title;
  final String body;
  final String? categoryName;
  final List<String> tags;
  final DateTime? updatedAt;
  final List<RelatedArticle> related;

  @override
  List<Object?> get props => <Object?>[
    id,
    slug,
    title,
    body,
    categoryName,
    tags,
    updatedAt,
    related,
  ];
}

/// Query of `GET /help/articles`.
class HelpQuery extends Equatable {
  const HelpQuery({
    required this.audience,
    this.categoryId,
    this.text = '',
    this.page = 1,
  });

  /// `passenger` or `driver`.
  final String audience;
  final String? categoryId;
  final String text;
  final int page;

  @override
  List<Object?> get props => <Object?>[audience, categoryId, text, page];
}
