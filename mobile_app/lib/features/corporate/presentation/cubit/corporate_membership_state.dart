import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_profile.dart';
import 'package:equatable/equatable.dart';

/// Progress of loading the employee profile.
enum CorporateLoadStatus { idle, loading, ready, failure }

/// State of [CorporateMembershipCubit].
class CorporateMembershipState extends Equatable {
  const CorporateMembershipState({
    this.active = false,
    this.status = CorporateLoadStatus.idle,
    this.profile,
    this.failure,
  });

  /// Bound to a signed-in rider.
  final bool active;
  final CorporateLoadStatus status;

  /// `null` = not a member (or not loaded yet).
  final CorporateProfile? profile;
  final Failure? failure;

  bool get isLoading => status == CorporateLoadStatus.loading;
  bool get isMember => profile != null;

  /// The corporate payment may be offered (`membership.status = active`).
  bool get isActive => profile?.isActive ?? false;

  CorporateMembershipState copyWith({
    bool? active,
    CorporateLoadStatus? status,
    CorporateProfile? profile,
    Failure? failure,
    bool clearProfile = false,
    bool clearFailure = false,
  }) => CorporateMembershipState(
    active: active ?? this.active,
    status: status ?? this.status,
    profile: clearProfile ? null : profile ?? this.profile,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[active, status, profile, failure];
}
