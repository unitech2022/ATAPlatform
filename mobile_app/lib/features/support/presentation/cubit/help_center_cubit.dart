import 'dart:async';

import 'package:ata_app/core/models/page_result.dart';
import 'package:ata_app/features/support/domain/entities/help.dart';
import 'package:ata_app/features/support/domain/usecases/get_help_categories.dart';
import 'package:ata_app/features/support/domain/usecases/search_help_articles.dart';
import 'package:ata_app/features/support/presentation/cubit/help_center_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// The help center (`docs/11` §F18.3, public API filtered by [audience]):
/// categories, and the article list of a category or a search. Typing
/// searches after a [debounce] pause.
class HelpCenterCubit extends Cubit<HelpCenterState> {
  HelpCenterCubit({
    required this._getCategories,
    required this._searchArticles,
    required this.audience,
    this.debounce = defaultDebounce,
  }) : super(const HelpCenterState());

  static const Duration defaultDebounce = Duration(milliseconds: 400);

  final GetHelpCategories _getCategories;
  final SearchHelpArticles _searchArticles;

  /// `passenger` or `driver`.
  final String audience;
  final Duration debounce;

  Timer? _timer;
  int _generation = 0;

  Future<void> load() async {
    emit(state.copyWith(loading: true, clearFailure: true));
    final result = await _getCategories(audience);
    if (isClosed) return;
    emit(
      result.fold(
        (failure) => state.copyWith(loading: false, failure: failure),
        (List<HelpCategory> c) => state.copyWith(loading: false, categories: c),
      ),
    );
  }

  /// The search box changed: results follow after the debounce pause.
  void queryChanged(String text) {
    if (text == state.text) return;
    _timer?.cancel();
    emit(state.copyWith(text: text, clearSearchFailure: true));
    if (state.browsing) {
      _generation++;
      emit(state.copyWith(articles: const <HelpArticleSummary>[]));
      return;
    }
    emit(state.copyWith(searching: true));
    _timer = Timer(debounce, () => _search(1));
  }

  void selectCategory(String categoryId) {
    _timer?.cancel();
    emit(state.copyWith(categoryId: categoryId, searching: true));
    unawaited(_search(1));
  }

  /// Back to the category grid (clears the category and the search).
  void reset() {
    _timer?.cancel();
    _generation++;
    emit(
      state.copyWith(
        text: '',
        clearCategory: true,
        articles: const <HelpArticleSummary>[],
        searching: false,
        hasMore: false,
        clearSearchFailure: true,
        resetVersion: state.resetVersion + 1,
      ),
    );
  }

  Future<void> retry() => _search(1);

  Future<void> loadMore() async {
    if (state.searching || !state.hasMore) return;
    emit(state.copyWith(searching: true));
    await _search(state.page + 1);
  }

  Future<void> _search(int page) async {
    final int generation = ++_generation;
    final result = await _searchArticles(
      HelpQuery(
        audience: audience,
        categoryId: state.categoryId,
        text: state.text,
        page: page,
      ),
    );
    if (isClosed || generation != _generation) return;
    emit(
      result.fold(
        (failure) => state.copyWith(searching: false, searchFailure: failure),
        (PageResult<HelpArticleSummary> p) => state.copyWith(
          searching: false,
          clearSearchFailure: true,
          articles: page == 1
              ? p.items
              : <HelpArticleSummary>[...state.articles, ...p.items],
          page: page,
          hasMore: p.items.isNotEmpty && p.hasMore,
        ),
      ),
    );
  }

  @override
  Future<void> close() {
    _timer?.cancel();
    return super.close();
  }
}
