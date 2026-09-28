import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/pricing/domain/entities/demand_level.dart';
import 'package:ata_app/features/trip/domain/entities/geo_point.dart';
import 'package:equatable/equatable.dart';

/// Lifecycle of the demand badge.
enum DemandStatus { idle, loading, ready, failure }

/// State of [DemandCubit].
class DemandState extends Equatable {
  const DemandState({
    this.status = DemandStatus.idle,
    this.location,
    this.level,
    this.failure,
  });

  final DemandStatus status;
  final GeoPoint? location;
  final DemandLevel? level;
  final Failure? failure;

  /// The badge only appears above the normal level.
  bool get showBadge => level?.code.isElevated ?? false;

  DemandState copyWith({
    DemandStatus? status,
    GeoPoint? location,
    DemandLevel? level,
    Failure? failure,
    bool clearFailure = false,
  }) => DemandState(
    status: status ?? this.status,
    location: location ?? this.location,
    level: level ?? this.level,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[status, location, level, failure];
}
