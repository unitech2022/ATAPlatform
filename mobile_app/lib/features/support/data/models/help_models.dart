import 'package:ata_app/features/support/domain/entities/help.dart';
import 'package:ata_app/features/trip/data/models/json_readers.dart';

/// JSON mapping of the public help API (`docs/11` §F18.3).
abstract final class HelpModels {
  static HelpCategory category(Map<String, dynamic> json) => HelpCategory(
    id: JsonReaders.string(json, 'id'),
    code: JsonReaders.string(json, 'code'),
    name: JsonReaders.string(json, 'name'),
    icon: JsonReaders.string(json, 'icon'),
    articlesCount: JsonReaders.integer(json, 'articlesCount'),
  );

  static HelpArticleSummary summary(Map<String, dynamic> json) =>
      HelpArticleSummary(
        id: JsonReaders.string(json, 'id'),
        slug: JsonReaders.string(json, 'slug'),
        title: JsonReaders.string(json, 'title'),
        excerpt: JsonReaders.string(json, 'excerpt'),
        categoryId: JsonReaders.optionalString(json, 'categoryId'),
        updatedAt: JsonReaders.date(json, 'updatedAt'),
      );

  static HelpArticle article(Map<String, dynamic> json) => HelpArticle(
    id: JsonReaders.string(json, 'id'),
    slug: JsonReaders.string(json, 'slug'),
    title: JsonReaders.string(json, 'title'),
    body: JsonReaders.string(json, 'body'),
    categoryName: JsonReaders.object(json, 'category')?['name']?.toString(),
    tags: (json['tags'] is List<dynamic>)
        ? (json['tags'] as List<dynamic>)
              .map((dynamic t) => t.toString())
              .toList(growable: false)
        : const <String>[],
    updatedAt: JsonReaders.date(json, 'updatedAt'),
    related: JsonReaders.objects(json, 'related')
        .map(
          (Map<String, dynamic> r) => RelatedArticle(
            slug: JsonReaders.string(r, 'slug'),
            title: JsonReaders.string(r, 'title'),
          ),
        )
        .toList(growable: false),
  );
}
