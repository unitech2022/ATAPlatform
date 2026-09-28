import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/trip/domain/entities/reliability_summary.dart';
import 'package:equatable/equatable.dart';

/// State of `ReliabilityCubit`.
class ReliabilityState extends Equatable {
  const ReliabilityState({this.loading = false, this.summary, this.failure});

  final bool loading;
  final ReliabilitySummary? summary;
  final Failure? failure;

  ReliabilityState copyWith({
    bool? loading,
    ReliabilitySummary? summary,
    Failure? failure,
    bool clearFailure = false,
  }) => ReliabilityState(
    loading: loading ?? this.loading,
    summary: summary ?? this.summary,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[loading, summary, failure];
}
