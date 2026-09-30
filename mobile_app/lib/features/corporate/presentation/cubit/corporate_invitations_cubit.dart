import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/localization/failure_text.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/corporate/domain/entities/corporate_invitation.dart';
import 'package:ata_app/features/corporate/domain/usecases/accept_corporate_invitation.dart';
import 'package:ata_app/features/corporate/domain/usecases/decline_corporate_invitation.dart';
import 'package:ata_app/features/corporate/domain/usecases/get_corporate_invitations.dart';
import 'package:ata_app/features/corporate/presentation/cubit/corporate_invitations_state.dart';
import 'package:ata_app/features/corporate/presentation/cubit/corporate_membership_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:fpdart/fpdart.dart';

/// Pending company invitations of the rider: list, accept, decline.
/// Accepting makes the rider an active member; whoever listens to
/// [CorporateInvitationsState.outcome] reloads the membership.
class CorporateInvitationsCubit extends Cubit<CorporateInvitationsState> {
  CorporateInvitationsCubit({
    required this._getInvitations,
    required this._accept,
    required this._decline,
  }) : super(const CorporateInvitationsState());

  final GetCorporateInvitations _getInvitations;
  final AcceptCorporateInvitation _accept;
  final DeclineCorporateInvitation _decline;

  /// Forget everything (signed out).
  void reset() => emit(const CorporateInvitationsState());

  Future<void> load() async {
    if (state.isLoading) return;
    emit(state.copyWith(status: CorporateLoadStatus.loading));
    final result = await _getInvitations(const NoParams());
    if (isClosed) return;
    emit(
      result.fold(
        (Failure failure) => state.copyWith(
          status: CorporateLoadStatus.failure,
          failure: failure,
        ),
        (List<CorporateInvitation> items) => state.copyWith(
          status: CorporateLoadStatus.ready,
          invitations: items,
          clearFailure: true,
        ),
      ),
    );
  }

  Future<void> accept(String id) =>
      _act(id, () => _accept(id), InvitationOutcome.accepted);

  Future<void> decline(String id) =>
      _act(id, () => _decline(id), InvitationOutcome.declined);

  /// The outcome was shown (snackbar).
  void outcomeShown() => emit(
    state.copyWith(outcome: InvitationOutcome.none, clearActionFailure: true),
  );

  Future<void> _act(
    String id,
    Future<Either<Failure, Unit>> Function() action,
    InvitationOutcome outcome,
  ) async {
    if (state.isBusy) return;
    final CorporateInvitation? invitation = _byId(id);
    emit(state.copyWith(busyId: id, clearActionFailure: true));
    final Either<Failure, Unit> result = await action();
    if (isClosed) return;
    final Failure? failure = result.fold((Failure f) => f, (_) => null);
    if (failure == null) {
      emit(
        state.copyWith(
          invitations: _without(id),
          clearBusy: true,
          outcome: outcome,
          outcomeCompany: invitation?.companyName,
        ),
      );
      return;
    }
    // An expired invitation can never be accepted: drop it from the list.
    emit(
      state.copyWith(
        invitations: failure.code == ErrorCodes.invitationExpired
            ? _without(id)
            : null,
        clearBusy: true,
        actionFailure: failure,
      ),
    );
  }

  CorporateInvitation? _byId(String id) {
    for (final CorporateInvitation i in state.invitations) {
      if (i.id == id) return i;
    }
    return null;
  }

  List<CorporateInvitation> _without(String id) => state.invitations
      .where((CorporateInvitation i) => i.id != id)
      .toList(growable: false);
}
