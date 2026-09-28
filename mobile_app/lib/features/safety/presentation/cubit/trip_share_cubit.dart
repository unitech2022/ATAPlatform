import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/core/usecases/use_case.dart';
import 'package:ata_app/features/safety/domain/entities/trip_share.dart';
import 'package:ata_app/features/safety/domain/entities/trusted_contact.dart';
import 'package:ata_app/features/safety/domain/usecases/create_trip_share.dart';
import 'package:ata_app/features/safety/domain/usecases/get_trip_shares.dart';
import 'package:ata_app/features/safety/domain/usecases/get_trusted_contacts.dart';
import 'package:ata_app/features/safety/domain/usecases/revoke_trip_share.dart';
import 'package:ata_app/features/safety/presentation/cubit/trip_share_state.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// Trip sharing (F12): creates a tracking link (handed to share_plus by the
/// page through [TripShareState.shareRequest]), sends links to trusted
/// contacts by SMS, lists the trip's links and revokes them.
class TripShareCubit extends Cubit<TripShareState> {
  TripShareCubit({
    required this._createShare,
    required this._getShares,
    required this._revokeShare,
    required this._getContacts,
    required this.tripId,
    DateTime Function()? now,
  }) : _now = now ?? DateTime.now,
       super(const TripShareState());

  final CreateTripShare _createShare;
  final GetTripShares _getShares;
  final RevokeTripShare _revokeShare;
  final GetTrustedContacts _getContacts;
  final String tripId;
  final DateTime Function() _now;

  /// Loads the trip's links and the trusted contacts (for SMS sharing).
  Future<void> load() async {
    emit(state.copyWith(loading: true, clearFailure: true));
    final shares = await _getShares(tripId);
    final contacts = await _getContacts(const NoParams());
    if (isClosed) return;
    emit(
      state.copyWith(
        loading: false,
        shares: shares.getOrElse((_) => state.shares),
        contacts: contacts.getOrElse((_) => state.contacts),
        failure: shares.getLeft().toNullable(),
      ),
    );
  }

  /// `POST …/shares {channel: link}` then asks the page to share the URL.
  Future<void> shareLink() async {
    if (state.creating) return;
    emit(state.copyWith(creating: true, clearFailure: true));
    final result = await _createShare(CreateTripShareParams(tripId: tripId));
    if (isClosed) return;
    result.fold(
      (Failure f) => emit(state.copyWith(creating: false, failure: f)),
      (List<TripShare> created) {
        if (created.isEmpty) {
          emit(state.copyWith(creating: false));
          return;
        }
        emit(
          state.copyWith(
            creating: false,
            shares: <TripShare>[...created, ...state.shares],
            linkToShare: created.first.url,
            shareRequest: state.shareRequest + 1,
          ),
        );
      },
    );
  }

  void toggleContact(String contactId) {
    final Set<String> ids = Set<String>.of(state.selectedContactIds);
    if (!ids.remove(contactId)) ids.add(contactId);
    emit(state.copyWith(selectedContactIds: ids));
  }

  /// `POST …/shares {channel: sms, contactIds}`.
  Future<void> sendToContacts() async {
    final List<String> ids = state.contacts
        .map((TrustedContact c) => c.id)
        .where(state.selectedContactIds.contains)
        .toList(growable: false);
    if (ids.isEmpty || state.creating) return;
    emit(state.copyWith(creating: true, clearFailure: true, smsSent: 0));
    final result = await _createShare(
      CreateTripShareParams(
        tripId: tripId,
        channel: ShareChannel.sms,
        contactIds: ids,
      ),
    );
    if (isClosed) return;
    result.fold(
      (Failure f) => emit(state.copyWith(creating: false, failure: f)),
      (List<TripShare> created) => emit(
        state.copyWith(
          creating: false,
          shares: <TripShare>[...created, ...state.shares],
          selectedContactIds: const <String>{},
          smsSent: ids.length,
        ),
      ),
    );
  }

  /// `DELETE /safety/shares/{id}`: the public page answers `410` after.
  Future<void> revoke(String shareId) async {
    if (state.busyId != null) return;
    emit(state.copyWith(busyId: shareId, clearFailure: true));
    final result = await _revokeShare(shareId);
    if (isClosed) return;
    result.fold(
      (Failure f) => emit(state.copyWith(clearBusy: true, failure: f)),
      (_) => emit(
        state.copyWith(
          clearBusy: true,
          shares: state.shares
              .map((TripShare s) => s.id == shareId ? s.revoked(_now()) : s)
              .toList(growable: false),
        ),
      ),
    );
  }
}
