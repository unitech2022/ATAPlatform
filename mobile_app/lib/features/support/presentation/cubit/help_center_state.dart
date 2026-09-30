import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/support/domain/entities/help.dart';
import 'package:equatable/equatable.dart';

/// State of `HelpCenterCubit`: the categories, the search text / chosen
/// category and the matching articles.
class HelpCenterState extends Equatable {
  const HelpCenterState({
    this.categories = const <HelpCategory>[],
    this.loading = false,
    this.failure,
    this.text = '',
    this.categoryId,
    this.articles = const <HelpArticleSummary>[],
    this.page = 1,
    this.hasMore = false,
    this.searching = false,
    this.searchFailure,
    this.resetVersion = 0,
  });

  final List<HelpCategory> categories;
  final bool loading;
  final Failure? failure;

  /// What the user typed (before the debounce fired).
  final String text;
  final String? categoryId;
  final List<HelpArticleSummary> articles;
  final int page;
  final bool hasMore;
  final bool searching;
  final Failure? searchFailure;

  /// Bumped by `reset` (rebuilds the search box empty).
  final int resetVersion;

  /// No search text and no category: the category grid is shown.
  bool get browsing => text.trim().isEmpty && categoryId == null;

  HelpCategory? get category {
    for (final HelpCategory c in categories) {
      if (c.id == categoryId) return c;
    }
    return null;
  }

  HelpCenterState copyWith({
    List<HelpCategory>? categories,
    bool? loading,
    Failure? failure,
    bool clearFailure = false,
    String? text,
    String? categoryId,
    bool clearCategory = false,
    List<HelpArticleSummary>? articles,
    int? page,
    bool? hasMore,
    bool? searching,
    Failure? searchFailure,
    bool clearSearchFailure = false,
    int? resetVersion,
  }) => HelpCenterState(
    categories: categories ?? this.categories,
    loading: loading ?? this.loading,
    failure: clearFailure ? null : failure ?? this.failure,
    text: text ?? this.text,
    categoryId: clearCategory ? null : categoryId ?? this.categoryId,
    articles: articles ?? this.articles,
    page: page ?? this.page,
    hasMore: hasMore ?? this.hasMore,
    searching: searching ?? this.searching,
    searchFailure: clearSearchFailure
        ? null
        : searchFailure ?? this.searchFailure,
    resetVersion: resetVersion ?? this.resetVersion,
  );

  @override
  List<Object?> get props => <Object?>[
    categories,
    loading,
    failure,
    text,
    categoryId,
    articles,
    page,
    hasMore,
    searching,
    searchFailure,
    resetVersion,
  ];
}
