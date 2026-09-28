import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/trip/domain/entities/reliability_summary.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:ata_app/features/trip/domain/usecases/get_reliability.dart';
import 'package:ata_app/features/trip/presentation/cubit/reliability_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Reliability summary (cancellation rate, points, restriction) of the
/// passenger (account page) or the driver (dashboard card).
class ReliabilityCubit extends Cubit<ReliabilityState> {
  ReliabilityCubit({required this._getReliability, required this.role})
    : super(const ReliabilityState());

  final GetReliability _getReliability;
  final TripActor role;

  Future<void> load() async {
    if (state.loading) return;
    emit(state.copyWith(loading: true, clearFailure: true));
    final result = await _getReliability(role);
    if (isClosed) return;
    result.fold(
      (Failure failure) =>
          emit(state.copyWith(loading: false, failure: failure)),
      (ReliabilitySummary summary) =>
          emit(state.copyWith(loading: false, summary: summary)),
    );
  }
}
