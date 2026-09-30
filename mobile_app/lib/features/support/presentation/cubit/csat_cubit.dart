import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/features/support/domain/entities/ticket_requests.dart';
import 'package:ata_app/features/support/domain/usecases/rate_ticket.dart';
import 'package:equatable/equatable.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// State of `CsatCubit`.
class CsatState extends Equatable {
  const CsatState({
    this.score = 0,
    this.comment = '',
    this.submitting = false,
    this.submitted = false,
    this.failure,
  });

  static const int commentMax = 500;

  final int score;
  final String comment;
  final bool submitting;

  /// Sent (or already rated): the prompt gives way to a thank-you.
  final bool submitted;
  final Failure? failure;

  bool get canSubmit => score >= 1 && !submitting && !submitted;

  CsatState copyWith({
    int? score,
    String? comment,
    bool? submitting,
    bool? submitted,
    Failure? failure,
    bool clearFailure = false,
  }) => CsatState(
    score: score ?? this.score,
    comment: comment ?? this.comment,
    submitting: submitting ?? this.submitting,
    submitted: submitted ?? this.submitted,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[
    score,
    comment,
    submitting,
    submitted,
    failure,
  ];
}

/// The one-time support rating (1–5 stars + comment) after a ticket was
/// resolved or closed (`POST /support/tickets/{id}/csat`).
class CsatCubit extends Cubit<CsatState> {
  CsatCubit({required this._rate, required this.ticketId})
    : super(const CsatState());

  final RateTicket _rate;
  final String ticketId;

  void selectScore(int score) {
    if (state.submitted) return;
    emit(state.copyWith(score: score.clamp(1, 5), clearFailure: true));
  }

  void commentChanged(String text) => emit(state.copyWith(comment: text));

  Future<void> submit() async {
    if (!state.canSubmit) return;
    emit(state.copyWith(submitting: true, clearFailure: true));
    final result = await _rate(
      TicketRatingRequest(
        ticketId: ticketId,
        score: state.score,
        comment: state.comment,
      ),
    );
    if (isClosed) return;
    emit(
      result.fold(
        // `409 conflict`: it was rated already (elsewhere): same outcome.
        (Failure f) => f.code == ErrorCodes.conflict
            ? state.copyWith(submitting: false, submitted: true)
            : state.copyWith(submitting: false, failure: f),
        (_) => state.copyWith(submitting: false, submitted: true),
      ),
    );
  }
}
