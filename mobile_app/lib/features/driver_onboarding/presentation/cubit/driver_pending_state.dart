import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/driver_onboarding/domain/entities/driver_application.dart';
import 'package:equatable/equatable.dart';

/// State of the "application pending" screen.
class DriverPendingState extends Equatable {
  const DriverPendingState({
    this.application,
    this.loading = false,
    this.failure,
    this.portalOpenFailed = false,
  });

  final DriverApplication? application;
  final bool loading;
  final Failure? failure;
  final bool portalOpenFailed;

  DriverPendingState copyWith({
    DriverApplication? application,
    bool? loading,
    Failure? failure,
    bool? portalOpenFailed,
    bool clearFailure = false,
  }) => DriverPendingState(
    application: application ?? this.application,
    loading: loading ?? this.loading,
    failure: clearFailure ? null : failure ?? this.failure,
    portalOpenFailed: portalOpenFailed ?? this.portalOpenFailed,
  );

  @override
  List<Object?> get props => <Object?>[
    application,
    loading,
    failure,
    portalOpenFailed,
  ];
}
