import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/features/support/domain/entities/help.dart';
import 'package:ata_app/features/support/domain/entities/ticket_requests.dart';
import 'package:ata_app/features/support/domain/usecases/get_help_article.dart';
import 'package:ata_app/features/support/domain/usecases/send_article_feedback.dart';
import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// "Was this article helpful?" progress.
enum ArticleFeedbackStatus { none, sending, sent }

/// State of `HelpArticleCubit`.
class HelpArticleState extends Equatable {
  const HelpArticleState({
    this.article,
    this.loading = false,
    this.failure,
    this.feedback = ArticleFeedbackStatus.none,
    this.helpful,
    this.feedbackFailure,
  });

  final HelpArticle? article;
  final bool loading;
  final Failure? failure;
  final ArticleFeedbackStatus feedback;

  /// The answer given.
  final bool? helpful;
  final Failure? feedbackFailure;

  HelpArticleState copyWith({
    HelpArticle? article,
    bool? loading,
    Failure? failure,
    bool clearFailure = false,
    ArticleFeedbackStatus? feedback,
    bool? helpful,
    Failure? feedbackFailure,
    bool clearFeedbackFailure = false,
  }) => HelpArticleState(
    article: article ?? this.article,
    loading: loading ?? this.loading,
    failure: clearFailure ? null : failure ?? this.failure,
    feedback: feedback ?? this.feedback,
    helpful: helpful ?? this.helpful,
    feedbackFailure: clearFeedbackFailure
        ? null
        : feedbackFailure ?? this.feedbackFailure,
  );

  @override
  List<Object?> get props => <Object?>[
    article,
    loading,
    failure,
    feedback,
    helpful,
    feedbackFailure,
  ];
}

/// One help article (`GET /help/articles/{slug}`) and its helpful yes / no
/// feedback.
class HelpArticleCubit extends Cubit<HelpArticleState> {
  HelpArticleCubit({
    required this._getArticle,
    required this._sendFeedback,
    required this.slug,
  }) : super(const HelpArticleState());

  final GetHelpArticle _getArticle;
  final SendArticleFeedback _sendFeedback;
  final String slug;

  Future<void> load() async {
    emit(state.copyWith(loading: true, clearFailure: true));
    final result = await _getArticle(slug);
    if (isClosed) return;
    emit(
      result.fold(
        (failure) => state.copyWith(loading: false, failure: failure),
        (HelpArticle a) => state.copyWith(loading: false, article: a),
      ),
    );
  }

  /// Sends the answer once. The API accepts one answer per article and
  /// day: a repeat (`rate_limited` / `conflict`) still counts as sent.
  Future<void> sendFeedback({required bool helpful}) async {
    final HelpArticle? article = state.article;
    if (article == null || state.feedback != ArticleFeedbackStatus.none) {
      return;
    }
    emit(
      state.copyWith(
        feedback: ArticleFeedbackStatus.sending,
        helpful: helpful,
        clearFeedbackFailure: true,
      ),
    );
    final result = await _sendFeedback(
      ArticleFeedback(articleId: article.id, helpful: helpful),
    );
    if (isClosed) return;
    emit(
      result.fold(
        (Failure f) =>
            f.code == ErrorCodes.rateLimited || f.code == ErrorCodes.conflict
            ? state.copyWith(feedback: ArticleFeedbackStatus.sent)
            : state.copyWith(
                feedback: ArticleFeedbackStatus.none,
                feedbackFailure: f,
              ),
        (_) => state.copyWith(feedback: ArticleFeedbackStatus.sent),
      ),
    );
  }
}
