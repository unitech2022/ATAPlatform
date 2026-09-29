import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/rating/domain/entities/rating_summary.dart';
import 'package:equatable/equatable.dart';

/// State of [RatingSummaryCubit].
class RatingSummaryState extends Equatable {
  const RatingSummaryState({this.summary, this.loading = false, this.failure});

  final RatingSummary? summary;
  final bool loading;
  final Failure? failure;

  @override
  List<Object?> get props => <Object?>[summary, loading, failure];
}
