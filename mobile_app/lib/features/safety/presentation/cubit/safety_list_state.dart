import 'package:ata_app/core/errors/failures.dart';
import 'package:equatable/equatable.dart';

/// State shared by the safety list cubits (cases, lost items).
class SafetyListState<T> extends Equatable {
  const SafetyListState({
    this.loading = false,
    this.items = const [],
    this.busyId,
    this.failure,
  });

  final bool loading;
  final List<T> items;

  /// Row being updated (driver lost item answer).
  final String? busyId;
  final Failure? failure;

  bool get isEmpty => !loading && failure == null && items.isEmpty;

  SafetyListState<T> copyWith({
    bool? loading,
    List<T>? items,
    String? busyId,
    Failure? failure,
    bool clearBusy = false,
    bool clearFailure = false,
  }) => SafetyListState<T>(
    loading: loading ?? this.loading,
    items: items ?? this.items,
    busyId: clearBusy ? null : busyId ?? this.busyId,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[loading, items, busyId, failure];
}
