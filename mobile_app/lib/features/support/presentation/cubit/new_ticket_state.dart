import 'package:ata_app/core/errors/failures.dart';
import 'package:ata_app/features/rides/domain/entities/trip_summary.dart';
import 'package:ata_app/features/support/domain/entities/ticket.dart';
import 'package:ata_app/features/support/domain/entities/ticket_enums.dart';
import 'package:equatable/equatable.dart';

/// State of `NewTicketCubit`.
class NewTicketState extends Equatable {
  const NewTicketState({
    this.type,
    this.tripId,
    this.trips = const <TripSummary>[],
    this.tripsLoading = false,
    this.tripsFailure,
    this.subject = '',
    this.message = '',
    this.submitting = false,
    this.failure,
    this.created,
  });

  static const int subjectMax = 160;
  static const int messageMax = 4000;

  final TicketType? type;
  final String? tripId;

  /// Recent finished trips to choose from.
  final List<TripSummary> trips;
  final bool tripsLoading;
  final Failure? tripsFailure;
  final String subject;
  final String message;
  final bool submitting;
  final Failure? failure;

  /// The ticket the API created (the page opens its thread).
  final TicketDetail? created;

  bool get needsTrip => type?.needsTrip ?? false;

  bool get hasTrip => tripId != null && tripId!.isNotEmpty;

  /// Fare dispute section: only for payment issues of a chosen trip.
  bool get canDispute => type == TicketType.paymentIssue && hasTrip;

  bool get valid =>
      type != null &&
      subject.trim().isNotEmpty &&
      subject.trim().length <= subjectMax &&
      message.trim().isNotEmpty &&
      message.trim().length <= messageMax &&
      (!needsTrip || hasTrip);

  NewTicketState copyWith({
    TicketType? type,
    String? tripId,
    bool clearTrip = false,
    List<TripSummary>? trips,
    bool? tripsLoading,
    Failure? tripsFailure,
    bool clearTripsFailure = false,
    String? subject,
    String? message,
    bool? submitting,
    Failure? failure,
    bool clearFailure = false,
    TicketDetail? created,
  }) => NewTicketState(
    type: type ?? this.type,
    tripId: clearTrip ? null : tripId ?? this.tripId,
    trips: trips ?? this.trips,
    tripsLoading: tripsLoading ?? this.tripsLoading,
    tripsFailure: clearTripsFailure ? null : tripsFailure ?? this.tripsFailure,
    subject: subject ?? this.subject,
    message: message ?? this.message,
    submitting: submitting ?? this.submitting,
    failure: clearFailure ? null : failure ?? this.failure,
    created: created ?? this.created,
  );

  @override
  List<Object?> get props => <Object?>[
    type,
    tripId,
    trips,
    tripsLoading,
    tripsFailure,
    subject,
    message,
    submitting,
    failure,
    created,
  ];
}
