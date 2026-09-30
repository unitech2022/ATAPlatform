import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/rides/domain/entities/trip_summary.dart';
import 'package:ata_app/features/support/domain/entities/fare_dispute.dart';
import 'package:ata_app/features/support/domain/entities/ticket.dart';
import 'package:ata_app/features/support/domain/entities/ticket_enums.dart';
import 'package:ata_app/features/support/domain/entities/ticket_requests.dart';
import 'package:ata_app/features/support/domain/usecases/create_ticket.dart';
import 'package:ata_app/features/support/domain/usecases/get_support_trips.dart';
import 'package:ata_app/features/support/presentation/cubit/new_ticket_state.dart';
import 'package:ata_app/features/trip/domain/entities/trip_step.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

/// The "new ticket" form (`POST /support/tickets`): type, related trip
/// (picked from the recent trips when the type needs one), subject and
/// message. Attachments and the fare dispute fields live in their own
/// cubits; the page hands their result to [submit].
class NewTicketCubit extends Cubit<NewTicketState> {
  NewTicketCubit({
    required this._getTrips,
    required this._create,
    required this.actor,
    TicketType? initialType,
    String? tripId,
    String initialSubject = '',
  }) : super(
         NewTicketState(
           type: initialType,
           tripId: tripId,
           subject: initialSubject,
         ),
       );

  final GetSupportTrips _getTrips;
  final CreateTicket _create;
  final TripActor actor;

  /// Loads the recent trips (mandatory for some types, optional for the
  /// others).
  Future<void> start() => _loadTrips();

  void selectType(TicketType type) {
    if (type == state.type) return;
    emit(state.copyWith(type: type, clearFailure: true));
  }

  void selectTrip(String? tripId) => emit(
    tripId == null
        ? state.copyWith(clearTrip: true, clearFailure: true)
        : state.copyWith(tripId: tripId, clearFailure: true),
  );

  void subjectChanged(String value) =>
      emit(state.copyWith(subject: value, clearFailure: true));

  void messageChanged(String value) =>
      emit(state.copyWith(message: value, clearFailure: true));

  Future<void> retryTrips() => _loadTrips();

  /// Creates the ticket. [dispute] is only sent for a payment issue.
  Future<void> submit({
    List<String> fileIds = const <String>[],
    DisputeDraft? dispute,
  }) async {
    final TicketType? type = state.type;
    if (type == null || !state.valid || state.submitting) return;
    emit(state.copyWith(submitting: true, clearFailure: true));
    final result = await _create(
      NewTicketRequest(
        type: type,
        subject: state.subject.trim(),
        message: state.message.trim(),
        tripId: state.hasTrip ? state.tripId : null,
        fileIds: fileIds,
        dispute: state.canDispute ? dispute : null,
      ),
    );
    if (isClosed) return;
    emit(
      result.fold(
        (Failure f) => state.copyWith(submitting: false, failure: f),
        (TicketDetail t) => state.copyWith(submitting: false, created: t),
      ),
    );
  }

  Future<void> _loadTrips() async {
    emit(state.copyWith(tripsLoading: true, clearTripsFailure: true));
    final result = await _getTrips(actor);
    if (isClosed) return;
    emit(
      result.fold(
        (Failure f) => state.copyWith(tripsLoading: false, tripsFailure: f),
        (List<TripSummary> t) => state.copyWith(tripsLoading: false, trips: t),
      ),
    );
  }
}
