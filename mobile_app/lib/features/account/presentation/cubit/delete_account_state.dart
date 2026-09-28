import 'package:ata_app/core/errors/failures.dart';
import 'package:equatable/equatable.dart';

/// State of the delete-account confirmation.
class DeleteAccountState extends Equatable {
  const DeleteAccountState({
    this.submitting = false,
    this.deleted = false,
    this.failure,
  });

  final bool submitting;
  final bool deleted;
  final Failure? failure;

  DeleteAccountState copyWith({
    bool? submitting,
    bool? deleted,
    Failure? failure,
    bool clearFailure = false,
  }) => DeleteAccountState(
    submitting: submitting ?? this.submitting,
    deleted: deleted ?? this.deleted,
    failure: clearFailure ? null : failure ?? this.failure,
  );

  @override
  List<Object?> get props => <Object?>[submitting, deleted, failure];
}
