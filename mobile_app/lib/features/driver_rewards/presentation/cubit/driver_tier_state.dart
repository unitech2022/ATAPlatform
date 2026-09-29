import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/driver_rewards/domain/entities/driver_tier_info.dart';
import 'package:equatable/equatable.dart';

/// State of [DriverTierCubit].
class DriverTierState extends Equatable {
  const DriverTierState({this.info, this.loading = false, this.failure});

  final DriverTierInfo? info;
  final bool loading;
  final Failure? failure;

  @override
  List<Object?> get props => <Object?>[info, loading, failure];
}
