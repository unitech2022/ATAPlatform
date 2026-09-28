import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/safety/domain/entities/trip_share.dart';
import 'package:ata_app/features/safety/domain/entities/trusted_contact.dart';
import 'package:equatable/equatable.dart';

/// State of `TripShareCubit`.
class TripShareState extends Equatable {
  const TripShareState({
    this.loading = false,
    this.shares = const <TripShare>[],
    this.contacts = const <TrustedContact>[],
    this.selectedContactIds = const <String>{},
    this.creating = false,
    this.busyId,
    this.linkToShare,
    this.shareRequest = 0,
    this.smsSent = 0,
    this.failure,
  });

  final bool loading;
  final List<TripShare> shares;
  final List<TrustedContact> contacts;
  final Set<String> selectedContactIds;
  final bool creating;

  /// Share being revoked.
  final String? busyId;

  /// Latest link created with `channel: link`, to hand to share_plus.
  final String? linkToShare;

  /// Bumped each time [linkToShare] must be shared (listener trigger).
  final int shareRequest;

  /// Contacts that were just sent an SMS.
  final int smsSent;
  final Failure? failure;

  List<TripShare> get activeShares =>
      shares.where((TripShare s) => !s.isRevoked).toList(growable: false);

  TripShareState copyWith({
    bool? loading,
    List<TripShare>? shares,
    List<TrustedContact>? contacts,
    Set<String>? selectedContactIds,
    bool? creating,
    String? busyId,
    String? linkToShare,
    int? shareRequest,
    int? smsSent,
    Failure? failure,
    bool clearBusy = false,
    bool clearFailure = false,
  }) => TripShareState(
    loading: loading ?? this.loading,
    shares: shares ?? this.shares,
    contacts: contacts ?? this.contacts,
    selectedContactIds: selectedContactIds ?? this.selectedContactIds,
    creating: creating ?? this.creating,
    busyId: clearBusy ? null : busyId ?? this.busyId,
    linkToShare: linkToShare ?? this.linkToShare,
    shareRequest: shareRequest ?? this.shareRequest,
    smsSent: smsSent ?? this.smsSent,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[
    loading,
    shares,
    contacts,
    selectedContactIds,
    creating,
    busyId,
    linkToShare,
    shareRequest,
    smsSent,
    failure,
  ];
}
