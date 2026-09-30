import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_invitation.dart';
import 'package:ata_app/features/corporate/presentation/cubit/corporate_membership_state.dart';
import 'package:equatable/equatable.dart';

/// What the last accept / decline did.
enum InvitationOutcome { none, accepted, declined }

/// State of [CorporateInvitationsCubit].
class CorporateInvitationsState extends Equatable {
  const CorporateInvitationsState({
    this.status = CorporateLoadStatus.idle,
    this.invitations = const <CorporateInvitation>[],
    this.busyId,
    this.failure,
    this.actionFailure,
    this.outcome = InvitationOutcome.none,
    this.outcomeCompany,
  });

  final CorporateLoadStatus status;
  final List<CorporateInvitation> invitations;

  /// The invitation being accepted / declined.
  final String? busyId;

  /// Failure of loading the list.
  final Failure? failure;

  /// Failure of the last accept / decline (`invitation_expired`,
  /// `corporate_member_elsewhere`).
  final Failure? actionFailure;
  final InvitationOutcome outcome;
  final String? outcomeCompany;

  bool get isLoading => status == CorporateLoadStatus.loading;
  bool get isBusy => busyId != null;
  bool get hasInvitations => invitations.isNotEmpty;

  CorporateInvitationsState copyWith({
    CorporateLoadStatus? status,
    List<CorporateInvitation>? invitations,
    String? busyId,
    Failure? failure,
    Failure? actionFailure,
    InvitationOutcome? outcome,
    String? outcomeCompany,
    bool clearBusy = false,
    bool clearFailure = false,
    bool clearActionFailure = false,
  }) => CorporateInvitationsState(
    status: status ?? this.status,
    invitations: invitations ?? this.invitations,
    busyId: clearBusy ? null : busyId ?? this.busyId,
    failure: clearFailure ? null : failure ?? this.failure,
    actionFailure: clearActionFailure
        ? null
        : actionFailure ?? this.actionFailure,
    outcome: outcome ?? this.outcome,
    outcomeCompany: outcomeCompany ?? this.outcomeCompany,
  );

  @override
  List<Object?> get props => <Object?>[
    status,
    invitations,
    busyId,
    failure,
    actionFailure,
    outcome,
    outcomeCompany,
  ];
}
