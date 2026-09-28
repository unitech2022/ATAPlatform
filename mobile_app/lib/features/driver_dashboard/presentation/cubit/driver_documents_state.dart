import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/driver_onboarding/domain/entities/driver_application.dart';
import 'package:equatable/equatable.dart';

/// State of the documents & vehicle tab.
class DriverDocumentsState extends Equatable {
  const DriverDocumentsState({
    this.application,
    this.loading = false,
    this.failure,
  });

  final DriverApplication? application;
  final bool loading;
  final Failure? failure;

  DriverDocumentsState copyWith({
    DriverApplication? application,
    bool? loading,
    Failure? failure,
    bool clearFailure = false,
  }) => DriverDocumentsState(
    application: application ?? this.application,
    loading: loading ?? this.loading,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[application, loading, failure];
}
