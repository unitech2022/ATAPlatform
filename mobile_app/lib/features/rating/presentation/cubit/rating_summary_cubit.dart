import 'package:ata_app/features/rating/domain/entities/rating_summary.dart';
import 'package:ata_app/features/rating/domain/usecases/get_rating_summary.dart';
import 'package:ata_app/features/rating/presentation/cubit/rating_summary_state.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// The ratee's own summary (`/driver/ratings`).
class RatingSummaryCubit extends Cubit<RatingSummaryState> {
  RatingSummaryCubit({required this._getSummary, required this.role})
    : super(const RatingSummaryState());

  final GetRatingSummary _getSummary;
  final TripActor role;

  Future<void> load() async {
    emit(RatingSummaryState(summary: state.summary, loading: true));
    final result = await _getSummary(role);
    if (isClosed) return;
    emit(
      result.fold(
        (failure) =>
            RatingSummaryState(summary: state.summary, failure: failure),
        (RatingSummary summary) => RatingSummaryState(summary: summary),
      ),
    );
  }
}
