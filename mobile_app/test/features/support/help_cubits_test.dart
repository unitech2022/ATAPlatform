import 'package:ata_app/features/support/domain/entities/help.dart';
import 'package:ata_app/features/support/domain/usecases/get_help_article.dart';
import 'package:ata_app/features/support/domain/usecases/get_help_categories.dart';
import 'package:ata_app/features/support/domain/usecases/search_help_articles.dart';
import 'package:ata_app/features/support/domain/usecases/send_article_feedback.dart';
import 'package:ata_app/features/support/presentation/cubit/help_article_cubit.dart';
import 'package:ata_app/features/support/presentation/cubit/help_center_cubit.dart';
import 'package:ata_app/features/support/presentation/cubit/help_center_state.dart';
import 'package:flutter_test/flutter_test.dart';

import '../../helpers/support_fakes.dart';

void main() {
  late FakeSupportRepository repo;

  setUp(() => repo = FakeSupportRepository());

  HelpCenterCubit center({String audience = 'passenger'}) => HelpCenterCubit(
    getCategories: GetHelpCategories(repo),
    searchArticles: SearchHelpArticles(repo),
    audience: audience,
    debounce: const Duration(milliseconds: 20),
  );

  group('HelpCenterCubit', () {
    test('loads the categories', () async {
      final HelpCenterCubit cubit = center();
      await cubit.load();
      expect(cubit.state.categories, fakeCategories);
      expect(cubit.state.loading, isFalse);
      expect(cubit.state.browsing, isTrue);
      await cubit.close();
    });

    test('a category failure is kept for a retry', () async {
      repo.categoriesFailure = apiFailure('boom');
      final HelpCenterCubit cubit = center();
      await cubit.load();
      expect(cubit.state.failure?.code, 'boom');
      repo.categoriesFailure = null;
      await cubit.load();
      expect(cubit.state.failure, isNull);
      expect(cubit.state.categories, isNotEmpty);
      await cubit.close();
    });

    test(
      'typing searches after the debounce, once, for the last text',
      () async {
        final HelpCenterCubit cubit = center(audience: 'driver');
        await cubit.load();
        cubit
          ..queryChanged('إل')
          ..queryChanged('إلغاء');
        expect(cubit.state.searching, isTrue);
        expect(repo.queries, isEmpty);

        await Future<void>.delayed(const Duration(milliseconds: 80));
        expect(repo.queries, hasLength(1));
        expect(repo.queries.single.text, 'إلغاء');
        expect(repo.queries.single.audience, 'driver');
        expect(cubit.state.articles.single.slug, 'cancellation-fees');
        expect(cubit.state.searching, isFalse);
        expect(cubit.state.browsing, isFalse);
        await cubit.close();
      },
    );

    test('clearing the text before the pause cancels the search', () async {
      final HelpCenterCubit cubit = center();
      cubit
        ..queryChanged('رسوم')
        ..queryChanged('');
      await Future<void>.delayed(const Duration(milliseconds: 60));
      expect(repo.queries, isEmpty);
      expect(cubit.state.browsing, isTrue);
      expect(cubit.state.articles, isEmpty);
      await cubit.close();
    });

    test('picking a category lists its articles, reset goes back', () async {
      final HelpCenterCubit cubit = center();
      await cubit.load();
      cubit.selectCategory('c1');
      await Future<void>.delayed(Duration.zero);
      expect(repo.queries.single.categoryId, 'c1');
      expect(
        cubit.state.articles.map((HelpArticleSummary a) => a.slug),
        <String>['schedule-a-ride'],
      );
      expect(cubit.state.category?.name, 'الرحلات');

      cubit.reset();
      expect(cubit.state.browsing, isTrue);
      expect(cubit.state.articles, isEmpty);
      expect(cubit.state.resetVersion, 1);
      await cubit.close();
    });

    test('a search failure is reported and can be retried', () async {
      repo.searchFailure = apiFailure('boom');
      final HelpCenterCubit cubit = center();
      cubit.selectCategory('c1');
      await Future<void>.delayed(Duration.zero);
      expect(cubit.state.searchFailure?.code, 'boom');
      repo.searchFailure = null;
      await cubit.retry();
      expect(cubit.state.searchFailure, isNull);
      expect(cubit.state.articles, hasLength(1));
      await cubit.close();
    });

    test('load more appends the next page', () async {
      repo
        ..pageSize = 1
        ..articles = <HelpArticleSummary>[
          ...fakeArticles.map(
            (HelpArticleSummary a) => HelpArticleSummary(
              id: a.id,
              slug: a.slug,
              title: 'رحلة ${a.title}',
              categoryId: 'c1',
            ),
          ),
        ];
      final HelpCenterCubit cubit = center();
      cubit.selectCategory('c1');
      await Future<void>.delayed(Duration.zero);
      expect(cubit.state.articles, hasLength(1));
      expect(cubit.state.hasMore, isTrue);

      await cubit.loadMore();
      expect(cubit.state.articles, hasLength(2));
      expect(cubit.state.hasMore, isFalse);
      expect(repo.queries.last.page, 2);
      await cubit.close();
    });

    test('an old answer never overwrites a newer search', () async {
      final HelpCenterCubit cubit = center();
      cubit.selectCategory('c1');
      cubit.selectCategory('c2');
      await Future<void>.delayed(Duration.zero);
      expect(cubit.state.categoryId, 'c2');
      expect(cubit.state.articles.single.categoryId, 'c2');
      await cubit.close();
    });

    test('the state exposes browsing only without text and category', () {
      expect(const HelpCenterState().browsing, isTrue);
      expect(const HelpCenterState(text: 'x').browsing, isFalse);
      expect(const HelpCenterState(categoryId: 'c1').browsing, isFalse);
    });
  });

  group('HelpArticleCubit', () {
    HelpArticleCubit article() => HelpArticleCubit(
      getArticle: GetHelpArticle(repo),
      sendFeedback: SendArticleFeedback(repo),
      slug: 'schedule-a-ride',
    );

    test('loads the article with its Markdown body', () async {
      final HelpArticleCubit cubit = article();
      await cubit.load();
      expect(cubit.state.article?.title, 'كيف أجدول رحلة؟');
      expect(cubit.state.article?.body, contains('##'));
      await cubit.close();
    });

    test('a load failure is kept', () async {
      repo.articleFailure = apiFailure('http_404');
      final HelpArticleCubit cubit = article();
      await cubit.load();
      expect(cubit.state.article, isNull);
      expect(cubit.state.failure?.code, 'http_404');
      await cubit.close();
    });

    test('helpful yes / no is sent once with the article id', () async {
      final HelpArticleCubit cubit = article();
      await cubit.load();
      await cubit.sendFeedback(helpful: false);
      expect(repo.feedbacks.single.articleId, 'a1');
      expect(repo.feedbacks.single.helpful, isFalse);
      expect(cubit.state.feedback, ArticleFeedbackStatus.sent);
      expect(cubit.state.helpful, isFalse);

      await cubit.sendFeedback(helpful: true);
      expect(repo.feedbacks, hasLength(1));
      await cubit.close();
    });

    test('feedback before the article loaded is ignored', () async {
      final HelpArticleCubit cubit = article();
      await cubit.sendFeedback(helpful: true);
      expect(repo.feedbacks, isEmpty);
      await cubit.close();
    });

    test('a repeated answer of the day still counts as sent', () async {
      repo.feedbackFailure = apiFailure('rate_limited');
      final HelpArticleCubit cubit = article();
      await cubit.load();
      await cubit.sendFeedback(helpful: true);
      expect(cubit.state.feedback, ArticleFeedbackStatus.sent);
      expect(cubit.state.feedbackFailure, isNull);
      await cubit.close();
    });

    test('other failures allow answering again', () async {
      repo.feedbackFailure = apiFailure('boom');
      final HelpArticleCubit cubit = article();
      await cubit.load();
      await cubit.sendFeedback(helpful: true);
      expect(cubit.state.feedback, ArticleFeedbackStatus.none);
      expect(cubit.state.feedbackFailure?.code, 'boom');

      repo.feedbackFailure = null;
      await cubit.sendFeedback(helpful: true);
      expect(cubit.state.feedback, ArticleFeedbackStatus.sent);
      expect(cubit.state.feedbackFailure, isNull);
      await cubit.close();
    });
  });
}
